using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class PartyInventory : IInventoryContainer
{
    public List<EquipmentInstance> items = new();

    public List<EquipmentInstance> Items => items;

    public bool CanAccept(EquipmentInstance item) => true;

    public bool TryAdd(EquipmentInstance item)
    {
        items.Add(item);
        return true;
    }

    public bool TryRemove(EquipmentInstance item)
    {
        return items.Remove(item);
    }
}

