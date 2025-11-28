using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    public float zoomSpeed = 20f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    public float panSpeed = 20f;

    Camera cam;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    void Update()
    {
        HandleZoom();
        HandlePan();
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
    }

    void HandlePan()
    {
        if (Input.GetMouseButton(2))
        {
            float moveX = -Input.GetAxis("Mouse X") * panSpeed * Time.deltaTime;
            float moveY = -Input.GetAxis("Mouse Y") * panSpeed * Time.deltaTime;
            transform.position += new Vector3(moveX,10, moveY);
        }
    }

    public void CenterOn(Vector3 worldPos)
    {
        transform.position = new Vector3(worldPos.x, 10, transform.position.z);
    }
}
