using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [SerializeField] string nextScene = "K_TestHub";
    [SerializeField] Collider2D portalCollider;
    bool _used;

    void Reset() { portalCollider = GetComponent<Collider2D>(); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used) return;
        if (!other.CompareTag("Player")) return;

        _used = true;
        if (portalCollider) portalCollider.enabled = false;
        GameInitiator.Instance.ChangeState(GameInitiator.GameState.InDungeon);
        //SceneLoader.I.GoTo(nextScene);
    }
}
