# Loading Screens

## Current Scope

The loading-screen overlay covers full Unity scene transitions:

- `MainMenu` to `MainArea`
- `MainArea` to `MainMenu`
- `MainArea` to `ProceduralTiles`
- `ProceduralTiles` to `MainArea`
- death respawn scene returns that already route through `SceneTransitionService`
- biome-to-biome gate transitions inside `ProceduralTiles`

Biome-to-biome swaps are not Unity scene loads. They still run through `WorldTransitionSystem`, but `WorldGenRunner` injects a `BiomeLoadingTransitionService` wrapper into gate sites so presentation remains outside the world-state system.

## Runtime Owner

`SceneTransitionService` owns the first pass because it is already the centralized scene-transition service and persists across scene loads. The loading overlay is a transition concern, not a normal gameplay window, so it stays outside the scene-local `UIManager` registration flow.

The implementation is split by responsibility:

- `SceneTransitionService` coordinates async scene loading, activation timing, transition hooks, and fade sequencing.
- `SceneLoadingOverlay` owns a small persistent UI Toolkit `UIDocument` host and drives the cloned loading overlay template.
- `LoadingScreenOverlay.uxml` and `LoadingScreenOverlay.uss` own the loading-screen layout and styling.
- `SceneUiInputSuspender` owns temporary input blocking for uGUI `EventSystem` instances and UI Toolkit picking.

Scene-local UI remains rebuilt normally after each load. The loading screen only blocks input and covers the scene while the async scene operation is in progress.

## First-Pass Behavior

When a scene load starts:

1. `SceneTransitionService` shows a persistent full-screen overlay.
2. The overlay chooses a random configured background image.
3. A solid cover fades over the outgoing scene.
4. The async scene load begins once the screen is fully covered.
5. The cover holds briefly as a clean black screen.
6. The loading background fades in over the black cover.
7. The loading label starts as `Loading.` once the loading art is visible.
8. Dots cycle continuously as `Loading.`, `Loading..`, `Loading...` while the scene is still waiting.
9. Scene activation is held until the third dot has appeared, the async scene is ready, and the minimum display time has passed.
10. Once the scene is ready to activate, the loading label hides so the reveal phase no longer reads as active loading.
11. The loading overlay fades out after the target scene has loaded and settled briefly.

If no background images are assigned, the overlay falls back to a solid color.

The current background pool uses `LoadingScreen001` through `LoadingScreen009` from `Assets/Art/PixelArtRPGMegaPack/Textures/Extra/`. Backgrounds preserve their aspect ratio and cover the full screen; images with a mismatched aspect ratio are cropped at the edges instead of stretched. The UI Toolkit background element supports a small centered overscan so bad outermost sprite pixels do not sit on the screen edge. A solid fallback-color backing sits behind the art, so semi-transparent loading images cannot reveal the outgoing or incoming scene.

The loading label sits in a full-width bottom strip with left-aligned text, matching warm accent lines above and below the label, bold text, and stable dot spacing so the label does not shift as dots animate.

The overlay blocks scene UI input while active. It uses an invisible full-screen raycast blocker for uGUI and temporarily suspends UI Toolkit picking so menu buttons behind the loading screen do not receive hover or click events during the transition. It also raises a temporary gameplay-input lock through `UIEventsSO`, which lets `InputManager` clear movement and held combat inputs during scene loads and biome swaps.

The minimum display duration is inspector-tuned on `SceneTransitionService` through a weighted range, with the default range biased toward shorter loading screens. Fade timings use the same range model; set a range's min and max to the same value when a timing should stay fixed.

## Biome Gate Behavior

Biome transitions reuse the same overlay sequencing without pretending to be scene loads:

1. A gate site calls its injected `IGateTransitionService`.
2. `BiomeLoadingTransitionService` checks that the world transition system can currently use the gate.
3. `SceneTransitionService` shows the same loading overlay used for scene transitions.
4. Once the outgoing biome is fully covered, the wrapper calls `WorldTransitionSystem.UseGate(...)`.
5. `WorldTransitionSystem` clears the old biome runtime state, binds the next biome, resets chunk state, rebuilds feature lifecycle state, and moves the streaming anchor.
6. The overlay stays up while `WorldStreamingRuntime` processes the new view.
7. The reveal waits until the visible chunk set is settled or until the configured biome streaming timeout is reached.

`WorldTransitionSystem` remains responsible for biome state. `BiomeLoadingTransitionService` is only the bridge between gate interaction, loading presentation, and streaming readiness.

If `ProceduralTiles` is launched directly without a `SceneTransitionService` instance in the scene or carried over from the main menu, `WorldGenRunner` falls back to direct world transitions. In that setup, biome gates still work but they do not show the loading overlay.

## Later Polish

Tips can be added as optional payload data later. The likely model is a data asset containing short tip strings and selection rules, with `SceneTransitionService` choosing a tip for each scene load.
