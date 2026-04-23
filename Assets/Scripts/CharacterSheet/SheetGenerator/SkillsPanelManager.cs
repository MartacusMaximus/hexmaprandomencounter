using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SkillsPanelManager : MonoBehaviour
{
    [Header("Prefabs & References")]
    public GameObject skillRowPrefab;         // assign UISkill_PREF
    public List<Transform> skillSlotParents;  // 10 empty GameObjects in layout; must be size 10
    public Button addSkillButton;
    public TMP_InputField newSkillNameInput;

    [Header("Data")]
    public CharacterData characterData;
    public CharacterCreationManager creationManager; // to notify point recalculation

    private List<SkillRowController> slotControllers = new List<SkillRowController>();

    private const int maxSkills = 10;
    
    private void Start()
    {
        // instantiate prefabs inside each slot parent and keep references
        slotControllers.Clear();
        for (int i = 0; i < maxSkills; i++)
        {
            var parent = skillSlotParents[i];
            GameObject inst = Instantiate(skillRowPrefab, parent, false);
            // Ensure prefab RectTransform stretches to parent full size (avoids hitbox mismatch)
            RectTransform r = inst.GetComponent<RectTransform>();
            if (r != null)
            {
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
            }

            var ctrl = inst.GetComponent<SkillRowController>();
            ctrl.Initialize(i, this);
            slotControllers.Add(ctrl);
        }

        addSkillButton.onClick.AddListener(OnAddSkillClicked);
        RefreshAllSlots();
    }

    public void RefreshAllSlots()
    {
        for (int i = 0; i < slotControllers.Count; i++)
        {
            SkillEntry s = (i < characterData.skills.Count) ? characterData.skills[i] : null;
            slotControllers[i].Fill(s);

            // Rebuild layout for the slot parent to ensure button hitboxes match visuals
            var parentRect = skillSlotParents[i] as RectTransform;
            if (parentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }
    }

    public void OnAddSkillClicked()
    {
        string name = newSkillNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) { Debug.Log("Skills: empty name."); return; }
        if (characterData.skills.Count >= maxSkills) { Debug.Log("Skills: full."); return; }

        int costForNew = creationManager.GetCostForNewSkill();
        if (!creationManager.CanSpendPoints(costForNew)) { Debug.Log("Skills: insufficient points."); return; }

        var newSkill = new SkillEntry { skillName = name, value = 3 };
        characterData.skills.Add(newSkill);

        creationManager.SpendPoints(costForNew, $"AddSkill:{name}");

        // Immediately refresh UI AND force a layout rebuild so the click regions are updated
        RefreshAllSlots();
        Canvas.ForceUpdateCanvases();
        foreach (var parent in skillSlotParents)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);

        newSkillNameInput.text = "";
        Debug.Log($"Skills: added '{name}'.");
    }
    // change skill value at a slot index (slot index maps to list index)
    public void ChangeSkillValue(int slotIndex, int delta)
    {
        if (slotIndex < 0 || slotIndex >= slotControllers.Count) return;

        if (slotIndex >= characterData.skills.Count)
        {
            Debug.Log($"SkillsPanelManager: no skill at slot {slotIndex} to change.");
            return;
        }

        var skill = characterData.skills[slotIndex];
        int old = skill.value;
        int newVal = Mathf.Clamp(old + delta, 3, 18);

        if (delta > 0)
        {
            // cost per +1 is 1 point
            if (!creationManager.CanSpendPoints(1))
            {
                Debug.Log("SkillsPanelManager: not enough points to increase skill.");
                return;
            }
            skill.value = newVal;
            Debug.Log($"SkillsPanelManager: Increased skill '{skill.skillName}' from {old} to {skill.value}.");
        }
        else if (delta < 0)
        {
            if (old == 3)
            {
                // Decrease at base removes skill entirely
                characterData.skills.RemoveAt(slotIndex);
                Debug.Log($"SkillsPanelManager: Removed skill '{skill.skillName}' from slot {slotIndex} when decrease at 3.");
                // Refund the base cost that was originally spent for this skill (base 3). Refund model: refund base + increments?
                // Simpler: refund the base cost 3 only (increment refunds should be handled at the time they were made).
                creationManager.RefundPoints(creationManager.GetCostForNewSkill(), $"RemoveSkill:{skill.skillName}");
            }
            else
            {
                // refund 1 point for -1
                skill.value = newVal;
                Debug.Log($"SkillsPanelManager: Decreased skill '{skill.skillName}' from {old} to {skill.value}. Refunded 1.");
            }
        }

        creationManager.RecalculatePointsFromCharacterData();
        RefreshAllSlots();
    }

    public void RollSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= characterData.skills.Count) return;
        var skill = characterData.skills[slotIndex];
        Debug.Log($"SkillsPanelManager: Rolling skill '{skill.skillName}' (value {skill.value})");
        // Use CharacterCreationManager's dice roller helper
        creationManager.RollSkillByValue(skill.skillName, skill.value);
    }
}
