using UnityEngine;
using UnityEngine.EventSystems;


public class InventoryItemUI : MonoBehaviour,
    IBeginDragHandler, IEndDragHandler
{
    public EquipmentInstance item;
    public IInventoryContainer sourceContainer;

    public void OnBeginDrag(PointerEventData eventData)
    {
        DragManager.Instance.BeginDrag(item, sourceContainer);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DragManager.Instance.EndDrag();
    }
}


