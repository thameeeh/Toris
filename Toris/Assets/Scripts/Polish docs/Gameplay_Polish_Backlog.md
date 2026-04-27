# Gameplay Polish Backlog

This document is a lightweight backlog for polish work that is still worth doing before final submission.

It is intentionally not a full bug database.

Use it to keep track of:

- things that are already functional but still rough
- things that are intentionally left as groundwork for later
- obvious polish passes worth returning to after bigger features

## Necromancer

### Working Baseline

- grave site encounter exists
- grave layouts can now be authored and spawned into the world
- necromancer + blood mage flow is functional
- loot exists

### Still Rough

- encounter completion feedback can be stronger once the fight is over
- grave clear state / site clear readability can be improved
- keep validating summon pressure and necromancer spacing in playtests after the recent AI fixes

### Later / Nice To Have

- extra reveal/presentation polish around the grave encounter
- more authored grave layout variants
- stronger post-fight reward presentation if the encounter should feel more special

## Wolf

### Working Baseline

- wolf pack / den encounter exists
- leader + minion behavior works
- howl / alert / death issues were recently cleaned up

### Still Rough

- keep watching for any remaining edge cases around den alert escalation
- leader/minion encounter readability can still be improved
- den clear feedback can be stronger once the encounter is finished

### Later / Nice To Have

- more encounter presentation around wolf dens
- stronger distinction between leader pressure and minion pressure

## World Dressing

### Working Baseline

- procedural world generation works
- special sites like graves and dens can exist in the generated world
- layout authoring workflow now works for handcrafted procedural features

### Still Rough

- generic biome areas can still feel too empty
- lakes and shorelines need a dressing pass
- more small authored points of interest are needed so the world feels less plain

### Later / Nice To Have

- shoreline decoration pass
- lake-edge vignettes
- more site types such as shrines, camps, and other authored landmarks
- additional grave variants made with the layout tool

## Persistence And Ability Follow-Ups

### Working Baseline

- player inventory, equipment, progression, and runtime stats transfer between `MainArea` and `ProceduralTiles`
- ability unlocks are now respected by gameplay runtime

### Still Rough

- active consumable timers / cooldown behavior between scenes is still a design choice to revisit
- repeated scene-swap testing should continue to make sure no state edge cases remain

### Groundwork Only

- ability slot persistence is implemented as groundwork
- there is still no real player-facing ability loadout / slot assignment system yet

## General Combat Feel

### Still Rough

- once content stabilizes, do one broader range / timing / reward tuning pass across encounters
- continue checking whether enemy rewards, aggression, and pacing feel consistent between encounters

## Good Bigger Tasks After This

- necromancer encounter completion / post-fight polish
- world dressing pass for generic biome spaces
- real ability loadout / slot assignment system
