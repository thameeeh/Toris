using UnityEngine;

public class GraphCameraController : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 50f;

    [Header("Pan Settings")]
    public float panSpeed = 1f;

    private Camera cam;
    private Vector3 lastMousePosition;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleZoom();
        HandlePan();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - (scroll * zoomSpeed), minZoom, maxZoom);
        }
    }

    private void HandlePan()
    {
        // Pan with Middle Mouse (0) or Right Mouse (1)
        if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2) || Input.GetMouseButton(1))
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            
            // Convert pixel delta to world units based on camera zoom
            float unitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
            Vector3 move = new Vector3(-delta.x * unitsPerPixel, -delta.y * unitsPerPixel, 0);
            
            transform.Translate(move, Space.World);
            lastMousePosition = Input.mousePosition;
        }
    }
}
