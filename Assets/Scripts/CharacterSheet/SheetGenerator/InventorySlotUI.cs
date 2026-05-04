using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;
using KnightsAndGM.Shared;

public class InventorySlotUI : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI parts")]
    public TextMeshProUGUI tmpNameText;
    public TextMeshProUGUI tmpDetailText;
    public TextMeshProUGUI tmpTraitText;
    public TextMeshProUGUI tmpAbilityText;
    public TextMeshProUGUI chunkRequirementIndicator;
    public TextMeshProUGUI linkedRequirementIndicator;
    public Image topChunkImg;
    public Image bottomChunkImg;
    public Image leftChunkImg;
    public Image rightChunkImg;
    public Image centerChunkImg;
    public Image backgroundImage; 

    [HideInInspector] public int slotIndex = -1;
    [HideInInspector] public InventoryGrid parentGrid;

    // runtime
    private GameObject dragIcon;
    private RectTransform dragIconRect;
    private Canvas rootCanvas;
    private CanvasGroup dragIconCanvasGroup;
    private Vector2 pointerOffset;

    // MUST be called by InventoryGrid on setup
    public void Initialize(InventoryGrid grid, int index, Canvas canvas)
    {
        parentGrid = grid;
        slotIndex = index;
        rootCanvas = canvas;
        EnsureRuntimeUi();
    }

    // update UI from EquipmentData (null => empty)
    public void UpdateFromEquipment(EquipmentInstance inst, PortableSlotActivationModel activation = null)
    {
        EnsureRuntimeUi();
        var eq = inst != null ? inst.equipment : null;

        // ALWAYS update the name text
        if (tmpNameText != null)
            tmpNameText.text = inst != null ? inst.DisplayName : "Empty";

        if (tmpDetailText != null)
        {
            if (eq == null)
            {
                tmpDetailText.text = string.Empty;
            }
            else
            {
                var detailParts = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(inst.DamageDiceNotation))
                {
                    detailParts.Add(inst.DamageDiceNotation);
                }

                if (inst.ArmorValue > 0)
                {
                    detailParts.Add($"A{inst.ArmorValue}");
                }

                if (detailParts.Count == 0)
                {
                    detailParts.Add(eq.displayCategory);
                }

                tmpDetailText.text = string.Join("  ", detailParts);
            }
        }

        if (tmpTraitText != null)
        {
            tmpTraitText.text = eq == null
                ? string.Empty
                : string.Join(", ", inst.GetResolvedTraitNames());
        }

        if (tmpAbilityText != null)
        {
            if (eq == null)
            {
                tmpAbilityText.text = string.Empty;
            }
            else if (eq.ability != null)
            {
                var rulesText = inst.RulesText;
                var abilityText = eq.ability.description ?? string.Empty;
                tmpAbilityText.text = string.IsNullOrWhiteSpace(rulesText)
                    ? abilityText
                    : (string.IsNullOrWhiteSpace(abilityText) ? rulesText : $"{rulesText}\n{abilityText}");
            }
            else
            {
                tmpAbilityText.text = inst.RulesText;
            }
        }

        // ALWAYS update chunk images (do not early-return)
        SetChunkImage(leftChunkImg, eq != null ? eq.leftHalf : ChunkColor.None, activation != null && activation.LeftLinked);
        SetChunkImage(rightChunkImg, eq != null ? eq.rightHalf : ChunkColor.None, activation != null && activation.RightLinked);
        SetChunkImage(topChunkImg, eq != null ? eq.topHalf : ChunkColor.None, activation != null && activation.TopLinked);
        SetChunkImage(bottomChunkImg, eq != null ? eq.bottomHalf : ChunkColor.None, activation != null && activation.BottomLinked);
        SetChunkImage(centerChunkImg, eq != null ? eq.centerChunk : ChunkColor.None, activation != null && activation.CenterActive);

        UpdateRequirementIndicator(chunkRequirementIndicator, eq != null && eq.ability != null && eq.ability.requiredChunkCount > 0, activation != null && activation.ChunkRequirementMet, "[ ]");
        UpdateRequirementIndicator(linkedRequirementIndicator, eq != null && eq.ability != null && eq.ability.requiresLinkedChunk, activation != null && activation.LinkedRequirementMet, "<>");

        // Optionally change visual alpha if empty
        if (backgroundImage != null)
            backgroundImage.enabled = (eq != null);
    }

    private void SetChunkImage(Image img, ChunkColor color, bool isActive)
    {
        if (img == null) return;
        if (color == ChunkColor.None)
        {
            img.gameObject.SetActive(false);
            return;
        }
        img.gameObject.SetActive(true);
        img.color = ColorForChunk(color);
        var outline = img.GetComponent<Outline>();
        if (outline == null)
        {
            outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(4f, 4f);
        }

        outline.enabled = isActive;
    }

    private Color ColorForChunk(ChunkColor c)
    {
        switch (c)
        {
            case ChunkColor.Red: return new Color(0.85f, 0.2f, 0.2f, 1f);
            case ChunkColor.Green: return new Color(0.2f, 0.75f, 0.3f, 1f);
            case ChunkColor.Blue: return new Color(0.25f, 0.45f, 0.95f, 1f);
            case ChunkColor.Rainbow: return new Color(1f, 0.8f, 0.15f, 1f);
            default: return Color.clear;
        }
    }

    private void UpdateRequirementIndicator(TextMeshProUGUI indicator, bool visible, bool active, string markerText)
    {
        if (indicator == null)
        {
            return;
        }

        indicator.gameObject.SetActive(visible);
        indicator.text = markerText;
        indicator.color = active
            ? new Color(0.08f, 0.55f, 0.15f, 1f)
            : new Color(0.45f, 0.45f, 0.45f, 1f);
    }

    private void EnsureRuntimeUi()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (tmpNameText != null)
        {
            var nameRect = tmpNameText.rectTransform;
            nameRect.anchorMin = new Vector2(0.05f, 0.72f);
            nameRect.anchorMax = new Vector2(0.95f, 0.98f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            tmpNameText.enableWordWrapping = true;
            tmpNameText.alignment = TextAlignmentOptions.TopLeft;
        }

        tmpDetailText ??= CreateTextElement("DetailText", new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.72f), 12f, FontStyles.Bold);
        tmpTraitText ??= CreateTextElement("TraitText", new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.60f), 10f, FontStyles.Normal);
        tmpAbilityText ??= CreateTextElement("AbilityText", new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.48f), 9f, FontStyles.Normal);
        chunkRequirementIndicator ??= CreateTextElement("ChunkRequirement", new Vector2(0.05f, 0.02f), new Vector2(0.16f, 0.10f), 11f, FontStyles.Bold);
        linkedRequirementIndicator ??= CreateTextElement("LinkedRequirement", new Vector2(0.17f, 0.02f), new Vector2(0.28f, 0.10f), 11f, FontStyles.Bold);
    }

    private TextMeshProUGUI CreateTextElement(string objectName, Vector2 anchorMin, Vector2 anchorMax, float fontSize, FontStyles style)
    {
        var existing = transform.Find(objectName);
        if (existing != null)
        {
            return existing.GetComponent<TextMeshProUGUI>();
        }

        var child = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        child.transform.SetParent(transform, false);
        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = child.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.enableWordWrapping = true;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        text.raycastTarget = false;
        return text;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform, eventData.position, eventData.pressEventCamera, out pointerOffset);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentGrid == null || parentGrid.character == null) return;
        var inst = parentGrid.character.inventory[slotIndex];
        if (inst == null || inst.equipment == null) return; // nothing to drag

        if (PartyWorkbenchDragController.Instance != null &&
            PartyWorkbenchDragController.Instance.BeginCharacterInventoryDrag(parentGrid, slotIndex, inst, eventData, (transform as RectTransform).rect.size))
        {
            return;
        }

        // create simple drag icon (Image + TMP name)
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(rootCanvas.transform, false);
        dragIconRect = dragIcon.AddComponent<RectTransform>();
        dragIconRect.sizeDelta = (transform as RectTransform).sizeDelta;

        // canvas group so it doesn't block raycasts
        dragIconCanvasGroup = dragIcon.AddComponent<CanvasGroup>();
        dragIconCanvasGroup.blocksRaycasts = false;

        var img = dragIcon.AddComponent<Image>();
        if (backgroundImage != null)
        {
            img.sprite = backgroundImage.sprite;
            img.type = backgroundImage.type;
            img.preserveAspect = backgroundImage.preserveAspect;
            img.color = new Color(1f,1f,1f,0.9f);
        }
        else
        {
            img.color = new Color(1f,1f,1f,0.9f);
        }

        // name text
        var textGO = new GameObject("Name");
        textGO.transform.SetParent(dragIcon.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = inst.DisplayName;

        // immediately position
        SetDragIconPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (PartyWorkbenchDragController.Instance != null && PartyWorkbenchDragController.Instance.HasActiveDrag)
        {
            PartyWorkbenchDragController.Instance.UpdateDrag(eventData);
            return;
        }

        if (dragIcon == null) return;
        SetDragIconPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (PartyWorkbenchDragController.Instance != null && PartyWorkbenchDragController.Instance.HasActiveDrag)
        {
            PartyWorkbenchDragController.Instance.EndDrag(eventData);
            return;
        }

        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        // perform a raycast to find target slot
        int targetSlot = parentGrid.GetSlotIndexAtPointer(eventData);
        if (targetSlot >= 0 && targetSlot != slotIndex)
        {
            parentGrid.SwapSlots(slotIndex, targetSlot);
        }
        else
        {
        }
    }

    private void SetDragIconPosition(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out pos);
        dragIconRect.anchoredPosition = pos - pointerOffset;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (PartyWorkbenchDragController.Instance == null)
        {
            return;
        }

        PartyWorkbenchDragController.Instance.DropOnCharacterSlot(parentGrid, slotIndex);
    }
}
