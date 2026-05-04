using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealthBar : MonoBehaviour
{
    private const float FullHealthVisibleThreshold = 0.999f;
    private const float MinimumInnerSize = 0.01f;

    private static Sprite pixelSprite;

    [Header("Binding")]
    [SerializeField] private Enemy enemy;

    [Header("Layout")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField, Min(0.1f)] private float width = 0.75f;
    [SerializeField, Min(0.01f)] private float height = 0.08f;
    [SerializeField, Min(0f)] private float borderSize = 0.015f;

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private bool hideWhenDead = true;

    [Header("Rendering")]
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.02f, 0.02f, 0.85f);
    [SerializeField] private Color fillColor = new Color(0.85f, 0.12f, 0.08f, 0.95f);
    [SerializeField] private int sortingOrder = 10000;
    [SerializeField] private string sortingLayerName = string.Empty;

    private Transform root;
    private Transform fillTransform;
    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer fillRenderer;
    private float lastCurrentHealth = float.NaN;
    private float lastMaxHealth = float.NaN;

    private void Awake()
    {
        CacheEnemy();
        EnsureVisuals();
        SyncHealthBar(true);
    }

    private void OnEnable()
    {
        CacheEnemy();
        EnsureVisuals();
        SyncHealthBar(true);
    }

    private void LateUpdate()
    {
        SyncHealthBar(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        width = Mathf.Max(0.1f, width);
        height = Mathf.Max(0.01f, height);
        borderSize = Mathf.Max(0f, borderSize);

        if (!Application.isPlaying)
            return;

        EnsureVisuals();
        ApplyVisualSettings();
        SyncHealthBar(true);
    }
#endif

    private void CacheEnemy()
    {
        if (enemy == null)
            TryGetComponent(out enemy);
    }

    private void EnsureVisuals()
    {
        if (root != null)
            return;

        GameObject rootObject = new GameObject("EnemyHealthBar");
        rootObject.transform.SetParent(transform, false);
        root = rootObject.transform;

        backgroundRenderer = CreateSegment("Background", root);
        fillRenderer = CreateSegment("Fill", root);
        fillTransform = fillRenderer.transform;

        ApplyVisualSettings();
    }

    private SpriteRenderer CreateSegment(string segmentName, Transform parent)
    {
        GameObject segmentObject = new GameObject(segmentName);
        segmentObject.transform.SetParent(parent, false);

        SpriteRenderer spriteRenderer = segmentObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = PixelSprite;
        return spriteRenderer;
    }

    private void ApplyVisualSettings()
    {
        if (root == null || backgroundRenderer == null || fillRenderer == null)
            return;

        root.localPosition = localOffset;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        backgroundRenderer.color = backgroundColor;
        backgroundRenderer.sortingOrder = sortingOrder;

        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = sortingOrder + 1;

        if (!string.IsNullOrWhiteSpace(sortingLayerName))
        {
            backgroundRenderer.sortingLayerName = sortingLayerName;
            fillRenderer.sortingLayerName = sortingLayerName;
        }

        backgroundRenderer.transform.localPosition = Vector3.zero;
        backgroundRenderer.transform.localScale = new Vector3(width, height, 1f);
    }

    private void SyncHealthBar(bool force)
    {
        if (root == null || fillTransform == null || enemy == null)
            return;

        float maxHealth = Mathf.Max(MinimumInnerSize, enemy.MaxHealth);
        float currentHealth = Mathf.Clamp(enemy.CurrentHealth, 0f, maxHealth);

        if (!force
            && Mathf.Approximately(currentHealth, lastCurrentHealth)
            && Mathf.Approximately(maxHealth, lastMaxHealth))
        {
            return;
        }

        lastCurrentHealth = currentHealth;
        lastMaxHealth = maxHealth;

        float healthRatio = Mathf.Clamp01(currentHealth / maxHealth);
        bool shouldShow = ShouldShow(currentHealth, healthRatio);
        if (root.gameObject.activeSelf != shouldShow)
            root.gameObject.SetActive(shouldShow);

        UpdateFill(healthRatio);
    }

    private bool ShouldShow(float currentHealth, float healthRatio)
    {
        if (hideWhenDead && currentHealth <= 0f)
            return false;

        if (hideWhenFull && healthRatio >= FullHealthVisibleThreshold)
            return false;

        return true;
    }

    private void UpdateFill(float healthRatio)
    {
        float innerWidth = Mathf.Max(MinimumInnerSize, width - (borderSize * 2f));
        float innerHeight = Mathf.Max(MinimumInnerSize, height - (borderSize * 2f));
        float fillWidth = innerWidth * healthRatio;
        float leftEdge = -innerWidth * 0.5f;

        fillTransform.localPosition = new Vector3(leftEdge + (fillWidth * 0.5f), 0f, 0f);
        fillTransform.localScale = new Vector3(Mathf.Max(MinimumInnerSize, fillWidth), innerHeight, 1f);
    }

    private static Sprite PixelSprite
    {
        get
        {
            if (pixelSprite != null)
                return pixelSprite;

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            pixelSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            pixelSprite.hideFlags = HideFlags.HideAndDontSave;
            return pixelSprite;
        }
    }
}
