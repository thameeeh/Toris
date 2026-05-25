# Intro Prologue Sequence Plan

This is the real opening of the game, not a placeholder tutorial pass. The tutorial layer should support it, but the sequence itself should feel like the player is entering the world for the first time: a short story introduction, a controlled first path, a first enemy, a small reward, and then arrival at Safe Haven where the existing Guide flow can begin.

## Core Intent

- Make the first playable minutes feel authored and intentional.
- Teach only what the player needs immediately.
- Use text, image, music, and SFX to establish mood before gameplay.
- Let the player physically walk into Safe Haven instead of simply appearing there.
- Keep tutorial tips optional, based on the fresh-save Yes/No prompt.
- Keep the story prologue separate from tutorial tips: the prologue is part of the game, the tips are guidance.

## Confirmed Direction

- New saves always play the story intro, even when tutorial tips are disabled.
- Story intro pages should be player-paced with a `Continue` / press-to-forward control.
- The prologue should most likely be a separate scene that transitions into `MainArea` / Safe Haven when complete.
- The first real enemy is likely a Viking character, but the Necromancer can stand in while the flow is being built and tested.
- Failed-shot teaching should happen after the player naturally releases too early for the first time.
- Overdraw / over-hold teaching should happen after the player naturally holds too long for the first time.
- For now, the player walks up to the Guide and interacts manually after reaching Safe Haven.
- Later, Safe Haven arrival may use a short authored walk-up animation into the Guide conversation.

## Fresh Save Flow

| Order | Beat | What Happens | Tutorial If Enabled |
| --- | --- | --- | --- |
| 1 | New Save Prompt | Player picks an empty slot and is asked whether they want tutorial tips. | Stores tutorial enabled/disabled for this save slot. |
| 2 | Story Intro Screen | Fade in image/text with music and light SFX. Use 1-2 background images and short text pages. Plays on every new save. | No mechanical tips yet. |
| 3 | Spawn In Intro Area | Player appears at the start of a controlled path. Everything except the intended route is blocked by level design. | Movement tip appears. |
| 4 | First Walk | Player moves along the path and learns basic movement in a safe space. | Movement tip completes when movement is detected. |
| 5 | First Threat Reveal | A weak story enemy blocks the path. Test with Necromancer; likely ship with a Viking intro enemy. | Shooting intro starts. |
| 6 | Bow Draw Lesson | Player learns to hold LMB to draw. | Tip waits for bow draw start. |
| 7 | Failed Shot Lesson | Player learns that releasing too early fails, but only after they do it once. | Tip appears after first dry release. |
| 8 | Ready Shot Lesson | Player learns to hold until ready, then release. | Tip waits for `ShootReady`, then `ShotFired`. |
| 9 | Overdraw Lesson | Player learns that holding too long worsens aim, but only after they do it once. | Tip appears after first overdraw / over-hold signal. |
| 10 | First Kill | Enemy dies. Player receives small XP/gold and possibly one simple drop. | Short reward/loot tip if needed. |
| 11 | Path To Haven | Player continues forward through a calm final stretch. | No tip unless pickup/loot needs explaining. |
| 12 | Safe Haven Arrival | Player crosses into the Safe Haven area. | Optional location title/card, no heavy mechanics. |
| 13 | Guide Handoff | Player walks up to the Guide and interacts. Existing `Guide_Intro` conversation continues from here. | Tutorial overlay should stay out of dialogue unless explicitly authored. |

## Story Presentation

The opening story should be short and readable. It should not explain the entire world. It should give enough context that arriving at Safe Haven feels meaningful.

### Presentation Style

- Full-screen fade from black.
- One background image at a time.
- 3-5 sentences total per screen, not a wall of text.
- Slow text reveal or simple page advance.
- Music starts low and continues into the playable intro if it fits.
- SFX should be sparse: wind, distant magic, low impact, faint camp/haven ambience.
- Advance with click/confirm so the player controls the reading pace.

### Possible Story Cards

These are draft copy, not final lore.

| Card | Background | Text Draft |
| --- | --- | --- |
| 1 | Dark road / wilderness edge | The road behind you is gone now. Whatever life you had before the border, it belongs to someone else. |
| 2 | Distant corrupted woods / ruined path | Out here, the wilds are not empty. Things that should be dead still move, and things that still breathe learn to hide. |
| 3 | Faint haven light / far gate | Somewhere ahead is Safe Haven, a place built by people who had nowhere else to go. Reach it, and you might last the night. |

## Intro Area Design

This should be a bespoke first-run area or controlled first-run section, not normal free exploration.

Recommendation: make this a separate prologue scene. It keeps the authored intro path, blockers, enemy tuning, story screen timing, and skip logic away from normal `MainArea` complexity. When the prologue is complete, transition into `MainArea` at the Safe Haven entry spawn point. If the save has already completed the prologue, load `MainArea` directly.

### Layout Goals

- One obvious forward path.
- Blocking is done by authored environment pieces: cliffs, trees, broken walls, debris, water, darkness, collapsed road, or similar.
- The player should not need a minimap or quest marker to know where to go.
- The first enemy should be placed where it cannot be ignored but also cannot overwhelm the player.
- The post-combat path should be calmer, giving the player a breath before Safe Haven.

### Suggested Map Shape

1. Spawn at a narrow trail.
2. Short movement corridor.
3. Small clearing with the first enemy.
4. Reward pickup space.
5. Exit trail with Safe Haven visible or implied ahead.
6. Transition trigger / gate / threshold into Safe Haven.

## First Enemy

The first enemy should communicate that the world is dangerous, but it should not be a real test of build strength.

### Good Candidates

| Candidate | Why It Works | Risk |
| --- | --- | --- |
| Test Necromancer | Good temporary implementation target because the enemy already exists. | Not the intended final flavor if the Viking becomes the real intro enemy. |
| Weakened Necromancer | Connects immediately to the darker enemy faction and feels story-relevant. | If it looks too powerful, losing to it feels silly. |
| Apprentice Necromancer | Same theme, easier to justify as fragile. | Requires a new enemy variant/name. |
| Wounded Blood Mage | Visually dramatic and magical. | Blood mage may imply mechanics the player has not learned yet. |
| Intro Viking | Fits the character asset you have and can become the real authored first enemy. | Needs setup/tuning if it is not already an enemy prefab. |
| Custom Intro Cultist / Exile | Easy to tune and story-control. | Requires new enemy setup. |

Recommended implementation path: **test with Necromancer**, then replace with the **Intro Viking** once its enemy setup is ready.

The enemy can have:

- Low health.
- Slow attack cadence.
- Poor pursuit range.
- No summoning or only a harmless/slow cast tell.
- Guaranteed small XP and gold reward.
- Optional small loot drop if pickup needs teaching.

## Shooting Tutorial Behavior

The bow lesson must account for the actual mechanic: holding matters, releasing too early fails, and a ready shot fires.

### Tutorial-On Flow

1. Pause briefly when enemy appears.
2. Tip: "Hold LMB to draw your bow."
3. Unpause and wait for `PlayerBowController.DrawStarted`.
4. Wait naturally for the first early release. If it happens, tip: "That was too early. The bow has to be ready before the arrow flies."
5. Tip: "Hold until the bow is ready."
6. Wait for `PlayerBowController.ShootReady`.
7. Tip: "Release to fire."
8. Wait for `PlayerBowController.ShotFired`.
9. If the player holds too long for the first time, tip: "Holding too long strains the shot and makes it less accurate."
10. Let combat continue until enemy dies.

### Tutorial-Off Flow

- No instructional overlay.
- Same enemy, same path, same story.
- The player learns organically.

## Rewards

The first reward should be visible but not economically important.

Recommended reward:

- Small XP amount.
- Small gold amount.
- Optional guaranteed simple item drop only if we want to teach pickup.

If the player must pick something up, the path should pause naturally around the drop. Do not require inventory management yet unless the opening explicitly wants that.

## Safe Haven Arrival

Safe Haven should feel like relief after the enemy encounter.

Possible arrival beats:

- Music changes or warms.
- The path opens slightly.
- A gate, lanterns, campfire light, or settlement silhouette appears.
- A location title appears: `Safe Haven`.
- The Guide is placed near the entry route or becomes the first obvious interactable.

The Guide then starts or continues the existing introduction conversation. This keeps the prologue from fighting the current quest setup.

## Technical Shape

### New Or Updated Systems

| System | Responsibility | Boundary |
| --- | --- | --- |
| Prologue scene/area setup | Owns the authored path, blockers, first enemy, and Safe Haven transition. | Level content, not tutorial logic. |
| Story intro screen | Displays image/text/music before gameplay. | Presentation only; does not own quest or tutorial state. |
| Tutorial runtime | Shows optional tips, waits for signals, marks completed steps. | Tutorial decisions live here. |
| Tutorial signal bridge | Converts neutral gameplay events into tutorial signals. | Subscribes to player/bow events, but does not change player behavior. |
| Save/session | Stores tutorial enabled and completed steps. | Persistence only. |
| Quest/dialogue | Starts Guide flow after arrival. | Does not need to know the mechanical tutorial internals. |

### Needed Tutorial Runtime Extensions

- Full-screen story card presentation, probably separate from the small tooltip overlay.
- Signal-gated tutorial steps.
- Step mode that pauses for explanation, then unpauses for the required action.
- No-anchor centered prompts for movement/combat instructions.
- Optional anchor prompts for HUD bars, later.
- Per-step button text such as `Continue`, `Try it`, `Got it`.
- Prologue-completed save flag so future loads can skip directly to `MainArea`.
- Optional one-shot reactive tips for first dry release and first overdraw.

### Needed Gameplay Signals

Use existing events where possible:

- Movement detected from `PlayerInputReaderSO.Move`.
- Bow draw from `PlayerBowController.DrawStarted`.
- Bow ready from `PlayerBowController.ShootReady`.
- Dry release from `PlayerBowController.DryReleased`.
- Shot fired from `PlayerBowController.ShotFired`.
- Overdraw / over-hold from bow draw duration crossing `BowSO.overHoldStartsAt`.
- Enemy killed from the enemy/death event path, if one exists.
- Item picked up from item pickup event path, if needed.
- Safe Haven entered from a transition/trigger event.

These should be bridged into tutorial/prologue events without adding tutorial-specific logic into player or enemy controllers.

## Suggested Step IDs

| Step ID | Purpose |
| --- | --- |
| `prologue.story.card_1` | First story screen |
| `prologue.story.card_2` | Second story screen |
| `prologue.story.card_3` | Third story screen |
| `prologue.movement` | Basic movement |
| `prologue.first_threat` | Enemy reveal |
| `prologue.bow.draw` | Hold LMB to draw |
| `prologue.bow.dry_release` | Early release failure |
| `prologue.bow.ready` | Wait until ready |
| `prologue.bow.fire` | Release to shoot |
| `prologue.bow.overdraw` | Holding too long worsens aim |
| `prologue.reward` | XP/gold/drop explanation |
| `prologue.safe_haven_arrival` | Arrival title or short note |
| `smith.forge_tab.intro` | Existing Smith contextual tip |

## Resolved Authoring Decisions

- Story intro screens play for every new save.
- Story cards are player-paced with a continue/forward input.
- Separate prologue scene is recommended, then transition to `MainArea`.
- Test with Necromancer; likely ship with a Viking intro enemy.
- Do not force a failed shot. Teach it after the player fails naturally.
- Teach overdraw only after the player overdraws naturally.
- Player walks to the Guide and interacts for now.

## Remaining Authoring Decisions

1. Should the first enemy drop a physical item, or only grant XP/gold?
2. What exact XP/gold reward should the first kill grant?
3. Should story cards be skippable after first viewing on a save slot?
4. What is the final name/flavor of the Viking intro enemy?
5. What background images and music/SFX should the story cards use?
