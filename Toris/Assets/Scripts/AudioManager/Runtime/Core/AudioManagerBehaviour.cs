using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManagerBehaviour : MonoBehaviour
{
    private static AudioManagerBehaviour activeInstance;

    [Header("Libraries")]
    [SerializeField] private SfxLibrary sfxLibrary;
    [SerializeField] private MusicLibrary musicLibrary;

    [Header("SFX Pool")]
    [SerializeField] private int initialSfxVoices = 32;
    [SerializeField] private AudioMixerGroup defaultSfxMixerGroup;

    private IAudioRuntimeTick[] ticks;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        activeInstance = this;
        DontDestroyOnLoad(gameObject);

        AudioBootstrap.Initialize(
            owner: gameObject,
            sfxLibrary: sfxLibrary,
            musicLibrary: musicLibrary,
            initialSfxVoices: initialSfxVoices,
            defaultSfxMixerGroup: defaultSfxMixerGroup);

        ticks = AudioBootstrap.RuntimeTicks;
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
}
