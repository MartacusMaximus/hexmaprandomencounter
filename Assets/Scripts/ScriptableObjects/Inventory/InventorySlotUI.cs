using UnityEngine;
using UnityEngine.EventSystems;


public class InventorySlotDropZone : MonoBehaviour, IDropHandler
{
    public IInventoryContainer targetContainer;

    public void OnDrop(PointerEventData eventData)
    {
        DragManager.Instance.TryDrop(targetContainer);
    }
}

