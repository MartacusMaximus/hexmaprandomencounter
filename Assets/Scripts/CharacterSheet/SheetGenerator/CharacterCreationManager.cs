using System.Collections.Generic;
using KnightsAndGM.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreationManager : MonoBehaviour
{
    [Header("References")]
    public CharacterData workingCharacter;
    public TMP_Text pointsLeftText;
    public TMP_Text vigorText;
    public TMP_Text clarityText;
    public TMP_Text spiritText;
    public Button vigorPlus;
    public Button vigorMinus;
    public Button clarityPlus;
    public Button clarityMinus;
    public Button spiritPlus;
    public Button spiritMinus;
    public TMP_InputField newSkillNameInput;
    public Button addSkillButton;
    public Transform skillsListParent;
    public Toggle flawToggle1;
    public Toggle flawToggle2;
    public Toggle coreAbilityToggle;

    private const int baseVirtueStart = 6;
    private const int virtueMax = 18;
    private const int newSkillCost = 3;
    private const int maxSkills = 10;

    [Header("Point Accounting")]
    public int startingPoints = 50;
    public int deedPointsPerDeed = 3;
    public int flawGrant = 5;
    public bool coreAbilityPurchased = false;
    public int coreAbilityCost = 15;

    private readonly List<string> transactionLog = new List<string>();
    private readonly IDiceRoller diceRoller = new DiceRollerRandom();

    private CharacterCreationConfig rulesConfig;
    private bool suppressToggleCallbacks;

    public int PointsLeft { get; private set; }

    private void Awake()
    {
        rulesConfig = new CharacterCreationConfig
        {
            BaseVirtueStart = baseVirtueStart,
            VirtueMax = virtueMax,
            StartingPoints = startingPoints,
            DeedPointsPerDeed = deedPointsPerDeed,
            FlawGrant = flawGrant,
            CoreAbilityCost = coreAbilityCost,
            NewSkillCost = newSkillCost,
            MaxSkills = maxSkills
        };

        if (workingCharacter.vigor < baseVirtueStart) workingCharacter.vigor = baseVirtueStart;
        if (workingCharacter.clarity < baseVirtueStart) workingCharacter.clarity = baseVirtueStart;
        if (workingCharacter.spirit < baseVirtueStart) workingCharacter.spirit = baseVirtueStart;

        vigorPlus.onClick.AddListener(() => ChangeVirtue("Vigor", +1));
        vigorMinus.onClick.AddListener(() => ChangeVirtue("Vigor", -1));
        clarityPlus.onClick.AddListener(() => ChangeVirtue("Clarity", +1));
        clarityMinus.onClick.AddListener(() => ChangeVirtue("Clarity", -1));
        spiritPlus.onClick.AddListener(() => ChangeVirtue("Spirit", +1));
        spiritMinus.onClick.AddListener(() => ChangeVirtue("Spirit", -1));
        addSkillButton.onClick.AddListener(OnAddSkillClicked);
        flawToggle1.onValueChanged.AddListener(_ => OnFlawToggleChanged());
        flawToggle2.onValueChanged.AddListener(_ => OnFlawToggleChanged());
        coreAbilityToggle.onValueChanged.AddListener(OnCoreAbilityToggled);
    }

    private void Start()
    {
        workingCharacter.EnsureInventorySlots();
        SyncUiToCharacter();
        RecalculatePointsFromCharacterData();
        UpdateAttributeText();
    }

    private void ChangeVirtue(string virtueName, int delta)
    {
        var currentValue = GetVirtueValue(virtueName);
        if (!CharacterCreationRules.CanAdjustVirtue(currentValue, delta, rulesConfig))
        {
            Debug.Log($"CharacterCreation: {virtueName} cannot move outside {baseVirtueStart}-{virtueMax}.");
            return;
        }

        SetVirtueValue(virtueName, currentValue + delta);
        RecalculatePointsFromCharacterData();

        if (PointsLeft < 0)
        {
            SetVirtueValue(virtueName, currentValue);
            RecalculatePointsFromCharacterData();
            Debug.Log("Not enough points to increase virtue.");
            return;
        }

        Debug.Log($"Virtue change: {virtueName} from {currentValue} to {GetVirtueValue(virtueName)}. Points left: {PointsLeft}");
        UpdateAttributeText();
        UpdatePointsText();
    }

    private void OnAddSkillClicked()
    {
        var name = newSkillNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.Log("Skill name empty.");
            return;
        }

        if (!CharacterCreationRules.CanAddSkill(CharacterRulesAdapter.ToModel(workingCharacter), rulesConfig))
        {
            Debug.Log("Not enough points to add new skill.");
            return;
        }

        workingCharacter.skills.Add(new SkillEntry { skillName = name, value = 3 });
        RecalculatePointsFromCharacterData();
        Debug.Log($"Added skill '{name}' (value 3). Points left {PointsLeft}.");
        newSkillNameInput.text = string.Empty;
        UpdatePointsText();
    }

    private void OnFlawToggleChanged()
    {
        if (suppressToggleCallbacks)
        {
            return;
        }

        workingCharacter.flawCount = (flawToggle1.isOn ? 1 : 0) + (flawToggle2.isOn ? 1 : 0);
        RecalculatePointsFromCharacterData();
        Debug.Log($"Flaws changed: {workingCharacter.flawCount} selected.");
    }

    private void OnCoreAbilityToggled(bool enabled)
    {
        if (suppressToggleCallbacks)
        {
            return;
        }

        workingCharacter.hasCoreAbility = enabled;
        RecalculatePointsFromCharacterData();

        if (PointsLeft >= 0)
        {
            Debug.Log(enabled
                ? $"Core ability purchased for {coreAbilityCost} points. Points left: {PointsLeft}"
                : $"Core ability removed. Points left: {PointsLeft}");
            return;
        }

        suppressToggleCallbacks = true;
        workingCharacter.hasCoreAbility = false;
        coreAbilityToggle.isOn = false;
        suppressToggleCallbacks = false;
        RecalculatePointsFromCharacterData();
        Debug.Log("Not enough points for core ability.");
    }

    private void UpdateAttributeText()
    {
        vigorText.text = workingCharacter.vigor.ToString();
        clarityText.text = workingCharacter.clarity.ToString();
        spiritText.text = workingCharacter.spirit.ToString();
    }

    private void UpdatePointsText()
    {
        pointsLeftText.text = PointsLeft.ToString();
    }

    public void OnDamageRollPressed()
    {
        var highest = diceRoller.RollAndTakeHighest(2, 6);
        Debug.Log($"Damage roll (2d6, highest) = {highest}");
    }

    public void OnReactionRollPressed(string virtue)
    {
        var relevantValue = GetVirtueValue(virtue);
        diceRoller.Roll(1, 20, out int total);
        Debug.Log($"Reaction roll vs {virtue} ({relevantValue}): rolled {total}. Success = {total <= relevantValue}");
    }

    public int GetCostForNewSkill()
    {
        return newSkillCost;
    }

    public bool CanSpendPoints(int amount)
    {
        RecalculatePointsFromCharacterData();
        return PointsLeft >= amount;
    }

    public void SpendPoints(int amount, string reason)
    {
        transactionLog.Add($"Spend {amount}: {reason}");
        RecalculatePointsFromCharacterData();
        Debug.Log($"CharacterCreation: Registered spend {amount} for {reason}. PointsLeft now {PointsLeft}.");
    }

    public void RefundPoints(int amount, string reason)
    {
        transactionLog.Add($"Refund {amount}: {reason}");
        RecalculatePointsFromCharacterData();
        Debug.Log($"CharacterCreation: Registered refund {amount} for {reason}. PointsLeft now {PointsLeft}.");
    }

    public void RollSkillByValue(string skillName, int skillValue)
    {
        var roller = new DiceRollerRandom();
        roller.Roll(1, 20, out int total);
        var success = total <= skillValue;
        Debug.Log($"Skill roll '{skillName}' value {skillValue}: rolled {total} -> success {success}");
    }

    public void RecalculatePointsFromCharacterData()
    {
        PointsLeft = CharacterCreationRules.CalculatePointsLeft(CharacterRulesAdapter.ToModel(workingCharacter), rulesConfig);
        workingCharacter.cachedPointsLeft = PointsLeft;
        UpdatePointsText();
        UpdateAttributeText();
        SyncUiToCharacter();
        Debug.Log($"RecalculatePoints: PointsLeft = {PointsLeft}");
    }

    private int GetVirtueValue(string virtueName)
    {
        switch (virtueName)
        {
            case "Vigor":
                return workingCharacter.vigor;
            case "Clarity":
                return workingCharacter.clarity;
            case "Spirit":
                return workingCharacter.spirit;
            default:
                return 0;
        }
    }

    private void SetVirtueValue(string virtueName, int value)
    {
        switch (virtueName)
        {
            case "Vigor":
                workingCharacter.vigor = value;
                break;
            case "Clarity":
                workingCharacter.clarity = value;
                break;
            case "Spirit":
                workingCharacter.spirit = value;
                break;
        }
    }

    private void SyncUiToCharacter()
    {
        suppressToggleCallbacks = true;
        flawToggle1.isOn = workingCharacter.flawCount > 0;
        flawToggle2.isOn = workingCharacter.flawCount > 1;
        coreAbilityToggle.isOn = workingCharacter.hasCoreAbility;
        suppressToggleCallbacks = false;
    }
}
