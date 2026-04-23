using UnityEngine;
using UnityEngine.InputSystem;

public class HexInput : MonoBehaviour
{
    private HexCell hoveredCell;

    void Update()
    {
        HandleHover();
        HandleClick();
    }

    void HandleHover()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            HexCell cell = hit.collider.GetComponentInParent<HexCell>();

            if (cell != hoveredCell)
            {
                if (hoveredCell != null)
                    hoveredCell.SetHighlight(false);

                hoveredCell = cell;

                if (hoveredCell != null)
                    hoveredCell.SetHighlight(true);
            }
        }
        else
        {
            if (hoveredCell != null)
                hoveredCell.SetHighlight(false);

            hoveredCell = null;
        }
    }

    void HandleClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (hoveredCell != null)
                PartyCursorController.Instance.TryClickCell(hoveredCell);
        }
    }
}
