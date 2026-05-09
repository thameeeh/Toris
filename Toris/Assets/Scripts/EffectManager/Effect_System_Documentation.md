# Effect System Design Document

## 1. Core Architecture Overview

The Effect System provides data-driven visual effect playback using pooled prefabs and centralized runtime control.

### Main Components

#### EffectDefinition

Serializable data that describes a single effect.

- ID.
- Prefab.
- Category.
- Prewarm behavior.
- Pool caps.
- One-shot lifetime.

#### EffectLibrary

ScriptableObject asset that implements `IEffectCatalog`.

- Stores a list of `EffectDefinition` entries.
- Exposes lookup by effect ID.
- Validates duplicate IDs.
- Builds a dictionary for fast ID-to-definition mapping.

#### EffectManager

Coordinator that implements `IEffectManager`.

- Accepts high-level requests through `EffectRequest`, `AttachedEffectRequest`, and `PersistentEffectRequest`.
- Resolves `EffectDefinition` data through the catalog.
- Forwards the request to a concrete `IEffectRuntime` implementation.
- Gameplay talks to this interface, not the runtime directly.

#### EffectRuntimePool

Unity-facing runtime that implements `IEffectRuntime` and `IEffectRuntimeTick`.

- Pools GameObjects per effect definition.
- Activates and deactivates instances.
- Handles transform parenting and attachment.
- Prewarms pools at startup.
- Applies effect parameters to `IEffectParametersReceiver` components.
- Calls lifecycle callbacks for pooled instances through `IEffectPoolListener`.
- Tracks one-shot lifetimes during runtime ticking.

#### EffectInstancePool

Component added to each pooled effect instance.

- Stores runtime and handle data.
- Can be triggered by animation events through `OnEffectFinished()`.
- Resets internal state when returned to the pool.

#### IEffectPoolListener

Interface for effect scripts that need spawn and release callbacks.

```csharp
void OnEffectSpawned();
void OnEffectReleased();
```

Use this for custom reset logic, such as clearing timers, restarting particles, resetting animation state, stopping tweens, or restoring color and scale.

#### IEffectParametersReceiver

Interface for effect scripts that want request-time variant data.

```csharp
void ApplyEffectParameters(EffectVariant variant, float magnitude);
```

Use this when a reused prefab should vary by color, scale, intensity, or other authored parameters.

#### EffectManagerBehavior

Scene-level bridge between Unity and the effect system.

- Holds a reference to an `EffectLibrary`.
- Creates an `EffectManager` in `Awake()`.
- Prewarms all definitions that request it.
- Exposes static `Instance` for gameplay, typed as `IEffectManager`.
- Exposes `BehaviorInstance` for runtime configuration.

#### EffectsBootstrap

Small MonoBehaviour responsible for runtime wiring.

- Creates an `EffectRuntimePool`.
- Wires it into `EffectManagerBehavior` through `ConfigureRuntime()`.
- Optionally defines a root transform under which pools live.

#### VFX Hubs

Actor-level MonoBehaviours, such as `PlayerVfx` and `EnemyVfx`.

- Own runtime VFX state, especially persistent `EffectHandle` values.
- Provide helper methods for one-shot, attached, and persistent playback.
- Evaluate assigned VFX rule assets when an event bridge sends a gameplay event.

#### PlayerVfxEventBridge

Player-level event adapter.

- Subscribes to player gameplay events from bow, dash, health, stamina, death, and status systems.
- Converts those source-specific callbacks into `PlayerVfxEventContext`.
- Sends normalized events to `PlayerVfx`.
- Keeps the VFX hub from hardcoding every individual player trigger.

#### Player VFX Rules

`PlayerVfxRuleSO` is the primary player VFX authoring asset.

- Chooses a `PlayerVfxEventType` trigger.
- Optionally filters by `PlayerStatusEffectType`.
- Holds effect ID, playback mode, offset, rotation mode, variant, magnitude, minimum amount, and cooldown.
- Can play one-shot effects, attached one-shot effects, start persistent attached effects, or release persistent effects.
- Does not store runtime handles; `PlayerVfx` owns those handles by persistent key.

#### Legacy VFX Modules

Stateless ScriptableObject assets, such as `PlayerVfxModule_Bow`, `PlayerVfxModule_Dash`, `EnemyVfxModule_ImpactHit`, and `EnemyVfxModule_Death`.

- Decide which effect IDs to play in response to actor events.
- Hold authoring data such as effect IDs, offsets, alignment flags, variants, and magnitude.
- Do not store runtime handles or mutable state.
- Ask the hub to play or release effects.

## 2. Runtime Lifecycle

### 2.1 Startup Sequence

1. Scene loads with:
   - `EffectManagerBehavior` in the hierarchy.
   - `EffectsBootstrap`, often on the same GameObject.
   - An `EffectLibrary` assigned to `EffectManagerBehavior`.

2. `EffectManagerBehavior.Awake()`:
   - Ensures a single active instance.
   - Optionally marks the GameObject with `DontDestroyOnLoad`.
   - Constructs a new `EffectManager` using the assigned catalog and current runtime.
   - Uses `NullEffectRuntime` until the real runtime is configured.
   - Prewarms definitions that have `PrewarmPool` enabled.

3. `EffectsBootstrap.Start()`:
   - Creates an `EffectRuntimePool` with the configured effects root.
   - Calls `EffectManagerBehavior.BehaviorInstance.ConfigureRuntime(runtime)`.
   - Rebuilds the manager with the real pooled runtime.

Result:

- `EffectManagerBehavior.Instance` is backed by a pooling runtime.
- Prewarmed pools are ready before gameplay requests need them.

### 2.2 One-Shot Effect Playback

Gameplay constructs an `EffectRequest`.

```csharp
EffectManagerBehavior.Instance.Play(new EffectRequest
{
    EffectId = "your_effect_id",
    Position = someWorldPosition,
    Rotation = Quaternion.identity,
    Parent = null,
    Variant = default,
    Magnitude = 1f
});
```

Playback flow:

1. `EffectManager` validates `EffectId`.
2. `EffectManager` calls `catalog.TryGetDefinition(EffectId, out definition)`.
3. If the definition is found, the request is forwarded to `runtime.Play(definition, request)`.
4. `EffectRuntimePool` acquires or instantiates an instance from the matching pool.
5. The instance transform is placed at the requested world position and rotation, or parented when a parent is supplied.
6. `EffectInstancePool` is initialized with the runtime, handle, and one-shot state.
7. If the effect category is `OneShot`, `EffectRuntimePool.Tick()` tracks `OneShotLifetimeSeconds`.
8. `IEffectParametersReceiver` components receive variant and magnitude data.
9. `IEffectPoolListener.OnEffectSpawned()` is called on relevant components.
10. When the lifetime expires or an animation event calls `OnEffectFinished()`, the instance is released back to the pool.

### 2.3 Persistent Effects

Use persistent effects for long-lived visuals that must be stopped manually.

```csharp
EffectHandle handle = EffectManagerBehavior.Instance.PlayPersistent(new PersistentEffectRequest
{
    EffectId = "your_effect_id",
    Anchor = transform,
    LocalPosition = Vector3.zero,
    LocalRotation = Quaternion.identity,
    Variant = default,
    Magnitude = 1f
});
```

Persistent playback flow:

1. `EffectManager` resolves the effect definition.
2. `EffectRuntimePool.PlayPersistent()` acquires an instance.
3. If `Anchor` is not null, the instance is parented to it.
4. If `Anchor` is null, the instance is parented to the global effects root.
5. The instance is initialized as non-one-shot.
6. The returned `EffectHandle` must be stored by the gameplay owner.
7. When the state ends, call `EffectManagerBehavior.Instance.Release(handle)`.

### 2.4 Attached Effects

Use attached effects for visuals bound to a moving transform.

```csharp
EffectManagerBehavior.Instance.PlayAttached(new AttachedEffectRequest
{
    EffectId = "your_effect_id",
    Anchor = transform,
    LocalPosition = Vector3.zero,
    LocalRotation = Quaternion.identity,
    Variant = default,
    Magnitude = 1f
});
```

Attached playback requires a non-null `Anchor`. A null anchor skips the spawn and logs a warning.

### 2.5 Bulk Release Behaviors

`ReleaseAll()` releases every active effect.

`ReleaseAll(anchor)` releases all active effects whose anchor matches the supplied transform. Use this when an entity dies, despawns, or loses a state that owns multiple attached visuals.

### 2.6 Player Hub and Rule Dispatch

The player prefab uses a VFX hub plus an event bridge when visual effects are tied to gameplay events.

Player hub flow:

1. `PlayerVfxEventBridge` resolves local dependencies such as `PlayerBowController`, `PlayerController`, `PlayerMotor`, `PlayerStats`, `PlayerStatusController`, `Rigidbody2D`, and `PlayerFacing`.
2. `PlayerVfxEventBridge.OnEnable()` subscribes to player gameplay events.
3. When gameplay changes, the bridge creates a `PlayerVfxEventContext`.
4. `PlayerVfx` evaluates all assigned `PlayerVfxRuleSO` assets against that context.
5. Matching rules play configured effect IDs or release persistent effects.
6. `PlayerVfx` owns persistent effect handles by key and releases them during `OnDisable()`.

Supported player rule triggers:

- Bow: `BowDrawStarted`, `BowShootReady`, `BowShotReleased`, `BowShotFired`, `BowDryReleased`.
- Dash: `DashStarted`, `DashCompleted`.
- Resources: `HealthChanged`, `Healed`, `Damaged`, `StaminaChanged`, `StaminaRestored`, `StaminaSpent`.
- Life state: `PlayerDied`.
- Status effects: `StatusApplied`, `StatusRemoved`, `StatusDamageTick`.

Enemy hub flow:

1. `EnemyVfx` resolves its local `Enemy` dependency.
2. `OnEnable()` subscribes to `Damaged`, `Died`, and `Despawned`.
3. The hub forwards those events to assigned `EnemyVfxModule` assets.
4. Modules play configured effects through the hub.
5. Persistent effects are released when the enemy is disabled or despawned.

Gameplay scripts should expose events for VFX hubs to observe instead of directly spawning authored effect content.

## 3. Data and Authoring Workflow

### 3.1 EffectDefinition and EffectLibrary

Each effect is represented by one `EffectDefinition`.

Definition fields:

- `Id`: unique string key used by gameplay.
- `Prefab`: GameObject spawned by the runtime.
- `Category`: `OneShot`, `Persistent`, or `Attached`.
- `PrewarmPool`: whether to pre-instantiate instances at startup.
- `PrewarmCount`: desired inactive instance count.
- `MaxPoolSize`: hard cap on total active and inactive instances. `0` means uncapped.
- `MaxInactive`: hard cap on inactive instances retained in the pool. `0` means uncapped.
- `OneShotLifetimeSeconds`: automatic lifetime for one-shot effects.

`EffectLibrary` stores definitions and is assigned to `EffectManagerBehavior` in the scene.

### 3.2 Designer Workflow

1. Create a new prefab for the effect.
2. Build the visual effect using particles, sprites, animation, VFX Graph, or custom scripts.
3. Add `EffectInstancePool` to the root object if it is not already present.
4. Configure sorting layer and order so the effect draws correctly.
5. Add custom reset scripts that implement `IEffectPoolListener` when needed.
6. Add parameter receiver scripts that implement `IEffectParametersReceiver` when variants or magnitude should affect visuals.
7. Add a new entry to the `EffectLibrary`.
8. Assign ID, prefab, category, lifetime, and pool settings.
9. For player effects, create a `PlayerVfxRuleSO` asset.
10. Configure the trigger, playback mode, effect ID, placement, filters, and parameters.
11. Assign the rule to the `PlayerVfx.rules` list on the player prefab.
12. For legacy enemy effects, create or update an `EnemyVfxModule` asset and assign it to the enemy hub.

Gameplay scripts should not store or instantiate direct references to effect prefabs.

### 3.3 Player Rule Recipes

Potion healing burst:

- Trigger: `Healed`.
- Playback Mode: `AttachedOneShot` or `OneShotAtPlayer`.
- Effect ID: healing effect ID from `EffectLibrary`.
- Minimum Amount: set above passive regeneration if regen should not trigger it.
- Cooldown Seconds: optional throttle if several heals happen quickly.

Stamina or mana-style restore burst:

- Trigger: `StaminaRestored`.
- Playback Mode: `AttachedOneShot`.
- Effect ID: restore effect ID.
- Minimum Amount: set above normal regeneration if only potions should trigger it.

Poison aura:

- Create one rule with Trigger `StatusApplied`, Filter Status Type enabled, Status Type `Poison`, Playback Mode `StartPersistentAttached`, and a persistent key such as `status_poison`.
- Create a second rule with Trigger `StatusRemoved`, the same status filter, Playback Mode `ReleasePersistent`, and the same persistent key.

Bleed tick burst:

- Trigger: `StatusDamageTick`.
- Filter Status Type enabled, Status Type `Bleeding`.
- Playback Mode: `AttachedOneShot` or `OneShotAtPlayer`.
- Optional: use event amount as magnitude so larger DOT ticks can scale the visual.

## 4. Pooling Details and State Reset

For each `EffectDefinition`, `EffectRuntimePool` maintains an internal pool.

Pool structure:

- `EffectPool.Definition`: the definition it serves.
- `EffectPool.PoolRoot`: child transform named `Pool_<Id>`.
- `EffectPool.Inactive`: stack of inactive GameObjects ready for reuse.
- `EffectPool.TotalCreated`: active plus inactive instance count.

`Prewarm()` instantiates inactive prefabs under the pool root. Runtime acquisition pops from inactive instances first, then instantiates only if the pool has capacity.

Release flow:

1. Remove the handle from active tracking.
2. Remove any one-shot lifetime tracking.
3. Call `IEffectPoolListener.OnEffectReleased()`.
4. Reparent the object to its pool root.
5. Disable the GameObject.
6. Push it back into the inactive stack, unless `MaxInactive` would be exceeded.
7. Destroy overflow inactive objects and reduce `TotalCreated`.

## 5. Usage Patterns and Best Practices

- Always access the effect system through `IEffectManager` via `EffectManagerBehavior.Instance`.
- Use `OneShot` for short-lived fire-and-forget visuals.
- Use `Persistent` for long-lived effects that must be stopped manually.
- Use `Attached` for effects visually bound to moving objects.
- Store persistent handles on the gameplay owner, not on ScriptableObjects.
- Call `ReleaseAll(anchor)` when an entity dies or a state ends and all related attached effects should be removed.
- Implement `IEffectPoolListener` for any effect with mutable state.
- Implement `IEffectParametersReceiver` when shared prefabs need request-specific color, intensity, scale, or other variation.
