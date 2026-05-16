using UnityEngine;

public class NodeDragger : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool isDragging = false;
    private Camera cam;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
    }

    void OnMouseDown()
    {
        isDragging = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    void OnMouseUp()
    {
        isDragging = false;
    }

    void FixedUpdate()
    {
        if (isDragging)
        {
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            rb.MovePosition(new Vector2(mousePos.x, mousePos.y));
        }
    }
}
