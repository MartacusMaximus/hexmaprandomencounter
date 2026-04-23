using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropZone : MonoBehaviour, IDropHandler
{
    public IInventoryContainer target;

    public void OnDrop(PointerEventData eventData)
    {
        DragManager.Instance.TryDrop(target);
    }
}

