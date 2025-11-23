using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IEffectRuntime implementation that uses simple GameObject pooling
/// for all effect prefabs referenced by EffectDefinition.
/// </summary>
public sealed class EffectRuntimePool : IEffectRuntime
{
    private readonly Transform _root;

    // Pool per definition
    private sealed class EffectPool
    {
        public readonly EffectDefinition Definition;
        public readonly Transform PoolRoot;
        public readonly Stack<GameObject> Inactive = new();

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

    // ---- IEffectRuntime implementation ----

    public void Play(EffectDefinition definition, EffectRequest request)
    {
        if (definition == null || definition.Prefab == null)
            return;

        var go = AcquireInstance(definition);
        if (go == null)
            return;

        var handle = RegisterActive(definition, go, anchor: null, isAttached: request.Parent != null);

        ApplyTransform(go.transform, request.Position, request.Rotation, request.Parent);

        var inst = GetOrAddInstanceComponent(go);
        bool isOneShot = definition.Category == EffectCategory.OneShot;
        inst.Initialize(this, handle, isOneShot);
    }

    public EffectHandle PlayPersistent(EffectDefinition definition, PersistentEffectRequest request)
    {
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

        return handle;
    }

    public void Release(EffectHandle handle)
    {
        if (!handle.IsValid)
            return;

        if (!_active.TryGetValue(handle, out var instance))
            return;

        _active.Remove(handle);

        var def = instance.Definition;
        if (def == null || instance.GameObject == null)
            return;

        var pool = GetOrCreatePool(def);

        var go = instance.GameObject;
        var t = go.transform;

        t.SetParent(pool.PoolRoot, false);
        go.SetActive(false);

        pool.Inactive.Push(go);
    }

    public void ReleaseAll()
    {
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
        if (anchor == null || _active.Count == 0)
            return;

        var list = new List<EffectHandle>();

        foreach (var kvp in _active)
        {
            if (kvp.Value.Anchor == anchor)
            {
                list.Add(kvp.Key);
            }
        }

        foreach (var handle in list)
        {
            Release(handle);
        }
    }

    public void Prewarm(EffectDefinition definition, int count)
    {
        if (definition == null || definition.Prefab == null || count <= 0)
            return;

        var pool = GetOrCreatePool(definition);

        for (int i = 0; i < count; i++)
        {
            var go = Object.Instantiate(definition.Prefab, pool.PoolRoot);
            go.SetActive(false);
            pool.Inactive.Push(go);
        }
    }

    // ---- Internal helpers ----

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
            go = Object.Instantiate(definition.Prefab, pool.PoolRoot);
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
        _nextKey++;
        var handle = EffectHandle.FromKey(_nextKey);

        _active.Add(handle, new ActiveInstance
        {
            GameObject = go,
            Definition = definition,
            Anchor = anchor,
            IsAttached = isAttached
        });

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
}
