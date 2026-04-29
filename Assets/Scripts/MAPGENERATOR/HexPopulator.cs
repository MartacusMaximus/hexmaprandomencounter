using UnityEngine;

public class HexPopulator : MonoBehaviour
{
    [System.Serializable]
    public struct LandmarkAssignment
    {
        public int q;
        public int r;
        public GameObject landmarkPrefab;
    }

    [System.Serializable]
    public struct TravelAssignment
    {
        public int q;
        public int r;
        public bool isHolding;
        public bool hasIndoorShelter;
        public bool hasProperRoad;
        public bool hasBoatRoute;
        public bool winterExposed;
        public bool direWeatherRegion;
        public HexCell.TravelBarrierMask blockedExits;
    }

    public LandmarkAssignment[] assignments;
    public TravelAssignment[] travelAssignments;

    public void PopulateLandmarks()
    {
        foreach (var a in assignments)
        {
            string name = $"{a.q},{a.r}";
            var go = GameObject.Find(name);
            if (go == null) { Debug.LogWarning("No hex found for landmark " + name); continue; }

            var cell = go.GetComponent<HexCell>();
            if (cell == null) { Debug.LogWarning("No HexCell on " + name); continue; }

            cell.landmarkPrefab = a.landmarkPrefab;
            if (a.landmarkPrefab != null)
            {
                // instantiate the landmark as child of the hex (visual)
                cell.landmarkInstance = Instantiate(a.landmarkPrefab, go.transform);
                cell.landmarkInstance.transform.localPosition = Vector3.zero;
            }
        }

        foreach (var assignment in travelAssignments)
        {
            string name = $"{assignment.q},{assignment.r}";
            var go = GameObject.Find(name);
            if (go == null) { continue; }

            var cell = go.GetComponent<HexCell>();
            if (cell == null) { continue; }

            cell.isHolding = assignment.isHolding;
            cell.hasIndoorShelter = assignment.hasIndoorShelter;
            cell.hasProperRoad = assignment.hasProperRoad;
            cell.hasBoatRoute = assignment.hasBoatRoute;
            cell.winterExposed = assignment.winterExposed;
            cell.direWeatherRegion = assignment.direWeatherRegion;
            cell.blockedExits = assignment.blockedExits;
        }
    }
}
