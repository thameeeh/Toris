using UnityEngine;
using System;

public class BackgroundClickHandler : MonoBehaviour
{
    public event Action OnBackgroundClick;

    void Update()
    {
        // If left mouse button is clicked
        if (Input.GetMouseButtonDown(0))
        {
            // Raycast from camera to mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

            // If we didn't hit anything (i.e., we hit the background)
            if (hit.collider == null)
            {
                OnBackgroundClick?.Invoke();
            }
        }
    }
}
