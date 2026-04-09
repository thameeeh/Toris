using UnityEngine;

public class NecromancerCastingRangeCheck : MonoBehaviour
{
    private const string PlayerTag = "Player";

    private Necromancer _necromancer;

    private void Awake()
    {
        _necromancer = GetComponentInParent<Necromancer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_necromancer != null && collision.CompareTag(PlayerTag))
            _necromancer.SetCastingRangeBool(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_necromancer != null && collision.CompareTag(PlayerTag))
            _necromancer.SetCastingRangeBool(false);
    }
}
