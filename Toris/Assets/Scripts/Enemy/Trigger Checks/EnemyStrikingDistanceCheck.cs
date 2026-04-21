using UnityEngine;

public class EnemyStrikingDistanceCheck : MonoBehaviour
{
    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsPlayerCollision(collision))
        {
            _enemy.SetStrikingDistanceBool(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayerCollision(collision))
        {
            _enemy.SetStrikingDistanceBool(false);
        }
    }

    private static bool IsPlayerCollision(Collider2D collision)
    {
        return collision != null
               && (collision.CompareTag("Player")
                   || collision.GetComponentInParent<PlayerDamageReceiver>() != null);
    }
}
