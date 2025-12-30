/// <summary>
/// ====================================================================================
/// AUDIO SYSTEM DESIGN DOCUMENT
/// ====================================================================================
///
/// 1. CORE ARCHITECTURE OVERVIEW
/// -----------------------------
///
/// The Audio System provides runtime sound effect and music playback using pooling,
/// data-driven definitions, and centralized control.
///
/// --- MAIN COMPONENTS ---
///
/// - AudioManagerBehaviour (MonoBehaviour):
///   Scene-level entry point.
///   * Lives in the initial scene (optionally DontDestroyOnLoad).
///   * Owns the lifetime of the entire audio system.
///   * Calls AudioBootstrap.Initialize() in Awake().
///   * Drives runtime audio logic by ticking registered systems each frame
///     using unscaled delta time.
///
/// - AudioBootstrap (static):
///   Initialization utility.
///   * Constructs the core audio runtime objects.
///   * Exposes static access points:
///       AudioBootstrap.Sfx
///       AudioBootstrap.Music
///   * Registers runtime systems that require per-frame ticking.
///
/// - IAudioRuntimeTick:
///   Small interface implemented by runtime systems that need Update-like behavior.
///   * void Tick(float unscaledDeltaTime)
///
/// - SfxManager (implements ISfxManager):
///   High-level SFX API.
///   * Used by gameplay-facing code.
///   * Resolves SfxDefinition by ID.
///   * Forwards requests to AudioVoicePool.
///   * Returns AudioVoiceHandle for runtime control.
///   Gameplay never interacts with AudioSources directly.
///
/// - AudioVoicePool (implements IAudioRuntimeTick):
///   Core runtime responsible for:
///   * AudioSource pooling.
///   * Voice acquisition and release.
///   * Concurrency limits.
///   * Cooldowns.
///   * Voice stealing.
///   * Looping and attached playback.
///   * Fade-in / fade-out behavior.
///
/// - AudioVoiceHandle:
///   Lightweight value-type identifier for a playing voice.
///   * Returned by play calls.
///   * Used to stop or fade sounds.
///   * Becomes invalid once the voice is released.
///
/// - SfxDefinition (ScriptableObject):
///   Serializable data describing how an SFX behaves:
///   * ID
///   * AudioClips
///   * Mixer routing
///   * Volume / pitch ranges
///   * Spatial settings
///   * Concurrency limits
///   * Voice stealing mode
///   * Cooldown duration
///
/// - SfxLibrary (ScriptableObject):
///   Lookup asset mapping SFX IDs to SfxDefinitions.
///   * Builds a dictionary for fast lookup.
///   * Validates duplicate IDs.
///
/// - MusicManager (implements IMusicManager, IAudioRuntimeTick):
///   Runtime music controller.
///   * Uses two AudioSources for crossfading.
///   * Supports play, stop, and smooth transitions.
///
/// - MusicDefinition / MusicLibrary:
///   Data equivalents of SfxDefinition and SfxLibrary for music playback.
///
/// - SFX Hubs (e.g., PlayerSfx):
///   Actor-level MonoBehaviours.
///   * Subscribe to gameplay events.
///   * Own runtime audio state (loop handles).
///   * Forward events to ScriptableObject SFX modules.
///
/// - SFX Modules (ScriptableObjects):
///   Stateless assets that:
///   * Decide which sounds to play in response to events.
///   * Contain IDs and configuration.
///   * Do not store runtime state.
///
/// ====================================================================================
/// 2. STARTUP SEQUENCE
/// -----------------------------
///
/// 1) Scene loads with:
///    - AudioManagerBehaviour present in the hierarchy.
///    - SfxLibrary and MusicLibrary assigned via inspector.
///    - Optional AudioMixerGroups assigned.
///
/// 2) AudioManagerBehaviour.Awake():
///    - Ensures a single active instance.
///    - Calls AudioBootstrap.Initialize(...).
///
/// 3) AudioBootstrap.Initialize():
///    - Creates AudioVoicePool:
///        * Instantiates N child GameObjects under the owner.
///        * Adds AudioSource to each.
///        * Configures spatial defaults.
///        * Enqueues all voices into the free pool.
///    - Creates SfxManager with:
///        * SfxLibrary
///        * AudioVoicePool
///    - Creates MusicManager with:
///        * MusicLibrary
///        * Owner GameObject
///    - Registers AudioVoicePool and MusicManager as runtime tick targets.
///
/// 4) AudioManagerBehaviour stores runtime tick list.
///
/// Result:
///    - AudioBootstrap.Sfx and AudioBootstrap.Music are valid.
///    - Audio system is fully initialized before gameplay logic runs.
///
/// ====================================================================================
/// 3. RUNTIME TICKING
/// -----------------------------
///
/// 1) AudioManagerBehaviour.Update():
///    - Retrieves Time.unscaledDeltaTime.
///    - Calls Tick(unscaledDeltaTime) on all registered runtime systems.
///
/// Systems ticked:
///    - AudioVoicePool
///    - MusicManager
///
/// This guarantees:
///    - Audio fades and cleanup continue during pause or slow-motion.
///    - No audio logic depends on Time.timeScale.
///
/// ====================================================================================
/// 4. ONE-SHOT SFX PLAYBACK
/// -----------------------------
///
/// 1) Gameplay (via SFX hub) calls:
///       AudioBootstrap.Sfx.Play("Sfx_Id", request);
///
/// 2) SfxManager.Play():
///    - Calls SfxLibrary.TryGet(id, out SfxDefinition).
///    - If not found, returns AudioVoiceHandle.Invalid.
///    - Forwards request to AudioVoicePool.TryPlayOneShot().
///
/// 3) AudioVoicePool.TryPlayOneShot():
///    - Validates:
///        * Definition exists.
///        * At least one AudioClip is present.
///        * Cooldown has elapsed.
///    - Checks per-SFX concurrency limit.
///    - Attempts to acquire a voice:
///        * Uses free voice if available.
///        * Otherwise applies voice stealing rules.
///    - Configures AudioSource:
///        * Output mixer group.
///        * Spatial settings.
///        * Randomized volume and pitch.
///    - Assigns a unique handle ID.
///    - Registers voice as active.
///    - Calls AudioSource.Play().
///
/// 4) AudioVoiceHandle is returned to the caller.
///
/// 5) During AudioVoicePool.Tick():
///    - Non-looping voices are monitored.
///    - When AudioSource.isPlaying == false:
///        * Voice is released back to the pool.
///
/// ====================================================================================
/// 5. ATTACHED AND LOOPING SFX
/// -----------------------------
///
/// 1) Gameplay hub determines a looping condition (e.g., movement).
///
/// 2) Hub calls:
///       AudioBootstrap.Sfx.PlayAttachedLoop(...);
///
/// 3) SfxManager forwards call to AudioVoicePool.TryPlayAttachedLoop().
///
/// 4) AudioVoicePool.TryPlayAttachedLoop():
///    - Same acquisition and validation as one-shot playback.
///    - Sets:
///        * AudioSource.loop = true
///        * VoiceRecord.isLooping = true
///        * followTarget and followOffset
///    - Registers voice as active.
///    - Starts playback.
///
/// 5) During Tick():
///    - Voice position is updated each frame from followTarget.
///    - Loop continues indefinitely.
///
/// 6) Hub stops loop:
///       AudioBootstrap.Sfx.Stop(handle, fadeOutSeconds);
///
/// 7) AudioVoicePool.TryStop():
///    - Initializes fade-out state.
///    - Gradually interpolates volume.
///    - On completion:
///        * Voice is released back to the pool.
///
/// ====================================================================================
/// 6. VOICE STEALING AND CONCURRENCY
/// -----------------------------
///
/// - Each SfxDefinition can specify:
///   * MaxSimultaneousInstances
///   * VoiceStealMode:
///       - DropNew
///       - StealOldest
///       - StealQuietest
///
/// - When concurrency limit is reached:
///   * DropNew: new request is ignored.
///   * StealOldest: oldest active voice is released.
///   * StealQuietest: quietest active voice is released.
///
/// Cooldown enforcement:
///   - Each SFX ID tracks last successful play time.
///   - Requests during cooldown are rejected.
///
/// ====================================================================================
/// 7. MUSIC PLAYBACK AND CROSSFADE
/// -----------------------------
///
/// 1) Gameplay calls:
///       AudioBootstrap.Music.Play("Music_Id");
///
/// 2) MusicManager.Play():
///    - Resolves MusicDefinition via MusicLibrary.
///    - If the same clip is already playing, returns.
///    - Configures inactive AudioSource:
///        * Assigns clip.
///        * Sets output mixer group.
///        * Sets volume = 0.
///        * Starts playback.
///    - Initializes fade-in and fade-out timers.
///
/// 3) During MusicManager.Tick():
///    - Active source fades out.
///    - Inactive source fades in.
///    - When fade completes:
///        * Sources are swapped.
///        * Old source is stopped and cleared.
///
/// 4) MusicManager.Stop():
///    - Fades out active source.
///    - On completion, stops playback and clears clip.
///
/// ====================================================================================
/// 8. SFX HUB AND MODULE DISPATCH
/// -----------------------------
///
/// 1) Actor prefab contains a single SFX hub (e.g., PlayerSfx).
///
/// 2) Hub.OnEnable():
///    - Subscribes to gameplay events (bow, dash, movement, etc.).
///    - Initializes SFX modules.
///
/// 3) Gameplay event occurs.
///
/// 4) Hub receives event and forwards it to all modules:
///       module.OnEvent(context);
///
/// 5) Module decides:
///    - Which SFX ID to play.
///    - Whether the sound is one-shot or looping.
///
/// 6) Hub owns all runtime state:
///    - AudioVoiceHandles.
///    - Loop lifetime.
///    - Fade-out control.
///
/// ScriptableObject modules remain stateless and reusable.
///
/// ====================================================================================
/// 9. AUTHORING WORKFLOW
/// -----------------------------
///
/// 1) Import audio clips into the project.
///
/// 2) Create SfxDefinition or MusicDefinition:
///    - Assign clips.
///    - Configure mixer routing and behavior.
///
/// 3) Add definitions to the appropriate Library asset.
///
/// 4) Reference SFX IDs from SFX modules.
///
/// 5) Gameplay triggers audio exclusively via hubs and managers.
///
/// No gameplay script references AudioSources or prefabs directly.
///
/// ====================================================================================
/// </summary>
internal static class AudioSystem_Documentation { }
