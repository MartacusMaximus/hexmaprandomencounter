using UnityEngine;

public abstract class TraitSO : ScriptableObject
{
    public string traitName;
    [TextArea] public string description;

    public virtual void ModifyDamageRoll(ref int diceCount, ref int flatBonus) { }

    public virtual bool CanBeUsed(CharacterData character, InventoryGrid grid)
    {
        return true;
    }

    public virtual int RequiredHands() { return 0; }

    public virtual bool IsWeapon() { return false; }
    public virtual bool IsArmor() { return false; }
}
