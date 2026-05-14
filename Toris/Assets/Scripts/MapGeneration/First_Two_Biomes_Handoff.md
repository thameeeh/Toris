# First Two Biomes Enemy Content Handoff

## Context

Toris uses procedural generation across many seeds, so the first two biomes should be designed through systemic content budgets and spawn rules, not fixed authored routes.

The goal is to reshape the first two biomes around enemy and wildlife identity:

- Biome 1 is Plains / Quiet Wildlands.
- Biome 2 is Forest.

The Plains should feel alive before it feels dangerous. The Forest should be the first biome where the world becomes clearly hostile and territorial.

Primary design doc:

- `Enemy/First_Two_Biomes_Enemy_Content_Plan.md`

Do not update `CHANGELOG.md` for this handoff work. Avoid global documentation unless it is necessary for coding patterns or implementation details.

## Current Direction

### Plains / Quiet Wildlands

Core feeling:

- Alive, readable, spacious, and calmer.
- Wildlife-forward.
- Light danger only.
- No active supernatural combat.

Intended enemy/wildlife identity:

- Deer are central wildlife.
- Badgers become passive/reactive wildlife.
- Boars are a possible light-danger experiment.
- Wolves are rare or absent.
- Necromancer graves do not appear here.

### Forest

Core feeling:

- Denser, less safe, territorial.
- The player starts being hunted.
- Wolves define the biome.

Intended enemy/wildlife identity:

- Wolves and wolf dens become the main combat identity.
- Deer can appear as background food-chain texture.
- Badgers may appear but stay primarily reactive.
- Boars can exist, but must not compete with wolves as the main Forest identity.
- Necromancer graves can appear here or later as rare optional danger.
- Blood Mages remain tied to Necromancer encounters, not normal Forest spawns.

## Decisions Already Made

### Badger

Badger should be repurposed away from its old aggressive dash/unburrow damage concept.

New intended role:

- Passive/reactive wildlife.
- Flees from danger, nearby combat, and loud activity.
- Uses burrow animation as defensive escape or idle behavior.
- Cannot be damaged while fully burrowed.
- Adds life and motion without adding early combat noise.

Reason:

- The old damage/unburrow behavior was clunky and too expensive for what it contributed.

### Boar

Boar may become the Plains light-danger creature, but only if it can be made readable with the available animations.

Possible attack concept:

- Use run animation as a charge.
- Boar commits to a mostly straight burst.
- Damage only applies during the charge window.
- The charge should pass through or past the player rather than sticking to them.

Fallback:

- If this feels unfair, unreadable, or silly, make Boar passive/reactive wildlife instead.
- If Boar does not carry light danger, allow wolves to remain rare Plains danger.

### Wolves

Wolves should mostly move out of Plains and become the Forest's main identity.

Possible Plains presence:

- Rare danger.
- Distant howls.
- Tracks or signs.
- Occasional pressure near Forest transition.

Forest presence:

- Wolf dens.
- Patrols or spawned packs.
- Chase pressure.
- Clear territorial identity.

### Necromancer

No Necromancer graves in Plains.

Plains can have inactive strange POIs, but they should not trigger Necromancer combat.

Necromancer graves fit better in Forest or later. If used in Forest, keep them rare so they do not drown out wolves.

Blood Mages stay encounter-bound to Necromancer.

## First Implementation Steps

1. Audit current Plains and Forest spawning.

   Build a quick matrix of current content:

   - Creature or POI.
   - Current biome assignment.
   - Current spawn source or config.
   - Intended biome assignment.
   - Intended role.

   Include at least:

   - Deer.
   - Badger.
   - Boar, if already present in assets/config.
   - Wolves.
   - Wolf dens.
   - Necromancer graves.
   - Blood Mages only as Necromancer-owned summons.

   MapGeneration-side audit:

   - `MapGeneration/First_Two_Biomes_MapGen_Audit.md`

2. Make Plains calmer using existing content first.

   Before writing new behavior, adjust data/config so Plains becomes less wolf-heavy and more wildlife-forward.

3. Move wolf identity into Forest.

   Make Forest the place where wolf dens and regular wolf pressure belong.

4. Repurpose Badger.

   Implement passive/reactive behavior:

   - Flee from threats.
   - Burrow defensively.
   - Ignore damage while fully burrowed.

5. Prototype Boar separately.

   Do not commit Boar to spawn tables until the run-charge behavior feels readable in a small test setup.

6. Validate through seed sampling.

   Do not design around a fixed 10-minute authored route. Sample multiple generated seeds and check whether the content mix reliably creates the intended biome identity.

## Validation Questions

For Plains:

- Does it feel alive before dangerous?
- Are deer and passive wildlife visible enough?
- Are wolves rare or absent enough?
- Do strange POIs imply mystery without forcing combat?
- Does the biome avoid feeling empty after wolf pressure is reduced?

For Forest:

- Do wolves clearly define the biome?
- Are wolf dens frequent enough to establish territory?
- Does Forest danger feel meaningfully higher than Plains?
- Does any supernatural content remain rare and special?

For Badger:

- Does it feel like wildlife instead of a failed enemy?
- Does burrowing read clearly?
- Does burrow invulnerability feel intuitive?
- Does fleeing from combat/noise make the world feel more reactive?

For Boar:

- Is the charge readable before impact?
- Can the player dodge or avoid it?
- Does contact damage feel fair?
- Does it avoid becoming a worse wolf?

## Things To Avoid

- Do not design a fixed first 10-minute route. This is a procedural game.
- Do not add Necromancer graves to Plains.
- Do not make every cool creature hostile.
- Do not let Forest lose its wolf identity by overloading it with supernatural enemies.
- Do not make Badger a combat enemy unless the passive/reactive version fails completely.
- Do not rely on one generated seed as proof that pacing works.

## Suggested Next Artifact

For MapGeneration-only work, use `MapGeneration/First_Two_Biomes_MapGen_Audit.md` as the working matrix and procedural checklist.

When the separate enemy/content handoff starts, add the final content matrix section to `Enemy/First_Two_Biomes_Enemy_Content_Plan.md`.

Suggested columns:

| Content | Current Biome | Current Source | Intended Biome | Intended Role | Notes |
| --- | --- | --- | --- | --- | --- |
