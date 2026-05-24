using UnityEngine;

namespace OutlandHaven.Inventory
{
    public class WorldItemDropPresentation : MonoBehaviour
    {
        private const float TravelDuration = 0.35f;
        private const float ArcHeight = 0.35f;
        private const float BobHeight = 0.07f;
        private const float BobSpeed = 2.4f;
        private const float GlowScale = 1.55f;
        private const float GlowAlpha = 0.28f;
        private const float GlowPulseAlpha = 0.06f;
        private const float ShadowScale = 0.35f;
        private const float ShadowAirScaleMultiplier = 0.8f;
        private const float ShadowAirAlphaMultiplier = 0.45f;
        private const int ShadowSortingOffset = -2;
        private const int GlowSortingOffset = -1;

        private Transform _itemVisual;
        private Collider2D _pickupCollider;
        private SpriteRenderer _glowRenderer;
        private Transform _shadowTransform;
        private SpriteRenderer[] _shadowRenderers;
        private Color[] _shadowBaseColors;
        private Vector3 _shadowBaseScale;
        private Vector3 _startLocalPosition;
        private float _startTime;
        private float _bobPhase;
        private bool _isInitialized;
        private bool _hasLanded;

        public void Initialize(
            Transform itemVisual,
            Collider2D pickupCollider,
            Vector3 startWorldPosition,
            Vector3 landingWorldPosition,
            GameObject glowPrefab,
            GameObject shadowPrefab,
            int itemSortingOrder)
        {
            _itemVisual = itemVisual;
            _pickupCollider = pickupCollider;
            _startLocalPosition = startWorldPosition - landingWorldPosition;
            _startTime = Time.time;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
            _hasLanded = false;
            _isInitialized = _itemVisual != null;

            if (_pickupCollider != null)
                _pickupCollider.enabled = false;

            if (!_isInitialized)
                return;

            _itemVisual.localPosition = _startLocalPosition;
            SpawnGlow(glowPrefab, itemSortingOrder);
            SpawnShadow(shadowPrefab, itemSortingOrder);
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            if (!_hasLanded)
            {
                UpdateLandingMotion();
                return;
            }

            UpdateIdleMotion();
        }

        private void UpdateLandingMotion()
        {
            float progress = Mathf.Clamp01((Time.time - _startTime) / TravelDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            Vector3 localPosition = Vector3.Lerp(_startLocalPosition, Vector3.zero, easedProgress);
            localPosition.y += Mathf.Sin(progress * Mathf.PI) * ArcHeight;
            _itemVisual.localPosition = localPosition;

            ApplyShadowAirProgress(Mathf.Sin(progress * Mathf.PI));

            if (progress < 1f)
                return;

            _hasLanded = true;
            _itemVisual.localPosition = Vector3.zero;
            ApplyShadowAirProgress(0f);

            if (_pickupCollider != null)
                _pickupCollider.enabled = true;
        }

        private void UpdateIdleMotion()
        {
            float bobOffset = Mathf.Sin((Time.time * BobSpeed) + _bobPhase) * BobHeight;
            _itemVisual.localPosition = new Vector3(0f, bobOffset, 0f);

            if (_glowRenderer == null)
                return;

            Color glowColor = _glowRenderer.color;
            glowColor.a = GlowAlpha + (Mathf.Sin((Time.time * BobSpeed) + _bobPhase) * GlowPulseAlpha);
            _glowRenderer.color = glowColor;
        }

        private void SpawnGlow(GameObject glowPrefab, int itemSortingOrder)
        {
            if (glowPrefab == null || _itemVisual == null)
                return;

            GameObject glowObject = Instantiate(glowPrefab, _itemVisual);
            glowObject.name = "Glow";
            Transform glowTransform = glowObject.transform;
            glowTransform.localPosition = Vector3.zero;
            glowTransform.localRotation = Quaternion.identity;
            glowTransform.localScale = Vector3.one * GlowScale;

            if (!glowObject.TryGetComponent(out _glowRenderer))
                _glowRenderer = glowObject.GetComponentInChildren<SpriteRenderer>(true);

            if (_glowRenderer == null)
                return;

            _glowRenderer.sortingOrder = itemSortingOrder + GlowSortingOffset;
            _glowRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            Color glowColor = _glowRenderer.color;
            glowColor.a = GlowAlpha;
            _glowRenderer.color = glowColor;
        }

        private void SpawnShadow(GameObject shadowPrefab, int itemSortingOrder)
        {
            if (shadowPrefab == null)
                return;

            GameObject shadowObject = Instantiate(shadowPrefab, transform);
            shadowObject.name = "Shadow";
            _shadowTransform = shadowObject.transform;
            _shadowTransform.localPosition = Vector3.zero;
            _shadowTransform.localRotation = Quaternion.identity;
            _shadowTransform.localScale = Vector3.one * ShadowScale;

            _shadowBaseScale = _shadowTransform.localScale;
            _shadowRenderers = shadowObject.GetComponentsInChildren<SpriteRenderer>(true);
            _shadowBaseColors = new Color[_shadowRenderers.Length];

            for (int i = 0; i < _shadowRenderers.Length; i++)
            {
                SpriteRenderer shadowRenderer = _shadowRenderers[i];
                _shadowBaseColors[i] = shadowRenderer.color;
                shadowRenderer.sortingOrder = itemSortingOrder + ShadowSortingOffset;
                shadowRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            }
        }

        private void ApplyShadowAirProgress(float airProgress)
        {
            if (_shadowRenderers == null)
                return;

            if (_shadowTransform != null)
            {
                float scaleMultiplier = Mathf.Lerp(1f, ShadowAirScaleMultiplier, airProgress);
                _shadowTransform.localScale = _shadowBaseScale * scaleMultiplier;
            }

            float alphaMultiplier = Mathf.Lerp(1f, ShadowAirAlphaMultiplier, airProgress);
            for (int i = 0; i < _shadowRenderers.Length; i++)
            {
                SpriteRenderer shadowRenderer = _shadowRenderers[i];
                if (shadowRenderer == null)
                    continue;

                Color color = _shadowBaseColors[i];
                color.a *= alphaMultiplier;
                shadowRenderer.color = color;
            }
        }
    }
}
