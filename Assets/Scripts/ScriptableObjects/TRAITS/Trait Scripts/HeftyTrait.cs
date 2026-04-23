using UnityEngine;

[CreateAssetMenu(menuName = "Traits/Hefty")]
public class HeftyTrait : TraitSO
{
    public override int RequiredHands() => 1;
}

