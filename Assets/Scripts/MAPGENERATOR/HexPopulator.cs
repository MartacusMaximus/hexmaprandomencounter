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

    public LandmarkAssignment[] assignments;

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
    }
}
