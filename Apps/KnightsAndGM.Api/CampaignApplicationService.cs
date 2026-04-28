using System.Security.Claims;
using KnightsAndGM.Api.Infrastructure;
using KnightsAndGM.Api.Seed;
using KnightsAndGM.Api.Security;
using KnightsAndGM.Shared;

namespace KnightsAndGM.Api;

public sealed class CampaignApplicationService
{
    private readonly IGameRepository repository;
    private readonly JwtService jwtService;
    private readonly EncounterService encounterService;
    private readonly TravelRuleSet travelRuleSet = new TravelRuleSet(1);
    private static readonly CharacterCreationConfig defaultCharacterConfig = new CharacterCreationConfig
    {
        BaseVirtueStart = 6,
        VirtueMax = 18,
        StartingPoints = 50,
        DeedPointsPerDeed = 3,
        FlawGrant = 5,
        CoreAbilityCost = 15,
        NewSkillCost = 3,
        MaxSkills = 10
    };

    public CampaignApplicationService(IGameRepository repository, JwtService jwtService, EncounterService encounterService)
    {
        this.repository = repository;
        this.jwtService = jwtService;
        this.encounterService = encounterService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        return await GetOrCreateSessionAsync(request.Username, request.Role, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        return await GetOrCreateSessionAsync(request.Username, request.Role, cancellationToken);
    }

    public async Task<CampaignMapResponse> GetMapAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken)
    {
        var membership = await GetMembershipAsync(campaignId, userId, cancellationToken);
        var state = await LoadStateAsync(campaignId, cancellationToken);
        var visibleTiles = membership.Role == CampaignRole.GameMaster
            ? state.Tiles.Select(CloneTile).ToList()
            : state.Tiles.Select(CreatePlayerFacingTile).ToList();

        var visibleIds = new HashSet<Guid>(state.Tiles.Where(tile => membership.Role == CampaignRole.GameMaster || tile.IsExploredPublic).Select(tile => tile.Id));

        return new CampaignMapResponse
        {
            CampaignId = state.CampaignId,
            Name = state.Name,
            Role = membership.Role,
            Radius = state.Radius,
            Tiles = visibleTiles,
            Notes = state.Notes.Where(note => visibleIds.Contains(note.HexTileId)).OrderBy(note => note.CreatedAtUtc).ToList(),
            Parties = state.Parties
        };
    }

    public async Task<PartySnapshot> GetPartyAsync(Guid campaignId, Guid partyId, Guid userId, CancellationToken cancellationToken)
    {
        await GetMembershipAsync(campaignId, userId, cancellationToken);
        var state = await LoadStateAsync(campaignId, cancellationToken);
        return state.Parties.FirstOrDefault(party => party.Id == partyId)
            ?? throw new InvalidOperationException("Party not found.");
    }

    public async Task<MovePartyResponse> MovePartyAsync(Guid campaignId, Guid partyId, Guid userId, MovePartyRequest request, CancellationToken cancellationToken)
    {
        await GetMembershipAsync(campaignId, userId, cancellationToken);
        var state = await LoadStateAsync(campaignId, cancellationToken);
        var party = state.Parties.FirstOrDefault(candidate => candidate.Id == partyId)
            ?? throw new InvalidOperationException("Party not found.");

        var targetCoordinate = new HexCoordinate(request.TargetQ, request.TargetR);
        var targetTile = state.Tiles.FirstOrDefault(tile => tile.Coordinate == targetCoordinate)
            ?? throw new InvalidOperationException("Target hex not found.");

        var resolution = travelRuleSet.ResolveMove(party.CurrentHex, targetCoordinate, party.RemainingEffort);
        if (!resolution.Success)
        {
            throw new InvalidOperationException(resolution.ErrorMessage);
        }

        var previousHex = party.CurrentHex;
        party.CurrentHex = targetCoordinate;
        party.RemainingEffort = resolution.RemainingEffort;

        var logEntry = new TravelLogEntry
        {
            Id = Guid.NewGuid(),
            From = previousHex,
            To = targetCoordinate,
            Cost = resolution.EffortCost,
            OccurredAtUtc = DateTime.UtcNow,
            Summary = $"Moved from {previousHex} to {targetCoordinate}."
        };

        party.TravelLog.Add(logEntry);

        Guid? newlyDiscoveredHexId = null;
        if (!targetTile.IsExploredPublic)
        {
            targetTile.IsExploredPublic = true;
            newlyDiscoveredHexId = targetTile.Id;
            state.Discoveries.Add(new HexDiscoveryModel
            {
                Id = Guid.NewGuid(),
                HexTileId = targetTile.Id,
                DiscoveredByPartyId = party.Id,
                DiscoveredByUserId = userId,
                DiscoveredAtUtc = DateTime.UtcNow
            });
        }

        var prompt = encounterService.CreatePrompt(state.Seed, targetCoordinate, targetTile.Terrain, party.TravelLog.Count);
        state.Encounters.Add(new EncounterLedgerEntryModel
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            PartyId = party.Id,
            HexTileId = targetTile.Id,
            CreatedAtUtc = DateTime.UtcNow,
            PartyName = party.Name,
            Resolved = false,
            Prompt = prompt
        });

        await repository.SaveCampaignStateAsync(state, cancellationToken);

        return new MovePartyResponse
        {
            Party = party,
            TravelLogEntry = logEntry,
            NewlyDiscoveredHexId = newlyDiscoveredHexId
        };
    }

    public async Task<PublicNoteModel> CreateNoteAsync(Guid campaignId, Guid hexId, Guid userId, string body, CancellationToken cancellationToken)
    {
        var membership = await GetMembershipAsync(campaignId, userId, cancellationToken);
        var state = await LoadStateAsync(campaignId, cancellationToken);
        var tile = state.Tiles.FirstOrDefault(candidate => candidate.Id == hexId)
            ?? throw new InvalidOperationException("Target hex not found.");

        if (membership.Role != CampaignRole.GameMaster && !tile.IsExploredPublic)
        {
            throw new InvalidOperationException("Players can only attach notes to publicly explored hexes.");
        }

        var user = await repository.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var note = new PublicNoteModel
        {
            Id = Guid.NewGuid(),
            HexTileId = hexId,
            AuthorUserId = userId,
            AuthorName = user.DisplayName,
            Body = body.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        state.Notes.Add(note);
        await repository.SaveCampaignStateAsync(state, cancellationToken);
        return note;
    }

    public async Task<IReadOnlyList<EncounterLedgerEntryModel>> GetEncountersAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken)
    {
        var membership = await GetMembershipAsync(campaignId, userId, cancellationToken);
        if (membership.Role != CampaignRole.GameMaster)
        {
            throw new InvalidOperationException("Only GMs can view encounter prompts.");
        }

        var state = await LoadStateAsync(campaignId, cancellationToken);
        return state.Encounters.OrderByDescending(entry => entry.CreatedAtUtc).ToList();
    }

    public async Task<IReadOnlyList<CharacterSheetResponse>> GetCharactersAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken)
    {
        await GetMembershipAsync(campaignId, userId, cancellationToken);
        var state = await LoadStateAsync(campaignId, cancellationToken);
        return state.Characters
            .Select(BuildCharacterResponse)
            .OrderBy(character => character.Character.Name)
            .ToList();
    }

    public async Task<CharacterSheetResponse> CreateCharacterAsync(Guid campaignId, Guid userId, SaveCharacterRequest request, CancellationToken cancellationToken)
    {
        await GetMembershipAsync(campaignId, userId, cancellationToken);
        var state = await LoadStateAsync(campaignId, cancellationToken);

        var character = NormalizeCharacter(request.Character);
        character.Id = character.Id == Guid.Empty ? Guid.NewGuid() : character.Id;

        state.Characters.Add(character);
        if (state.Parties.Count > 0 && !state.Parties[0].CharacterIds.Contains(character.Id))
        {
            state.Parties[0].CharacterIds.Add(character.Id);
        }

        await repository.SaveCampaignStateAsync(state, cancellationToken);
        return BuildCharacterResponse(character);
    }

    public async Task<CharacterSheetResponse> UpdateCharacterAsync(Guid campaignId, Guid characterId, Guid userId, SaveCharacterRequest request, CancellationToken cancellationToken)
    {
        await GetMembershipAsync(campaignId, userId, cancellationToken);
        var state = await LoadStateAsync(campaignId, cancellationToken);
        var existing = state.Characters.FirstOrDefault(character => character.Id == characterId)
            ?? throw new InvalidOperationException("Character not found.");

        var updated = NormalizeCharacter(request.Character);
        updated.Id = characterId;

        existing.Name = updated.Name;
        existing.Vigor = updated.Vigor;
        existing.Clarity = updated.Clarity;
        existing.Spirit = updated.Spirit;
        existing.FlawCount = updated.FlawCount;
        existing.HasCoreAbility = updated.HasCoreAbility;
        existing.DeedCount = updated.DeedCount;
        existing.CachedPointsLeft = updated.CachedPointsLeft;
        existing.Skills = updated.Skills;
        existing.Inventory = updated.Inventory;

        await repository.SaveCampaignStateAsync(state, cancellationToken);
        return BuildCharacterResponse(existing);
    }

    public static Guid GetUserId(ClaimsPrincipal user)
    {
        return Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Missing user id claim."));
    }

    private async Task<AuthResponse> GetOrCreateSessionAsync(string username, CampaignRole requestedRole, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);
        var normalizedUsername = username.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            throw new InvalidOperationException("Username is required.");
        }

        var user = await repository.GetUserByUsernameAsync(normalizedUsername, cancellationToken);
        if (user == null)
        {
            user = await repository.CreateUserAsync(normalizedUsername, normalizedUsername, requestedRole, cancellationToken);
        }
        else if (user.Role != requestedRole)
        {
            user = await repository.UpdateUserRoleAsync(user.Id, requestedRole, cancellationToken);
        }

        await repository.EnsureMembershipAsync(
            SeedCampaignFactory.DefaultCampaignId,
            SeedCampaignFactory.DefaultCampaignName,
            user.Id,
            user.Role,
            cancellationToken);

        return await BuildAuthResponseAsync(user, cancellationToken);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(AuthUserRecord user, CancellationToken cancellationToken)
    {
        var campaignUser = new CampaignUser
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.Role
        };

        var memberships = await repository.GetMembershipsAsync(user.Id, cancellationToken);
        return new AuthResponse
        {
            Token = jwtService.IssueToken(campaignUser),
            User = campaignUser,
            Campaigns = memberships
                .Select(membership => new CampaignSummaryResponse
                {
                    CampaignId = membership.CampaignId,
                    Name = membership.CampaignName,
                    Role = membership.Role
                })
                .ToList()
        };
    }

    private async Task<CampaignMembershipRecord> GetMembershipAsync(Guid campaignId, Guid userId, CancellationToken cancellationToken)
    {
        var memberships = await repository.GetMembershipsAsync(userId, cancellationToken);
        return memberships.FirstOrDefault(membership => membership.CampaignId == campaignId)
            ?? throw new InvalidOperationException("User is not a member of this campaign.");
    }

    private async Task<CampaignStateModel> LoadStateAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        await repository.EnsureSeedDataAsync(cancellationToken);
        return await repository.GetCampaignStateAsync(campaignId, cancellationToken)
            ?? throw new InvalidOperationException("Campaign not found.");
    }

    private static HexTileModel CreatePlayerFacingTile(HexTileModel tile)
    {
        if (tile.IsExploredPublic)
        {
            return CloneTile(tile);
        }

        return new HexTileModel
        {
            Id = tile.Id,
            Coordinate = tile.Coordinate,
            Terrain = tile.Terrain,
            IsExploredPublic = false,
            PublicLandmarkName = string.Empty,
            SecretLandmarkName = string.Empty,
            SecretDetails = string.Empty,
            LandmarkKind = string.Empty
        };
    }

    private static HexTileModel CloneTile(HexTileModel tile)
    {
        return new HexTileModel
        {
            Id = tile.Id,
            Coordinate = tile.Coordinate,
            Terrain = tile.Terrain,
            IsExploredPublic = tile.IsExploredPublic,
            PublicLandmarkName = tile.PublicLandmarkName,
            SecretLandmarkName = tile.SecretLandmarkName,
            SecretDetails = tile.SecretDetails,
            LandmarkKind = tile.LandmarkKind
        };
    }

    private static CharacterSheetModel NormalizeCharacter(CharacterSheetModel input)
    {
        var character = input ?? new CharacterSheetModel();
        character.Name = string.IsNullOrWhiteSpace(character.Name) ? "New Character" : character.Name.Trim();
        character.Vigor = Math.Clamp(character.Vigor <= 0 ? defaultCharacterConfig.BaseVirtueStart : character.Vigor, defaultCharacterConfig.BaseVirtueStart, defaultCharacterConfig.VirtueMax);
        character.Clarity = Math.Clamp(character.Clarity <= 0 ? defaultCharacterConfig.BaseVirtueStart : character.Clarity, defaultCharacterConfig.BaseVirtueStart, defaultCharacterConfig.VirtueMax);
        character.Spirit = Math.Clamp(character.Spirit <= 0 ? defaultCharacterConfig.BaseVirtueStart : character.Spirit, defaultCharacterConfig.BaseVirtueStart, defaultCharacterConfig.VirtueMax);
        character.FlawCount = Math.Clamp(character.FlawCount, 0, 2);
        character.DeedCount = Math.Max(0, character.DeedCount);
        character.Skills = (character.Skills ?? new List<CharacterSkillModel>())
            .Where(skill => !string.IsNullOrWhiteSpace(skill.Name))
            .Select(skill => new CharacterSkillModel
            {
                Name = skill.Name.Trim(),
                Value = Math.Clamp(skill.Value <= 0 ? defaultCharacterConfig.NewSkillCost : skill.Value, defaultCharacterConfig.NewSkillCost, defaultCharacterConfig.VirtueMax)
            })
            .Take(defaultCharacterConfig.MaxSkills)
            .ToList();

        var normalizedInventory = new List<EquipmentSlotModel>();
        var incomingInventory = character.Inventory ?? new List<EquipmentSlotModel>();
        for (var index = 0; index < 9; index++)
        {
            var source = incomingInventory.FirstOrDefault(slot => slot.SlotIndex == index) ?? (index < incomingInventory.Count ? incomingInventory[index] : null);
            normalizedInventory.Add(new EquipmentSlotModel
            {
                SlotIndex = index,
                Equipment = source?.Equipment
            });
        }

        character.Inventory = normalizedInventory;
        character.CachedPointsLeft = CharacterCreationRules.CalculatePointsLeft(character, defaultCharacterConfig);
        return character;
    }

    private static CharacterSheetResponse BuildCharacterResponse(CharacterSheetModel character)
    {
        var normalized = NormalizeCharacter(new CharacterSheetModel
        {
            Id = character.Id,
            Name = character.Name,
            Vigor = character.Vigor,
            Clarity = character.Clarity,
            Spirit = character.Spirit,
            FlawCount = character.FlawCount,
            HasCoreAbility = character.HasCoreAbility,
            DeedCount = character.DeedCount,
            CachedPointsLeft = character.CachedPointsLeft,
            Skills = character.Skills.Select(skill => new CharacterSkillModel
            {
                Name = skill.Name,
                Value = skill.Value
            }).ToList(),
            Inventory = character.Inventory.Select(slot => new EquipmentSlotModel
            {
                SlotIndex = slot.SlotIndex,
                Equipment = slot.Equipment
            }).ToList()
        });

        return new CharacterSheetResponse
        {
            Character = normalized,
            Creation = defaultCharacterConfig,
            PointsLeft = CharacterCreationRules.CalculatePointsLeft(normalized, defaultCharacterConfig),
            DamageBonus = InventoryStatCalculator.GetVigorDamageBonus(normalized),
            GuardBonus = InventoryStatCalculator.GetSpiritGuardBonus(normalized),
            ClaritySaveModifier = InventoryStatCalculator.GetClaritySaveModifier(normalized),
            ArmorTotal = InventoryStatCalculator.GetEffectiveArmorTotal(normalized),
            ChunkCounts = InventoryStatCalculator.CountChunks(normalized).ToDictionary(entry => entry.Key.ToString(), entry => entry.Value)
        };
    }
}
