using UnityEngine;

public sealed class NecromancerSummonProtectionVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator shieldAnimator;
    [SerializeField] private Color activeTint = new Color(1f, 0.35f, 0.35f, 0.85f);
    [SerializeField] private bool hideRendererWhenInactive = true;

    private bool _isVisible;

    private void Awake()
    {
        CacheSpriteRenderer();
        CacheAnimator();
        ApplyVisibility(false);
    }

    public void SetVisible(bool isVisible)
    {
        CacheSpriteRenderer();
        CacheAnimator();

        if (_isVisible == isVisible)
            return;

        _isVisible = isVisible;
        ApplyVisibility(_isVisible);

        if (_isVisible)
            StartPlayback();
        else
            StopPlayback();
    }

    private void CacheSpriteRenderer()
    {
        if (spriteRenderer == null)
            TryGetComponent(out spriteRenderer);
    }

    private void CacheAnimator()
    {
        if (shieldAnimator == null)
            TryGetComponent(out shieldAnimator);
    }

    private void ApplyVisibility(bool isVisible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = !hideRendererWhenInactive || isVisible;
            spriteRenderer.color = activeTint;
        }
    }

    private void StartPlayback()
    {
        if (shieldAnimator == null)
            return;

        shieldAnimator.enabled = true;
        shieldAnimator.Play(0, 0, 0f);
        shieldAnimator.Update(0f);
    }

    private void StopPlayback()
    {
        if (shieldAnimator == null)
            return;

        shieldAnimator.enabled = false;
    }
}
