using UnityEngine;
using UnityEngine.EventSystems;

public class PartyWorkbenchStorageDropZone : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        PartyWorkbenchDragController.Instance?.DropOnCampaignStorage();
    }
}

public class PartyWorkbenchCharacterListDropZone : MonoBehaviour, IDropHandler
{
    public bool acceptsPartyMembers;

    public void OnDrop(PointerEventData eventData)
    {
        PartyWorkbenchDragController.Instance?.DropOnPartyList(acceptsPartyMembers);
    }
}
