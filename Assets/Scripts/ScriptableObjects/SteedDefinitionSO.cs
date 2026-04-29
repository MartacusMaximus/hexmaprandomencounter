using UnityEngine;

[CreateAssetMenu(fileName = "SteedDefinition", menuName = "Game/Mythic Bastionland/Steed Definition")]
public class SteedDefinitionSO : ScriptableObject
{
    public string steedName;
    public int vigor;
    public int clarity;
    public int spirit;
    public int guard;
    public bool startsExhausted;

    public int StorageRows => 2;
    public int StorageColumns => 2;
}
