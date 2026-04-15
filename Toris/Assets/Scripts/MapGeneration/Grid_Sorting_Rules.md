# Grid Sorting Rules

## Summary
These are the locked rendering rules for the procedural world grid.

The goal is simple:
- water sits below ground to help sell depth
- ground sits below actors and world props
- player, enemies, decorations, and obstacle visuals share the same sorting band so pivot-based front/back swapping works naturally
- elevated walls and special height cases are handled separately and are not part of the current procedural-world baseline

## Locked Layer Model

### Ground Band
- `Water`
  - sorting layer: `Ground`
  - sorting order: `0`
  - purpose: deepest visible world surface
- `Terrain`
  - sorting layer: `Ground`
  - sorting order: `0`
  - purpose: main walkable ground and any terrain that should visually sit above water

### Actor And World Band
- `Decoration`
  - sorting layer: `Default`
  - sorting order: `0`
  - purpose: non-blocking world dressing that should sort naturally against actors
- `Obstacle`
  - sorting layer: `Default`
  - sorting order: `0`
  - purpose: blocking stump/trunk/base visuals that should sort naturally against actors
- `Canopy`
  - sorting layer: `Default`
  - sorting order: `0`
  - purpose: tall tree tops and upper visuals that should also sort naturally against actors unless a specific effect needs otherwise
- `Player`
  - sorting layer: `Default`
  - sorting order: `0`
  - sort point: `Pivot`
- `Enemies`
  - sorting layer: `Default`
  - sorting order: `0`
  - sort point: `Pivot`
- other runtime world sprites or NPCs
  - sorting layer: `Default`
  - sorting order: `0`
  - sort point: `Pivot`

## Why This Model Works
- Water staying below terrain gives the world more depth.
- Terrain staying on the `Ground` layer keeps the base world visually stable.
- Putting the player, enemies, decorations, obstacle bases, and canopies on the same `Default` / `0` band lets them resolve front/back order by their own pivots instead of a forced fixed order.
- This prevents the common failure mode where a canopy or obstacle always renders above the player just because it was given a higher sorting order.

## Rules To Keep Stable
- Do not put `Canopy` on a higher fixed sorting order than the player unless you intentionally want it to always render above actors.
- Do not put `Obstacle` on a higher fixed sorting order than the player unless you intentionally want it to always render above actors.
- Keep actor-like visuals that need natural overlap in the same sorting layer and sorting order.
- Use sprite or tile pivots to control local overlap behavior inside that shared band.
- Treat elevated walls, bridges, roofs, and other true height systems as separate authored cases.

## Current Procedural-World Scope
These rules are for the current procedural world only.

Out of scope for now:
- elevated walls
- special roof reveal logic
- multi-floor sorting systems
- bespoke height zones

Those can be added later as explicit systems instead of muddying the base world rules now.

## Quick Debug Checklist
If something draws in the wrong order, check these in order:
1. Sorting layer
2. Sorting order
3. `Sort Point`
4. sprite or tile pivot
5. tilemap renderer mode
6. only after that, investigate generation placement
