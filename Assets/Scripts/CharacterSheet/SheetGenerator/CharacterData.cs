using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/CharacterData")]
public class CharacterData : ScriptableObject
{

    public int cachedPointsLeft;

    public string characterName = "New Character";
    public int vigor = 6;
    public int clarity = 6;
    public int spirit = 6;

    public bool movedThisTurn = false;

    [Header("Creation flags")]
    public int flawCount = 0;             // 0..2
    public bool hasCoreAbility = false;   // toggled if core bought
    public int deedCount = 0;             // increments grant 3 pts each

    [Header("Skills (ordered)")]
    public List<SkillEntry> skills = new List<SkillEntry>();

    [Header("Inventory (3x3)")]
    public List<EquipmentInstance> inventory = new List<EquipmentInstance>(); // size 9

    public void EnsureInventorySlots()
    {
        while (inventory.Count < 9) inventory.Add(null);
        while (inventory.Count > 9) inventory.RemoveAt(inventory.Count - 1);
    }
}


[Serializable]
public class SkillEntry
{
    public string skillName;
    public int value; // between 3 and 18
}

[Serializable]
public class EquipmentInstance
{
    public EquipmentData equipment; 
}
