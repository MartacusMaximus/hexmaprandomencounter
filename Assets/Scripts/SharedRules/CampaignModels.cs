using System;
using System.Collections.Generic;

namespace KnightsAndGM.Shared
{
    public enum CampaignRole
    {
        Player = 0,
        GameMaster = 1
    }

    public enum PortableChunkColor
    {
        None = 0,
        Red = 1,
        Green = 2,
        Blue = 3,
        Rainbow = 4
    }

    public enum PortableEquipmentLocation
    {
        None = 0,
        Head = 1,
        Torso = 2,
        Legs = 3,
        Hands = 4,
        Waist = 5,
        Shield = 6
    }

    public sealed class CampaignUser
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public CampaignRole Role { get; set; }
    }

    public sealed class CharacterSkillModel
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public sealed class PortableAbilityModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int RequiredChunkCount { get; set; }
        public PortableChunkColor RequiredColor { get; set; }
        public bool RequiresLinkedChunk { get; set; }
        public int AddDamageFlat { get; set; }
        public int AddGuardFlat { get; set; }
        public int ModifyReaction { get; set; }
    }

    public sealed class PortableEquipmentModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int PointCost { get; set; }
        public string Rarity { get; set; }
        public string DisplayCategory { get; set; }
        public string RulesText { get; set; }
        public string DamageDiceNotation { get; set; }
        public int ArmorValue { get; set; }
        public bool CostsCreationPoints { get; set; }
        public bool IsWeapon { get; set; }
        public bool IsArmor { get; set; }
        public int RequiredHands { get; set; }
        public PortableEquipmentLocation ArmorLocation { get; set; }
        public List<string> TraitNames { get; set; } = new List<string>();
        public List<string> SourceTags { get; set; } = new List<string>();
        public PortableAbilityModel Ability { get; set; }
        public PortableChunkColor LeftHalf { get; set; }
        public PortableChunkColor RightHalf { get; set; }
        public PortableChunkColor TopHalf { get; set; }
        public PortableChunkColor BottomHalf { get; set; }
        public PortableChunkColor CenterChunk { get; set; }
        public bool IsBondedProperty { get; set; }
        public bool ContributesToEquippedBonuses { get; set; } = true;
        public bool RequiresContainerStorage { get; set; }
        public bool OccupiesFullContainer { get; set; }
        public List<string> GeneratedTags { get; set; } = new List<string>();
    }

    public sealed class PortableSlotActivationModel
    {
        public int SlotIndex { get; set; }
        public bool CenterActive { get; set; }
        public bool TopLinked { get; set; }
        public bool BottomLinked { get; set; }
        public bool LeftLinked { get; set; }
        public bool RightLinked { get; set; }
        public bool ChunkRequirementMet { get; set; }
        public bool LinkedRequirementMet { get; set; }
        public bool AbilityActive { get; set; }
    }

    public sealed class PortableInventoryActivationModel
    {
        public Dictionary<PortableChunkColor, int> ChunkCounts { get; set; } = new Dictionary<PortableChunkColor, int>();
        public List<PortableSlotActivationModel> Slots { get; set; } = new List<PortableSlotActivationModel>();
        public int ActiveAbilityDamageFlat { get; set; }
        public int ActiveAbilityGuardFlat { get; set; }
        public int ActiveAbilityReactionModifier { get; set; }
    }

    public sealed class EquipmentSlotModel
    {
        public int SlotIndex { get; set; }
        public PortableEquipmentModel Equipment { get; set; }
    }

    public sealed class CharacterSheetModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Vigor { get; set; }
        public int Clarity { get; set; }
        public int Spirit { get; set; }
        public int FlawCount { get; set; }
        public bool HasCoreAbility { get; set; }
        public int DeedCount { get; set; }
        public int CachedPointsLeft { get; set; }
        public List<CharacterSkillModel> Skills { get; set; } = new List<CharacterSkillModel>();
        public List<EquipmentSlotModel> Inventory { get; set; } = new List<EquipmentSlotModel>();
    }

    public sealed class TravelLogEntry
    {
        public Guid Id { get; set; }
        public HexCoordinate From { get; set; }
        public HexCoordinate To { get; set; }
        public int Cost { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public string Summary { get; set; }
    }

    public sealed class PartySnapshot
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public HexCoordinate CurrentHex { get; set; }
        public int RemainingEffort { get; set; }
        public int TotalEffort { get; set; }
        public List<Guid> CharacterIds { get; set; } = new List<Guid>();
        public List<PortableEquipmentModel> SharedInventory { get; set; } = new List<PortableEquipmentModel>();
        public List<TravelLogEntry> TravelLog { get; set; } = new List<TravelLogEntry>();
    }

    public sealed class HexTileModel
    {
        public Guid Id { get; set; }
        public HexCoordinate Coordinate { get; set; }
        public TerrainType Terrain { get; set; }
        public bool IsExploredPublic { get; set; }
        public string PublicLandmarkName { get; set; }
        public string SecretLandmarkName { get; set; }
        public string SecretDetails { get; set; }
        public string LandmarkKind { get; set; }
    }

    public sealed class HexDiscoveryModel
    {
        public Guid Id { get; set; }
        public Guid HexTileId { get; set; }
        public Guid DiscoveredByPartyId { get; set; }
        public Guid? DiscoveredByUserId { get; set; }
        public DateTime DiscoveredAtUtc { get; set; }
    }

    public sealed class PublicNoteModel
    {
        public Guid Id { get; set; }
        public Guid HexTileId { get; set; }
        public Guid AuthorUserId { get; set; }
        public string AuthorName { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public sealed class CampaignMapSnapshot
    {
        public Guid CampaignId { get; set; }
        public string Name { get; set; }
        public int Radius { get; set; }
        public List<HexTileModel> Tiles { get; set; } = new List<HexTileModel>();
        public List<PartySnapshot> Parties { get; set; } = new List<PartySnapshot>();
        public List<PublicNoteModel> Notes { get; set; } = new List<PublicNoteModel>();
    }

    public sealed class EncounterLedgerEntryModel
    {
        public Guid Id { get; set; }
        public Guid CampaignId { get; set; }
        public Guid PartyId { get; set; }
        public Guid HexTileId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string PartyName { get; set; }
        public bool Resolved { get; set; }
        public EncounterPrompt Prompt { get; set; }
    }

    public sealed class CampaignStateModel
    {
        public Guid CampaignId { get; set; }
        public string Name { get; set; }
        public string Seed { get; set; }
        public int Radius { get; set; }
        public List<HexTileModel> Tiles { get; set; } = new List<HexTileModel>();
        public List<HexDiscoveryModel> Discoveries { get; set; } = new List<HexDiscoveryModel>();
        public List<CharacterSheetModel> Characters { get; set; } = new List<CharacterSheetModel>();
        public List<PartySnapshot> Parties { get; set; } = new List<PartySnapshot>();
        public List<PublicNoteModel> Notes { get; set; } = new List<PublicNoteModel>();
        public List<EncounterLedgerEntryModel> Encounters { get; set; } = new List<EncounterLedgerEntryModel>();
    }
}
