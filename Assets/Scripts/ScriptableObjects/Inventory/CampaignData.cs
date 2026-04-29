using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Game/CampaignData")]
public class CampaignData : ScriptableObject
{
    public List<CharacterData> allCharacters = new();
    public CampaignInventory campaignInventory;
    public PartyData activeParty = new PartyData();
    public MythicBastionlandContentLibrarySO mythicContentLibrary;
}
