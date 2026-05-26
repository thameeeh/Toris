using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Player Bow Events SO", menuName = "Player/Events/Player Bow Events")]
public sealed class PlayerBowEventsSO : ScriptableObject
{
    public event Action<PlayerBowController> UnderdrawReleased;
    public event Action<PlayerBowController> OverdrawStarted;

    public void RaiseUnderdrawReleased(PlayerBowController source)
    {
        UnderdrawReleased?.Invoke(source);
    }

    public void RaiseOverdrawStarted(PlayerBowController source)
    {
        OverdrawStarted?.Invoke(source);
    }
}
