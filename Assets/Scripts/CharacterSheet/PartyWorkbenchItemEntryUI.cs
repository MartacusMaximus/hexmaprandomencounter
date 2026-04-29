using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PartyWorkbenchItemEntryUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ReworkPartyWorkbenchController controller;
    public EquipmentData equipment;
    public EquipmentInstance itemInstance;
    public bool comesFromCatalog;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI detailText;

    public void Configure(
        ReworkPartyWorkbenchController owner,
        EquipmentData sourceEquipment,
        EquipmentInstance storedInstance,
        bool fromCatalog,
        TextMeshProUGUI title,
        TextMeshProUGUI detail)
    {
        controller = owner;
        equipment = sourceEquipment;
        itemInstance = storedInstance;
        comesFromCatalog = fromCatalog;
        titleText = title;
        detailText = detail;

        if (titleText != null)
        {
            titleText.text = storedInstance != null ? storedInstance.DisplayName : sourceEquipment.itemName;
        }

        if (detailText != null)
        {
            detailText.text = storedInstance != null
                ? storedInstance.RulesText
                : $"{sourceEquipment.displayCategory}  {sourceEquipment.rarity}  {sourceEquipment.pointCost}pt";
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller == null || PartyWorkbenchDragController.Instance == null)
        {
            return;
        }

        var rect = transform as RectTransform;
        if (comesFromCatalog)
        {
            PartyWorkbenchDragController.Instance.BeginCatalogItemDrag(equipment, eventData, rect.rect.size);
            return;
        }

        PartyWorkbenchDragController.Instance.BeginStorageItemDrag(itemInstance, eventData, rect.rect.size);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PartyWorkbenchDragController.Instance?.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PartyWorkbenchDragController.Instance?.EndDrag(eventData);
    }
}
