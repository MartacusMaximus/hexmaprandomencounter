using System.Security.Cryptography;
using System.Text;
using KnightsAndGM.Shared;

namespace KnightsAndGM.Api.Seed;

public sealed class SeedCampaignFactory
{
    public static readonly Guid DefaultCampaignId = Guid.Parse("1f18ecf2-29d2-4aa8-a111-5449f0c01001");
    public const string DefaultCampaignName = "City of Light";
    public const string DefaultCampaignSeed = "city-of-light-standard-map";
    private const int StandardRadius = 6;

    public CampaignStateModel CreateDefaultCampaign()
    {
        var characterId = Guid.Parse("73e4e8c9-0dbf-4f2a-9b7c-083a277d0001");
        var partyId = Guid.Parse("fc5814f5-2a8b-470b-b037-2d5b6f7e0001");
        var tiles = BuildStandardTiles();
        var cityCenter = tiles.First(tile => tile.Coordinate == new HexCoordinate(0, 0));

        var campaign = new CampaignStateModel
        {
            CampaignId = DefaultCampaignId,
            Name = DefaultCampaignName,
            Seed = DefaultCampaignSeed,
            Radius = StandardRadius,
            Tiles = tiles
        };

        var character = new CharacterSheetModel
        {
            Id = characterId,
            Name = "Frontier Knight",
            Vigor = 8,
            Clarity = 7,
            Spirit = 6,
            FlawCount = 0,
            HasCoreAbility = false,
            DeedCount = 0,
            Skills = new List<CharacterSkillModel>
            {
                new CharacterSkillModel { Name = "Scout", Value = 4 },
                new CharacterSkillModel { Name = "Survive", Value = 4 },
                new CharacterSkillModel { Name = "Smite", Value = 5 }
            },
            Inventory = Enumerable.Range(0, 9)
                .Select(index => new EquipmentSlotModel { SlotIndex = index })
                .ToList()
        };
        campaign.Characters.Add(character);

        campaign.Parties.Add(new PartySnapshot
        {
            Id = partyId,
            Name = "First Expedition",
            CurrentHex = cityCenter.Coordinate,
            RemainingEffort = 6,
            TotalEffort = 6,
            CharacterIds = new List<Guid> { characterId }
        });

        foreach (var tile in tiles.Where(tile => tile.IsExploredPublic))
        {
            campaign.Discoveries.Add(new HexDiscoveryModel
            {
                Id = Guid.NewGuid(),
                HexTileId = tile.Id,
                DiscoveredByPartyId = partyId,
                DiscoveredAtUtc = DateTime.UtcNow
            });
        }

        campaign.Notes.Add(new PublicNoteModel
        {
            Id = Guid.NewGuid(),
            HexTileId = cityCenter.Id,
            AuthorUserId = Guid.Empty,
            AuthorName = "Chronicler",
            Body = "The City of Light is charted. Terrain colors are public knowledge, but landmark signs only become public once an expedition reaches the hex.",
            CreatedAtUtc = DateTime.UtcNow
        });

        return campaign;
    }

    private static List<HexTileModel> BuildStandardTiles()
    {
        var landmarks = BuildLandmarks();
        var tiles = new List<HexTileModel>();

        for (var r = -StandardRadius; r <= StandardRadius; r++)
        {
            var qMin = Math.Max(-StandardRadius, -r - StandardRadius);
            var qMax = Math.Min(StandardRadius, -r + StandardRadius);
            for (var q = qMin; q <= qMax; q++)
            {
                var coordinate = new HexCoordinate(q, r);
                landmarks.TryGetValue(coordinate, out var landmark);

                tiles.Add(new HexTileModel
                {
                    Id = CreateDeterministicGuid($"{DefaultCampaignSeed}:{q},{r}"),
                    Coordinate = coordinate,
                    Terrain = GetTerrainFor(coordinate),
                    IsExploredPublic = coordinate.DistanceTo(new HexCoordinate(0, 0)) <= 1,
                    PublicLandmarkName = landmark?.PublicName ?? string.Empty,
                    SecretLandmarkName = landmark?.SecretName ?? string.Empty,
                    SecretDetails = landmark?.SecretDetails ?? string.Empty,
                    LandmarkKind = landmark?.Kind ?? string.Empty
                });
            }
        }

        return tiles;
    }

    private static TerrainType GetTerrainFor(HexCoordinate coordinate)
    {
        var distance = coordinate.DistanceTo(new HexCoordinate(0, 0));
        if (distance <= 1)
        {
            return TerrainType.City;
        }

        if (coordinate.R >= 4)
        {
            return coordinate.Q <= -2 ? TerrainType.Desert
                : coordinate.Q <= 3 ? TerrainType.Desert
                : coordinate.Q == 4 ? TerrainType.Plains
                : TerrainType.Water;
        }

        if (coordinate.R >= 2)
        {
            if (coordinate.Q <= -3) return TerrainType.Tundra;
            if (coordinate.Q <= -1) return TerrainType.Plains;
            if (coordinate.Q <= 2) return TerrainType.Badlands;
            if (coordinate.Q <= 4) return TerrainType.Forest;
            return TerrainType.Mountain;
        }

        if (coordinate.R >= 0)
        {
            if (coordinate.Q <= -4) return TerrainType.Water;
            if (coordinate.Q <= -2) return coordinate.R <= 0 ? TerrainType.Water : TerrainType.Swamp;
            if (coordinate.Q <= 1) return TerrainType.Plains;
            if (coordinate.Q <= 3) return TerrainType.Forest;
            if (coordinate.Q <= 5) return TerrainType.Mountain;
            return TerrainType.Tar;
        }

        if (coordinate.R >= -2)
        {
            if (coordinate.Q <= -3) return TerrainType.Water;
            if (coordinate.Q <= -1) return TerrainType.Swamp;
            if (coordinate.Q <= 1) return TerrainType.Plains;
            if (coordinate.Q <= 4) return coordinate.R == -2 ? TerrainType.Deadwood : TerrainType.Forest;
            return TerrainType.Mountain;
        }

        if (coordinate.R >= -4)
        {
            if (coordinate.Q <= -3) return TerrainType.Swamp;
            if (coordinate.Q <= -1) return TerrainType.Water;
            if (coordinate.Q <= 2) return TerrainType.Deadwood;
            if (coordinate.Q <= 4) return TerrainType.Deadwood;
            return TerrainType.Tundra;
        }

        if (coordinate.Q <= 1) return TerrainType.Swamp;
        if (coordinate.Q <= 5) return TerrainType.Deadwood;
        return TerrainType.Tundra;
    }

    private static Dictionary<HexCoordinate, LandmarkSeed> BuildLandmarks()
    {
        return new Dictionary<HexCoordinate, LandmarkSeed>
        {
            [new HexCoordinate(0, 0)] = new LandmarkSeed("Great Temple of Illumination", "Temple Vaults", "The public heart of the city and the safest place to gather expeditions.", "temple"),
            [new HexCoordinate(-1, 0)] = new LandmarkSeed("City Ward", "Guild Barracks", "The western ward shelters caravans, militias, and public records.", "city"),
            [new HexCoordinate(1, -3)] = new LandmarkSeed("Crown Lair", "Dark Brood Pit", "Signs of an older predator surround the ridge.", "lair"),
            [new HexCoordinate(5, -4)] = new LandmarkSeed("North Dungeon", "Sunken Vault", "A sealed descent lies below the poisoned canopy.", "dungeon"),
            [new HexCoordinate(-4, 0)] = new LandmarkSeed("West Lair", "Bog Beast Nest", "The marsh keeps swallowing scouts on this route.", "lair"),
            [new HexCoordinate(-3, 4)] = new LandmarkSeed("South Shrine", "Ashen Catacomb", "Pilgrims still leave offerings here before heading into the dunes.", "ruins"),
            [new HexCoordinate(0, 5)] = new LandmarkSeed("Southern Gate", "Temple Bastion", "A fortified chapel marks the southern approach.", "temple"),
            [new HexCoordinate(4, 2)] = new LandmarkSeed("Hill Fortress", "Old War Keep", "Broken defenses still command the ridge line.", "fortress"),
            [new HexCoordinate(3, 4)] = new LandmarkSeed("Dune Village", "Smuggler Camp", "Trade caravans rest here when the roads are calm.", "village"),
            [new HexCoordinate(5, 1)] = new LandmarkSeed("East Ruins", "Watchtower Cellars", "Collapsed stonework hides an older tunnel beneath.", "ruins"),
            [new HexCoordinate(2, -1)] = new LandmarkSeed("Ridge Village", "Toll Cache", "A hill settlement watches over the greener road.", "village"),
            [new HexCoordinate(-2, 3)] = new LandmarkSeed("Fallen Fort", "Silent Armory", "A broken bastion still stands over the southern flats.", "fortress"),
            [new HexCoordinate(4, -1)] = new LandmarkSeed("Hunter Lair", "Trophy Hollow", "The pines here are marked by claw and bone charms.", "lair"),
            [new HexCoordinate(-1, -4)] = new LandmarkSeed("Bog Dungeon", "Flooded Crypt", "A stone mouth sinks under brackish water after every rain.", "dungeon"),
            [new HexCoordinate(-2, -2)] = new LandmarkSeed("West Ruins", "Old Signal Post", "A shattered marker tower still points back toward the city.", "ruins"),
            [new HexCoordinate(2, 3)] = new LandmarkSeed("Lantern Hamlet", "Quartermaster Stash", "A modest hamlet supplies expeditions and hides reserve stores.", "village"),
            [new HexCoordinate(6, 0)] = new LandmarkSeed("Stone Teeth", "Hidden Pass", "The cliffs are harsh, but there is a survivable route through them.", "ruins"),
            [new HexCoordinate(-5, 3)] = new LandmarkSeed("Glacier Lair", "Ice Maw", "Frozen bones ring a cave mouth at the world edge.", "lair")
        };
    }

    private static Guid CreateDeterministicGuid(string seed)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(bytes);
    }

    private sealed record LandmarkSeed(string PublicName, string SecretName, string SecretDetails, string Kind);
}
