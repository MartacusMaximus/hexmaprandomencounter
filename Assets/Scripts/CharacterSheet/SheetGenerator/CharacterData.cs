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
    public string resolvedDamageDiceNotation = string.Empty;
    public int resolvedArmorValue = -1;
    public int resolvedRequiredHands = -1;
    public List<string> resolvedTraitNames = new List<string>();
    public List<string> resolvedGeneratedTags = new List<string>();
    public int resolvedSeeBelowRowIndex = -1;
    [NonSerialized]
    public List<EquipmentInstance> containedItems = new List<EquipmentInstance>();

    public string DisplayName => equipment != null ? equipment.itemName : string.Empty;
    public string RulesText => string.IsNullOrWhiteSpace(resolvedRulesText)
        ? (equipment != null ? equipment.rulesText : string.Empty)
        : resolvedRulesText;
    public string DamageDiceNotation => string.IsNullOrWhiteSpace(resolvedDamageDiceNotation)
        ? (equipment != null ? equipment.damageDiceNotation : string.Empty)
        : resolvedDamageDiceNotation;
    public int ArmorValue => resolvedArmorValue >= 0
        ? resolvedArmorValue
        : (equipment != null ? equipment.armorValue : 0);
    public int RequiredHands => resolvedRequiredHands >= 0
        ? resolvedRequiredHands
        : (equipment != null ? equipment.RequiredHands() : 0);

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

    public IEnumerable<string> GetResolvedTraitNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (equipment != null)
        {
            foreach (var trait in equipment.traits)
            {
                if (trait != null && !string.IsNullOrWhiteSpace(trait.traitName))
                {
                    names.Add(trait.traitName);
                }
            }
        }

        foreach (var traitName in resolvedTraitNames)
        {
            if (!string.IsNullOrWhiteSpace(traitName))
            {
                names.Add(traitName);
            }
        }

        return names;
    }

    public void CopyResolutionFrom(EquipmentInstance source)
    {
        if (source == null)
        {
            return;
        }

        resolvedRulesText = source.resolvedRulesText;
        resolvedDamageDiceNotation = source.resolvedDamageDiceNotation;
        resolvedArmorValue = source.resolvedArmorValue;
        resolvedRequiredHands = source.resolvedRequiredHands;
        resolvedSeeBelowRowIndex = source.resolvedSeeBelowRowIndex;
        resolvedTraitNames = new List<string>(source.resolvedTraitNames ?? new List<string>());
        resolvedGeneratedTags = new List<string>(source.resolvedGeneratedTags ?? new List<string>());
    }

    private void ClearResolvedOverrides()
    {
        resolvedRulesText = string.Empty;
        resolvedDamageDiceNotation = string.Empty;
        resolvedArmorValue = -1;
        resolvedRequiredHands = -1;
        resolvedSeeBelowRowIndex = -1;
        resolvedTraitNames = new List<string>();
        resolvedGeneratedTags = new List<string>();
    }

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
        ClearResolvedOverrides();
        resolvedSeeBelowRowIndex = random.Next(0, rowCount);
        var resolved = MythicEquipmentTableResolver.ResolveEquipment(
            equipment,
            equipment.seeBelowTable,
            resolvedSeeBelowRowIndex);
        resolvedRulesText = resolved.rulesText ?? string.Empty;
        resolvedDamageDiceNotation = resolved.damageDiceNotation ?? string.Empty;
        resolvedArmorValue = resolved.armorValue;
        resolvedRequiredHands = resolved.requiredHands;
        resolvedTraitNames = resolved.traitNames ?? new List<string>();
        resolvedGeneratedTags = resolved.generatedTags ?? new List<string>();
    }
}
