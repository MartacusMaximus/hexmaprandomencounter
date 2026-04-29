using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PartyWorkbenchContainerSlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public ReworkPartyWorkbenchController controller;
    public EquipmentInstance sourceContainer;
    public SteedInstance sourceSteed;
    public int slotIndex = -1;

    public void Configure(ReworkPartyWorkbenchController owner, EquipmentInstance container, SteedInstance steed, int index)
    {
        controller = owner;
        sourceContainer = container;
        sourceSteed = steed;
        slotIndex = index;
    }

    private EquipmentInstance CurrentItem
    {
        get
        {
            if (sourceContainer != null)
            {
                sourceContainer.EnsureInstance();
                return slotIndex >= 0 && slotIndex < sourceContainer.containedItems.Count ? sourceContainer.containedItems[slotIndex] : null;
            }

            if (sourceSteed != null)
            {
                sourceSteed.EnsureSlots();
                return slotIndex >= 0 && slotIndex < sourceSteed.storage.Count ? sourceSteed.storage[slotIndex] : null;
            }

            return null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller == null || PartyWorkbenchDragController.Instance == null)
        {
            return;
        }

        var item = CurrentItem;
        if (item == null || item.equipment == null)
        {
            return;
        }

        var rect = transform as RectTransform;
        if (sourceContainer != null)
        {
            PartyWorkbenchDragController.Instance.BeginContainerItemDrag(sourceContainer, slotIndex, item, eventData, rect.rect.size);
            return;
        }

        PartyWorkbenchDragController.Instance.BeginSteedItemDrag(sourceSteed, slotIndex, item, eventData, rect.rect.size);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PartyWorkbenchDragController.Instance?.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PartyWorkbenchDragController.Instance?.EndDrag(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (PartyWorkbenchDragController.Instance == null)
        {
            return;
        }

        if (sourceContainer != null)
        {
            PartyWorkbenchDragController.Instance.DropOnBackpackSlot(sourceContainer, slotIndex);
            return;
        }

        PartyWorkbenchDragController.Instance.DropOnSteedSlot(slotIndex);
    }
}
