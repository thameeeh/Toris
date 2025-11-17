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
    #region wolf methods
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
    public void StartTunneling() 
    {
        _badger.IsTunneling = true;
    }

    public void ChangeStateToIdle() 
    {
        _badger.StateMachine.ChangeState(_badger.IdleState);
    }
    #endregion
}
