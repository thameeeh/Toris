using System;
using UnityEngine;
using UnityEngine.Tilemaps;

// SFX-only data: maps painted ground tiles to footstep sound ids without changing traversal or terrain.
[CreateAssetMenu(menuName = "Audio/Player/Footstep Surface Map", fileName = "PlayerFootstepSurfaceMap")]
public sealed class FootstepSurfaceMap : ScriptableObject
{
    [SerializeField] private string fallbackSfxId = "player_footstep_leaf";
    [SerializeField] private SurfaceMapping[] surfaceMappings;

    public string FallbackSfxId => fallbackSfxId;

    public bool TryResolveSfxId(TileBase tile, out string sfxId)
    {
        sfxId = null;
        if (tile == null || surfaceMappings == null)
            return false;

        for (int i = 0; i < surfaceMappings.Length; i++)
        {
            SurfaceMapping mapping = surfaceMappings[i];
            if (mapping != null && mapping.Contains(tile))
            {
                sfxId = mapping.SfxId;
                return !string.IsNullOrWhiteSpace(sfxId);
            }
        }

        return false;
    }

    [Serializable]
    private sealed class SurfaceMapping
    {
        [SerializeField] private string label;
        [SerializeField] private string sfxId;
        [SerializeField] private TileBase[] tiles;

        public string SfxId => sfxId;

        public bool Contains(TileBase tile)
        {
            if (tile == null || tiles == null)
                return false;

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == tile)
                    return true;
            }

            return false;
        }
    }
}
