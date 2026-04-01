using System;
using UnityEngine;

public sealed class WorldEncounterPackageBinding
{
    private IWorldEncounterSite site;
    private WorldEncounterPackage encounterPackage;
    private Action initializedCallback;
    private Action clearedCallback;
    private Action<Vector3> damagedCallback;
    private bool suppressNextInitializedCallback;

    public void Bind(
        IWorldEncounterSite site,
        WorldEncounterPackage encounterPackage,
        Action initializedCallback,
        Action clearedCallback,
        Action<Vector3> damagedCallback)
    {
        Unbind(clearActiveState: false);

        this.site = site;
        this.encounterPackage = encounterPackage;
        this.initializedCallback = initializedCallback;
        this.clearedCallback = clearedCallback;
        this.damagedCallback = damagedCallback;
        suppressNextInitializedCallback = false;

        if (site == null)
            return;

        site.Initialized += HandleInitialized;
        site.Cleared += HandleCleared;
        site.DamagedAlert += HandleDamaged;

        if (site.IsInitialized)
        {
            SetPackageActive(!site.IsCleared);

            if (!site.IsCleared)
            {
                suppressNextInitializedCallback = true;
                initializedCallback?.Invoke();
            }
        }
    }

    public void Unbind()
    {
        Unbind(clearActiveState: true);
    }

    private void Unbind(bool clearActiveState)
    {
        if (site != null)
        {
            site.Initialized -= HandleInitialized;
            site.Cleared -= HandleCleared;
            site.DamagedAlert -= HandleDamaged;
        }

        if (clearActiveState)
            SetPackageActive(false);

        site = null;
        encounterPackage = default;
        initializedCallback = null;
        clearedCallback = null;
        damagedCallback = null;
        suppressNextInitializedCallback = false;
    }

    private void HandleInitialized()
    {
        SetPackageActive(true);

        if (suppressNextInitializedCallback)
        {
            suppressNextInitializedCallback = false;
            return;
        }

        initializedCallback?.Invoke();
    }

    private void HandleCleared()
    {
        SetPackageActive(false);
        clearedCallback?.Invoke();
    }

    private void HandleDamaged(Vector3 threatPoint)
    {
        damagedCallback?.Invoke(threatPoint);
    }

    private void SetPackageActive(bool active)
    {
        if (!encounterPackage.IsValid)
            return;

        encounterPackage.State.IsActive = active;
    }
}
