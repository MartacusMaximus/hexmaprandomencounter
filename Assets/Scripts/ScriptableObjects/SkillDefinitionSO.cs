using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Game/Skill Definition")]
public class SkillDefinitionSO : ScriptableObject
{
    public string skillName;
    [TextArea] public string description;
    public List<string> sourceTags = new List<string>();
}
