using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;


public class PlayerWalk : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private InputSystem_Actions _playerInputActions;
    private Vector2 _input; 
    private Rigidbody2D _rigidbody2D;

    private void OnEnable()
    {
        _playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInputActions.Player.Disable();
    }

    private void Awake()
    {
        _playerInputActions = new InputSystem_Actions();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        GatherInput();
        Move();
    }

    private void Move()
    {
        Vector2 moveDirection = _input;
        moveDirection = ConvertIntoIsometric(moveDirection) * speed * Time.deltaTime;
        
        Debug.Log(moveDirection / Time.deltaTime);

        _rigidbody2D.MovePosition(_rigidbody2D.position + moveDirection);
    }

    private Vector2 ConvertIntoIsometric(Vector2 v2) 
    {
        /*
         * multiply Moving Vector by [  1,  -1]
         *                           [0.5, 0.5] 
         * to convert it into isometric space, basicaly we are squishing the y axis by half
        */

        Vector2 result = new (v2.x * Mathf.Cos(0) - v2.y * Mathf.Sin(0),
                             (v2.x * Mathf.Sin(0) + v2.y * Mathf.Cos(0)) * 0.5f);
        return result;
    }

    private void GatherInput() 
    {
        _input = _playerInputActions.Player.Move.ReadValue<Vector2>();
    }
}
