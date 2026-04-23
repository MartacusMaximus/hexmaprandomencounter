using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class SkillRowController : MonoBehaviour
{
    public TMP_Text skillNameText;
    public TMP_Text skillValueText;
    public Button increaseButton;
    public Button decreaseButton;
    public Button rollButton;

    // index of this slot in the skills panel (0..9)
    public int slotIndex = -1;
    public SkillsPanelManager panelManager;

    private void Awake()
    {
        increaseButton.onClick.AddListener(OnIncreasePressed);
        decreaseButton.onClick.AddListener(OnDecreasePressed);
        rollButton.onClick.AddListener(OnRollPressed);
    }

    public void Initialize(int index, SkillsPanelManager manager)
    {
        slotIndex = index;
        panelManager = manager;
    }

    // fill UI from SkillEntry (can be null)
    public void Fill(SkillEntry skill)
    {
        if (skill == null)
        {
            skillNameText.text = "Empty";
            skillValueText.text = "";
            increaseButton.interactable = false;
            decreaseButton.interactable = false;
            rollButton.interactable = false;
        }
        else
        {
            skillNameText.text = skill.skillName;
            skillValueText.text = skill.value.ToString();
            increaseButton.interactable = true;
            decreaseButton.interactable = true;
            rollButton.interactable = true;
        }
    }

    private void OnIncreasePressed()
    {
        if (panelManager == null) return;
        panelManager.ChangeSkillValue(slotIndex, +1);
    }

    private void OnDecreasePressed()
    {
        if (panelManager == null) return;
        panelManager.ChangeSkillValue(slotIndex, -1);
    }

    private void OnRollPressed()
    {
        if (panelManager == null) return;
        panelManager.RollSkill(slotIndex);
    }
}
