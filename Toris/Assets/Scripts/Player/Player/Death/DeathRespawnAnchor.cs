using System;
using UnityEngine;

public sealed class DeathRespawnAnchor : MonoBehaviour
{
    private const string DefaultAnchorId = "MainArea_DeathRespawn";

    [SerializeField] private string _anchorId = DefaultAnchorId;

    public string AnchorId => string.IsNullOrWhiteSpace(_anchorId) ? DefaultAnchorId : _anchorId.Trim();

    public static bool TryFind(string requestedAnchorId, out DeathRespawnAnchor anchor)
    {
        string normalizedRequestedId = string.IsNullOrWhiteSpace(requestedAnchorId)
            ? DefaultAnchorId
            : requestedAnchorId.Trim();

        DeathRespawnAnchor[] anchors = FindObjectsByType<DeathRespawnAnchor>(FindObjectsSortMode.None);
        DeathRespawnAnchor fallback = null;

        for (int i = 0; i < anchors.Length; i++)
        {
            DeathRespawnAnchor candidate = anchors[i];
            if (candidate == null)
                continue;

            fallback ??= candidate;

            if (string.Equals(candidate.AnchorId, normalizedRequestedId, StringComparison.OrdinalIgnoreCase))
            {
                anchor = candidate;
                return true;
            }
        }

        anchor = fallback;
        return anchor != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_anchorId))
        {
            _anchorId = DefaultAnchorId;
        }
    }
#endif
}
