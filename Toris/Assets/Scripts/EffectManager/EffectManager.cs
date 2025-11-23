using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Exposes the contract for effect playback orchestration.
/// </summary>
public interface IEffectManager
{
    /// <summary>Play a one-shot effect.</summary>
    void Play(EffectRequest request);

    /// <summary>Play a persistent/attached effect and return a handle for manual release.</summary>
    EffectHandle PlayPersistent(PersistentEffectRequest request);

    /// <summary>Release a specific persistent effect by handle.</summary>
    void Release(EffectHandle handle);

    /// <summary>Release all currently active persistent/attached effects.</summary>
    void ReleaseAll();

    /// <summary>Release all effects associated with a specific anchor (e.g., a Transform).</summary>
    void ReleaseAll(Transform anchor);

    /// <summary>Get an effect definition by string ID.</summary>
    bool TryGetDefinition(string effectId, out EffectDefinition definition);

    /// <summary>All definitions available through this manager.</summary>
    IReadOnlyList<EffectDefinition> Definitions { get; }
}

/// <summary>
/// Supplies effect definitions that are available at runtime.
/// </summary>
public interface IEffectCatalog
{
    IReadOnlyList<EffectDefinition> Definitions { get; }
    bool TryGetDefinition(string effectId, out EffectDefinition definition);
}

/// <summary>
/// Executes low-level effect lifecycle operations. A Unity-facing implementation
/// will be plugged in later to perform pooling, instantiation, and teardown.
/// </summary>
public interface IEffectRuntime
{
    /// <summary>Play a one-shot effect using a resolved definition.</summary>
    void Play(EffectDefinition definition, EffectRequest request);

    /// <summary>Play a persistent or attached effect and return a handle for later release.</summary>
    EffectHandle PlayPersistent(EffectDefinition definition, PersistentEffectRequest request);

    /// <summary>Release a single instance associated with a handle.</summary>
    void Release(EffectHandle handle);

    /// <summary>Release all tracked instances.</summary>
    void ReleaseAll();

    /// <summary>Release all instances attached to / anchored by the given transform.</summary>
    void ReleaseAll(Transform anchor);

    /// <summary>Prewarm internal pools for a specific definition.</summary>
    void Prewarm(EffectDefinition definition, int count);
}

/// <summary>
/// Plain C# coordinator that translates high-level requests into runtime operations.
/// </summary>
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

/// <summary>
/// No-op manager used before the Unity binding spins up.
/// </summary>
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
        // Intentionally blank.
    }

    public EffectHandle PlayPersistent(PersistentEffectRequest request)
    {
        return EffectHandle.Invalid;
    }

    public void Release(EffectHandle handle)
    {
        // Intentionally blank.
    }

    public void ReleaseAll()
    {
        // Intentionally blank.
    }

    public void ReleaseAll(Transform anchor)
    {
        // Intentionally blank.
    }

    public bool TryGetDefinition(string effectId, out EffectDefinition definition)
    {
        definition = null;
        return false;
    }
}

/// <summary>
/// No-op runtime implementation used while the real pooling/instantiation layer is built.
/// </summary>
public sealed class NullEffectRuntime : IEffectRuntime
{
    public static readonly NullEffectRuntime Instance = new();

    private NullEffectRuntime()
    {
    }

    public void Play(EffectDefinition definition, EffectRequest request)
    {
        // Intentionally blank.
    }

    public EffectHandle PlayPersistent(EffectDefinition definition, PersistentEffectRequest request)
    {
        // Intentionally blank; always return invalid.
        return EffectHandle.Invalid;
    }

    public void Release(EffectHandle handle)
    {
        // Intentionally blank.
    }

    public void ReleaseAll()
    {
        // Intentionally blank.
    }

    public void ReleaseAll(Transform anchor)
    {
        // Intentionally blank.
    }

    public void Prewarm(EffectDefinition definition, int count)
    {
        // Intentionally blank.
    }
}

/// <summary>
/// In-memory catalog useful for tests or temporary setups.
/// </summary>
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

/// <summary>
/// Serializable data describing an effect prefab and its default configuration.
/// </summary>
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

    public string Id => id;
    public GameObject Prefab => prefab;
    public EffectCategory Category => category;
    public bool PrewarmPool => prewarmPool;
    public int PrewarmCount => Mathf.Max(0, prewarmCount);
}

/// <summary>
/// Broad classification flags for downstream systems to reason about effects.
/// </summary>
public enum EffectCategory
{
    OneShot,
    Persistent,
    Attached,
}

/// <summary>
/// Request payload for one-shot effect playback.
/// </summary>
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

/// <summary>
/// Request payload for persistent effects that need manual release.
/// </summary>
[Serializable]
public struct PersistentEffectRequest
{
    public string EffectId;
    public Transform Anchor;
    public Vector3 LocalPosition;
    public Quaternion LocalRotation;
    public EffectVariant Variant;
}

/// <summary>
/// Lightweight descriptor for selecting a specific variant of a base effect.
/// </summary>
[Serializable]
public struct EffectVariant
{
    public string VariantId;
    public Color ColorOverride;
}

/// <summary>
/// Handle returned for persistent effects; wraps opaque state to simplify release calls.
/// </summary>
[Serializable]
public struct EffectHandle : IEquatable<EffectHandle>
{
    [SerializeField]
    private int key;

    public static EffectHandle Invalid => new EffectHandle(-1);

    // Internal ctor so only the runtime (same assembly) can mint handles.
    internal EffectHandle(int key)
    {
        this.key = key;
    }

    // Small factory for clarity.
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
