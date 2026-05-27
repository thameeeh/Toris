using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManagerBehaviour : MonoBehaviour
{
    private static AudioManagerBehaviour activeInstance;

    [Header("Libraries")]
    [SerializeField] private SfxLibrary sfxLibrary;
    [SerializeField] private MusicLibrary musicLibrary;

    [Header("Events")]
    [SerializeField] private UIEventsSO uiEvents;

    [Header("SFX Pool")]
    [SerializeField] private int initialSfxVoices = 32;
    [SerializeField] private AudioMixerGroup defaultSfxMixerGroup;

    private IAudioRuntimeTick[] ticks;
    private bool uiEventsBound;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        activeInstance = this;
        DontDestroyOnLoad(gameObject);

        AudioVolumeSettings.Load();

        AudioBootstrap.Initialize(
            owner: gameObject,
            sfxLibrary: sfxLibrary,
            musicLibrary: musicLibrary,
            initialSfxVoices: initialSfxVoices,
            defaultSfxMixerGroup: defaultSfxMixerGroup);

        ticks = AudioBootstrap.RuntimeTicks;
    }

    private void OnEnable()
    {
        if (activeInstance == this && uiEvents != null)
        {
            uiEvents.OnGameplaySfxPauseChanged += HandleGameplaySfxPauseChanged;
            uiEventsBound = true;
        }
    }

    private void OnDisable()
    {
        if (uiEventsBound && uiEvents != null)
        {
            uiEvents.OnGameplaySfxPauseChanged -= HandleGameplaySfxPauseChanged;
            uiEventsBound = false;
        }

        if (activeInstance == this)
            AudioBootstrap.Sfx?.SetGameplayPaused(false);
    }

    private void Update()
    {
        float unscaledDeltaTime = Time.unscaledDeltaTime;

        if (ticks == null) return;

        for (int i = 0; i < ticks.Length; i++)
        {
            ticks[i].Tick(unscaledDeltaTime);
        }
    }

    private void OnApplicationQuit()
    {
        AudioVolumeSettings.Save();
    }

    private void HandleGameplaySfxPauseChanged(bool paused)
    {
        // SFX-only bridge: UI pause state suspends gameplay voices without silencing menu feedback.
        AudioBootstrap.Sfx?.SetGameplayPaused(paused);
    }
}
