using UnityEngine;

[CreateAssetMenu(menuName = "Anchors/PlayerProgressionAnchorSO", fileName = "PlayerProgressionAnchor")]
public class PlayerProgressionAnchorSO : ScriptableObject
{
    [System.NonSerialized]
    private PlayerProgression _instance;

    public PlayerProgression Instance => _instance;

    public bool IsReady => _instance != null;

    public void Provide(PlayerProgression instance)
    {
        if (instance == null)
        {
            Debug.LogError("[PlayerProgressionAnchorSO] Attempted to provide a null PlayerProgression.");
            return;
        }

        _instance = instance;
    }

    public void Clear()
    {
        _instance = null;
    }
}
