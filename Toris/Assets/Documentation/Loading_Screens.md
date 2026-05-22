# Loading Screens

## Current Scope

The first loading-screen pass only covers full Unity scene transitions:

- `MainMenu` to `MainArea`
- `MainArea` to `MainMenu`
- `MainArea` to `ProceduralTiles`
- `ProceduralTiles` to `MainArea`
- death respawn scene returns that already route through `SceneTransitionService`

Biome-to-biome transitions inside `ProceduralTiles` are intentionally deferred. Those swaps are not Unity scene loads; they run through `WorldTransitionSystem` and need a separate wrapper so world generation remains isolated from presentation concerns.

## Runtime Owner

`SceneTransitionService` owns the first pass because it is already the centralized scene-transition service and persists across scene loads. The loading overlay is a transition concern, not a normal gameplay window, so it stays outside the scene-local `UIManager` registration flow.

Scene-local UI remains rebuilt normally after each load. The loading screen only blocks input and covers the scene while the async scene operation is in progress.

## First-Pass Behavior

When a scene load starts:

1. `SceneTransitionService` shows a persistent full-screen overlay.
2. The overlay chooses the next configured background image in rotation.
3. The loading label starts as `Loading`.
4. Dots appear one by one until `Loading...`.
5. Scene activation is held until the third dot has appeared and the async scene is ready.
6. The overlay hides after the target scene has loaded and settled briefly.

If no background images are assigned, the overlay falls back to a solid color.

The current background rotation uses `LoadingScreen001` through `LoadingScreen009` from `Assets/Art/PixelArtRPGMegaPack/Textures/Extra/`. Backgrounds preserve their aspect ratio and cover the full screen; images with a mismatched aspect ratio are cropped at the edges instead of stretched.

## Deferred Biome Transition Plan

Biome transitions should not be folded directly into `SceneTransitionService` because they do not use Unity scene loading. A later pass should add a small coroutine-capable bridge around `WorldTransitionSystem.UseGate(...)`:

1. Show the same loading overlay.
2. Play the same three-dot timing.
3. Execute the biome swap on the third-dot beat.
4. Let streaming rebuild the active chunks.
5. Hide the overlay once the transition has settled.

That keeps `WorldTransitionSystem` focused on biome state and lets presentation stay in a UI/transition layer.

## Later Polish

Tips can be added as optional payload data later. The likely model is a data asset containing short tip strings and selection rules, with `SceneTransitionService` choosing a tip for each scene load.
