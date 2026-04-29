using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MythicBastionlandContentLibrary", menuName = "Game/Mythic Bastionland/Content Library")]
public class MythicBastionlandContentLibrarySO : ScriptableObject
{
    public List<KnightDefinitionSO> knights = new List<KnightDefinitionSO>();
    public List<SeerDefinitionSO> seers = new List<SeerDefinitionSO>();
    public List<MythSO> myths = new List<MythSO>();
    public List<AbilitySO> abilities = new List<AbilitySO>();
    public List<EquipmentData> equipment = new List<EquipmentData>();
    public PersonSO people;
    public NameSO names;
    public CharacteristicSO characteristics;
    public ObjectSO objects;
    public BeastSO beasts;
    public StateSO states;
    public ThemeSO themes;
    public DwellingSO dwellings;
    public SanctumSO sanctums;
    public MonumentSO monuments;
    public HazardSO hazards;
    public CurseSO curses;
    public RuinSO ruins;

    public static MythicBastionlandContentLibrarySO LoadDefault()
    {
        return Resources.Load<MythicBastionlandContentLibrarySO>("MythicBastionland/MythicBastionlandContentLibrary");
    }
}
