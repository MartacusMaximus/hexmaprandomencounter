using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentLibrary", menuName = "Game/Equipment Library")]
public class EquipmentLibrarySO : ScriptableObject
{
    public List<EquipmentData> items = new List<EquipmentData>();
}
