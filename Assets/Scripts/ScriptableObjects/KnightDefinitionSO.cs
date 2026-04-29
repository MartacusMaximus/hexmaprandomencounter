using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KnightDefinition", menuName = "Game/Mythic Bastionland/Knight Definition")]
public class KnightDefinitionSO : ScriptableObject
{
    public int pageNumber;
    public string knightName;
    [TextArea] public string titleVerse;
    [TextArea] public string passionText;
    public AbilitySO grantedAbility;
    public SeerDefinitionSO linkedSeer;
    public SteedDefinitionSO steed;
    public List<EquipmentData> bondedProperty = new List<EquipmentData>();
    public MythicRollTable randomFlavorTable = new MythicRollTable();
}
