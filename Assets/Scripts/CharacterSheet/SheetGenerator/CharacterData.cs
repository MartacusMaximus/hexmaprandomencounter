using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Game/CharacterData")]
public class CharacterData : ScriptableObject
{
    public int cachedPointsLeft;
    public string characterId = string.Empty;

    public string characterName = "New Character";
    public string knightRole = "Knight";
    public bool isAlive = true;
    public string currentStatus = "Available";
    public int vigor = 6;
    public int clarity = 6;
    public int spirit = 6;

    public bool movedThisTurn = false;

    [Header("Creation flags")]
    public int flawCount = 0;             // 0..2
    public bool hasCoreAbility = false;   // toggled if core bought
    public int deedCount = 0;             // increments grant 3 pts each
    public KnightingStatus knightingStatus = KnightingStatus.Unknighted;

    [Header("Knighting")]
    public KnightDefinitionSO assignedKnight;
    public SeerDefinitionSO linkedSeer;
    public AbilitySO grantedKnightAbility;
    public List<AbilitySO> knownAbilities = new List<AbilitySO>();

    [Header("Skills (ordered)")]
    public List<SkillEntry> skills = new List<SkillEntry>();

    [Header("Inventory (3x3)")]
    public List<EquipmentInstance> inventory = new List<EquipmentInstance>(); // size 9

    [Header("Steed")]
    public SteedInstance steed = new SteedInstance();

    public void EnsureInventorySlots()
    {
        EnsureIdentity();

        while (inventory.Count < 9) inventory.Add(null);
        while (inventory.Count > 9) inventory.RemoveAt(inventory.Count - 1);

        foreach (var item in inventory)
        {
            item?.EnsureInstance();
        }

        RefreshKnightingStatus();
        steed?.EnsureSlots();
    }

    public void EnsureIdentity()
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            characterId = Guid.NewGuid().ToString("N");
        }
    }

    public void RefreshKnightingStatus()
    {
        if (assignedKnight != null)
        {
            knightingStatus = KnightingStatus.Knighted;
            knightRole = assignedKnight.knightName;
            return;
        }

        if (deedCount >= 5)
        {
            knightingStatus = KnightingStatus.ReadyForKnighting;
            return;
        }

        knightingStatus = KnightingStatus.Unknighted;
    }

    public bool HasKnownAbility(AbilitySO ability)
    {
        return ability != null && knownAbilities.Contains(ability);
    }

    public void LearnAbility(AbilitySO ability, bool isKnightGranted = false)
    {
        if (ability == null)
        {
            return;
        }

        if (!knownAbilities.Contains(ability))
        {
            knownAbilities.Add(ability);
        }

        if (isKnightGranted)
        {
            grantedKnightAbility = ability;
        }
    }

    public void AssignKnight(KnightDefinitionSO knight)
    {
        EnsureIdentity();
        assignedKnight = knight;
        linkedSeer = knight != null ? knight.linkedSeer : null;
        grantedKnightAbility = knight != null ? knight.grantedAbility : null;
        if (knight != null)
        {
            LearnAbility(knight.grantedAbility, true);
            steed = new SteedInstance
            {
                ownerCharacterId = characterId,
                definition = knight.steed
            };
            steed.ResetFromDefinition();
        }

        RefreshKnightingStatus();
    }
}

public enum KnightingStatus
{
    Unknighted = 0,
    ReadyForKnighting = 1,
    Knighted = 2
}

[Serializable]
public sealed class SteedInstance
{
    public string ownerCharacterId = string.Empty;
    public SteedDefinitionSO definition;
    public int vigor;
    public int clarity;
    public int spirit;
    public int guard;
    public bool exhausted;
    public List<EquipmentInstance> storage = new List<EquipmentInstance>();

    public void ResetFromDefinition()
    {
        if (definition == null)
        {
            return;
        }

        vigor = definition.vigor;
        clarity = definition.clarity;
        spirit = definition.spirit;
        guard = definition.guard;
        exhausted = false;
        EnsureSlots();
    }

    public void EnsureSlots()
    {
        while (storage.Count < 4)
        {
            storage.Add(null);
        }

        while (storage.Count > 4)
        {
            storage.RemoveAt(storage.Count - 1);
        }

        foreach (var item in storage)
        {
            item?.EnsureInstance();
        }
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
    public string instanceId = string.Empty;
    public string ownerCharacterId = string.Empty;
    public bool bondedToOwner;
    public EquipmentData equipment;
    [TextArea] public string resolvedRulesText = string.Empty;
    public int resolvedSeeBelowRowIndex = -1;
    [NonSerialized]
    public List<EquipmentInstance> containedItems = new List<EquipmentInstance>();

    public string DisplayName => equipment != null ? equipment.itemName : string.Empty;
    public string RulesText => string.IsNullOrWhiteSpace(resolvedRulesText)
        ? (equipment != null ? equipment.rulesText : string.Empty)
        : resolvedRulesText;

    public void EnsureInstance()
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            instanceId = Guid.NewGuid().ToString("N");
        }

        var slotCount = ContainerSlotCount;
        while (containedItems.Count < slotCount)
        {
            containedItems.Add(null);
        }

        while (containedItems.Count > slotCount)
        {
            containedItems.RemoveAt(containedItems.Count - 1);
        }
    }

    public int ContainerSlotCount => equipment != null && equipment.IsContainer ? equipment.ContainerSlotCount : 0;

    public void ResolveSeeBelow(System.Random random = null)
    {
        if (equipment == null || !equipment.HasSeeBelowTable)
        {
            return;
        }

        var rowCount = MythicEquipmentTableResolver.GetRowCount(equipment.seeBelowTable);
        if (rowCount <= 0)
        {
            return;
        }

        random ??= new System.Random(Guid.NewGuid().GetHashCode());
        resolvedSeeBelowRowIndex = random.Next(0, rowCount);
        resolvedRulesText = MythicEquipmentTableResolver.ResolveSeeBelowText(
            equipment.rulesText,
            equipment.seeBelowTable,
            resolvedSeeBelowRowIndex);
    }
}
