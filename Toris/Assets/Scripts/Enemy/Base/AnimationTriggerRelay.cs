using UnityEngine;

public class AnimationTriggerRelay : MonoBehaviour
{
    private Wolf _wolf;
    private Badger _badger;
    void Start()
    {
        _wolf = GetComponentInParent<Wolf>();
        _badger = GetComponentInParent<Badger>();
    }

    //method path this -> wolf -> enemy -> player
    public void DealDamage()
    {
        _wolf.DamagePlayer(_wolf.AttackDamage);
        _badger.DamagePlayer(_badger.AttackDamage);
    }

    public void DestroyWolf()
    {
        _wolf.DestroyGameObject();
    }

    public void MoveWhileBite(int i)
    {
        if (i == 1) _wolf.IsMovingWhileBiting = true;
        else _wolf.IsMovingWhileBiting = false;
    }

    public void IsBurrowing()
    {
        _badger.IsCurrentlyBurrowing(false);
    }

    public void Wonder()
    {
        _badger.Wonder();
    }
}
