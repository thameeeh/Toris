Identifier: global.PlayerHUDBridge : MonoBehaviour

Architectural Role: Decoupled Event / Data Aggregator Bridge

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - PushInitialState(): Triggers initialization events for current state (Health, Stamina, Level, Gold).
  - Properties: Exposes getters for CurrentHealth, MaxHealth, CurrentStamina, MaxStamina, CurrentLevel, CurrentExperience, CurrentGold, ExperienceProgressNormalized.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on PlayerStats, PlayerProgression, PlayerStatusController.
- Downstream: Subscribed to by UI Presenters/Views to render player data without coupling to backend systems.

Data Schema:
- PlayerStats _playerStats -> Reference to raw combat stats.
- PlayerProgression _playerProgression -> Reference to RPG progression state.
- PlayerStatusController _playerStatusController -> Reference to active status effects.
- Events: OnHealthChanged, OnStaminaChanged, OnLevelChanged, OnGoldChanged, OnStatusApplied, OnStatusRemoved, OnStatusDamageTick.

Side Effects & Lifecycle:
- OnValidate: Logs warnings if references are missing.
- OnEnable/OnDisable: Wires/unwires internal event listeners from the target data sources.
- Start: Automatically invokes PushInitialState() to broadcast current values on startup.
