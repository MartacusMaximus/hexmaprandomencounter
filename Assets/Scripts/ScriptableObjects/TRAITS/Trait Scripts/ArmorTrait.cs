using UnityEngine;

[CreateAssetMenu(menuName = "Traits/Armor")]
public class ArmorTrait : TraitSO
{
    public EquipLocation location;

    public override bool IsArmor() => true;
}
