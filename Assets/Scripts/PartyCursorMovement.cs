using UnityEngine;
using System.Collections;

public class PartyCursorMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private HexCell currentCell;

    public void SetStartCell(HexCell cell)
    {
        currentCell = cell;
        transform.position = cell.transform.position;
    }

    public void TryMoveTo(HexCell target)
    {
        if (!AreNeighbors(currentCell, target)) return;

        StopAllCoroutines();
        StartCoroutine(MoveTo(target));
    }

    IEnumerator MoveTo(HexCell target)
    {
        Vector3 start = transform.position;
        Vector3 end = target.transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        currentCell = target;
        
        CameraController.Instance.CenterOn(transform.position);

        RandomEncounterManager.Instance.OnEnterHex(currentCell);
    }

bool AreNeighbors(HexCell a, HexCell b)
{
    int ax = a.q, az = a.r, ay = -ax - az;
    int bx = b.q, bz = b.r, by = -bx - bz;

    return Mathf.Abs(bx - ax) + Mathf.Abs(by - ay) + Mathf.Abs(bz - az) == 2;
}

        }
