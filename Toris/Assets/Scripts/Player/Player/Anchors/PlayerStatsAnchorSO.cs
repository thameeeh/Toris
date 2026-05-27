using UnityEngine;

[CreateAssetMenu(menuName = "Outland Haven/Player/Anchors/Stats Anchor", fileName = "PlayerStatsAnchor")]
public class PlayerStatsAnchorSO : ScriptableObject
{
    [System.NonSerialized]
    private PlayerStats _instance;

    public PlayerStats Instance => _instance;

    public bool IsReady => _instance != null;

    public void Provide(PlayerStats instance)
    {
        if (instance == null)
        {
            Debug.LogError("[PlayerStatsAnchorSO] Attempted to provide a null PlayerStats.");
            return;
        }

        _instance = instance;
    }

    public void Clear()
    {
        _instance = null;
    }
}
