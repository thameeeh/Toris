using UnityEngine;

public class AnimalBehaviour : MonoBehaviour
{
    public StateAnimal idleState;
    public StateAnimal walkState;
    public StateAnimal runState;

    private StateAnimal _state = null;
    public bool IsRunning { get; private set; }
    public float Speed { get; private set; }            //for state logic
    public Vector2 MovementVector { get; private set; }
    protected Animator _animator;
    protected Rigidbody2D _rigidbody;
    public SpriteRenderer _spriteRenderer { get; private set; }

    private GameObject _player = null;




    public bool IsDead { get; set; }
    private void Start()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _player = GameObject.FindGameObjectWithTag("Player");
        _spriteRenderer = GetComponent<SpriteRenderer>();

        idleState.Setup(_animator, this);
        walkState.Setup(_animator, this);
        runState.Setup(_animator, this);
        _state = idleState;
    }

    private void Update()
    {
        if (_state.isComplete)
        {
            SelectState();
        }
        _state.Do();
        
        float dist = (_player.transform.position - transform.position).magnitude;
        if (dist < 2)
        {
            IsRunning = true;
            Speed = 3;
            MovementVector = (transform.position - _player.transform.position).normalized;
            Move();
        }
        else if (dist < 4)
        {
            IsRunning = false;
            Speed = 2;
            MovementVector = (transform.position - _player.transform.position).normalized;
            Move();
        }
        else
        {
            MovementVector = Vector2.zero;
            Move();
        }
        _state.AnimationDirection(MovementVector);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Weapon"))
        {
            IsDead = true;
            Debug.Log("Animal is dead");
        }
    }

    private void Move() 
    {
        Vector2 moveDirection = MovementVector;
        moveDirection = ConvertIntoIsometric(moveDirection);

        moveDirection = Speed * Time.deltaTime * moveDirection;

        _rigidbody.MovePosition(_rigidbody.position + moveDirection);
    }

    private Vector2 ConvertIntoIsometric(Vector2 v2)
    {
        /*
         * multiply Moving Vector by [   1,   1]
         *                           [-0.5, 0.5] 
         * to convert it into isometric space, basicaly we are squishing the y axis by half
        */

        Vector2 result = new(v2.x, v2.y / 2);
        return result.normalized;
    }
    void SelectState()
    {
        if (MovementVector != Vector2.zero)
        {
            if (IsRunning) _state = runState;
            else _state = walkState;
        }
        else _state = idleState;
        _state.Enter();
    }   
}
