using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class PartyWorkbenchCharacterEntryUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public ReworkPartyWorkbenchController controller;
    public CharacterData character;
    public bool fromParty;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI detailText;

    public void Configure(
        ReworkPartyWorkbenchController owner,
        CharacterData sourceCharacter,
        bool isFromParty,
        TextMeshProUGUI title,
        TextMeshProUGUI detail)
    {
        controller = owner;
        character = sourceCharacter;
        fromParty = isFromParty;
        titleText = title;
        detailText = detail;

        if (titleText != null)
        {
            titleText.text = sourceCharacter.characterName;
        }

        if (detailText != null)
        {
            var life = sourceCharacter.isAlive ? "Alive" : "Dead";
            detailText.text = $"{sourceCharacter.knightRole}  {life}  {sourceCharacter.currentStatus}";
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller == null || PartyWorkbenchDragController.Instance == null)
        {
            return;
        }

        var rect = transform as RectTransform;
        PartyWorkbenchDragController.Instance.BeginGuildCharacterDrag(character, fromParty, eventData, rect.rect.size);
    }

    public void OnDrag(PointerEventData eventData)
    {
        PartyWorkbenchDragController.Instance?.UpdateDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PartyWorkbenchDragController.Instance?.EndDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller?.SelectCharacter(character);
    }
}
