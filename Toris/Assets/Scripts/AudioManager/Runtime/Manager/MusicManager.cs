using UnityEngine;
using UnityEngine.Audio;

public interface IMusicManager
{
    void Play(string id, float? fadeInSeconds = null, float? fadeOutSeconds = null);
    void Stop(float fadeOutSeconds = 1.0f);
}

public sealed class MusicManager : IMusicManager, IAudioRuntimeTick
{
    private readonly MusicLibrary library;

    private readonly AudioSource sourceA;
    private readonly AudioSource sourceB;

    private AudioSource activeSource;
    private AudioSource inactiveSource;

    private float activeBaseVolume;
    private float inactiveBaseVolume;
    private float fadeTimeRemaining;
    private float fadeDuration;
    private float activeStartVolume;
    private float inactiveTargetVolume;
    private float fadeInDuration;
    private float fadeOutDuration;
    private bool isStopping;

    public MusicManager(MusicLibrary library, GameObject owner)
    {
        this.library = library;

        sourceA = owner.AddComponent<AudioSource>();
        sourceB = owner.AddComponent<AudioSource>();

        sourceA.loop = true;
        sourceB.loop = true;

        activeSource = sourceA;
        inactiveSource = sourceB;

        activeSource.volume = 0f;
        inactiveSource.volume = 0f;
        activeBaseVolume = 0f;
        inactiveBaseVolume = 0f;
    }
    public void Play(string id, float? fadeInSeconds = null, float? fadeOutSeconds = null)
    {
        if (library == null) return;
        if (!library.TryGet(id, out MusicDefinition definition)) return;
        if (definition == null) return;
        if (definition.Clip == null) return;

        // If already playing this clip, do nothing.
        if (activeSource.isPlaying && activeSource.clip == definition.Clip)
            return;

        fadeInDuration = Mathf.Max(0f, fadeInSeconds ?? definition.FadeInSeconds);
        fadeOutDuration = Mathf.Max(0f, fadeOutSeconds ?? definition.FadeOutSeconds);
        fadeDuration = Mathf.Max(0.0001f, Mathf.Max(fadeInDuration, fadeOutDuration));
        fadeTimeRemaining = fadeDuration;

        activeStartVolume = activeBaseVolume;
        inactiveTargetVolume = Mathf.Clamp(definition.Volume, 0f, 2f);

        // Configure inactive source
        inactiveSource.Stop();
        inactiveSource.clip = definition.Clip;
        inactiveSource.loop = true;
        inactiveBaseVolume = 0f;
        inactiveSource.volume = ResolveMusicOutputVolume(inactiveBaseVolume);

        if (definition.OutputMixerGroup != null)
            inactiveSource.outputAudioMixerGroup = definition.OutputMixerGroup;

        inactiveSource.Play();

        isStopping = false;
    }
    public void Stop(float fadeOutSeconds = 1.0f)
    {
        if (!activeSource.isPlaying)
            return;

        fadeInDuration = 0f;
        fadeOutDuration = Mathf.Max(0f, fadeOutSeconds);
        fadeDuration = Mathf.Max(0.0001f, fadeOutDuration);
        fadeTimeRemaining = fadeDuration;

        activeStartVolume = activeBaseVolume;
        inactiveTargetVolume = 0f;

        // Ensure inactive is not participating
        inactiveSource.Stop();
        inactiveSource.clip = null;
        inactiveBaseVolume = 0f;
        inactiveSource.volume = ResolveMusicOutputVolume(inactiveBaseVolume);

        isStopping = true;
    }
    public void Tick(float unscaledDeltaTime)
    {
        UpdateSourceVolumesFromSettings();

        if (fadeTimeRemaining <= 0f) return;

        if (unscaledDeltaTime < 0f) unscaledDeltaTime = 0f;
        fadeTimeRemaining -= unscaledDeltaTime;

        float tGlobal = 1f - Mathf.Clamp01(fadeTimeRemaining / Mathf.Max(0.0001f, fadeDuration));

        // Fade-out factor uses fadeOutDuration
        float tOut = fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(tGlobal * (fadeDuration / Mathf.Max(0.0001f, fadeOutDuration)));
        // Fade-in factor uses fadeInDuration
        float tIn = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(tGlobal * (fadeDuration / Mathf.Max(0.0001f, fadeInDuration)));

        // Always fade active down (stop or crossfade)
        activeBaseVolume = Mathf.Lerp(activeStartVolume, 0f, tOut);
        activeSource.volume = ResolveMusicOutputVolume(activeBaseVolume);

        if (!isStopping)
        {
            // Crossfade: fade inactive up
            inactiveBaseVolume = Mathf.Lerp(0f, inactiveTargetVolume, tIn);
            inactiveSource.volume = ResolveMusicOutputVolume(inactiveBaseVolume);
        }

        if (fadeTimeRemaining > 0f)
            return;

        // Finish
        activeSource.Stop();
        activeBaseVolume = 0f;
        activeSource.volume = ResolveMusicOutputVolume(activeBaseVolume);

        if (isStopping)
        {
            activeSource.clip = null;
            return;
        }

        // Swap sources: inactive becomes active
        var temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        activeBaseVolume = inactiveBaseVolume;
        inactiveBaseVolume = 0f;

        inactiveSource.Stop();
        inactiveSource.clip = null;
        inactiveSource.volume = ResolveMusicOutputVolume(inactiveBaseVolume);
    }

    private void UpdateSourceVolumesFromSettings()
    {
        activeSource.volume = ResolveMusicOutputVolume(activeBaseVolume);
        inactiveSource.volume = ResolveMusicOutputVolume(inactiveBaseVolume);
    }

    private static float ResolveMusicOutputVolume(float baseVolume)
    {
        return Mathf.Clamp(baseVolume * AudioVolumeSettings.EffectiveMusicVolume, 0f, 2f);
    }

}
