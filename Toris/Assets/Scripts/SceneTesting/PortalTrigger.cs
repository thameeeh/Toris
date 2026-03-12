using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] string nextScene = "K_TestHub";
    [SerializeField] Collider2D portalCollider;

    void Reset() { portalCollider = GetComponent<Collider2D>(); }

    private void Start()
    {
       portalCollider.enabled = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
    }
}
