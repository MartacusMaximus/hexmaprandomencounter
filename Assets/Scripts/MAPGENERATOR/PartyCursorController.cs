using UnityEngine;

public class PartyCursorController : MonoBehaviour
{
    public static PartyCursorController Instance;

    public PartyCursorMovement cursor;

    void Awake() => Instance = this;

    public void TryClickCell(HexCell cell)
    {
        cursor.TryMoveTo(cell);
        CameraController.Instance.CenterOn(cursor.transform.position);
    }

}
