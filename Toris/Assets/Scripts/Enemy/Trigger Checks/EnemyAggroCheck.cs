using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log("Aggro Check entered by: " + collision.name);
        if (IsPlayerCollision(collision))
        {
            _enemy.SetAggroStatus(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsPlayerCollision(collision))
        {
            _enemy.SetAggroStatus(false);
        }
    }

    private static bool IsPlayerCollision(Collider2D collision)
    {
        return collision != null
               && (collision.CompareTag("Player")
                   || collision.GetComponentInParent<PlayerDamageReceiver>() != null);
    }
}
