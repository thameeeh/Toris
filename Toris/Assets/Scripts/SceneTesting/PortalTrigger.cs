using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] string nextScene = "K_TestHub";
    [SerializeField] Collider2D portalCollider;

    void Reset() { portalCollider = GetComponent<Collider2D>(); }

    private void Update()
    {
        if(GameInitiator.Instance.GetState() == GameInitiator.GameState.InOverworld)
            portalCollider.enabled = false;
        else portalCollider.enabled = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameInitiator.Instance.ChangeState(GameInitiator.GameState.InOverworld);
        //SceneLoader.I.GoTo(nextScene);
    }
}
