using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Game/Ability")]
public class AbilitySO : ScriptableObject
{
    public string abilityName;
    [TextArea] public string description;

    // Requirements
    public int requiredChunkCount = 0;      // total chunks of specific colors
    public ChunkColor requiredColor = ChunkColor.None; // color required for count (None means any color)
    public bool requiresLinkedChunk = false; // requires that particular item's half-chunks be completed (linked)

    // Gameplay effect - for simplicity we will expose these shallow fields
    public int addDamageFlat = 0;
    public int addGuardFlat = 0;
    public int modifyReaction = 0;
}
