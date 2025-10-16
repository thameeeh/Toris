using UnityEngine;

public class AnimationTriggerRelay : MonoBehaviour
{
    private Wolf _wolf;
    void Start()
    {
        _wolf = GetComponentInParent<Wolf>();
    }

    public void DealDamage() 
    {
        _wolf.DealDamageToPlayer(_wolf.AttackDamage);
    }

    public void MoveWhileBite(int i) 
    {
        if(i == 1) _wolf.IsMovingWhileBiting = true;
        else _wolf.IsMovingWhileBiting = false;
    }
}
