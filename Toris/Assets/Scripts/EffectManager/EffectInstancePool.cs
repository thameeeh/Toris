using System.Collections;
using UnityEngine;

/// <summary>
/// Component added to pooled effect instances.
/// Handles auto-release for one-shot effects or manual release for persistent ones.
/// </summary>
public sealed class EffectInstancePool : MonoBehaviour
{
    [Tooltip("Lifetime in seconds for one-shot effects before auto-release.")]
    [SerializeField]
    private float oneShotLifetime = 1.0f;

    private EffectRuntimePool _runtime;
    private EffectHandle _handle;
    private bool _isOneShot;

    public void Initialize(EffectRuntimePool runtime, EffectHandle handle, bool isOneShot)
    {
        _runtime = runtime;
        _handle = handle;
        _isOneShot = isOneShot;

        StopAllCoroutines();

        if (_isOneShot && oneShotLifetime > 0f)
            StartCoroutine(AutoRelease());
    }

    private IEnumerator AutoRelease()
    {
        yield return new WaitForSeconds(oneShotLifetime);

        if (_runtime != null && _handle.IsValid)
            _runtime.Release(_handle);
    }

    // For animation events ("OnFinish" for example)
    public void OnEffectFinished()
    {
        if (_runtime != null && _handle.IsValid)
            _runtime.Release(_handle);
    }
}
