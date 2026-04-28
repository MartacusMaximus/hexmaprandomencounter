using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;


public class CharacterSheetUI : MonoBehaviour
{
    public GameObject sheetPanel; // the UI panel to show/hide
    public Button openButton;
    public Button closeButton;

    public CharacterData character;
    private IDiceRoller diceRoller = new DiceRollerRandom();

    // UI controls for rolling
    public Button damageRollButton;
    public Button reactionRollButton;
    public TMP_Dropdown reactionVirtueDropdown;
    public Transform skillsListParent; // if you want clickable skill rows

    public bool IsOpen => sheetPanel != null && sheetPanel.activeSelf;

    private void Start()
    {
        openButton.onClick.AddListener(OpenSheet);
        closeButton.onClick.AddListener(CloseSheet);

        damageRollButton.onClick.AddListener(OnDamageRoll);
        reactionRollButton.onClick.AddListener(OnReactionRoll);
    }

    public void OpenSheet()
    {
        sheetPanel.SetActive(true);
        Debug.Log("Character sheet opened.");
    }

    public void CloseSheet()
    {
        sheetPanel.SetActive(false);
        Debug.Log("Character sheet closed.");
    }

    private void OnDamageRoll()
    {
        // Example: collect all weapon dice from inventory and roll them, then take highest die per rules:
        var weaponDice = character.inventory
            .Where(e => e != null && e.equipment != null && !string.IsNullOrEmpty(e.equipment.damageDiceNotation))
            .Select(e => e.equipment.damageDiceNotation)
            .ToList();

        if (weaponDice.Count == 0)
        {
            // fallback to 2d6 default
            int highest = diceRoller.RollAndTakeHighest(2, 6);
            Debug.Log($"Damage roll fallback (2d6 highest) = {highest}");
            return;
        }

        // For simplicity, if multiple weapons present, parse each notation and roll them individually
        int best = int.MinValue;
        foreach (var notation in weaponDice)
        {
            diceRoller.RollNotation(notation, out int total);
            // choose highest die if notation has multiple dice; here we parse the dice and then choose the maximum individual die
            // For now treat total as candidate damage — adapt to your exact rule set (you specified "Add all the damage dice together" in inventory note)
            if (total > best) best = total;
        }
        Debug.Log($"Damage roll using inventory weapons => result {best}");
    }

    private void OnReactionRoll()
    {
        string virtue = reactionVirtueDropdown.options[reactionVirtueDropdown.value].text;
        int value = 0;
        if (virtue == "Vigor") value = character.vigor;
        if (virtue == "Clarity") value = character.clarity;
        if (virtue == "Spirit") value = character.spirit;

        var rolls = diceRoller.Roll(1, 20, out int total);
        bool success = total <= value;
        Debug.Log($"Reaction roll using {virtue} (target ≤ {value}): rolled {total} → success {success}");
    }

    // Example: clicking a skill row calls this to roll that skill
    public void OnSkillClicked(string skillName)
    {
        var skill = character.skills.Find(s => s.skillName == skillName);
        if (skill == null) { Debug.LogError($"Skill {skillName} not found."); return; }

        // Roll d20 and check if roll <= skill.value (per your rule "Roll below the value of a skill to succeed")
        var rolls = diceRoller.Roll(1, 20, out int total);
        bool success = total <= skill.value;
        Debug.Log($"Skill '{skill.skillName}' roll: rolled {total} vs skill {skill.value} → success {success}");
    }
}
