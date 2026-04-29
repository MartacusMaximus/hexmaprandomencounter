using System.Collections.Generic;

public static class InventoryOwnershipRules
{
    public static bool CanCharacterEquip(CharacterData character, EquipmentInstance item)
    {
        if (character == null || item == null || item.equipment == null)
        {
            return false;
        }

        character.EnsureIdentity();
        item.EnsureInstance();

        if (item.equipment.RequiresContainerStorage)
        {
            return false;
        }

        if (!item.bondedToOwner && !item.equipment.isBondedProperty)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(item.ownerCharacterId) || item.ownerCharacterId == character.characterId;
    }

    public static bool CanStoreInSharedInventory(EquipmentInstance item)
    {
        return item != null && item.equipment != null;
    }

    public static bool CanStoreInContainer(EquipmentInstance container, EquipmentInstance item)
    {
        if (container == null || item == null || container.equipment == null || !container.equipment.IsContainer)
        {
            return false;
        }

        if (item.equipment == null)
        {
            return false;
        }

        if (item.equipment.occupiesFullContainer)
        {
            return container.equipment.ContainerSlotCount >= 4;
        }

        return true;
    }

    public static bool TryAddToContainer(EquipmentInstance container, EquipmentInstance item)
    {
        if (!CanStoreInContainer(container, item))
        {
            return false;
        }

        container.EnsureInstance();
        item.EnsureInstance();

        if (item.equipment != null && item.equipment.occupiesFullContainer)
        {
            for (var index = 0; index < container.containedItems.Count; index++)
            {
                if (container.containedItems[index] != null)
                {
                    return false;
                }
            }

            container.containedItems[0] = item;
            for (var index = 1; index < container.containedItems.Count; index++)
            {
                container.containedItems[index] = new EquipmentInstance
                {
                    equipment = item.equipment,
                    ownerCharacterId = item.ownerCharacterId,
                    bondedToOwner = item.bondedToOwner,
                    instanceId = item.instanceId
                };
            }

            return true;
        }

        for (var index = 0; index < container.containedItems.Count; index++)
        {
            if (container.containedItems[index] == null)
            {
                container.containedItems[index] = item;
                return true;
            }
        }

        return false;
    }

    public static bool TryAddToSteed(SteedInstance steed, EquipmentInstance item)
    {
        if (steed == null || item == null || item.equipment == null)
        {
            return false;
        }

        steed.EnsureSlots();
        item.EnsureInstance();

        if (item.equipment.occupiesFullContainer)
        {
            for (var index = 0; index < steed.storage.Count; index++)
            {
                if (steed.storage[index] != null)
                {
                    return false;
                }
            }

            steed.storage[0] = item;
            for (var index = 1; index < steed.storage.Count; index++)
            {
                steed.storage[index] = new EquipmentInstance
                {
                    equipment = item.equipment,
                    ownerCharacterId = item.ownerCharacterId,
                    bondedToOwner = item.bondedToOwner,
                    instanceId = item.instanceId
                };
            }

            return true;
        }

        for (var index = 0; index < steed.storage.Count; index++)
        {
            if (steed.storage[index] == null)
            {
                steed.storage[index] = item;
                return true;
            }
        }

        return false;
    }

    public static string DescribeOwner(EquipmentInstance item, IReadOnlyList<CharacterData> characters)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.ownerCharacterId) || characters == null)
        {
            return string.Empty;
        }

        foreach (var character in characters)
        {
            if (character != null && character.characterId == item.ownerCharacterId)
            {
                return character.characterName;
            }
        }

        return string.Empty;
    }
}
