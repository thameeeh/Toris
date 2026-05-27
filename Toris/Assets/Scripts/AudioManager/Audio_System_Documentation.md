# Audio System Design Document

## 1. Core Architecture Overview

The Audio System provides runtime sound effect and music playback using pooling, data-driven definitions, and centralized control.

### Main Components

#### AudioManagerBehaviour

Scene-level entry point.

- Lives in the initial scene, optionally with `DontDestroyOnLoad`.
- Owns the lifetime of the entire audio system.
- Calls `AudioBootstrap.Initialize()` in `Awake()`.
- Drives runtime audio logic by ticking registered systems each frame using unscaled delta time.

#### AudioBootstrap

Static initialization utility.

- Constructs the core audio runtime objects.
- Exposes static access points:
  - `AudioBootstrap.Sfx`
  - `AudioBootstrap.Music`
- Registers runtime systems that require per-frame ticking.

#### IAudioRuntimeTick

Small interface implemented by runtime systems that need Update-like behavior.

```csharp
void Tick(float unscaledDeltaTime);
```

#### SfxManager

High-level SFX API used by gameplay-facing code.

- Resolves `SfxDefinition` assets by ID.
- Forwards requests to `AudioVoicePool`.
- Returns `AudioVoiceHandle` for runtime control.
- Gameplay never interacts with `AudioSource` components directly.

#### AudioVoicePool

Core runtime responsible for:

- `AudioSource` pooling.
- Voice acquisition and release.
- Concurrency limits.
- Cooldowns.
- Voice stealing.
- Looping and attached playback.
- Fade-in and fade-out behavior.

#### AudioVoiceHandle

Lightweight value-type identifier for a playing voice.

- Returned by play calls.
- Used to stop or fade sounds.
- Becomes invalid once the voice is released.

#### SfxDefinition

ScriptableObject data describing how an SFX behaves.

- ID.
- Audio clips.
- Mixer routing.
- Volume and pitch ranges.
- Spatial settings.
- Concurrency limits.
- Voice stealing mode.
- Cooldown duration.

#### SfxLibrary

ScriptableObject lookup asset mapping SFX IDs to `SfxDefinition` assets.

- Builds a dictionary for fast lookup.
- Validates duplicate IDs.

#### MusicManager

Runtime music controller.

- Implements `IMusicManager` and `IAudioRuntimeTick`.
- Uses two `AudioSource` components for crossfading.
- Supports play, stop, and smooth transitions.

#### MusicDefinition and MusicLibrary

Data equivalents of `SfxDefinition` and `SfxLibrary` for music playback.

#### SFX Hubs

Actor-level MonoBehaviours, such as `PlayerSfx`.

- Own runtime audio state, such as loop handles.
- Evaluate assigned SFX rule assets when an event bridge sends a gameplay event.

#### WorldAmbienceController

Persistent ambience owner on the `AudioManager` prefab.

- Plays wind as the broad MainArea/procedural biome bed.
- Layers forest ambience over wind in the configured forest biome index.
- Starts water ambience only when the player is near the configured outer water ring.
- Uses SFX loop handles, fade-in, and fade-out; it observes scene/world state but does not mutate gameplay or generation state.

#### PlayerSfxEventBridge

Player-level event adapter.

- Subscribes to player gameplay events from bow, dash, movement, health, stamina, death, and status systems.
- Converts those source-specific callbacks into `PlayerSfxEventContext`.
- Sends normalized events to `PlayerSfx`.
- Keeps the SFX hub from hardcoding every individual player trigger.

#### Player SFX Rules

`PlayerSfxRuleSO` is the primary player SFX authoring asset.

- Chooses a `PlayerSfxEventType` trigger.
- Optionally filters by `PlayerStatusEffectType`.
- Holds SFX ID, playback mode, loop key, fade-out, placement, volume, pitch, minimum amount, and cooldown.
- Can play one-shot sounds, attached one-shot sounds, start attached/world loops, or stop loops.
- Does not store runtime handles; `PlayerSfx` owns loop handles by key.

#### Legacy SFX Modules

Stateless ScriptableObject assets.

- Decide which sounds to play in response to events.
- Contain IDs and configuration.
- Do not store runtime state.
- Still supported through `PlayerSfx.legacyModules` for existing bow and dash assets.
- The player prefab now uses `PlayerFootstepSfxEmitter` for surface-aware footfalls; the old generic looping footstep module is a retirement candidate after reference checks.

## 2. Startup Sequence

1. Scene loads with:
   - `AudioManagerBehaviour` present in the hierarchy.
   - `SfxLibrary` and `MusicLibrary` assigned through the inspector.
   - Optional `AudioMixerGroup` assets assigned.

2. `AudioManagerBehaviour.Awake()`:
   - Ensures a single active instance.
   - Calls `AudioBootstrap.Initialize(...)`.

3. `AudioBootstrap.Initialize()`:
   - Creates `AudioVoicePool`.
   - Instantiates pooled child GameObjects under the owner.
   - Adds an `AudioSource` to each voice object.
   - Configures spatial defaults.
   - Enqueues all voices into the free pool.
   - Creates `SfxManager` with the `SfxLibrary` and `AudioVoicePool`.
   - Creates `MusicManager` with the `MusicLibrary` and owner GameObject.
   - Registers `AudioVoicePool` and `MusicManager` as runtime tick targets.

4. `AudioManagerBehaviour` stores the runtime tick list.

Result:

- `AudioBootstrap.Sfx` and `AudioBootstrap.Music` are valid.
- The audio system is fully initialized before gameplay logic runs.

## 3. Runtime Ticking

`AudioManagerBehaviour.Update()` retrieves `Time.unscaledDeltaTime` and calls `Tick(unscaledDeltaTime)` on all registered runtime systems.

Ticked systems:

- `AudioVoicePool`
- `MusicManager`

This guarantees:

- Audio fades and cleanup continue during pause or slow motion.
- Audio logic does not depend on `Time.timeScale`.

## 4. One-Shot SFX Playback

Gameplay code should usually play audio through an SFX hub or module.

```csharp
AudioBootstrap.Sfx.Play("Sfx_Id", request);
```

Playback flow:

1. `SfxManager.Play()` calls `SfxLibrary.TryGet(id, out SfxDefinition)`.
2. If the definition is not found, it returns `AudioVoiceHandle.Invalid`.
3. The request is forwarded to `AudioVoicePool.TryPlayOneShot()`.
4. `AudioVoicePool` validates the definition, available clips, cooldown, and concurrency limit.
5. The pool acquires a voice, configures the `AudioSource`, assigns a handle, registers the voice as active, and calls `AudioSource.Play()`.
6. `AudioVoiceHandle` is returned to the caller.
7. During `AudioVoicePool.Tick()`, finished non-looping voices are released back to the pool.

## 5. Attached and Looping SFX

Use attached and looping playback when the sound should follow a moving actor or continue until a state ends.

```csharp
AudioBootstrap.Sfx.PlayAttachedLoop(
    sfxId,
    targetTransform,
    Vector3.zero,
    request
);
```

Loop playback flow:

1. The gameplay hub determines a looping condition, such as movement.
2. The hub calls `AudioBootstrap.Sfx.PlayAttachedLoop(...)`.
3. `SfxManager` forwards the call to `AudioVoicePool.TryPlayAttachedLoop()`.
4. The voice is configured with `AudioSource.loop = true`, `VoiceRecord.isLooping = true`, optional `SfxPlayRequest.fadeInSeconds`, and follow target data.
5. During `Tick()`, the voice position is updated from the follow target, and any requested fade-in is applied.
6. The hub stops the loop through `AudioBootstrap.Sfx.Stop(handle, fadeOutSeconds)`.
7. `AudioVoicePool.TryStop()` fades the voice out, then releases it back to the pool.

## 6. Voice Stealing and Concurrency

Each `SfxDefinition` can specify:

- `MaxSimultaneousInstances`
- `VoiceStealMode`
- `CooldownSeconds`

Voice steal modes:

- `DropNew`: ignore the new request.
- `StealOldest`: release the oldest active voice.
- `StealQuietest`: release the quietest active voice.

Cooldown enforcement tracks the last successful play time per SFX ID. Requests during cooldown are rejected.

## 7. Music Playback and Crossfade

Gameplay calls:

```csharp
AudioBootstrap.Music.Play("Music_Id");
```

Music playback flow:

1. `MusicManager.Play()` resolves a `MusicDefinition` through `MusicLibrary`.
2. If the same clip is already playing, the request is ignored.
3. The inactive source receives the clip, mixer group, zero volume, and starts playback.
4. Fade-in and fade-out timers are initialized.
5. During `MusicManager.Tick()`, the active source fades out while the inactive source fades in.
6. When the fade completes, sources are swapped and the old source is stopped and cleared.

`MusicManager.Stop()` fades out the active source, then stops playback and clears the clip.

## 8. Player SFX Rule Dispatch

The player prefab uses an SFX hub plus an event bridge when sounds are tied to gameplay events.

Player hub flow:

1. `PlayerSfxEventBridge` resolves local dependencies such as `PlayerBowController`, `PlayerController`, `PlayerMotor`, `PlayerStats`, `PlayerStatusController`, `Rigidbody2D`, and `PlayerFacing`.
2. `PlayerSfxEventBridge.OnEnable()` subscribes to player gameplay events.
3. Movement start/stop is detected by the bridge using `movementStartSpeed` and current rigidbody velocity.
4. When gameplay changes, the bridge creates a `PlayerSfxEventContext`.
5. `PlayerSfx` evaluates all assigned `PlayerSfxRuleSO` assets against that context.
6. Matching rules play configured SFX IDs or stop loops by key.
7. `PlayerSfx` owns loop handles and stops them during `OnDisable()`.

Supported player rule triggers:

- Bow: `BowDrawStarted`, `BowShootReady`, `BowShotReleased`, `BowShotFired`, `BowDryReleased`.
- Dash: `DashStarted`, `DashCompleted`.
- Movement: `MovementStarted`, `MovementStopped`.
- Resources: `HealthChanged`, `Healed`, `Damaged`, `StaminaChanged`, `StaminaRestored`, `StaminaSpent`.
- Consumables: `ConsumableUsed`, `HealthConsumableUsed`, `ManaConsumableUsed`, `TimedConsumableUsed`.
- Life state: `PlayerDied`.
- Status effects: `StatusApplied`, `StatusRemoved`, `StatusDamageTick`.

Resource triggers are emitted from reason-aware player stat changes. Initialization, save/world-transfer restoration, and resolved-effect/stat recalculation update the SFX bridge snapshot silently and do not emit `Healed` or `Damaged`.
Rules can filter to a specific `PlayerResourceChangeReason` or ignore regeneration changes to prevent passive regeneration/heal-over-time ticks from spamming broad resource triggers.

ScriptableObject modules must remain stateless and reusable.

## 9. Authoring Workflow

1. Import audio clips into the project.
2. Create a `SfxDefinition` or `MusicDefinition`.
3. Assign clips.
4. Configure mixer routing, volume, pitch, spatial settings, concurrency, stealing, and cooldown behavior.
5. Add definitions to the appropriate library asset.
6. For player sounds, create a `PlayerSfxRuleSO` asset.
7. Configure the trigger, playback mode, SFX ID, loop key, fade-out, placement, filters, and request values.
8. Assign the rule to the `PlayerSfx.rules` list on the player prefab.
9. Trigger audio exclusively through hubs and managers.

Gameplay scripts should not reference `AudioSource` components or audio prefabs directly.

## 10. Player Rule Recipes

Healing potion burst:

- Trigger: `HealthConsumableUsed`.
- Playback Mode: `OneShotAtEventPosition`, `AttachedOneShot`, or `OneShot2D`.
- SFX ID: healing sound ID from `SfxLibrary`.
- Minimum Amount: optional. Leave at `0` unless only larger HP consumables should trigger it.
- Cooldown Seconds: optional throttle if several heals happen quickly.

Stamina or mana-style restore:

- Trigger: `ManaConsumableUsed`.
- Playback Mode: `AttachedOneShot` or `OneShot2D`.
- SFX ID: restore sound ID.
- Minimum Amount: optional. Leave at `0` unless only larger mana/stamina consumables should trigger it.

Poison loop:

- Create one rule with Trigger `StatusApplied`, Filter Status Type enabled, Status Type `Poison`, Playback Mode `StartAttachedLoop`, and a loop key such as `status_poison`.
- Create a second rule with Trigger `StatusRemoved`, the same status filter, Playback Mode `StopLoop`, and the same loop key.

Bleed tick:

- Trigger: `StatusDamageTick`.
- Filter Status Type enabled, Status Type `Bleeding`.
- Playback Mode: `AttachedOneShot` or `OneShot2D`.
- Optional: enable event amount as volume so larger DOT ticks can sound stronger.

## 11. Planned SFX Work

Player feedback batch:

- Player hurt impact/reaction sound.
- Player death sound.
- Status and effect sounds, prioritizing mana restore, poison, and bleeding feedback.

Cleanup:

- Verify that the generic looping `PlayerSfxModule_Footsteps` asset and `SFX_PlayerFootstep` definition are no longer referenced by live prefabs or scenes, then remove or archive the legacy path.

Balancing follow-up:

- `PlayerFootstepSfxEmitter` compresses audible cadence during movement-speed buffs without changing player travel speed or locomotion animation speed.
- Default tuning uses `boostedCadenceInfluence = 0.7` and `maxBoostedCadenceMultiplier = 1.7`, so a `2x` movement effect produces approximately `1.7x` footstep cadence and stronger buffs do not exceed that audible cap.
