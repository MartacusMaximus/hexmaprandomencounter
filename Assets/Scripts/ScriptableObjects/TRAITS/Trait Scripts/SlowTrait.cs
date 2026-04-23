using UnityEngine;

[CreateAssetMenu(menuName = "Traits/Slow")]
public class SlowTrait : TraitSO
{
    public override bool CanBeUsed(CharacterData character, InventoryGrid grid)
    {
        return !character.movedThisTurn;
    }
}