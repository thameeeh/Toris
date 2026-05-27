using System;
using OutlandHaven.UIToolkit;
using UnityEngine;

public sealed class UISfxEventBridge : MonoBehaviour
{
    // SFX-only bridge: listens to UI events and forwards SFX IDs to AudioBootstrap.
    // It must not open/close UI, mutate inventory, or decide gameplay success.
    private const string DefaultInventoryOpenSfxId = "ui_inventory_open";
    private const string DefaultInventoryCloseSfxId = "ui_inventory_close";

    [SerializeField] private UIEventsSO uiEvents;
    [SerializeField, Min(0f)] private float eventVolumeMultiplier = 1f;
    [SerializeField] private bool force2D = true;

    [Header("Default Screen SFX")]
    [Tooltip("Played when the inventory screen opens. Clear to disable the default inventory-open sound.")]
    [SerializeField] private string inventoryOpenSfxId = DefaultInventoryOpenSfxId;
    [Tooltip("Played when the inventory screen closes. Clear to disable the default inventory-close sound.")]
    [SerializeField] private string inventoryCloseSfxId = DefaultInventoryCloseSfxId;

    [Header("Additional Screen Rules")]
    [SerializeField] private ScreenSfxRule[] screenRules;

    private void OnEnable()
    {
        if (uiEvents == null)
            return;

        uiEvents.OnScreenOpen += HandleScreenOpen;
        uiEvents.OnScreenClose += HandleScreenClose;
        uiEvents.OnSfxRequested += HandleSfxRequested;
    }

    private void OnDisable()
    {
        if (uiEvents == null)
            return;

        uiEvents.OnScreenOpen -= HandleScreenOpen;
        uiEvents.OnScreenClose -= HandleScreenClose;
        uiEvents.OnSfxRequested -= HandleSfxRequested;
    }

    private void HandleScreenOpen(ScreenType screenType)
    {
        PlayFor(screenType, opened: true);
    }

    private void HandleScreenClose(ScreenType screenType)
    {
        PlayFor(screenType, opened: false);
    }

    private void PlayFor(ScreenType screenType, bool opened)
    {
        if (AudioBootstrap.Sfx == null)
            return;

        if (screenRules != null)
        {
            for (int i = 0; i < screenRules.Length; i++)
            {
                ScreenSfxRule rule = screenRules[i];
                if (rule.Screen != screenType)
                    continue;

                string ruleSfxId = opened ? rule.OpenSfxId : rule.CloseSfxId;
                if (string.IsNullOrWhiteSpace(ruleSfxId))
                    return;

                AudioBootstrap.Sfx.Play(ruleSfxId, rule.MakeRequest());
                return;
            }
        }

        string defaultSfxId = GetDefaultScreenSfxId(screenType, opened);
        if (string.IsNullOrWhiteSpace(defaultSfxId))
            return;

        AudioBootstrap.Sfx.Play(defaultSfxId, MakeDefaultRequest());
    }

    private void HandleSfxRequested(string sfxId)
    {
        if (AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(sfxId))
            return;

        SfxPlayRequest request = SfxPlayRequest.Default;
        request.volumeMultiplier = eventVolumeMultiplier > 0f ? eventVolumeMultiplier : 1f;
        request.force2D = force2D;
        request.allowDuringGameplayPause = true;
        AudioBootstrap.Sfx.Play(sfxId, request);
    }

    private string GetDefaultScreenSfxId(ScreenType screenType, bool opened)
    {
        if (screenType != ScreenType.Inventory)
            return null;

        return opened ? inventoryOpenSfxId : inventoryCloseSfxId;
    }

    private SfxPlayRequest MakeDefaultRequest()
    {
        SfxPlayRequest request = SfxPlayRequest.Default;
        request.volumeMultiplier = eventVolumeMultiplier > 0f ? eventVolumeMultiplier : 1f;
        request.force2D = force2D;
        request.allowDuringGameplayPause = true;
        return request;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (uiEvents == null)
            Debug.LogWarning($"[UISfxEventBridge] Missing UIEventsSO reference on {name}.", this);
    }
#endif

    [Serializable]
    private struct ScreenSfxRule
    {
        [SerializeField] private ScreenType screen;
        [SerializeField] private string openSfxId;
        [SerializeField] private string closeSfxId;
        [SerializeField, Min(0f)] private float volumeMultiplier;
        [SerializeField] private bool force2D;

        public ScreenType Screen => screen;
        public string OpenSfxId => openSfxId;
        public string CloseSfxId => closeSfxId;

        public SfxPlayRequest MakeRequest()
        {
            SfxPlayRequest request = SfxPlayRequest.Default;
            request.volumeMultiplier = volumeMultiplier > 0f ? volumeMultiplier : 1f;
            request.force2D = force2D;
            request.allowDuringGameplayPause = true;
            return request;
        }
    }
}
