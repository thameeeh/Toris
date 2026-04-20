using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    menuName = "WorldGen/Sites/Site Stamp Definition",
    fileName = "SiteStampDefinition")]
public sealed class SiteStampDefinition : ScriptableObject
{
    [Header("Ground")]
    [SerializeField] private bool stampGround = true;
    [SerializeField] private TileBase groundTile;
    [SerializeField] private Vector2Int groundOffset = Vector2Int.zero;
    [SerializeField, Min(1)] private int groundWidth = 1;
    [SerializeField, Min(1)] private int groundHeight = 1;

    [Header("Navigation Blocker")]
    [SerializeField] private bool addNavigationBlocker;
    [SerializeField] private Vector2Int blockerOffset = Vector2Int.zero;
    [SerializeField, Min(1)] private int blockerWidth = 1;
    [SerializeField, Min(1)] private int blockerHeight = 1;

    [Header("Visual Clear Zone")]
    [SerializeField] private bool clearProceduralVisualsAroundSite;
    [SerializeField] private Vector2Int clearVisualsOffset = Vector2Int.zero;
    [SerializeField, Min(1)] private int clearVisualsWidth = 1;
    [SerializeField, Min(1)] private int clearVisualsHeight = 1;

    [Header("Authored Layout")]
    [SerializeField] private SiteTileLayoutDefinition tileLayoutDefinition;
    [SerializeField] private List<SiteTileLayoutDefinition> tileLayoutVariants = new();

    public bool HasGroundStamp => stampGround && groundTile != null;
    public TileBase GroundTile => groundTile;
    public Vector2Int GroundOffset => groundOffset;
    public int GroundWidth => Mathf.Max(1, groundWidth);
    public int GroundHeight => Mathf.Max(1, groundHeight);

    public bool HasNavigationBlockerStamp => addNavigationBlocker;
    public Vector2Int BlockerOffset => blockerOffset;
    public int BlockerWidth => Mathf.Max(1, blockerWidth);
    public int BlockerHeight => Mathf.Max(1, blockerHeight);

    public bool HasVisualClearZone => clearProceduralVisualsAroundSite;
    public Vector2Int ClearVisualsOffset => clearVisualsOffset;
    public int ClearVisualsWidth => Mathf.Max(1, clearVisualsWidth);
    public int ClearVisualsHeight => Mathf.Max(1, clearVisualsHeight);

    public SiteTileLayoutDefinition TileLayoutDefinition => tileLayoutDefinition;
    public IReadOnlyList<SiteTileLayoutDefinition> TileLayoutVariants => tileLayoutVariants;
}
