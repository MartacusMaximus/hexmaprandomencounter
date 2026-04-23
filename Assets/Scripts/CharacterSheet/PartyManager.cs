using UnityEngine;
using System;
using System.Collections.Generic;


public static class UIRefreshBus
{
    public static event Action OnRefresh;

    public static void RequestRefresh()
    {
        OnRefresh?.Invoke();
    }
}
public class PartyManager : MonoBehaviour
{
    public CampaignData campaignData;
    public PartyInventory partyInventory;

    public CharacterData ActiveCharacter { get; private set; }

    public event Action<CharacterData> OnCharacterChanged;

    public void SelectCharacter(CharacterData character)
    {
        ActiveCharacter = character;
        Debug.Log($"Active character set to {character.characterName}");
        OnCharacterChanged?.Invoke(character);
        UIRefreshBus.RequestRefresh();
    }
}


