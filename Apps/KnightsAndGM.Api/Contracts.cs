using KnightsAndGM.Shared;

namespace KnightsAndGM.Api;

public sealed class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public CampaignRole Role { get; set; } = CampaignRole.Player;
}

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public CampaignRole Role { get; set; } = CampaignRole.Player;
}

public sealed class MovePartyRequest
{
    public int TargetQ { get; set; }
    public int TargetR { get; set; }
}

public sealed class CreateNoteRequest
{
    public string Body { get; set; } = string.Empty;
}

public sealed class SaveCharacterRequest
{
    public CharacterSheetModel Character { get; set; } = new CharacterSheetModel();
}

public sealed class CampaignSummaryResponse
{
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CampaignRole Role { get; set; }
}

public sealed class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public CampaignUser User { get; set; } = new CampaignUser();
    public List<CampaignSummaryResponse> Campaigns { get; set; } = new List<CampaignSummaryResponse>();
}

public sealed class CampaignMapResponse
{
    public Guid CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CampaignRole Role { get; set; }
    public int Radius { get; set; }
    public List<HexTileModel> Tiles { get; set; } = new List<HexTileModel>();
    public List<PublicNoteModel> Notes { get; set; } = new List<PublicNoteModel>();
    public List<PartySnapshot> Parties { get; set; } = new List<PartySnapshot>();
}

public sealed class MovePartyResponse
{
    public PartySnapshot Party { get; set; } = new PartySnapshot();
    public TravelLogEntry TravelLogEntry { get; set; } = new TravelLogEntry();
    public Guid? NewlyDiscoveredHexId { get; set; }
}

public sealed class CharacterSheetResponse
{
    public CharacterSheetModel Character { get; set; } = new CharacterSheetModel();
    public CharacterCreationConfig Creation { get; set; } = new CharacterCreationConfig();
    public int PointsLeft { get; set; }
    public int DamageBonus { get; set; }
    public int GuardBonus { get; set; }
    public int ClaritySaveModifier { get; set; }
    public int ArmorTotal { get; set; }
    public Dictionary<string, int> ChunkCounts { get; set; } = new Dictionary<string, int>();
}
