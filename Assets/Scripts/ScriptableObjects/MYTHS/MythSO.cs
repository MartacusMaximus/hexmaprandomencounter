using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Myth", menuName = "ScriptableObjects/Myth")]
public class MythSO : ScriptableObject
{
    public string mythName;
    [TextArea] public List<string> omens = new List<string>(); 
    public bool visibleByDefault = false; // some myths may be visible markers on map
    public GameObject visibleMarkerPrefab; // optional marker to spawn if visible
}
