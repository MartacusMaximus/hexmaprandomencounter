using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeerDefinition", menuName = "Game/Mythic Bastionland/Seer Definition")]
public class SeerDefinitionSO : ScriptableObject
{
    public string seerName;
    public int vigor;
    public int clarity;
    public int spirit;
    public int guard;
    [TextArea] public List<string> traits = new List<string>();
}
