using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum EquipmentContainerKind
{
    None = 0,
    Backpack = 1
}

[System.Serializable]
public enum EquipmentStorageRule
{
    Any = 0,
    ContainerOnly = 1
}

[CreateAssetMenu(menuName = "Game/Equipment")]
public class EquipmentData : ScriptableObject
{
    public string itemName;
    public int pointCost;
    public string rarity;
    public string displayCategory;
    [TextArea] public string rulesText;

    public string damageDiceNotation; // "1d6"
    public int armorValue;
    public bool costsCreationPoints = true;


    public ChunkColor leftHalf;
    public ChunkColor rightHalf;
    public ChunkColor topHalf;
    public ChunkColor bottomHalf;
    public ChunkColor centerChunk;

    public List<TraitSO> traits = new List<TraitSO>();
    public List<string> sourceTags = new List<string>();
    public AbilitySO ability;
    public MythicRollTable seeBelowTable = new MythicRollTable();
    public bool isBondedProperty;
    public bool contributesToEquippedBonuses = true;
    public EquipmentContainerKind containerKind = EquipmentContainerKind.None;
    public EquipmentStorageRule storageRule = EquipmentStorageRule.Any;
    public bool occupiesFullContainer;
    public bool usableByNonOwner = true;

    public bool IsWeapon => traits.Exists(t => t.IsWeapon());
    public bool IsArmor => traits.Exists(t => t.IsArmor());
    public bool IsContainer => containerKind != EquipmentContainerKind.None;
    public bool RequiresContainerStorage => storageRule == EquipmentStorageRule.ContainerOnly;
    public int ContainerSlotCount => IsContainer ? 4 : 0;
    public bool HasSeeBelowTable => MythicEquipmentTableResolver.HasTable(seeBelowTable);

    public int RequiredHands()
    {
        int hands = 0;
        foreach (var t in traits)
            hands += t.RequiredHands();
        return hands;
    }
}
