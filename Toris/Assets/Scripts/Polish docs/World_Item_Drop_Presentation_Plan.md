# World Item Drop Presentation Plan

This note captures the planned polish pass for items that spawn from defeated enemies.

The goal is not to change item data, loot tables, pickup rules, inventory transfer, or quest pickup facts. This is only presentation: make spawned world items easier to notice and nicer to read in the scene.

## Current State

- Enemy death loot resolves through `EnemyLootRuntime`.
- Item loot is created as a bare runtime `GameObject`.
- The spawned object receives:
  - `SpriteRenderer`
  - `CircleCollider2D`
  - `WorldItem`
- The item appears at a random nearby position immediately.
- There is no landing motion, shadow, glow, or idle presentation.

Because the system already has one central enemy loot spawn path, this can be improved in one focused place.

## Desired Result

When an enemy dies, item drops should:

- burst outward from the death position in random directions
- travel a short distance with a small arc
- settle on the ground after a short landing time
- show a reusable shadow underneath
- bob gently after landing
- have a subtle glow or halo so the icon separates from the ground
- remain normal `WorldItem` pickups after the presentation finishes

## Recommended Implementation

Create a small reusable presentation component:

`WorldItemDropPresentation`

Responsibilities:

- receives the visual sprite renderer, origin position, and final landing position
- animates the item from origin to landing position
- keeps pickup interaction disabled until the item has landed
- bobs only the visual child after landing
- updates shadow scale / alpha during the landing arc if needed
- pulses glow alpha very slightly after landing

The root object should own gameplay:

- `WorldItem`
- `CircleCollider2D`
- item layer
- final pickup position

The visual child should own presentation:

- item icon sprite renderer
- glow sprite renderer
- shadow prefab instance
- bobbing motion

This keeps the collider from wobbling forever while the icon bobs.

## Chosen Wiring

First implementation uses optional prefab references on each `EnemyLootTableSO`:

- `dropGlowPrefab`
- `dropShadowPrefab`

This avoids hardcoded asset paths and lets each loot table opt into, remove, or later vary presentation prefabs.

Spawned item visuals use `SpriteSortPoint.Pivot` and normal world sorting order so loot follows the same pivot-based sorting rules as other world sprites. Glow and shadow renderers use small negative order offsets so they sit behind the item icon.

## Shadow

Use the existing shared shadow prefab.

Best setup:

- instantiate or place the shadow as a child under the visual root
- keep it below the item sprite
- do not move it vertically during bobbing
- optionally squash / fade it slightly during the initial hop

The shadow should make the item feel grounded, while the bobbing icon makes it readable as something interactable.

## Glow Options

### Recommended: Soft Glow Sprite

Use a small transparent radial-gradient sprite behind the item icon.

Setup:

- child object named `Glow`
- `SpriteRenderer`
- sprite is a soft circular glow texture
- placed behind the item icon using lower sorting order
- color starts as warm pale yellow or soft white
- alpha stays low, roughly `0.15` to `0.35`
- scale is larger than the item icon, roughly `1.4x` to `1.8x`

This is the safest first version because it does not depend on post-processing, bloom, or custom shaders.

The glow can later become rarity-aware:

- common: soft white
- consumable: pale blue / green
- material: warm amber
- rare: purple / gold

For now, one generic readable glow is enough.

### Alternate: Enlarged Tinted Item Sprite

Duplicate the item sprite renderer behind the real icon, tint it, enlarge it, and lower the alpha.

Pros:

- no new texture required
- easy to generate in code

Cons:

- some item silhouettes will look muddy
- thin icons may not create a good halo
- it can look like a blurry duplicate instead of an actual glow

This is acceptable as a quick fallback, but the soft glow sprite will look cleaner.

### Later: Emissive Material And Bloom

Use an emissive material plus camera bloom.

Pros:

- can look very polished
- useful if the game already relies on bloom elsewhere

Cons:

- depends on render pipeline and post-processing setup
- can become visually noisy
- more tuning-heavy than this feature needs right now

This should wait unless the project already has a consistent glow/bloom pipeline.

## Drop Motion

First-pass values:

| Setting | Starting Value | Notes |
|---|---:|---|
| Scatter Radius | `0.75` to `1.25` | Slightly wider than current placement |
| Travel Time | `0.25s` to `0.45s` | Fast enough to feel responsive |
| Arc Height | `0.25` to `0.45` | Small hop, not cartoony launch |
| Bob Height | `0.05` to `0.10` | Subtle idle readability |
| Bob Speed | `2.0` to `3.0` | Gentle, not frantic |
| Glow Pulse | `0.05` alpha range | Optional and subtle |

Spawn logic should choose a random normalized direction and random distance, then animate toward that final position.

## Pickup Timing

Recommended behavior:

- item cannot be picked up while flying outward
- collider becomes enabled after landing
- pickup prompt / interaction works normally after that

This avoids odd cases where the player collects an item before it visually lands.

## EnemyLootRuntime Changes

Current runtime code creates the entire world item directly.

For a clean version:

- keep `EnemyLootRuntime` responsible for loot rolls
- let it request a spawned world item at an origin and landing target
- add presentation setup after creating the drop object
- avoid changing `WorldItem.Interact`

If we decide to use the existing shadow prefab directly, a small `WorldItemDrop.prefab` may be cleaner than pure code-created objects. The prefab can hold:

- root object with `WorldItem`
- trigger collider
- presentation component
- child item sprite renderer
- child glow renderer
- child shadow prefab instance

Then `EnemyLootRuntime` can instantiate that prefab instead of manually assembling every visual piece.

## Not Part Of This Pass

- magnet pickup behavior
- item persistence across unloaded chunks
- rarity beams
- item nameplates
- minimap markers
- loot filtering

Those are valid later, but this pass should stay focused on making existing enemy item drops visible and pleasant.
