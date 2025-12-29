using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioVoicePool : IAudioRuntimeTick
{
    private sealed class VoiceRecord
    {
        public int handleId;
        public AudioSource source;
        public Transform followTarget;
        public Vector3 followOffset;
        public string sfxId;
        public float startUnscaledTime;
        public bool isLooping;
        public float requestedFadeOutSeconds;
        public float fadeOutRemaining;
        public float fadeOutStartVolume;
        public float fadeOutTargetVolume;
    }

    private readonly List<VoiceRecord> allVoices = new List<VoiceRecord>();
    private readonly List<VoiceRecord> activeVoices = new List<VoiceRecord>();
    private readonly Queue<VoiceRecord> freeVoices = new Queue<VoiceRecord>();

    private readonly Dictionary<string, int> activeCountBySfxId = new Dictionary<string, int>();
    private readonly Dictionary<string, float> lastPlayTimeBySfxId = new Dictionary<string, float>();

    private readonly System.Random random = new System.Random();

    private int nextHandleId = 1;
    private float currentUnscaledTime;

    public AudioVoicePool(GameObject owner, int initialVoiceCount, AudioMixerGroup defaultMixerGroup)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (initialVoiceCount < 1) initialVoiceCount = 1;

        currentUnscaledTime = Time.unscaledTime;

        for (int i = 0; i < initialVoiceCount; i++)
        {
            var child = new GameObject($"AudioVoice_{i:00}");
            child.transform.SetParent(owner.transform, false);

            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.minDistance = 1f;
            source.maxDistance = 25f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;

            if (defaultMixerGroup != null)
                source.outputAudioMixerGroup = defaultMixerGroup;

            var record = new VoiceRecord
            {
                handleId = 0,
                source = source,
                followTarget = null,
                followOffset = Vector3.zero,
                sfxId = null,
                startUnscaledTime = 0f,
                isLooping = false,
                requestedFadeOutSeconds = 0f,

                fadeOutRemaining = 0f,
                fadeOutStartVolume = 0f,
                fadeOutTargetVolume = 0f
            };

            allVoices.Add(record);
            freeVoices.Enqueue(record);
        }
    }
    public bool TryPlayOneShot(
        SfxDefinition definition,
        Vector3 worldPosition,
        SfxPlayRequest request,
        out AudioVoiceHandle handle)
    {
        handle = AudioVoiceHandle.Invalid;

        if (definition == null) return false;
        if (!definition.HasAnyClips) return false;

        if (!CanPlay(definition)) return false;

        if (!TryGetVoiceFor(definition, out VoiceRecord voice)) return false;

        AudioClip clip = definition.PickClip(random);
        if (clip == null)
        {
            ReleaseVoice(voice);
            return false;
        }

        ConfigureSourceFromDefinition(voice.source, definition, request);
        voice.source.transform.position = request.explicitWorldPosition ?? worldPosition;
        voice.source.clip = clip;
        voice.source.loop = false;

        voice.handleId = nextHandleId++;
        voice.sfxId = definition.Id;
        voice.startUnscaledTime = currentUnscaledTime;
        voice.isLooping = false;

        voice.followTarget = null;
        voice.followOffset = Vector3.zero;

        voice.requestedFadeOutSeconds = 0f;
        voice.fadeOutRemaining = 0f;

        activeVoices.Add(voice);
        IncrementActiveCount(definition.Id);

        voice.source.Play();

        handle = new AudioVoiceHandle(voice.handleId);
        return true;
    }
    public bool TryPlayAttached(
        SfxDefinition definition,
        Transform followTarget,
        Vector3 localOffset,
        SfxPlayRequest request,
        out AudioVoiceHandle handle)
    {
        handle = AudioVoiceHandle.Invalid;

        if (definition == null) return false;
        if (followTarget == null) return false;
        if (!definition.HasAnyClips) return false;

        if (!CanPlay(definition)) return false;

        if (!TryGetVoiceFor(definition, out VoiceRecord voice)) return false;

        AudioClip clip = definition.PickClip(random);
        if (clip == null)
        {
            ReleaseVoice(voice);
            return false;
        }

        ConfigureSourceFromDefinition(voice.source, definition, request);

        voice.followTarget = followTarget;
        voice.followOffset = localOffset;

        Vector3 startPosition = followTarget.TransformPoint(localOffset);
        voice.source.transform.position = request.explicitWorldPosition ?? startPosition;

        voice.source.clip = clip;
        voice.source.loop = false;

        voice.handleId = nextHandleId++;
        voice.sfxId = definition.Id;
        voice.startUnscaledTime = currentUnscaledTime;
        voice.isLooping = false;

        voice.requestedFadeOutSeconds = 0f;
        voice.fadeOutRemaining = 0f;

        activeVoices.Add(voice);
        IncrementActiveCount(definition.Id);

        voice.source.Play();

        handle = new AudioVoiceHandle(voice.handleId);
        return true;
    }
    public bool TryPlayLoop(
        SfxDefinition definition,
        Vector3 worldPosition,
        SfxPlayRequest request,
        out AudioVoiceHandle handle)
    {
        handle = AudioVoiceHandle.Invalid;

        if (definition == null) return false;
        if (!definition.HasAnyClips) return false;

        if (!CanPlay(definition)) return false;

        if (!TryGetVoiceFor(definition, out VoiceRecord voice)) return false;

        AudioClip clip = definition.PickClip(random);
        if (clip == null)
        {
            ReleaseVoice(voice);
            return false;
        }

        ConfigureSourceFromDefinition(voice.source, definition, request);
        voice.source.transform.position = request.explicitWorldPosition ?? worldPosition;

        voice.source.clip = clip;
        voice.source.loop = true;

        voice.handleId = nextHandleId++;
        voice.sfxId = definition.Id;
        voice.startUnscaledTime = currentUnscaledTime;
        voice.isLooping = true;

        voice.followTarget = null;
        voice.followOffset = Vector3.zero;

        voice.requestedFadeOutSeconds = 0f;
        voice.fadeOutRemaining = 0f;

        activeVoices.Add(voice);
        IncrementActiveCount(definition.Id);

        voice.source.Play();

        handle = new AudioVoiceHandle(voice.handleId);
        return true;
    }
    public bool TryPlayAttachedLoop(
    SfxDefinition definition,
    Transform followTarget,
    Vector3 localOffset,
    SfxPlayRequest request,
    out AudioVoiceHandle handle)
    {
        handle = AudioVoiceHandle.Invalid;

        if (definition == null) return false;
        if (followTarget == null) return false;
        if (!definition.HasAnyClips) return false;

        if (!CanPlay(definition)) return false;

        if (!TryGetVoiceFor(definition, out VoiceRecord voice)) return false;

        AudioClip clip = definition.PickClip(random);
        if (clip == null)
        {
            ReleaseVoice(voice);
            return false;
        }

        ConfigureSourceFromDefinition(voice.source, definition, request);

        voice.followTarget = followTarget;
        voice.followOffset = localOffset;

        Vector3 startPosition = followTarget.TransformPoint(localOffset);
        voice.source.transform.position = request.explicitWorldPosition ?? startPosition;

        voice.source.clip = clip;
        voice.source.loop = true;

        voice.handleId = nextHandleId++;
        voice.sfxId = definition.Id;
        voice.startUnscaledTime = currentUnscaledTime;
        voice.isLooping = true;

        voice.requestedFadeOutSeconds = 0f;
        voice.fadeOutRemaining = 0f;

        activeVoices.Add(voice);
        IncrementActiveCount(definition.Id);

        voice.source.Play();

        handle = new AudioVoiceHandle(voice.handleId);
        return true;
    }

    public bool TryStop(AudioVoiceHandle handle, float fadeOutSeconds)
    {
        if (!handle.IsValid) return false;

        for (int i = 0; i < activeVoices.Count; i++)
        {
            VoiceRecord voice = activeVoices[i];
            if (voice.handleId != handle.id) continue;

            if (fadeOutSeconds <= 0f)
            {
                ReleaseVoice(voice);
                return true;
            }

            voice.requestedFadeOutSeconds = fadeOutSeconds;
            voice.fadeOutRemaining = fadeOutSeconds;
            voice.fadeOutStartVolume = voice.source.volume;
            voice.fadeOutTargetVolume = 0f;

            return true;
        }

        return false;
    }
    public void Tick(float unscaledDeltaTime)
    {
        if (unscaledDeltaTime < 0f) unscaledDeltaTime = 0f;
        currentUnscaledTime += unscaledDeltaTime;

        for (int i = activeVoices.Count - 1; i >= 0; i--)
        {
            VoiceRecord voice = activeVoices[i];
            AudioSource source = voice.source;

            // Follow target (attached sounds)
            if (voice.followTarget != null)
            {
                Vector3 followPosition = voice.followTarget.TransformPoint(voice.followOffset);
                source.transform.position = followPosition;
            }

            // Fade out (if requested)
            if (voice.fadeOutRemaining > 0f)
            {
                voice.fadeOutRemaining -= unscaledDeltaTime;

                float t = 1f - Mathf.Clamp01(voice.fadeOutRemaining / Mathf.Max(0.0001f, voice.requestedFadeOutSeconds));
                source.volume = Mathf.Lerp(voice.fadeOutStartVolume, voice.fadeOutTargetVolume, t);

                if (voice.fadeOutRemaining <= 0f)
                {
                    ReleaseVoice(voice);
                    continue;
                }
            }

            // Auto release finished one-shots
            if (!voice.isLooping && !source.isPlaying)
            {
                ReleaseVoice(voice);
                continue;
            }
        }
    }
    private bool CanPlay(SfxDefinition definition)
    {
        string id = definition.Id;
        if (string.IsNullOrWhiteSpace(id)) return false;

        // Cooldown
        if (definition.CooldownSeconds > 0f &&
            lastPlayTimeBySfxId.TryGetValue(id, out float lastTime))
        {
            float elapsed = currentUnscaledTime - lastTime;
            if (elapsed < definition.CooldownSeconds)
                return false;
        }

        // Per-sound concurrency (max instances)
        if (definition.MaxSimultaneousInstances > 0 &&
            activeCountBySfxId.TryGetValue(id, out int activeCount))
        {
            if (activeCount >= definition.MaxSimultaneousInstances)
            {
                if (definition.StealMode == SfxDefinition.VoiceStealMode.DropNew)
                    return false;

                // Allow attempt; TryGetVoiceFor will steal within this id.
            }
        }

        return true;
    }
    private bool TryGetVoiceFor(SfxDefinition definition, out VoiceRecord voice)
    {
        voice = null;

        string id = definition.Id;
        if (string.IsNullOrWhiteSpace(id)) return false;

        // First: enforce per-sfx max instances by stealing within the same id if needed.
        if (definition.MaxSimultaneousInstances > 0 &&
            activeCountBySfxId.TryGetValue(id, out int activeCount) &&
            activeCount >= definition.MaxSimultaneousInstances)
        {
            VoiceRecord candidate = FindStealCandidateWithinSameId(id, definition.StealMode);
            if (candidate == null) return false;

            ReleaseVoice(candidate);
        }

        // If we have a free voice, use it.
        if (freeVoices.Count > 0)
        {
            voice = freeVoices.Dequeue();
            lastPlayTimeBySfxId[id] = currentUnscaledTime; // only commit cooldown on success
            return true;
        }

        // Pool is full: apply steal mode globally.
        if (definition.StealMode == SfxDefinition.VoiceStealMode.DropNew)
            return false;

        VoiceRecord globalCandidate = FindStealCandidateGlobal(definition.StealMode);
        if (globalCandidate == null) return false;

        ReleaseVoice(globalCandidate);

        if (freeVoices.Count == 0) return false;

        voice = freeVoices.Dequeue();
        lastPlayTimeBySfxId[id] = currentUnscaledTime; // only commit cooldown on success
        return true;
    }

    private void ReleaseVoice(VoiceRecord voice)
    {
        if (voice == null) return;

        activeVoices.Remove(voice);

        if (!string.IsNullOrWhiteSpace(voice.sfxId))
            DecrementActiveCount(voice.sfxId);

        if (voice.source != null)
        {
            voice.source.Stop();
            voice.source.clip = null;
            voice.source.loop = false;
        }

        voice.handleId = 0;
        voice.sfxId = null;
        voice.startUnscaledTime = 0f;
        voice.isLooping = false;

        voice.followTarget = null;
        voice.followOffset = Vector3.zero;

        voice.requestedFadeOutSeconds = 0f;
        voice.fadeOutRemaining = 0f;
        voice.fadeOutStartVolume = 0f;
        voice.fadeOutTargetVolume = 0f;

        freeVoices.Enqueue(voice);
    }
    private void ConfigureSourceFromDefinition(AudioSource source, SfxDefinition definition, SfxPlayRequest request)
    {
        if (definition.OutputMixerGroup != null)
            source.outputAudioMixerGroup = definition.OutputMixerGroup;

        source.spatialBlend = request.force2D ? 0f : definition.SpatialBlend;
        source.minDistance = definition.MinDistance;
        source.maxDistance = definition.MaxDistance;

        float baseVolume = UnityEngine.Random.Range(definition.VolumeMin, definition.VolumeMax);
        float basePitch = UnityEngine.Random.Range(definition.PitchMin, definition.PitchMax);

        float volume = baseVolume * Mathf.Max(0f, request.volumeMultiplier);
        float pitch = (basePitch * request.pitchMultiplier) + request.pitchOffset;

        source.volume = Mathf.Clamp(volume, 0f, 2f);
        source.pitch = Mathf.Clamp(pitch, -3f, 3f);
    }

    private void IncrementActiveCount(string sfxId)
    {
        if (string.IsNullOrWhiteSpace(sfxId)) return;

        if (!activeCountBySfxId.TryGetValue(sfxId, out int count))
            count = 0;

        activeCountBySfxId[sfxId] = count + 1;
    }

    private void DecrementActiveCount(string sfxId)
    {
        if (string.IsNullOrWhiteSpace(sfxId)) return;

        if (!activeCountBySfxId.TryGetValue(sfxId, out int count))
            return;

        count -= 1;
        if (count <= 0) activeCountBySfxId.Remove(sfxId);
        else activeCountBySfxId[sfxId] = count;
    }

    private VoiceRecord FindStealCandidateWithinSameId(string sfxId, SfxDefinition.VoiceStealMode stealMode)
    {
        VoiceRecord best = null;

        for (int i = 0; i < activeVoices.Count; i++)
        {
            VoiceRecord voice = activeVoices[i];
            if (voice == null) continue;
            if (voice.sfxId != sfxId) continue;

            if (best == null)
            {
                best = voice;
                continue;
            }

            if (stealMode == SfxDefinition.VoiceStealMode.StealQuietest)
            {
                if (voice.source != null && best.source != null && voice.source.volume < best.source.volume)
                    best = voice;
            }
            else // StealOldest
            {
                if (voice.startUnscaledTime < best.startUnscaledTime)
                    best = voice;
            }
        }

        return best;
    }
    private VoiceRecord FindStealCandidateGlobal(SfxDefinition.VoiceStealMode stealMode)
    {
        VoiceRecord best = null;

        for (int i = 0; i < activeVoices.Count; i++)
        {
            VoiceRecord voice = activeVoices[i];
            if (voice == null) continue;

            if (best == null)
            {
                best = voice;
                continue;
            }

            if (stealMode == SfxDefinition.VoiceStealMode.StealQuietest)
            {
                if (voice.source != null && best.source != null && voice.source.volume < best.source.volume)
                    best = voice;
            }
            else // StealOldest
            {
                if (voice.startUnscaledTime < best.startUnscaledTime)
                    best = voice;
            }
        }

        return best;
    }

}
