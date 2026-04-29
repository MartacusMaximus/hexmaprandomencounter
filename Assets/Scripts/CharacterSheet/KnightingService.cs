using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class KnightingService
{
    public static bool IsReadyForKnighting(CharacterData character)
    {
        if (character == null)
        {
            return false;
        }

        character.EnsureInventorySlots();
        return character.knightingStatus == KnightingStatus.ReadyForKnighting;
    }

    public static KnightDefinitionSO RollKnight(CharacterData character, IEnumerable<KnightDefinitionSO> availableKnights, int? seed = null)
    {
        var candidates = availableKnights?.Where(knight => knight != null).ToList() ?? new List<KnightDefinitionSO>();
        if (character == null || candidates.Count == 0)
        {
            return null;
        }

        var random = seed.HasValue
            ? new System.Random(seed.Value)
            : new System.Random(Guid.NewGuid().GetHashCode());
        return candidates[random.Next(0, candidates.Count)];
    }

    public static bool AssignKnight(CharacterData character, KnightDefinitionSO knight, CampaignInventory sharedInventory = null)
    {
        if (character == null || knight == null || character.assignedKnight != null)
        {
            return false;
        }

        character.AssignKnight(knight);
        character.knightRole = knight.knightName;
        var random = new System.Random(Guid.NewGuid().GetHashCode());

        foreach (var property in knight.bondedProperty.Where(item => item != null))
        {
            var instance = new EquipmentInstance
            {
                equipment = property,
                ownerCharacterId = character.characterId,
                bondedToOwner = true
            };
            instance.EnsureInstance();
            instance.ResolveSeeBelow(random);

            if (!TryAddToInventory(character.inventory, instance))
            {
                sharedInventory?.TryAdd(instance);
            }
        }

        return true;
    }

    private static bool TryAddToInventory(List<EquipmentInstance> inventory, EquipmentInstance item)
    {
        if (inventory == null || item == null)
        {
            return false;
        }

        for (var index = 0; index < inventory.Count; index++)
        {
            if (inventory[index] == null)
            {
                inventory[index] = item;
                return true;
            }
        }

        return false;
    }
}
