using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class CharacterInventory : IInventoryContainer
{
    public List<EquipmentInstance> slots = new(9);

    public List<EquipmentInstance> Items => slots;

    public bool CanAccept(EquipmentInstance item)
    {
        return slots.Exists(s => s == null);
    }

    public bool TryAdd(EquipmentInstance item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                return true;
            }
        }
        return false;
    }

    public bool TryRemove(EquipmentInstance item)
    {
        int idx = slots.IndexOf(item);
        if (idx < 0) return false;
        slots[idx] = null;
        return true;
    }
}

