# First Two Biomes Enemy Content Plan

## Design Direction

Build the first two biomes around feeling and play rhythm first, then let story details grow from repeated motifs.

The Plains should establish that the world is alive, readable, and worth noticing. The Forest should be the first place where that living world becomes meaningfully dangerous.

This plan treats enemy and wildlife placement as systemic pacing:

- Plains teaches comfort, observation, gathering, and light caution.
- Forest introduces territory, pursuit, stronger enemy identity, and optional supernatural danger.

Because Toris is generated across many seeds, this plan should not depend on a fixed authored route. Validate the direction through spawn rules, encounter budgets, biome identity, and repeated seed sampling.

## Biome 1: Plains / Quiet Wildlands

### Core Promise

A gentler starting biome where the player learns the world through wildlife, gathering, movement, and small risks.

The player should feel:

- "This place is alive."
- "I can breathe and learn here."
- "Not everything is hostile."
- "Something may be wrong, but it is not fully awake yet."

### Enemy And Wildlife Pillars

- Wildlife is the main identity.
- Gathering and early survival are the main player activities.
- Light danger teaches caution without turning the biome into a combat zone.
- Rare unsettling landmarks can establish tone, but should not become active combat threats.

### Creature Direction

Deer should stay central. They are the baseline signal that the biome is natural and alive.

Badgers should return to world wildlife as passive, reactive creatures rather than enemies. Their burrow animations are still valuable, but the old dash/unburrow damage concept was too clunky and expensive for what it added.

The preferred Badger role is:

- Flee from danger, nearby combat, and loud activity.
- Burrow as a defensive escape or idle behavior.
- Avoid taking damage while fully burrowed.
- Add life and motion to the biome without adding combat noise.

Boars are a possible candidate for light danger, but only if the existing animation set can support a readable attack. Since there is no dedicated attack animation, the most likely combat version would use the run animation as a charge: the boar commits to a straight burst, passes through or past the player, and deals contact damage during the charge window.

If that charge does not feel readable or fair, boars should become passive/reactive wildlife instead and wolves can remain as rare Plains danger.

Wolves should mostly move out of this biome. If they appear at all, they should be rare pressure, distant howls, tracks, or occasional danger near the Forest transition.

Necromancer graves should not appear in the Plains. Supernatural hints can exist through other inactive POIs, but they should not trigger Necromancer combat here.

### Danger Budget

Plains should avoid becoming predator-dense.

Suggested danger profile:

- Common: harmless wildlife and gathering pressure.
- Common: deer and passive/reactive badgers.
- Uncommon: boars, if the charge design works.
- Rare: strange inactive landmark or supernatural hint.
- Very rare or absent: wolves, depending on whether boars can carry the light-danger role.
- Absent: Necromancer graves, Blood Mages, and active supernatural combat.

### Possible Enemy-Related POIs

- Deer grazing clearings.
- Small burrows or badger setts.
- Boar feeding grounds.
- Strange stone marker.
- Inactive strange POIs that imply deeper world history without starting combat.

### Player Learning Goals

By the end of Plains, the player should understand:

- How to read wildlife behavior.
- That some animals flee, some ignore, and some may charge if threatened.
- That POIs can be useful, strange, or risky without always being combat.
- That the world has deeper danger beyond the starting area.

## Biome 2: Forest

### Core Promise

The Forest is where nature becomes territorial and the player starts being hunted.

The player should feel:

- "The world is denser and less safe."
- "I need to pay attention to sound, spacing, and escape paths."
- "Wolves own this place."
- "The strange things hinted earlier may be closer here."

### Enemy And Wildlife Pillars

- Wolves are the primary combat identity.
- Denser terrain creates more navigation and escape decisions.
- POIs can have stronger danger and territory ownership.
- Supernatural content becomes more plausible, but should not drown out the Forest identity.

### Creature Direction

Wolves should become the core Forest enemy. Wolf dens, patrols, pack behavior, and chase pressure belong here.

Deer can still exist, but as part of the food-chain texture. They should be less dominant than in Plains.

Badgers can appear here too, still primarily as reactive wildlife. They may be more nervous or quicker to burrow because the Forest is more dangerous.

Boars can be more dangerous here if used in both biomes, but they should not compete with wolves as the main identity.

Necromancer graves fit better here or later as optional dangerous landmarks. The Forest is the earliest biome where an active Necromancer encounter could feel natural without overwhelming the opening experience, but it should still be rare enough that wolves remain the biome identity.

Blood Mages should remain tied to Necromancer encounters rather than becoming normal Forest spawns.

### Danger Budget

Suggested danger profile:

- Common: wolves, wolf signs, den territory, forest obstacles.
- Uncommon: defensive wildlife and risky resources.
- Rare: Necromancer grave or supernatural POI.
- Encounter-bound only: Blood Mages.

### Possible Enemy-Related POIs

- Wolf dens.
- Carcass sites or clawed trees.
- Hunter remains or abandoned camp.
- Deep forest shrine.
- Necromancer grave.
- Corrupted clearing, if the supernatural thread needs stronger presence.

### Player Learning Goals

By the end of Forest, the player should understand:

- Wolves are a real biome identity, not random noise.
- Some areas are owned by enemies.
- Running, positioning, and terrain matter.
- Optional POIs can be meaningfully dangerous.
- Supernatural threats exist, but are still special.

## Transition Between Biomes

The transition from Plains to Forest should feel like crossing from open living land into contested territory.

Useful enemy and wildlife signals:

- Deer becoming less common or more skittish.
- Distant howls before direct wolf encounters.
- Tracks, carcasses, claw marks, or ruined camps.
- More frequent territorial behavior.
- A clear increase in wolf pressure once the player is in Forest content.

## Content Authoring Order

1. Reduce regular wolf pressure from Plains.
2. Increase passive wildlife density in Plains.
3. Repurpose Badger as passive/reactive wildlife with fleeing and burrowing behavior.
4. Prototype Boar as light danger using a run-charge contact damage pattern.
5. If Boar charge works, keep wolves mostly out of Plains.
6. If Boar charge does not work, keep boars passive and allow wolves to appear rarely in Plains.
7. Move wolves and wolf dens into Forest as the main danger identity.
8. Keep Necromancer graves out of Plains; use inactive strange POIs for foreshadowing instead.
9. Validate pacing by sampling many generated seeds, not by authoring a fixed route.

## Playtest Questions

- Does Plains feel alive before it feels dangerous?
- Does the player get enough time to learn enemy and wildlife rules without being bored?
- Do passive and reactive creatures make the world feel richer without becoming noise?
- Can Boar charge be read and avoided if it becomes light danger?
- Does Forest feel meaningfully different from Plains?
- Do wolves feel like the Forest identity?
- Does supernatural content feel special rather than noisy?
- Across multiple seeds, does Plains reliably produce "alive first, dangerous second"?

## Open Decisions

- Can Boar charge be made readable and fair using only the run animation?
- If Boar charge fails, should wolves become rare Plains danger or should Plains stay nearly non-hostile?
- Should wolves be completely absent from Plains, or appear only near Forest borders?
- What seed-sampling thresholds define good pacing for Plains and Forest?
