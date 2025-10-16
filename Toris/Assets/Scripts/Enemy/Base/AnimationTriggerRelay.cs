using UnityEngine;

public class AnimationTriggerRelay : MonoBehaviour
{
    private Enemy _enemyTriggerEvent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _enemyTriggerEvent = GetComponentInParent<Enemy>();
    }

    public void DealDamage() 
    {
        _enemyTriggerEvent.DealDamageToPlayer(20);
    }
}
