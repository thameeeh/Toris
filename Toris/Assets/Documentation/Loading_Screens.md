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
2. The overlay chooses a random configured background image.
3. A solid cover fades over the outgoing scene.
4. The async scene load begins once the screen is fully covered.
5. The cover holds briefly as a clean black screen.
6. The loading background fades in over the black cover.
7. The loading label starts as `Loading`.
8. Dots cycle continuously as `Loading.`, `Loading..`, `Loading...` while the scene is still waiting.
9. Scene activation is held until the third dot has appeared, the async scene is ready, and the minimum display time has passed.
10. Once the scene is ready to activate, the loading label hides so the reveal phase no longer reads as active loading.
11. The loading overlay fades out after the target scene has loaded and settled briefly.

If no background images are assigned, the overlay falls back to a solid color.

The current background pool uses `LoadingScreen001` through `LoadingScreen009` from `Assets/Art/PixelArtRPGMegaPack/Textures/Extra/`. Backgrounds preserve their aspect ratio and cover the full screen; images with a mismatched aspect ratio are cropped at the edges instead of stretched.

The overlay blocks scene UI input while active. It uses an invisible full-screen raycast blocker for uGUI and temporarily suspends UI Toolkit picking so menu buttons behind the loading screen do not receive hover or click events during the transition.

The current minimum display duration is temporarily set to 10 seconds so the transition can be visually inspected during tuning. Reduce this before the final feel pass.

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
