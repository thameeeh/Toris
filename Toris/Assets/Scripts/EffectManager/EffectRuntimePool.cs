using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class EffectRuntimePool : IEffectRuntime, IEffectRuntimeTick
{
    private readonly Transform _root;

    private readonly Dictionary<GameObject, EffectHandle> _activeByGameObject = new();
    private readonly Dictionary<EffectHandle, float> _oneShotRemainingSeconds = new();

    private readonly List<EffectHandle> _oneShotHandlesToRelease = new();
    private readonly List<EffectHandle> _oneShotHandlesToIterate = new();
    private readonly List<EffectHandle> _oneShotHandlesToUpdate = new();
    private readonly List<float> _oneShotUpdatedSeconds = new();

    private sealed class EffectPool
    {
        public readonly EffectDefinition Definition;
        public readonly Transform PoolRoot;
        public readonly Stack<GameObject> Inactive = new();

        public int TotalCreated;
        public EffectPool(EffectDefinition definition, Transform parent)
        {
            Definition = definition;

            var poolGo = new GameObject($"Pool_{definition.Id}");
            poolGo.transform.SetParent(parent, false);
            PoolRoot = poolGo.transform;
        }
    }

    private struct ActiveInstance
    {
        public GameObject GameObject;
        public EffectDefinition Definition;
        public Transform Anchor;
        public bool IsAttached;
    }

    private readonly Dictionary<EffectDefinition, EffectPool> _pools = new();
    private readonly Dictionary<EffectHandle, ActiveInstance> _active = new();
    private int _nextKey = 0;

    public EffectRuntimePool(Transform root = null)
    {
        _root = root != null ? root : CreateDefaultRoot();
    }

    private static Transform CreateDefaultRoot()
    {
        var go = new GameObject("EffectsRuntimePoolRoot");
        Object.DontDestroyOnLoad(go);
        return go.transform;
    }

    #region IEffectRuntime implementation
    public void Play(EffectDefinition definition, EffectRequest request)
    {
        if (request.Parent != null)
        {
            Debug.LogWarning(
                $"Effect '{definition.Id}' was played with EffectRequest.Parent set. " +
                $"Use PlayAttached(AttachedEffectRequest) instead."
            );
        }

        CleanupDestroyedAnchors();

        if (definition == null || definition.Prefab == null)
            return;

        var go = AcquireInstance(definition);
        if (go == null)
            return;

        var anchor = request.Parent;
        var handle = RegisterActive(definition, go, anchor, isAttached: anchor != null);

        ApplyTransform(go.transform, request.Position, request.Rotation, request.Parent);

        var inst = GetOrAddInstanceComponent(go);
        bool isOneShot = definition.Category == EffectCategory.OneShot;
        inst.Initialize(this, handle, isOneShot);

        if (isOneShot)
        {
            float lifetimeSeconds = definition.OneShotLifetimeSeconds;
            if (lifetimeSeconds > 0f)
            {
                _oneShotRemainingSeconds[handle] = lifetimeSeconds;
            }
        }

        ApplyEffectParameters(go, request.Variant, request.Magnitude);

        NotifySpawned(go);
    }
    public EffectHandle PlayPersistent(EffectDefinition definition, PersistentEffectRequest request)
    {
        CleanupDestroyedAnchors();

        if (definition == null || definition.Prefab == null)
            return EffectHandle.Invalid;

        var go = AcquireInstance(definition);
        if (go == null)
            return EffectHandle.Invalid;

        var anchor = request.Anchor;
        var parent = anchor != null ? anchor : _root;

        ApplyTransformAttached(go.transform, parent, request.LocalPosition, request.LocalRotation);

        var handle = RegisterActive(definition, go, anchor, isAttached: anchor != null);

        var inst = GetOrAddInstanceComponent(go);
        inst.Initialize(this, handle, isOneShot: false);

        ApplyEffectParameters(go, request.Variant, request.Magnitude);

        NotifySpawned(go);

        return handle;
    }
    public void PlayAttached(EffectDefinition definition, AttachedEffectRequest request)
    {
        if (definition == null || definition.Prefab == null)
            return;

        Transform anchor = request.Anchor;
        if (anchor == null)
        {
            Debug.LogWarning($"Effect '{definition.Id}' PlayAttached called with null Anchor. Skipping spawn.");
            return;
        }

        GameObject gameObject = AcquireInstance(definition);
        if (gameObject == null)
            return;

        ApplyTransformAttached(
            gameObject.transform,
            anchor,
            request.LocalPosition,
            request.LocalRotation
        );

        EffectHandle handle = RegisterActive(
            definition,
            gameObject,
            anchor,
            isAttached: true
        );

        var instanceComponent = GetOrAddInstanceComponent(gameObject);

        bool isOneShot = definition.Category == EffectCategory.OneShot;
        instanceComponent.Initialize(this, handle, isOneShot);

        if (isOneShot)
        {
            float lifetimeSeconds = definition.OneShotLifetimeSeconds;
            if (lifetimeSeconds > 0f)
            {
                _oneShotRemainingSeconds[handle] = lifetimeSeconds;
            }
        }

        ApplyEffectParameters(gameObject, request.Variant, request.Magnitude);

        NotifySpawned(gameObject);
    }

    public void Release(EffectHandle handle)
    {
        if (!handle.IsValid)
            return;

        _oneShotRemainingSeconds.Remove(handle);

        if (!_active.TryGetValue(handle, out var activeInstance))
            return;

        _active.Remove(handle);

        GameObject gameObject = activeInstance.GameObject;

        if (gameObject != null)
        {
            _activeByGameObject.Remove(gameObject);
        }

        EffectDefinition definition = activeInstance.Definition;
        if (definition == null || gameObject == null)
            return;

        EffectPool pool = GetOrCreatePool(definition);

        NotifyReleased(gameObject);

        Transform transform = gameObject.transform;
        transform.SetParent(pool.PoolRoot, false);
        gameObject.SetActive(false);

        int maxInactive = definition.MaxInactive;
        if (maxInactive > 0 && pool.Inactive.Count >= maxInactive)
        {
            Object.Destroy(gameObject);
            pool.TotalCreated = Mathf.Max(0, pool.TotalCreated - 1);
            return;
        }

        pool.Inactive.Push(gameObject);
    }

    public void ReleaseAll()
    {
        CleanupDestroyedAnchors();
        _oneShotRemainingSeconds.Clear();
        if (_active.Count == 0)
            return;

        var keys = new List<EffectHandle>(_active.Keys);
        foreach (var handle in keys)
        {
            Release(handle);
        }
    }
    public void ReleaseAll(Transform anchor)
    {
        CleanupDestroyedAnchors();

        if (anchor == null || _active.Count == 0)
            return;

        List<EffectHandle> handlesToRelease = new List<EffectHandle>();

        foreach (var entry in _active)
        {
            if (entry.Value.Anchor == anchor)
            {
                handlesToRelease.Add(entry.Key);
            }
        }

        for (int index = 0; index < handlesToRelease.Count; index++)
        {
            Release(handlesToRelease[index]);
        }
    }

    public void Prewarm(EffectDefinition definition, int count)
    {
        if (definition == null || definition.Prefab == null || count <= 0)
            return;

        if (count <= 0)
            return;

        var pool = GetOrCreatePool(definition);

        int maxPoolSize = definition.MaxPoolSize;
        int remainingCapacity = maxPoolSize > 0 ? Mathf.Max(0, maxPoolSize - pool.TotalCreated) : count;
        int instancesToCreate = Mathf.Min(count, remainingCapacity);

        for (int index = 0; index < instancesToCreate; index++)
        {
            var gameObject = Object.Instantiate(definition.Prefab, pool.PoolRoot);
            pool.TotalCreated++;
            gameObject.SetActive(false);
            pool.Inactive.Push(gameObject);
        }
    }
    #endregion
#region Internal helpers

    private EffectPool GetOrCreatePool(EffectDefinition definition)
    {
        if (!_pools.TryGetValue(definition, out var pool))
        {
            pool = new EffectPool(definition, _root);
            _pools.Add(definition, pool);
        }
        return pool;
    }

    private GameObject AcquireInstance(EffectDefinition definition)
    {
        var pool = GetOrCreatePool(definition);

        GameObject go;

        if (pool.Inactive.Count > 0)
        {
            go = pool.Inactive.Pop();
        }
        else
        {
            int maxPoolSize = definition.MaxPoolSize;
            if(maxPoolSize > 0 && pool.TotalCreated >= maxPoolSize)
            {
                Debug.LogWarning($"Effect pool '{definition.Id}' hit MaxPoolSize ({maxPoolSize}). Skipping spawn.");
                return null;
            }

            go = Object.Instantiate(definition.Prefab, pool.PoolRoot);
            pool.TotalCreated++;
        }

        go.SetActive(true);
        return go;
    }

    private EffectHandle RegisterActive(
        EffectDefinition definition,
        GameObject go,
        Transform anchor,
        bool isAttached)
    {
        if (go == null)
            return EffectHandle.Invalid;

        if (_activeByGameObject.TryGetValue(go, out var existing))
        {
            return existing;
        }

        _nextKey++;
        var handle = EffectHandle.FromKey(_nextKey);

        _active.Add(handle, new ActiveInstance
        {
            GameObject = go,
            Definition = definition,
            Anchor = anchor,
            IsAttached = isAttached
        });

        _activeByGameObject.Add(go, handle);
        return handle;
    }

    private static void ApplyTransform(Transform t, Vector3 pos, Quaternion rot, Transform parent)
    {
        if (parent != null)
        {
            t.SetParent(parent, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
        }
        else
        {
            t.SetParent(null, false);
        }

        t.position = pos;
        t.rotation = rot;
    }

    private static void ApplyTransformAttached(Transform t, Transform parent, Vector3 localPos, Quaternion localRot)
    {
        if (parent != null)
        {
            t.SetParent(parent, false);
        }

        t.localPosition = localPos;
        t.localRotation = localRot;
    }

    private static EffectInstancePool GetOrAddInstanceComponent(GameObject go)
    {
        if (!go.TryGetComponent(out EffectInstancePool inst))
            inst = go.AddComponent<EffectInstancePool>();

        return inst;
    }

    private static readonly List<IEffectParametersReceiver> _parametersReceiverBuffer = new();
    private static void ApplyEffectParameters(GameObject gameObject, EffectVariant variant, float magnitude)
    {
        if (gameObject == null)
            return;

        float safeMagnitude = Mathf.Max(0f, magnitude);

        gameObject.GetComponentsInChildren(true, _parametersReceiverBuffer);

        for (int index = 0; index < _parametersReceiverBuffer.Count; index++)
        {
            var receiver = _parametersReceiverBuffer[index];
            if (receiver == null)
                continue;

            receiver.ApplyEffectParameters(variant, safeMagnitude);
        }

        _parametersReceiverBuffer.Clear();
    }

    private static readonly List<IEffectPoolListener> _listenerBuffer = new();
    private static void NotifySpawned(GameObject go)
    {
        go.GetComponentsInChildren(true, _listenerBuffer);
        for (int i = 0; i < _listenerBuffer.Count; i++)
        {
            _listenerBuffer[i].OnEffectSpawned();
        }
        _listenerBuffer.Clear();
    }

    private static void NotifyReleased(GameObject go)
    {
        go.GetComponentsInChildren(true, _listenerBuffer);
        for (int i = 0; i < _listenerBuffer.Count; i++)
        {
            _listenerBuffer[i].OnEffectReleased();
        }
        _listenerBuffer.Clear();
    }
    private void CleanupDestroyedAnchors()
    {
        if (_active.Count == 0)
            return;

        List<EffectHandle> handlesToRelease = new List<EffectHandle>();

        foreach (var entry in _active)
        {
            ActiveInstance activeInstance = entry.Value;

            if (!activeInstance.IsAttached)
                continue;

            if (activeInstance.Anchor == null)
            {
                handlesToRelease.Add(entry.Key);
            }
        }

        for (int index = 0; index < handlesToRelease.Count; index++)
        {
            Release(handlesToRelease[index]);
        }
    }
    public void Tick(float deltaTimeSeconds)
    {
        if (_oneShotRemainingSeconds.Count == 0)
            return;

        _oneShotHandlesToRelease.Clear();
        _oneShotHandlesToUpdate.Clear();

        _oneShotHandlesToIterate.Clear();
        foreach (var handle in _oneShotRemainingSeconds.Keys)
        {
            _oneShotHandlesToIterate.Add(handle);
        }

        for (int index = 0; index < _oneShotHandlesToIterate.Count; index++)
        {
            EffectHandle handle = _oneShotHandlesToIterate[index];

            if (!_oneShotRemainingSeconds.TryGetValue(handle, out float remainingSeconds))
                continue;

            float updatedRemainingSeconds = remainingSeconds - deltaTimeSeconds;

            if (updatedRemainingSeconds <= 0f)
            {
                _oneShotHandlesToRelease.Add(handle);
            }
            else
            {
                _oneShotHandlesToUpdate.Add(handle);
                _oneShotUpdatedSeconds.Add(updatedRemainingSeconds);
            }
        }

        for (int index = 0; index < _oneShotHandlesToUpdate.Count; index++)
        {
            EffectHandle handle = _oneShotHandlesToUpdate[index];
            float updatedRemainingSeconds = _oneShotUpdatedSeconds[index];

            _oneShotRemainingSeconds[handle] = updatedRemainingSeconds;
        }

        for (int index = 0; index < _oneShotHandlesToRelease.Count; index++)
        {
            Release(_oneShotHandlesToRelease[index]);
        }

        _oneShotUpdatedSeconds.Clear();
    }

}
#endregion