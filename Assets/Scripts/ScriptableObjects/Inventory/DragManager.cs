using UnityEngine;
using UnityEngine.EventSystems;

public class DragManager : MonoBehaviour
{
    public static DragManager Instance;

    private EquipmentInstance draggedItem;
    private IInventoryContainer sourceContainer;

    private void Awake()
    {
        Instance = this;
    }

    public void BeginDrag(EquipmentInstance item, IInventoryContainer source)
    {
        draggedItem = item;
        sourceContainer = source;
        Debug.Log($"Begin drag {item.DisplayName}");
    }

    public void EndDrag()
    {
        draggedItem = null;
        sourceContainer = null;
    }

    public void TryDrop(IInventoryContainer target)
    {
        if (draggedItem == null) return;
        if (!target.CanAccept(draggedItem)) return;

        sourceContainer.TryRemove(draggedItem);
        target.TryAdd(draggedItem);

        Debug.Log($"Moved {draggedItem.DisplayName}");
        EndDrag();
        UIRefreshBus.RequestRefresh();
    }
}


