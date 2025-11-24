/// <summary>
/// ====================================================================================
/// EFFECT SYSTEM DESIGN DOCUMENT
/// ====================================================================================
///
/// 1. CORE ARCHITECTURE OVERVIEW
/// -----------------------------
/// The Effect System is made for visual effect implementation
///
/// --- MAIN COMPONENTS ---
///
/// - EffectDefinition:
///   Serializable data that describes a single effect (ID, prefab, category, prewarm).
///
/// - EffectLibrary (ScriptableObject, implements IEffectCatalog):
///   Asset that stores a list of EffectDefinitions and exposes lookup by EffectId.
///   Validates duplicates and builds a dictionary for fast ID to definition mapping.
///
/// - EffectManager (implements IEffectManager):
///   Coordinator. It:
///   * Accepts high-level requests (EffectRequest / PersistentEffectRequest).
///   * Resolves EffectDefinition via the catalog.
///   * Forwards the request to a concrete IEffectRuntime implementation.
///   Gameplay only talks to this interface, never to the runtime directly.
///
/// - EffectRuntimePool (implements IEffectRuntime):
///   Unity-facing runtime that performs:
///   * GameObject pooling per effect definition.
///   * Instance activation / deactivation.
///   * Transform parenting and attachment.
///   * Prewarming of pools at startup.
///   * Lifecycle callbacks for pooled instances (IEffectPoolListener).
///
/// - EffectInstancePool:
///   Component added to each pooled effect instance:
///   * Stores runtime + handle.
///   * Handles auto-release for one-shot effects (via coroutine).
///   * Can be triggered by animation events to release effects.
///   * Resets its internal state when returned to pool.
///
/// - IEffectPoolListener:
///   Small interface that effect scripts can implement to receive:
///   * OnEffectSpawned() when taken from pool and activated.
///   * OnEffectReleased() right before being returned to pool.
///   This allows custom effects (scripts, particles, audio) to reset state.
///
/// - EffectManagerBehavior (MonoBehaviour, implements IEffectManager):
///   Scene-level bridge between Unity and the effect system:
///   * Holds a reference to an EffectLibrary.
///   * On Awake(): creates EffectManager with the current catalog + runtime.
///   * Prewarms all definitions that request it.
///   * Exposes a static Instance for gameplay, typed as IEffectManager.
///   * Exposes BehaviorInstance for runtime configuration (ConfigureRuntime).
///
/// - EffectsBootstrap:
///   Tiny MonoBehaviour responsible for:
///   * Creating an EffectRuntimePool at runtime.
///   * Wiring it into the EffectManagerBehavior via ConfigureRuntime.
///   * Optionally defining a root Transform under which pools will live.
///
/// ====================================================================================
/// 2. RUNTIME LIFECYCLE
/// -----------------------------
///
/// --- 2.1. Startup Sequence ---
///
/// 1) Scene loads with:
///    - EffectManagerBehavior in the hierarchy. (later initialized as a separate prefab at startup)
///    - EffectsBootstrap (often on the same GameObject).
///    - An EffectLibrary assigned to the EffectManagerBehavior.
///
/// 2) EffectManagerBehavior.Awake():
///    - Ensures a single activeInstance (optional DontDestroyOnLoad).
///    - Constructs a new EffectManager with:
///        * IEffectCatalog -> the assigned EffectLibrary (or override).
///        * IEffectRuntime -> current runtimeOverride (NullEffectRuntime by default).
///    - Prewarms pools by iterating through catalog.Definitions:
///        * For each EffectDefinition with PrewarmPool == true:
///              runtime.Prewarm(def, def.PrewarmCount).
///
/// 3) EffectsBootstrap.Start():
///    - Creates a new EffectRuntimePool, passing in a root Transform for organizational
///      parenting (e.g., an empty "EffectsRoot" GameObject).
///    - Calls EffectManagerBehavior.BehaviorInstance.ConfigureRuntime(runtime).
///    - This rebuilds the EffectManager with the real EffectRuntimePool instead of
///      NullEffectRuntime.
///
/// Result: By the time gameplay runs, EffectManager.Instance is backed by a pooling
/// runtime, and all prewarmed pools are ready.
///
/// --- 2.2. One-Shot Effect Playback ---
///
/// 1) Gameplay code constructs an EffectRequest:
///    - EffectId: string key for the effect
///    - Position: world position.
///    - Rotation: orientation.
///    - Parent: optional transform to parent to.
///    - Variant / Magnitude: reserved for future variant behavior.
///
/// 2) Gameplay calls:
///       EffectManagerBehavior.Instance.Play(request);
///
/// 3) EffectManager:
///    - Validates EffectId.
///    - Calls catalog.TryGetDefinition(EffectId, out definition).
///    - If definition found, calls runtime.Play(definition, request).
///
/// 4) EffectRuntimePool.Play():
///    - AcquireInstance(definition):
///        * Get or create EffectPool for the given definition.
///        * Pop an inactive GameObject from pool.Inactive if available,
///          otherwise instantiate a new one under the pool root.
///        * go.SetActive(true).
///    - RegisterActive():
///        * Assigns a new EffectHandle (FromKey) and stores ActiveInstance in _active.
///    - ApplyTransform():
///        * If request.Parent != null: parent to Parent with local zero; set world pos/rot.
///        * If Parent == null: detach (SetParent(null)) and place at world Position/Rotation.
///    - Add or get EffectInstancePool component and Initialize():
///        * Stores runtime reference and handle.
///        * If effect is OneShot, starts coroutine AutoRelease() with configured lifetime.
///    - NotifySpawned():
///        * Collects IEffectPoolListener components on the instance and calls OnEffectSpawned().
///
/// 5) EffectInstancePool.AutoRelease():
///    - Waits for oneShotLifetime seconds.
///    - Calls runtime.Release(handle) if runtime is still valid.
///
/// 6) EffectRuntimePool.Release():
///    - Validates handle and finds ActiveInstance in _active.
///    - Removes the handle entry from _active.
///    - Calls NotifyReleased(go) to inform all IEffectPoolListener components.
///    - Reparents object to its EffectPool.PoolRoot and disables it (SetActive(false)).
///    - Pushes the GameObject back into pool.Inactive.
///
/// The object is now ready to be reused with a clean state.
///
/// --- 2.3. Persistent / Attached Effects ---
///
/// 1) Gameplay constructs a PersistentEffectRequest:
///    - EffectId: string.
///    - Anchor: Transform to attach to.
///    - LocalPosition / LocalRotation: relative offset from anchor.
///    - Variant: reserved for future use.
///
/// 2) Gameplay calls:
///       var handle = EffectManagerBehavior.Instance.PlayPersistent(request);
///
/// 3) EffectManager resolves definition and calls:
///       runtime.PlayPersistent(definition, request);
///
/// 4) EffectRuntimePool.PlayPersistent():
///    - AcquireInstance(definition) as above.
///    - Determine parent:
///        * If Anchor != null: parent = Anchor.
///        * Else: parent = _root (global effects root).
///    - ApplyTransformAttached():
///        * Parent to chosen transform.
///        * Set localPosition / localRotation to requested values.
///    - RegisterActive() to get EffectHandle.
///    - Initialize EffectInstancePool (isOneShot = false).
///    - NotifySpawned() for all IEffectPoolListener components.
///
/// 5) When gameplay wants to stop the persistent effect:
///       EffectManagerBehavior.Instance.Release(handle);
///    The same Release() process puts the instance back into the pool.
///
/// --- 2.4. Bulk Release Behaviors ---
///
/// - ReleaseAll():
///   EffectRuntimePool collects all active handles and releases them one by one,
///   effectively clearing all active effects.
///
/// - ReleaseAll(anchor):
///   EffectRuntimePool iterates _active to find all ActiveInstances whose Anchor matches
///   the given Transform, then releases those. This is used when, for example, an enemy
///   dies and all attached effects should be removed.
///
/// ====================================================================================
/// 3. DATA & AUTHORING WORKFLOW
/// -----------------------------
///
/// --- 3.1. EffectDefinition & EffectLibrary ---
///
/// - Each effect in the game is represented by a single EffectDefinition:
///   * Id: unique string key.
///   * Prefab: the GameObject to spawn (particles, sprite, VFX graph, etc.).
///   * Category: OneShot, Persistent, or Attached.
///   * PrewarmPool: whether to pre-instantiate instances at startup.
///   * PrewarmCount: desired inactive instance count to create initially.
///
/// - EffectLibrary (ScriptableObject) stores a list of EffectDefinitions:
///   * Implements IEffectCatalog (Definitions + TryGetDefinition).
///   * Builds a dictionary for fast lookups.
///   * OnValidate() logs warnings for duplicate Ids.
///   * This asset is assigned to EffectManagerBehavior in the scene.
///
/// --- 3.2. Designer Workflow ---
///
/// 1) Create a new prefab for the effect:
///    - Build the visual effect (particles, sprite, animator, etc.).
///    - Add EffectInstancePool to the root object.
///    - Configure sorting layer (e.g., VFX) so it draws above relevant sprites.
///
/// 2) Add the prefab to the EffectLibrary:
///    - Add a new entry.
///    - Assign:
///       * Id (string key for gameplay).
///       * Prefab reference.
///       * Category (OneShot / Persistent / Attached).
///       * Prewarm settings if appropriate.
///
/// 3) Use in gameplay:
///    - From code, call:
///         EffectManagerBehavior.Instance.Play(new EffectRequest {
///             EffectId = "your_effect_id",
///             Position = someWorldPosition,
///             Rotation = Quaternion.identity
///         });
///
/// No gameplay script should hold direct references to effect prefabs.
///
/// ====================================================================================
/// 4. POOLING DETAILS & STATE RESET
/// -----------------------------
///
/// --- 4.1. Pool Structure ---
///
/// - For each EffectDefinition, EffectRuntimePool maintains an EffectPool:
///   * EffectPool.Definition: the definition it serves.
///   * EffectPool.PoolRoot: a child transform created under the global root, named "Pool_<Id>".
///   * EffectPool.Inactive: a stack of inactive GameObjects ready to be reused.
///
/// - Prewarm() instantiates PrewarmCount prefabs as children under PoolRoot and deactivates them.
///
/// - AcquireInstance():
///   * Pops from Inactive if available; otherwise instantiates a new one (parented to PoolRoot).
///   * Sets the instance active before returning it.
///
/// --- 4.2. IEffectPoolListener & State Reset ---
///
/// - Any component on the spawned prefab that needs to reset internal mutable state across
///   uses can implement IEffectPoolListener:
///       void OnEffectSpawned();
///       void OnEffectReleased();
///
/// - EffectRuntimePool calls:
///   * NotifySpawned(go):
///       - Finds all IEffectPoolListener components in children (active or inactive).
///       - Calls OnEffectSpawned() on each.
///   * NotifyReleased(go):
///       - Same search, then calls OnEffectReleased().
///
/// - Example use cases:
///   * Resetting timers, animation states, and procedural parameters.
///   * Stopping residual sounds or tweens when an effect is returned to the pool.
///   * Restoring default color, scale, or shader parameters.
///
/// - EffectInstancePool itself can implement these methods to clean up its own state.
///
/// ====================================================================================
/// 5. USAGE PATTERNS & BEST PRACTICES
/// -----------------------------
///
/// - Always access the effect system via IEffectManager:
///     EffectManagerBehavior.Instance.Play(...)
///   Do not store or manipulate effect prefabs directly in gameplay scripts.
///
/// - Use OneShot for short-lived, fire-and-forget visuals.
///
/// - Use Persistent / Attached for:
///   * Long-lived effects that must be stopped manually.
///   * Effects visually bound to moving objects (aura around an enemy, buff on player).
///
/// - Use ReleaseAll(anchor) when:
///   * An entity dies and you need to clear all visuals attached to it.
///   * A state ends and you want to remove all its related effects.
///
/// - If an effect has custom state (timers, toggles, etc.), implement IEffectPoolListener
///   and correctly reset state in OnEffectSpawned / OnEffectReleased.
///
/// ====================================================================================
/// </summary>
internal static class EffectSystem_Documentation { }
