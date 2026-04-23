using UnityEngine;

[CreateAssetMenu(menuName = "Traits/Deadly")]
public class DeadlyTrait : TraitSO
{
    public int extraDamageDice = 1;

    public override void ModifyDamageRoll(ref int diceCount, ref int flatBonus)
    {
        diceCount += extraDamageDice;
    }
}
