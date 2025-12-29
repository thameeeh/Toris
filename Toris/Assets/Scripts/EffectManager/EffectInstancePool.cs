using System.Collections;
using UnityEngine;

public sealed class EffectInstancePool : MonoBehaviour
{
    //[Tooltip("Lifetime in seconds for one-shot effects before auto-release.")]
    //[SerializeField]
    //private float oneShotLifetime = 1.0f;
    private EffectRuntimePool _runtime;
    private EffectHandle _handle;
    private bool _isOneShot;

    public void Initialize(EffectRuntimePool runtime, EffectHandle handle, bool isOneShot)
    {
        if (_handle.IsValid && _handle != handle)
        {
            if (_runtime != null)
                _runtime.Release(_handle);
        }
        _runtime = runtime;
        _handle = handle;
        _isOneShot = isOneShot;
    }

    public void OnEffectFinished()
    {
        if (_runtime != null && _handle.IsValid)
            _runtime.Release(_handle);
    }

    public void OnEffectSpawned()
    {
        // Can add extra per-spawn reset if needed.
    }

    public void OnEffectReleased()
    {
        _runtime = null;
        _handle = EffectHandle.Invalid;
    }
}
