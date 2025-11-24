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

    #region wolf methods
    //method path this -> wolf -> enemy -> player
    public void WolfDealDamage()
    {
        _wolf.DamagePlayer(_wolf.AttackDamage);
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
    #endregion
    #region badger methods
    //method path this -> wolf -> enemy -> player
    public void BadgerDealDamage()
    {
        _badger.DamagePlayer(_badger.AttackDamage);
    }
    public void StartTunneling() 
    {
        _badger.isTunneling = true;
    }

    public void ChangeStateToIdle() 
    {
        _badger.StateMachine.ChangeState(_badger.IdleState);
    }

    public void DestroyBadger()
    {
        _badger.DestroyBadger();
    }
    #endregion
}
