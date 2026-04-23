using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlotUI : MonoBehaviour,
    IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI parts")]
    public TextMeshProUGUI tmpNameText;
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
    }

    // update UI from EquipmentData (null => empty)
    public void UpdateFromEquipment(EquipmentData eq)
    {
        // ALWAYS update the name text
        if (tmpNameText != null)
            tmpNameText.text = eq != null ? eq.itemName : "Empty";

        // ALWAYS update chunk images (do not early-return)
        SetChunkImage(leftChunkImg, eq != null ? eq.leftHalf : ChunkColor.None);
        SetChunkImage(rightChunkImg, eq != null ? eq.rightHalf : ChunkColor.None);
        SetChunkImage(topChunkImg, eq != null ? eq.topHalf : ChunkColor.None);
        SetChunkImage(bottomChunkImg, eq != null ? eq.bottomHalf : ChunkColor.None);
        SetChunkImage(centerChunkImg, eq != null ? eq.centerChunk : ChunkColor.None);

        // Optionally change visual alpha if empty
        if (backgroundImage != null)
            backgroundImage.enabled = (eq != null);
    }

    private void SetChunkImage(Image img, ChunkColor color)
    {
        if (img == null) return;
        if (color == ChunkColor.None)
        {
            img.gameObject.SetActive(false);
            return;
        }
        img.gameObject.SetActive(true);
        img.color = ColorForChunk(color);
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
        tmp.text = inst.equipment.itemName;

        // immediately position
        SetDragIconPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null) return;
        SetDragIconPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
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
}
