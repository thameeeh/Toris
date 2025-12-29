using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEffectManager
{
    void Play(EffectRequest request);
    void PlayAttached(AttachedEffectRequest request);
    EffectHandle PlayPersistent(PersistentEffectRequest request);
    void Release(EffectHandle handle);
    void ReleaseAll();
    void ReleaseAll(Transform anchor);
    bool TryGetDefinition(string effectId, out EffectDefinition definition);
    IReadOnlyList<EffectDefinition> Definitions { get; }
}

public interface IEffectCatalog
{
    IReadOnlyList<EffectDefinition> Definitions { get; }
    bool TryGetDefinition(string effectId, out EffectDefinition definition);
}

public interface IEffectRuntime
{
    void Play(EffectDefinition definition, EffectRequest request);
    void PlayAttached(EffectDefinition definition, AttachedEffectRequest request);
    EffectHandle PlayPersistent(EffectDefinition definition, PersistentEffectRequest request);
    void Release(EffectHandle handle);
    void ReleaseAll();
    void ReleaseAll(Transform anchor);
    void Prewarm(EffectDefinition definition, int count);
}
public interface IEffectRuntimeTick
{
    void Tick(float deltaTimeSeconds);
}
public interface IEffectParametersReceiver
{
    void ApplyEffectParameters(EffectVariant variant, float magnitude);
}

public sealed class EffectManager : IEffectManager
{
    private readonly IEffectCatalog catalog;
    private readonly IEffectRuntime runtime;

    public EffectManager(IEffectCatalog catalog, IEffectRuntime runtime)
    {
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public IReadOnlyList<EffectDefinition> Definitions => catalog.Definitions;

    public void Play(EffectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EffectId))
        {
            return;
        }

        if (!catalog.TryGetDefinition(request.EffectId, out var definition))
        {
            return;
        }

        runtime.Play(definition, request);
    }
    public void PlayAttached(AttachedEffectRequest request)
    {
        if (string.IsNullOrEmpty(request.EffectId))
            return;

        if (!catalog.TryGetDefinition(request.EffectId, out var definition))
            return;

        runtime.PlayAttached(definition, request);
    }

    public EffectHandle PlayPersistent(PersistentEffectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EffectId))
        {
            return EffectHandle.Invalid;
        }

        if (!catalog.TryGetDefinition(request.EffectId, out var definition))
        {
            return EffectHandle.Invalid;
        }

        return runtime.PlayPersistent(definition, request);
    }

    public void Release(EffectHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        runtime.Release(handle);
    }

    public void ReleaseAll()
    {
        runtime.ReleaseAll();
    }

    public void ReleaseAll(Transform anchor)
    {
        if (anchor == null)
        {
            return;
        }

        runtime.ReleaseAll(anchor);
    }

    public bool TryGetDefinition(string effectId, out EffectDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(effectId))
        {
            definition = null;
            return false;
        }

        return catalog.TryGetDefinition(effectId, out definition);
    }
}

public sealed class NullEffectManager : IEffectManager
{
    public static readonly NullEffectManager Instance = new();

    private static readonly IReadOnlyList<EffectDefinition> emptyDefinitions = Array.Empty<EffectDefinition>();

    private NullEffectManager()
    {
    }

    public IReadOnlyList<EffectDefinition> Definitions => emptyDefinitions;

    public void Play(EffectRequest request)
    {
        // Intentionally blank
    }
    public void PlayAttached(AttachedEffectRequest request)
    {
        // Intentionally blank
    }


    public EffectHandle PlayPersistent(PersistentEffectRequest request)
    {
        return EffectHandle.Invalid;
    }

    public void Release(EffectHandle handle)
    {
        // Intentionally blank
    }

    public void ReleaseAll()
    {
        // Intentionally blank
    }

    public void ReleaseAll(Transform anchor)
    {
        // Intentionally blank
    }

    public bool TryGetDefinition(string effectId, out EffectDefinition definition)
    {
        definition = null;
        return false;
    }
}
public sealed class NullEffectRuntime : IEffectRuntime
{
    public static readonly NullEffectRuntime Instance = new();

    private NullEffectRuntime()
    {
    }

    public void Play(EffectDefinition definition, EffectRequest request)
    {
        // Intentionally blank
    }
    public void PlayAttached(EffectDefinition definition, AttachedEffectRequest request)
    {
        // Intentionally blank
    }


    public EffectHandle PlayPersistent(EffectDefinition definition, PersistentEffectRequest request)
    {
        // Intentionally blank; always return invalid
        return EffectHandle.Invalid;
    }

    public void Release(EffectHandle handle)
    {
        // Intentionally blank
    }

    public void ReleaseAll()
    {
        // Intentionally blank
    }

    public void ReleaseAll(Transform anchor)
    {
        // Intentionally blank
    }

    public void Prewarm(EffectDefinition definition, int count)
    {
        // Intentionally blank
    }
}

public sealed class InMemoryEffectCatalog : IEffectCatalog
{
    private readonly Dictionary<string, EffectDefinition> lookup;
    private readonly List<EffectDefinition> definitions;

    public static InMemoryEffectCatalog Empty { get; } =
        new InMemoryEffectCatalog(Array.Empty<EffectDefinition>());

    public InMemoryEffectCatalog(IEnumerable<EffectDefinition> entries)
    {
        definitions = new List<EffectDefinition>();
        lookup = new Dictionary<string, EffectDefinition>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                continue;
            }

            if (lookup.ContainsKey(entry.Id))
            {
                continue;
            }

            definitions.Add(entry);
            lookup.Add(entry.Id, entry);
        }
    }

    public IReadOnlyList<EffectDefinition> Definitions => definitions;

    public bool TryGetDefinition(string effectId, out EffectDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(effectId))
        {
            definition = null;
            return false;
        }

        return lookup.TryGetValue(effectId, out definition);
    }
}

[Serializable]
public class EffectDefinition
{
    [SerializeField]
    private string id = string.Empty;

    [SerializeField]
    private GameObject prefab;

    [SerializeField]
    private EffectCategory category = EffectCategory.OneShot;

    [SerializeField]
    private bool prewarmPool;

    [SerializeField]
    private int prewarmCount = 1;

    [Tooltip("Hard cap on total instances (active + inactive) for this effect.\n0 = no cap (not recommended).")]
    [SerializeField]
    private int maxPoolSize = 64;

    [Tooltip("Hard cap on inactive instances kept in the pool.\nInactive overflow instances are destroyed on release.\n0 = no cap (not recommended).")]
    [SerializeField]
    private int maxInactive = 32;

    [SerializeField]
    private float oneShotLifetimeSeconds = 1.0f;

    public float OneShotLifetimeSeconds => Mathf.Max(0f, oneShotLifetimeSeconds);
    public int MaxPoolSize => Mathf.Max(0, maxPoolSize);
    public int MaxInactive => Mathf.Max(0, maxInactive);
    public string Id => id;
    public GameObject Prefab => prefab;
    public EffectCategory Category => category;
    public bool PrewarmPool => prewarmPool;
    public int PrewarmCount => Mathf.Max(0, prewarmCount);
}

public enum EffectCategory
{
    OneShot,
    Persistent,
    Attached,
}

[Serializable]
public struct EffectRequest
{
    public string EffectId;
    public Vector3 Position;
    public Quaternion Rotation;
    public Transform Parent;
    public EffectVariant Variant;
    public float Magnitude;
}

[Serializable]
public struct PersistentEffectRequest
{
    public string EffectId;
    public Transform Anchor;
    public Vector3 LocalPosition;
    public Quaternion LocalRotation;
    public EffectVariant Variant;
    public float Magnitude;
}

[Serializable]
public struct AttachedEffectRequest
{
    public string EffectId;
    public Transform Anchor;
    public Vector3 LocalPosition;
    public Quaternion LocalRotation;
    public EffectVariant Variant;
    public float Magnitude;
}

[Serializable]
public struct EffectVariant
{
    public string VariantId;
    public Color ColorOverride;
}

[Serializable]
public struct EffectHandle : IEquatable<EffectHandle>
{
    [SerializeField]
    private int key;

    public static EffectHandle Invalid => new EffectHandle(-1);

    internal EffectHandle(int key)
    {
        this.key = key;
    }

    internal static EffectHandle FromKey(int key) => new EffectHandle(key);

    public bool IsValid => key >= 0;

    public bool Equals(EffectHandle other)
    {
        return key == other.key;
    }

    public override bool Equals(object obj)
    {
        return obj is EffectHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return key;
    }

    public static bool operator ==(EffectHandle left, EffectHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EffectHandle left, EffectHandle right)
    {
        return !left.Equals(right);
    }
}
