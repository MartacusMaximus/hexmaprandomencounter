using UnityEngine;
using System.Collections.Generic;


public interface IInventoryContainer
{
    List<EquipmentInstance> Items { get; }

    bool CanAccept(EquipmentInstance item);
    bool TryAdd(EquipmentInstance item);
    bool TryRemove(EquipmentInstance item);
}

