using System.Collections.Generic;
using UnityEngine;

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

    public bool IsWeapon => traits.Exists(t => t.IsWeapon());
    public bool IsArmor => traits.Exists(t => t.IsArmor());

    public int RequiredHands()
    {
        int hands = 0;
        foreach (var t in traits)
            hands += t.RequiredHands();
        return hands;
    }
}
