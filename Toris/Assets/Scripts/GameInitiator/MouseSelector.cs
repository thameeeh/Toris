using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MouseSelector : MonoBehaviour
{
    private void Start()
    {
        //SceneManager.LoadSceneAsync("Enemies", LoadSceneMode.Additive);
    }

    // Update is called once per frame

    private void OnEnable()
    {
        
    }
    void Update()
    {
        if(Mouse.current != null)
        {
            return;
        }

        if(Mouse.current.rightButton.wasPressedThisFrame)
        {
            Debug.Log("Right Clicked");

            Vector2 mousePosition = Mouse.current.position.ReadValue();

            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(mousePosition);

            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

            if(hit.collider != null)
            {
                Debug.Log("Clicked on: " + hit.collider.gameObject.name);
            }
        }

    }
}
