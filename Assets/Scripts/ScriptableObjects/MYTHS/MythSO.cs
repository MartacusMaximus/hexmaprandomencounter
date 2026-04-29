using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Myth", menuName = "ScriptableObjects/Myth")]
public class MythSO : ScriptableObject
{
    public int pageNumber;
    public string mythName;
    [TextArea] public List<string> omens = new List<string>();
    [TextArea] public string verse;
    public List<MythCastEntry> castEntries = new List<MythCastEntry>();
    public MythicRollTable flavorTable = new MythicRollTable();
    public bool visibleByDefault = false; // some myths may be visible markers on map
    public GameObject visibleMarkerPrefab; // optional marker to spawn if visible
}
