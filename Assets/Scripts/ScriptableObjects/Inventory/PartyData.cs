using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class PartyData : IInventoryContainer
{
    public string partyName = "Active Expedition";
    public List<CharacterData> members = new();
    public List<EquipmentInstance> partyInventory = new();

    public List<EquipmentInstance> Items => partyInventory;

    public bool CanAccept(EquipmentInstance item) => true;

    public bool TryAdd(EquipmentInstance item)
    {
        partyInventory.Add(item);
        return true;
    }

    public bool TryRemove(EquipmentInstance item)
    {
        return partyInventory.Remove(item);
    }
}
