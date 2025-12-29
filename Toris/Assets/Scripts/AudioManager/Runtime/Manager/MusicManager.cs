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

        activeStartVolume = activeSource.volume;
        inactiveTargetVolume = Mathf.Clamp(definition.Volume, 0f, 2f);

        // Configure inactive source
        inactiveSource.Stop();
        inactiveSource.clip = definition.Clip;
        inactiveSource.loop = true;
        inactiveSource.volume = 0f;

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

        activeStartVolume = activeSource.volume;
        inactiveTargetVolume = 0f;

        // Ensure inactive is not participating
        inactiveSource.Stop();
        inactiveSource.clip = null;
        inactiveSource.volume = 0f;

        isStopping = true;
    }
    public void Tick(float unscaledDeltaTime)
    {
        if (fadeTimeRemaining <= 0f) return;

        if (unscaledDeltaTime < 0f) unscaledDeltaTime = 0f;
        fadeTimeRemaining -= unscaledDeltaTime;

        float tGlobal = 1f - Mathf.Clamp01(fadeTimeRemaining / Mathf.Max(0.0001f, fadeDuration));

        // Fade-out factor uses fadeOutDuration
        float tOut = fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(tGlobal * (fadeDuration / Mathf.Max(0.0001f, fadeOutDuration)));
        // Fade-in factor uses fadeInDuration
        float tIn = fadeInDuration <= 0f ? 1f : Mathf.Clamp01(tGlobal * (fadeDuration / Mathf.Max(0.0001f, fadeInDuration)));

        // Always fade active down (stop or crossfade)
        activeSource.volume = Mathf.Lerp(activeStartVolume, 0f, tOut);

        if (!isStopping)
        {
            // Crossfade: fade inactive up
            inactiveSource.volume = Mathf.Lerp(0f, inactiveTargetVolume, tIn);
        }

        if (fadeTimeRemaining > 0f)
            return;

        // Finish
        activeSource.Stop();
        activeSource.volume = 0f;

        if (isStopping)
        {
            activeSource.clip = null;
            return;
        }

        // Swap sources: inactive becomes active
        var temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;

        inactiveSource.Stop();
        inactiveSource.clip = null;
        inactiveSource.volume = 0f;
    }

}
