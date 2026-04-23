using UnityEngine;

[CreateAssetMenu(menuName = "Traits/Weapon")]
public class WeaponTrait : TraitSO
{
    public override bool IsWeapon() => true;
}
