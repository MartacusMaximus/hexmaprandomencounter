using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterCreationManager : MonoBehaviour
{
    [Header("References")]
    public CharacterData workingCharacter; // can be a runtime instance, or a new SO copy
    public TMP_Text pointsLeftText;

    // UI references for attributes (assign in inspector)
    public TMP_Text vigorText, clarityText, spiritText;
    public Button vigorPlus, vigorMinus, clarityPlus, clarityMinus, spiritPlus, spiritMinus;

    // skill UI
    public TMP_InputField newSkillNameInput;
    public Button addSkillButton;
    public Transform skillsListParent; // parent to instantiate skill rows (if using prefab)

    // flaw toggles (two)
    public Toggle flawToggle1;
    public Toggle flawToggle2;

    // core ability toggle / selection
    public Toggle coreAbilityToggle; // simple example

    private const int baseVirtueStart = 6;
    private const int virtueMax = 18;
    private const int newSkillCost = 3; // add skill base cost
    private const int maxSkills = 10;

    [Header("Point Accounting")]
    public int startingPoints = 50;
    public int deedPointsPerDeed = 3;
    public int flawGrant = 5;
    public bool coreAbilityPurchased = false;
    public int coreAbilityCost = 15;

    private IDiceRoller diceRoller = new DiceRollerRandom();

    private List<string> transactionLog = new List<string>();

    public int PointsLeft { get; private set; } = 0;

    private void Start()
    {
        workingCharacter.EnsureInventorySlots();
        RecalculatePointsFromCharacterData();
        UpdateAttributeText();
    }


    private void Awake()
    {

        // Ensure virtues start values
        if (workingCharacter.vigor < baseVirtueStart) workingCharacter.vigor = baseVirtueStart;
        if (workingCharacter.clarity < baseVirtueStart) workingCharacter.clarity = baseVirtueStart;
        if (workingCharacter.spirit < baseVirtueStart) workingCharacter.spirit = baseVirtueStart;

        UpdatePointsText();

        // Hook UI events (simple wiring; you should also unregister on OnDestroy if needed)
        vigorPlus.onClick.AddListener(() => ChangeVirtue(ref workingCharacter.vigor, +1));
        vigorMinus.onClick.AddListener(() => ChangeVirtue(ref workingCharacter.vigor, -1));
        clarityPlus.onClick.AddListener(() => ChangeVirtue(ref workingCharacter.clarity, +1));
        clarityMinus.onClick.AddListener(() => ChangeVirtue(ref workingCharacter.clarity, -1));
        spiritPlus.onClick.AddListener(() => ChangeVirtue(ref workingCharacter.spirit, +1));
        spiritMinus.onClick.AddListener(() => ChangeVirtue(ref workingCharacter.spirit, -1));

        addSkillButton.onClick.AddListener(OnAddSkillClicked);
        flawToggle1.onValueChanged.AddListener((b) => OnFlawToggleChanged());
        flawToggle2.onValueChanged.AddListener((b) => OnFlawToggleChanged());
        coreAbilityToggle.onValueChanged.AddListener((b) => OnCoreAbilityToggled(b));
    }

    private void ChangeVirtue(ref int virtueField, int delta)
    {
        int old = virtueField;
        int newVal = Mathf.Clamp(virtueField + delta, baseVirtueStart, virtueMax);
        int cost = (newVal - old) * 1; // define cost per +1 (if you want variable costs change here). Example: using 1 point per +1

        if (delta > 0 && cost > PointsLeft)
        {
            Debug.Log("Not enough points to increase virtue.");
            return;
        }

        if (delta > 0) PointsLeft -= cost;
        else if (delta < 0) PointsLeft += -cost; // refund

        virtueField = newVal;
        Debug.Log($"Virtue change: from {old} to {virtueField} (delta {delta}). Points left: {PointsLeft}");
        UpdateAttributeText();
        UpdatePointsText();
    }

    private void OnAddSkillClicked()
    {
        string name = newSkillNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.Log("Skill name empty.");
            return;
        }
        if (workingCharacter.skills.Count >= maxSkills)
        {
            Debug.Log($"Cannot add more than {maxSkills} skills.");
            return;
        }
        if (PointsLeft < newSkillCost)
        {
            Debug.Log("Not enough points to add new skill.");
            return;
        }

        var s = new SkillEntry { skillName = name, value = 3 };
        workingCharacter.skills.Add(s);
        PointsLeft -= newSkillCost;
        Debug.Log($"Added skill '{name}' (value 3). Cost {newSkillCost}. Points left {PointsLeft}.");
        newSkillNameInput.text = "";
        UpdatePointsText();
        // TODO: instantiate UI row for skill under skillsListParent
    }

    private void OnFlawToggleChanged()
    {
        int selectedFlaws = (flawToggle1.isOn ? 1 : 0) + (flawToggle2.isOn ? 1 : 0);
        if (selectedFlaws > 2)
        {
            Debug.LogWarning("Max 2 flaws allowed.");
            // enforce by turning off the second toggle or clamp UI as appropriate
        }

        int granted = selectedFlaws * flawGrant;
        Debug.Log($"Flaws changed: {selectedFlaws} selected => grants {granted} points.");
    }

    private void OnCoreAbilityToggled(bool enabled)
    {
        if (enabled)
        {
            if (PointsLeft >= coreAbilityCost)
            {
                PointsLeft -= coreAbilityCost;
                Debug.Log($"Core ability purchased for {coreAbilityCost} points. Points left: {PointsLeft}");
            }
            else
            {
                Debug.Log("Not enough points for core ability.");
                coreAbilityToggle.isOn = false;
            }
        }
        else
        {
            PointsLeft += coreAbilityCost;
            Debug.Log($"Core ability removed, refunded {coreAbilityCost}. Points left: {PointsLeft}");
        }
        UpdatePointsText();
    }

    private void UpdateAttributeText()
    {
        vigorText.text = workingCharacter.vigor.ToString();
        clarityText.text = workingCharacter.clarity.ToString();
        spiritText.text = workingCharacter.spirit.ToString();
    }

    private void UpdatePointsText()
    {
        pointsLeftText.text = $"Points: {PointsLeft}";
    }

    // Sample method to test dice roller (hook to a button for testing)
    public void OnDamageRollPressed()
    {
        // Example: roll 2d6 and take highest (damage rule)
        int highest = diceRoller.RollAndTakeHighest(2, 6);
        Debug.Log($"Damage roll (2d6, highest) = {highest}");
    }

    public void OnReactionRollPressed(string virtue) // call with "Vigor" / "Clarity" / "Spirit"
    {
        int relevantValue = 0;
        switch (virtue)
        {
            case "Vigor": relevantValue = workingCharacter.vigor; break;
            case "Clarity": relevantValue = workingCharacter.clarity; break;
            case "Spirit": relevantValue = workingCharacter.spirit; break;
        }

        int sides = 20;
        var rolls = diceRoller.Roll(1, sides, out int total);
        Debug.Log($"Reaction roll vs {virtue} ({relevantValue}): rolled {total}. Success = {total <= relevantValue}");
    }

    public int GetCostForNewSkill() => 3;

    public bool CanSpendPoints(int amount)
    {
        RecalculatePointsFromCharacterData();
        return PointsLeft >= amount;
    }
    public void SpendPoints(int amount, string reason)
    {
        PointsLeft -= amount;
        transactionLog.Add($"Spend {amount}: {reason}");
        Debug.Log($"CharacterCreation: Spent {amount} for {reason}. PointsLeft now {PointsLeft}.");
    }
    public void RefundPoints(int amount, string reason)
    {
        PointsLeft += amount;
        transactionLog.Add($"Refund {amount}: {reason}");
        Debug.Log($"CharacterCreation: Refunded {amount} for {reason}. PointsLeft now {PointsLeft}.");
    }

    // roll helper used by SkillsPanelManager.RollSkill
    public void RollSkillByValue(string skillName, int skillValue)
    {
        // roll 1d20 and compare <= skillValue
        var dice = new DiceRollerRandom();
        dice.Roll(1, 20, out int total);
        bool success = total <= skillValue;
        Debug.Log($"Skill roll '{skillName}' value {skillValue}: rolled {total} → success {success}");
    }

    public void RecalculatePointsFromCharacterData()
    {
        int virtueBase = 6;
        int virtuesSpent = 0;
        virtuesSpent += Mathf.Max(0, workingCharacter.vigor - virtueBase);
        virtuesSpent += Mathf.Max(0, workingCharacter.clarity - virtueBase);
        virtuesSpent += Mathf.Max(0, workingCharacter.spirit - virtueBase);

        int skillsSpent = 0;
        foreach (var s in workingCharacter.skills)
        {
            if (s.value < 3) s.value = 3;
            skillsSpent += s.value; // base 3 included
        }

        int coreSpent = workingCharacterHasCoreAbility() ? coreAbilityCost : 0;

        int equipSpent = 0;
        foreach (var slot in workingCharacter.inventory)
        {
            if (slot != null && slot.equipment != null && slot.equipment.costsCreationPoints)
            {
                equipSpent += slot.equipment.pointCost;
            }
        }


        int otherSpent = 0;

        int totalSpent = virtuesSpent + skillsSpent + coreSpent + equipSpent + otherSpent;

        
        int flawCount = GetFlawCountFromCharacter(); // implement according to how you store flaws
        int flawGrants = flawCount * flawGrant;
        int deedGrants = workingCharacter.deedCount * deedPointsPerDeed;

        PointsLeft = startingPoints + flawGrants + deedGrants - totalSpent;
        Debug.Log($"RecalculatePoints: virtuesSpent {virtuesSpent}, skillsSpent {skillsSpent}, coreSpent {coreSpent}, equipSpent {equipSpent}, totalSpent {totalSpent}. FlawGrants {flawGrants}, DeedGrants {deedGrants}. PointsLeft = {PointsLeft}");

        UpdatePointsText();
        workingCharacter.cachedPointsLeft = PointsLeft;
    }

    private bool workingCharacterHasCoreAbility()
    {
        return workingCharacter != null && workingCharacter.hasCoreAbility;
    }

    private int GetFlawCountFromCharacter()
    {
        if (workingCharacter != null) return workingCharacter.flawCount;
        return 0;
    }
    }
