using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity binding that wires serialized assets into the plain C# <see cref="EffectManager"/>.
/// Consumers interact with this behaviour through the <see cref="IEffectManager"/> interface.
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectManagerBehavior : MonoBehaviour, IEffectManager
{
    private static EffectManagerBehavior activeInstance;

    [SerializeField]
    private EffectLibrary library;

    [SerializeField]
    private bool persistAcrossScenes = true;

    private IEffectCatalog catalogOverride;
    private IEffectRuntime runtimeOverride = NullEffectRuntime.Instance;
    private EffectManager manager;

    /// <summary>
    /// Global entry point for gameplay code. Will be a NullEffectManager until this behaviour
    /// successfully awakens.
    /// </summary>
    public static IEffectManager Instance { get; private set; } = NullEffectManager.Instance;

    /// <summary>
    /// Allows tests or bootstrap code to inject a custom catalog at runtime.
    /// </summary>
    public void ConfigureCatalog(IEffectCatalog catalog)
    {
        catalogOverride = catalog;
        RebuildManager();
    }

    /// <summary>
    /// Allows tests or bootstrap code to inject a custom runtime implementation.
    /// </summary>
    public void ConfigureRuntime(IEffectRuntime runtime)
    {
        runtimeOverride = runtime ?? NullEffectRuntime.Instance;
        RebuildManager();
    }

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Destroy(gameObject);
            return;
        }

        activeInstance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        RebuildManager();
        Instance = this;
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
        {
            activeInstance = null;
            Instance = NullEffectManager.Instance;
        }

        manager = null;
    }

    private void EnsureManager()
    {
        if (manager == null)
        {
            RebuildManager();
        }
    }

    private void RebuildManager()
    {
        var catalog = catalogOverride ?? (library != null
            ? (IEffectCatalog)library
            : InMemoryEffectCatalog.Empty);

        var runtime = runtimeOverride ?? NullEffectRuntime.Instance;

        manager = new EffectManager(catalog, runtime);

        // Prewarm pools based on definitions.
        foreach (var def in catalog.Definitions)
        {
            if (def == null)
            {
                continue;
            }

            if (def.PrewarmPool && def.PrewarmCount > 0)
            {
                runtime.Prewarm(def, def.PrewarmCount);
            }
        }
    }

    // ----- IEffectManager implementation -----

    public IReadOnlyList<EffectDefinition> Definitions
    {
        get
        {
            EnsureManager();
            return manager.Definitions;
        }
    }

    public void Play(EffectRequest request)
    {
        EnsureManager();
        manager.Play(request);
    }

    public EffectHandle PlayPersistent(PersistentEffectRequest request)
    {
        EnsureManager();
        return manager.PlayPersistent(request);
    }

    public void Release(EffectHandle handle)
    {
        EnsureManager();
        manager.Release(handle);
    }

    public void ReleaseAll()
    {
        EnsureManager();
        manager.ReleaseAll();
    }

    public void ReleaseAll(Transform anchor)
    {
        EnsureManager();
        manager.ReleaseAll(anchor);
    }

    public bool TryGetDefinition(string effectId, out EffectDefinition definition)
    {
        EnsureManager();
        return manager.TryGetDefinition(effectId, out definition);
    }
}
