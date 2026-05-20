using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    // UI-side coordinator for showing the death screen after the player death animation has time to play.
    public sealed class DeathScreenController : MonoBehaviour
    {
        private const string DefaultDeathInputLockId = "Death";
        private const string DefaultPlayerStatsAnchorResourcePath = "PlayerProgression/PlayerStatsAnchor";

        [Header("Template")]
        [SerializeField] private VisualTreeAsset _deathScreenTemplate;

        [Header("Dependencies")]
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private PlayerStatsAnchorSO _playerStatsAnchor;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _deathScreenDelaySeconds = 1.5f;
        [SerializeField] private string _deathInputLockId = DefaultDeathInputLockId;

        private DeathScreenView _view;
        private UIManager _uiManager;
        private PlayerStats _observedStats;
        private Coroutine _bindStatsRoutine;
        private Coroutine _openDeathScreenRoutine;
        private bool _deathFlowActive;

        private void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            ResolvePlayerStatsAnchor();
        }

        private void Start()
        {
            if (_deathScreenTemplate == null || _uiManager == null || _uiEvents == null)
                return;

            TemplateContainer deathScreenInstance = _deathScreenTemplate.Instantiate();

            _view = new DeathScreenView(deathScreenInstance, _uiEvents);
            _view.Initialize();
            _view.OnRespawnClicked += HandleRespawnClicked;
            _view.OnMainMenuClicked += HandleMainMenuClicked;

            _uiManager.RegisterView(_view, ScreenZone.FullScreen);
            _bindStatsRoutine = StartCoroutine(BindStatsWhenReady());
        }

        private void OnDisable()
        {
            UnbindObservedStats();

            if (_bindStatsRoutine != null)
            {
                StopCoroutine(_bindStatsRoutine);
                _bindStatsRoutine = null;
            }

            if (_openDeathScreenRoutine != null)
            {
                StopCoroutine(_openDeathScreenRoutine);
                _openDeathScreenRoutine = null;
            }
        }

        private void OnDestroy()
        {
            if (_view == null)
                return;

            _view.OnRespawnClicked -= HandleRespawnClicked;
            _view.OnMainMenuClicked -= HandleMainMenuClicked;
            _view.Dispose();
            _view = null;
        }

        private IEnumerator BindStatsWhenReady()
        {
            while (_observedStats == null)
            {
                TryBindObservedStats();
                if (_observedStats != null)
                    yield break;

                yield return null;
            }
        }

        private void TryBindObservedStats()
        {
            ResolvePlayerStatsAnchor();

            PlayerStats stats = null;
            if (_playerStatsAnchor != null && _playerStatsAnchor.IsReady)
            {
                stats = _playerStatsAnchor.Instance;
            }

            if (stats == null)
            {
                stats = FindFirstObjectByType<PlayerStats>();
            }

            if (stats == null || stats == _observedStats)
                return;

            UnbindObservedStats();
            _observedStats = stats;
            _observedStats.OnPlayerDied += HandlePlayerDied;

            if (_observedStats.IsDead && !DeathRespawnCoordinator.HasPendingRespawn)
            {
                // Death screen fallback for late binding in the death scene; suppressed during respawn scene loads.
                HandlePlayerDied();
            }
        }

        private void UnbindObservedStats()
        {
            if (_observedStats == null)
                return;

            _observedStats.OnPlayerDied -= HandlePlayerDied;
            _observedStats = null;
        }

        private void HandlePlayerDied()
        {
            if (_deathFlowActive || _uiEvents == null)
                return;

            // Lock input immediately; the visual death screen appears after the animation delay.
            _deathFlowActive = true;
            _uiEvents.OnGameplayInputLockRequested?.Invoke(ResolveDeathInputLockId());
            _uiEvents.OnRequestCloseAll?.Invoke();

            _openDeathScreenRoutine = StartCoroutine(OpenDeathScreenAfterDelay());
        }

        private IEnumerator OpenDeathScreenAfterDelay()
        {
            float delay = Mathf.Max(0f, _deathScreenDelaySeconds);
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            _uiEvents?.OnRequestOpen?.Invoke(ScreenType.DeathScreen, null);
            _openDeathScreenRoutine = null;
        }

        private void HandleRespawnClicked()
        {
            _uiEvents?.OnDeathRespawnRequested?.Invoke();
        }

        private void HandleMainMenuClicked()
        {
            _uiEvents?.OnDeathMainMenuRequested?.Invoke();
        }

        private void ResolvePlayerStatsAnchor()
        {
            if (_playerStatsAnchor != null)
                return;

            _playerStatsAnchor = Resources.Load<PlayerStatsAnchorSO>(DefaultPlayerStatsAnchorResourcePath);
        }

        private string ResolveDeathInputLockId()
        {
            return string.IsNullOrWhiteSpace(_deathInputLockId)
                ? DefaultDeathInputLockId
                : _deathInputLockId.Trim();
        }
    }
}
