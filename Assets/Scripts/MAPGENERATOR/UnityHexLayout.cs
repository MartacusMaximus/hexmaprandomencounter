using KnightsAndGM.Shared;
using UnityEngine;

public static class UnityHexLayout
{
    public static Vector3 OddRowOffsetToWorld(OffsetCoordinate coordinate, float hexSize)
    {
        var x = Mathf.Sqrt(3f) * hexSize * (coordinate.Column + 0.5f * (coordinate.Row & 1));
        var z = 1.5f * hexSize * coordinate.Row;
        return new Vector3(x, 0f, z);
    }
}
