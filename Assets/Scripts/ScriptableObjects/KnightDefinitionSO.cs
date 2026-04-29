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
    [TextArea] public string randomFlavorTableTitle;
    [TextArea] public List<string> randomFlavorTableRows = new List<string>();
    [TextArea] public string personHook;
    [TextArea] public string objectHook;
    [TextArea] public string beastHook;
    [TextArea] public string stateHook;
    [TextArea] public string themeHook;
}
