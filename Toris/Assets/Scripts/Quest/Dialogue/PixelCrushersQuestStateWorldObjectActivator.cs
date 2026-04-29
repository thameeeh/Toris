// TODO: Re-enable this later when quest-driven world-state changes become an active implementation slice.
//
// Purpose:
// - Watch a Pixel Crushers quest state.
// - Enable or disable assigned Toris world objects such as blockers, gates, portals, or revealed sites.
// - Keep this generic instead of writing one-off scripts like "OpenSpecificGateAfterGuideQuest".
//
// Intended future shape:
// - Quest Name: Pixel Crushers quest to watch.
// - Active When States: quest states that should activate the targets.
// - Invert Match: useful for blockers that remain active until a quest reaches Success.
// - Targets: world GameObjects to enable/disable.
//
// Notes:
// - Keep the component on a separate controller object if target objects may be disabled.
// - Make it save/load friendly by polling or refreshing after deferred Pixel Crushers save data applies.
