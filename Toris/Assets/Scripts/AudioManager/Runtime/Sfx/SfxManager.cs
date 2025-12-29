using UnityEngine;

public interface ISfxManager
{
    AudioVoiceHandle Play(string id);
    AudioVoiceHandle Play(string id, SfxPlayRequest request);

    AudioVoiceHandle PlayAt(string id, Vector3 worldPosition);
    AudioVoiceHandle PlayAt(string id, Vector3 worldPosition, SfxPlayRequest request);

    AudioVoiceHandle PlayAttached(string id, Transform target);
    AudioVoiceHandle PlayAttached(string id, Transform target, Vector3 localOffset, SfxPlayRequest request);
    AudioVoiceHandle PlayAttachedLoop(string id, Transform target, Vector3 localOffset, SfxPlayRequest request);

    AudioVoiceHandle PlayLoop(string id, Vector3 worldPosition, SfxPlayRequest request);

    bool Stop(AudioVoiceHandle handle, float fadeOutSeconds = 0f);
}

public sealed class SfxManager : ISfxManager
{
    private readonly SfxLibrary library;
    private readonly AudioVoicePool voicePool;

    public SfxManager(SfxLibrary library, AudioVoicePool voicePool)
    {
        this.library = library;
        this.voicePool = voicePool;
    }

    public AudioVoiceHandle Play(string id) => Play(id, SfxPlayRequest.Default);

    public AudioVoiceHandle Play(string id, SfxPlayRequest request)
    {
        if (!TryGetDefinition(id, out SfxDefinition definition)) return AudioVoiceHandle.Invalid;

        Vector3 worldPosition = request.explicitWorldPosition ?? Vector3.zero;
        return PlayAtInternal(definition, worldPosition, request);
    }

    public AudioVoiceHandle PlayAt(string id, Vector3 worldPosition) => PlayAt(id, worldPosition, SfxPlayRequest.Default);

    public AudioVoiceHandle PlayAt(string id, Vector3 worldPosition, SfxPlayRequest request)
    {
        if (!TryGetDefinition(id, out SfxDefinition definition)) return AudioVoiceHandle.Invalid;
        return PlayAtInternal(definition, worldPosition, request);
    }

    public AudioVoiceHandle PlayAttached(string id, Transform target) =>
        PlayAttached(id, target, Vector3.zero, SfxPlayRequest.Default);

    public AudioVoiceHandle PlayAttached(string id, Transform target, Vector3 localOffset, SfxPlayRequest request)
    {
        if (target == null) return AudioVoiceHandle.Invalid;
        if (!TryGetDefinition(id, out SfxDefinition definition)) return AudioVoiceHandle.Invalid;

        if (voicePool.TryPlayAttached(definition, target, localOffset, request, out AudioVoiceHandle handle))
            return handle;

        return AudioVoiceHandle.Invalid;
    }
    public AudioVoiceHandle PlayAttachedLoop(string id, Transform target, Vector3 localOffset, SfxPlayRequest request)
{
    if (target == null) return AudioVoiceHandle.Invalid;
    if (!TryGetDefinition(id, out SfxDefinition definition)) return AudioVoiceHandle.Invalid;

    if (voicePool.TryPlayAttachedLoop(definition, target, localOffset, request, out AudioVoiceHandle handle))
        return handle;

    return AudioVoiceHandle.Invalid;
}

    public AudioVoiceHandle PlayLoop(string id, Vector3 worldPosition, SfxPlayRequest request)
    {
        if (!TryGetDefinition(id, out SfxDefinition definition)) return AudioVoiceHandle.Invalid;

        if (voicePool.TryPlayLoop(definition, worldPosition, request, out AudioVoiceHandle handle))
            return handle;

        return AudioVoiceHandle.Invalid;
    }

    public bool Stop(AudioVoiceHandle handle, float fadeOutSeconds = 0f) =>
        voicePool.TryStop(handle, fadeOutSeconds);

    private bool TryGetDefinition(string id, out SfxDefinition definition)
    {
        definition = null;
        if (library == null) return false;
        return library.TryGet(id, out definition);
    }

    private AudioVoiceHandle PlayAtInternal(SfxDefinition definition, Vector3 worldPosition, SfxPlayRequest request)
    {
        if (voicePool.TryPlayOneShot(definition, worldPosition, request, out AudioVoiceHandle handle))
            return handle;

        return AudioVoiceHandle.Invalid;
    }
}
