using UnityEngine;
using System;

[CreateAssetMenu(fileName = "InputReaderSO", menuName = "Scriptable Objects/PlayerInputReaderSO")]
public class PlayerInputReaderSO : ScriptableObject
{
    public Vector2 Move { get; private set; }

    public Action OnShootStarted;
    public Action OnShootReleased;
    public Action OnDashPressed;
    public Action OnAbility1Pressed;
    public Action OnInteractPressed;

    public Action OnAbility2Started;
    public Action OnAbility2Released;

    [NonSerialized] public bool isAbility2Held = false;
    [NonSerialized] public bool IsShootHeld = false;

    public void SetMove(Vector2 move) => Move = move;
    
}
