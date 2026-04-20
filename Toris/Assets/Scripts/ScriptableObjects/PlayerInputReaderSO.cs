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
    public Action<int> OnAbilitySlotStarted;
    public Action<int> OnAbilitySlotReleased;


    /* OnInteractPressed Summary
     * It listens Interact Player Action
     * 
     *  USES
     *  * NPC to know when player is in range and interacted 'E'
     *  Script
     *      NPCInteraction
     *  
     *  * Player to interact with IInteractable objects
     *  Script: 
     *      PlayerInteractor
    */
    public Action OnInteractPressed;

    public Action OnAbility2Started;
    public Action OnAbility2Released;

    [NonSerialized] public bool isAbility2Held = false;
    [NonSerialized] public bool IsShootHeld = false;

    public void SetMove(Vector2 move) => Move = move;

    public void RaiseAbilitySlotStarted(int slotIndex) => OnAbilitySlotStarted?.Invoke(slotIndex);
    public void RaiseAbilitySlotReleased(int slotIndex) => OnAbilitySlotReleased?.Invoke(slotIndex);
}
