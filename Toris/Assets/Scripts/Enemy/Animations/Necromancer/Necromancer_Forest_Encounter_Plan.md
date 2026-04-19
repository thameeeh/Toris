# Necromancer Forest Encounter Plan

This document defines the first world-integration plan for the Necromancer.
It is a planning document, not a changelog.

## Purpose

The Necromancer is already designed as a combat enemy.
What is missing is its world-site integration inside the Forest biome.

This plan covers:

- what the Forest encounter is
- how often it appears
- how it is discovered and started
- how it persists after being cleared
- what technical pieces are still missing

## Resolved Design Decisions

### Encounter Count

- the Necromancer site is unique in identity, but not singular in count
- each Forest run should contain at least `1` and at most `3` Necromancer grave sites

### Site Fantasy

- the site is a grave
- the Necromancer was part of a traveler expedition into the Forest
- that group was attacked
- everyone is gone except the Necromancer
- what remains at the site is the grave, the memory of the failed expedition, and the Necromancer’s blood-magic / summoning identity

### Discovery

- the site is slightly tucked away from the main road
- a road branch should lead toward it
- that branch should trail off before the site itself so the site still feels hidden / discovered rather than fully civilized

### Initial Presence

- the Necromancer is hidden until the player interacts with the grave

### Fight Trigger Rules

- the Necromancer can enrage when the player enters its vicinity
- if the player attacks, the Necromancer always enrages

### Cleared State

- once defeated, the site is permanently cleared for that run
- the site remains in the world afterward, similar to the Wolf Den pattern
- the site should still be inspectable / lootable once, but the combat encounter does not return during that run

### Reward State

- there is currently no loot-table system in place
- this is now an identified missing piece for the enemy/world encounter pipeline
- reward design should become a priority immediately after the first Necromancer world integration pass

### Arena Feel

- the site should stay mostly clean for combat readability
- decoration should support identity, not clutter the fight

## First-Pass Encounter Structure

## Site Shape

The Necromancer site should be a small grave clearing in the Forest:

- central grave or burial marker
- enough open ground for Necromancer spacing and Blood Mage phase-two behavior
- only light edge decoration
- no heavy obstacle clutter inside the main combat space

The combat identity should remain:

- readable
- open enough for kiting and summon pressure
- clearly different from a Wolf Den

## Start Flow

The first-pass start flow should be:

1. player discovers the tucked-away grave site
2. player follows the side road until it trails off
3. the grave becomes visible at the end of the approach
4. player walks up and interacts with the grave
5. the Necromancer reveal begins
6. the Necromancer becomes the combat encounter

## Persistence Model

The site should follow the same high-level persistence idea as Wolf Den:

- site exists as a placed world site
- site has a cleared/consumed state for the current run
- once cleared, the site remains as a spent location rather than disappearing entirely

The important difference from Wolf Den is presentation:

- Wolf Den is an active monster site
- Necromancer grave site should become an exhausted grave / resolved event site

## Technical Fit With Current World Systems

The current world-generation/site system already supports the right general shape:

- `Biome build step`
- `Site placement rule`
- `WorldSiteDefinition`
- `site prefab`
- `runtime state service`
- `consumed/cleared persistence`

So the Necromancer should be integrated as its own Forest site rule rather than as a generic ambient enemy spawn.

## Recommended First-Pass Assets / Runtime Pieces

### World Placement

- `NecromancerGraveSitePlacementRuleDefinition`
- `WorldSiteDefinition` for the Necromancer grave
- Forest biome build-step integration

### Site Runtime

- `NecromancerGrave` site bridge/runtime component
- `NecromancerGraveEncounterConfig`
- site prefab with grave visuals + trigger / interaction point

### Encounter Spawn

- Necromancer spawned by the grave site runtime
- grave site owns the encounter state for that run
- cleared state prevents re-fight during the same run

## Reward System Gap

The current project does not yet have a proper loot-table/reward pipeline for world encounters.

That means the Necromancer encounter exposes a real missing system:

- unique encounter reward definition
- one-time site reward state
- optional reward drop vs interactable grave/container reward

This should become a near-term priority after the Necromancer is successfully integrated into the Forest, because otherwise the encounter will exist without a strong payoff structure.

## Decoration Guidance

The site should feel distinct without turning into procedural clutter.

The best first-pass decoration approach is:

- keep the central combat space mostly clean
- put identity props around the edges
- prefer authored site-prefab dressing over broad random clutter

### Recommended Decoration Layers

#### Layer 1: Authored Core Props

Put these directly in the site prefab:

- grave marker / burial mound
- disturbed dirt
- simple ritual traces
- maybe one or two remains from the expedition

This is the safest and most readable identity layer.

#### Layer 2: Edge Dressing

At the outer edge of the clearing, add small amounts of:

- broken supplies
- minor grave clutter
- candles, bones, or expedition remnants
- sparse dead vegetation or corrupted patches

This should not enter the main movement space much.

#### Layer 3: Path Branching

The main road should suggest the site without fully delivering the player onto it.

Recommended behavior:

- branch path from a nearby main road toward the grave
- stop the branch before the final clearing
- let the last stretch be natural forest approach

That keeps the site:

- discoverable
- slightly hidden
- less artificial

### Best Technical First Pass For Decoration

For this project, the most realistic first-pass decoration plan is:

1. stamp or reserve a clean clearing for the grave site
2. use an authored grave-site prefab for the signature props
3. optionally add a small deterministic edge scatter later if needed

This is much safer than trying to fully proceduralize the site dressing immediately.

## Recommended Implementation Order

1. Create the Necromancer grave planning/runtime config types
2. Create the grave site prefab and site bridge/runtime component
3. Create the Forest site placement rule
4. Register the site rule into the Forest biome build steps
5. Spawn the combat Necromancer from grave interaction
6. Persist cleared state for the run
7. Validate encounter readability in-world
8. Add the reward system as the next priority

## First-Pass Non-Goals

These should not block first integration:

- full reward table system
- heavy procedural site decoration
- lore text / dialogue polish
- advanced unique grave prop scatter system

## Immediate Next Step

The next implementation step should be:

- define the `Necromancer grave site` as a Forest world site that follows the Wolf Den persistence model, but starts on grave interaction and clears permanently for the run after the encounter is resolved
