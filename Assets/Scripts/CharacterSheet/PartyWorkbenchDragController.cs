using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PartyWorkbenchDragController : MonoBehaviour
{
    public static PartyWorkbenchDragController Instance { get; private set; }

    public Canvas rootCanvas;
    public ReworkPartyWorkbenchController owner;

    private DragPayload activePayload;
    private GameObject dragVisual;
    private RectTransform dragVisualRect;
    private bool dropHandled;

    public bool HasActiveDrag => activePayload != null;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Configure(ReworkPartyWorkbenchController controller, Canvas canvas)
    {
        owner = controller;
        rootCanvas = canvas;
        Instance = this;
    }

    public bool BeginCharacterInventoryDrag(InventoryGrid grid, int slotIndex, EquipmentInstance item, PointerEventData eventData, Vector2 size)
    {
        if (item == null || item.equipment == null || owner == null)
        {
            return false;
        }

        activePayload = DragPayload.FromCharacterSlot(grid, slotIndex, item);
        CreateVisual(item.equipment.itemName, size, eventData);
        return true;
    }

    public bool BeginCatalogItemDrag(EquipmentData equipment, PointerEventData eventData, Vector2 size)
    {
        if (equipment == null || owner == null)
        {
            return false;
        }

        activePayload = DragPayload.FromCatalog(equipment);
        CreateVisual(equipment.itemName, size, eventData);
        return true;
    }

    public bool BeginStorageItemDrag(EquipmentInstance item, PointerEventData eventData, Vector2 size)
    {
        if (item == null || item.equipment == null || owner == null)
        {
            return false;
        }

        activePayload = DragPayload.FromStorage(item);
        CreateVisual(item.equipment.itemName, size, eventData);
        return true;
    }

    public bool BeginContainerItemDrag(EquipmentInstance container, int slotIndex, EquipmentInstance item, PointerEventData eventData, Vector2 size)
    {
        if (container == null || item == null || item.equipment == null || owner == null)
        {
            return false;
        }

        activePayload = DragPayload.FromContainer(container, slotIndex, item);
        CreateVisual(item.equipment.itemName, size, eventData);
        return true;
    }

    public bool BeginSteedItemDrag(SteedInstance steed, int slotIndex, EquipmentInstance item, PointerEventData eventData, Vector2 size)
    {
        if (steed == null || item == null || item.equipment == null || owner == null)
        {
            return false;
        }

        activePayload = DragPayload.FromSteed(steed, slotIndex, item);
        CreateVisual(item.equipment.itemName, size, eventData);
        return true;
    }

    public bool BeginGuildCharacterDrag(CharacterData character, bool fromParty, PointerEventData eventData, Vector2 size)
    {
        if (character == null || owner == null)
        {
            return false;
        }

        activePayload = DragPayload.FromCharacterList(character, fromParty);
        CreateVisual(character.characterName, size, eventData);
        return true;
    }

    public void UpdateDrag(PointerEventData eventData)
    {
        if (dragVisualRect == null || rootCanvas == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint);
        dragVisualRect.anchoredPosition = localPoint;
    }

    public void DropOnCharacterSlot(InventoryGrid grid, int slotIndex)
    {
        if (activePayload == null || owner == null)
        {
            return;
        }

        if (owner.TryDropItemOnCharacterSlot(activePayload, grid, slotIndex))
        {
            dropHandled = true;
            ClearDrag();
        }
    }

    public void DropOnCampaignStorage()
    {
        if (activePayload == null || owner == null)
        {
            return;
        }

        if (owner.TryDropItemOnCampaignStorage(activePayload))
        {
            dropHandled = true;
            ClearDrag();
        }
    }

    public void DropOnBackpackSlot(EquipmentInstance container, int slotIndex)
    {
        if (activePayload == null || owner == null)
        {
            return;
        }

        if (owner.TryDropItemOnBackpackSlot(activePayload, container, slotIndex))
        {
            dropHandled = true;
            ClearDrag();
        }
    }

    public void DropOnSteedSlot(int slotIndex)
    {
        if (activePayload == null || owner == null)
        {
            return;
        }

        if (owner.TryDropItemOnSteedSlot(activePayload, slotIndex))
        {
            dropHandled = true;
            ClearDrag();
        }
    }

    public void DropOnPartyList(bool toParty)
    {
        if (activePayload == null || owner == null)
        {
            return;
        }

        if (owner.TryMoveCharacter(activePayload, toParty))
        {
            dropHandled = true;
            ClearDrag();
        }
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (activePayload == null)
        {
            return;
        }

        if (!dropHandled)
        {
            ClearDrag();
        }
    }

    private void CreateVisual(string label, Vector2 size, PointerEventData eventData)
    {
        if (rootCanvas == null)
        {
            return;
        }

        ClearVisualOnly();
        dropHandled = false;

        dragVisual = new GameObject("PartyWorkbenchDragVisual");
        dragVisual.transform.SetParent(rootCanvas.transform, false);
        dragVisualRect = dragVisual.AddComponent<RectTransform>();
        dragVisualRect.sizeDelta = new Vector2(Mathf.Max(160f, size.x), Mathf.Max(44f, size.y));

        var image = dragVisual.AddComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.14f, 0.9f);

        var canvasGroup = dragVisual.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        var textObject = new GameObject("Label");
        textObject.transform.SetParent(dragVisual.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        UpdateDrag(eventData);
    }

    private void ClearDrag()
    {
        activePayload = null;
        dropHandled = false;
        ClearVisualOnly();
    }

    private void ClearVisualOnly()
    {
        if (dragVisual != null)
        {
            Destroy(dragVisual);
        }

        dragVisual = null;
        dragVisualRect = null;
    }

    public sealed class DragPayload
    {
        public EquipmentData catalogEquipment;
        public EquipmentInstance itemInstance;
        public InventoryGrid sourceGrid;
        public int sourceSlotIndex = -1;
        public EquipmentInstance sourceContainer;
        public SteedInstance sourceSteed;
        public int sourceContainerSlotIndex = -1;
        public CharacterData character;
        public bool fromParty;

        public bool IsCatalogItem => catalogEquipment != null && itemInstance == null && character == null;
        public bool IsStoredItem => itemInstance != null && sourceGrid == null && sourceContainer == null && sourceSteed == null && character == null;
        public bool IsCharacterSlotItem => itemInstance != null && sourceGrid != null;
        public bool IsBackpackSlotItem => itemInstance != null && sourceContainer != null;
        public bool IsSteedSlotItem => itemInstance != null && sourceSteed != null;
        public bool IsCharacterEntry => character != null;

        public static DragPayload FromCatalog(EquipmentData equipment)
        {
            return new DragPayload { catalogEquipment = equipment };
        }

        public static DragPayload FromStorage(EquipmentInstance item)
        {
            return new DragPayload { itemInstance = item };
        }

        public static DragPayload FromCharacterSlot(InventoryGrid grid, int slotIndex, EquipmentInstance item)
        {
            return new DragPayload
            {
                sourceGrid = grid,
                sourceSlotIndex = slotIndex,
                itemInstance = item
            };
        }

        public static DragPayload FromContainer(EquipmentInstance container, int slotIndex, EquipmentInstance item)
        {
            return new DragPayload
            {
                sourceContainer = container,
                sourceContainerSlotIndex = slotIndex,
                itemInstance = item
            };
        }

        public static DragPayload FromSteed(SteedInstance steed, int slotIndex, EquipmentInstance item)
        {
            return new DragPayload
            {
                sourceSteed = steed,
                sourceContainerSlotIndex = slotIndex,
                itemInstance = item
            };
        }

        public static DragPayload FromCharacterList(CharacterData sourceCharacter, bool isFromParty)
        {
            return new DragPayload
            {
                character = sourceCharacter,
                fromParty = isFromParty
            };
        }
    }
}
