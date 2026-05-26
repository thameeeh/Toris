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
- The current first gameplay enemy is a prologue minion wolf variant. A story-specific enemy can replace it later if the intro needs a stronger narrative hook.
- Failed-shot teaching should happen after the player naturally releases too early for the first time.
- Overdraw / over-hold teaching should happen after the player naturally holds too long for the first time.
- Those reactive bow tips remain armed across later gameplay until each has been shown once for that save.
- For now, the player walks up to the Guide and interacts manually after reaching Safe Haven.
- Later, Safe Haven arrival may use a short authored walk-up animation into the Guide conversation.

## Current Implementation Status

Completed:

- New saves route to the `Prologue` scene.
- `Prologue` transitions into `MainArea` through `PrologueExitTrigger`.
- Hand-authored Prologue tilemaps initialize `TileNavWorld` through `PrologueNavigationBootstrap`.
- The first Prologue enemy can respect water and obstacle boundaries through the normal navigation path.
- Opening story cards now play at Prologue start through `PrologueStorySequenceController`.
- Arrival story cards can play from `PrologueExitTrigger` before the Safe Haven scene load.
- Story cards lock gameplay input until the player advances through them.
- `PrologueTutorialFlowController` starts the first optional gameplay tutorial beat after the opening story cards.
- The first beat shows a `WASD Move` prompt above the player and completes when movement input is detected.
- Tutorial capability locks now let the Prologue block unrelated inputs without taking over the whole input manager.
- The Prologue minion wolf variant can start dormant and wake only when `PrologueWolfEncounterTrigger` is entered.
- The wolf encounter trigger starts the optional `Hold LMB to shoot` prompt and keeps unrelated inputs gated during the fight.
- `PlayerBowController` now publishes neutral underdraw and overdraw signals through `PlayerBowEventsSO`.
- The shared gameplay UI shows one-shot reactive bow tips on the first early release and first overdraw, even after Prologue, pausing until the player continues with `Space` or `Enter`.
- The Prologue wolf now uses a fixed lesson loot table: one `Training Bow`, one `Minor Healing Potion`, 5 gold, and 8 XP.
- After the wolf dies, optional tips spotlight the XP/level reward, teach `E Pick Up`, then guide the player through the visible HUD menu into Inventory.
- The Prologue backpack and potion slots start empty so its first item interaction comes from the authored wolf drops.
- Inside Inventory, the optional lesson spotlights the dropped `Training Bow`, waits for it to be equipped, opens Stats, asks the player to drag the potion into a potion slot, then highlights the HUD hotkeys.
- Completing the final post-wolf HUD hotkey beat disables the authored `prologueBlocker` scene object so the player can continue toward Safe Haven.

Still open:

- Assign final background images, music, and SFX for the story cards.
- Add a prologue-completed save flag so future loads can skip directly to `MainArea`.
- Add optional ready-shot/release guidance on top of the existing bow ready and shot fired signals, if the fight still needs it after playtesting.

## Fresh Save Flow

| Order | Beat | What Happens | Tutorial If Enabled |
| --- | --- | --- | --- |
| 1 | New Save Prompt | Player picks an empty slot and is asked whether they want tutorial tips. | Stores tutorial enabled/disabled for this save slot. |
| 2 | Story Intro Screen | Fade in image/text with music and light SFX. Use 1-2 background images and short text pages. Plays on every new save. | No mechanical tips yet. |
| 3 | Spawn In Intro Area | Player appears at the start of a controlled path. Everything except the intended route is blocked by level design. | Movement tip appears. |
| 4 | First Walk | Player moves along the path and learns basic movement in a safe space. | Movement tip completes when movement is detected. |
| 5 | First Threat Reveal | A weak prologue minion wolf blocks the path. | Shooting intro starts. |
| 6 | Bow Draw Lesson | Player learns to hold LMB to draw. | Tip waits for bow draw start. |
| 7 | Failed Shot Lesson | Player learns that releasing too early fails, but only after they do it once. | Tip appears after first dry release. |
| 8 | Ready Shot Lesson | Player learns to hold until ready, then release. | Tip waits for `ShootReady`, then `ShotFired`. |
| 9 | Overdraw Lesson | Player learns that holding too long worsens aim, but only after they do it once. | Tip appears after first overdraw / over-hold signal. |
| 10 | First Kill | Enemy dies. Player receives fixed introductory XP/gold, a Training Bow, and a Minor Healing Potion. | Spotlight XP/level, show `E Pick Up`, guide clicks through the HUD menu into Inventory, guides equipping the Training Bow, opens Stats, prepares the potion, then points out the HUD hotkeys. |
| 11 | Path To Haven | Player completes the inventory lesson, then continues through a calm final stretch. | The path blocker handoff is the next authored slice. |
| 12 | Safe Haven Arrival | Player reaches the end trigger, sees final arrival story cards, then transitions into Safe Haven. | Optional location title/card, no heavy mechanics. |
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

Current implementation path: use the **prologue minion wolf** as the first playable threat. The candidates above remain useful if the opening later needs a more story-specific enemy.

The enemy can have:

- Low health.
- Slow attack cadence.
- Poor pursuit range.
- No summoning or only a harmless/slow cast tell.
- Guaranteed small XP and gold reward.
- Optional small loot drop if pickup needs teaching.

## Shooting Tutorial Behavior

The bow lesson must account for the actual mechanic: holding matters, releasing too early fails, and a ready shot fires.

## Playable Prologue Tutorial Flow

The playable tutorial should gate capabilities gently instead of locking the entire game. The player should always feel like they are playing, but only the systems needed for the current beat should be available. This prevents early menu spam, potion hotkeys, combat inputs, or unrelated panels from pulling the player away before those systems are introduced.

### Capability Gating Rule

- Enable only the next needed capability.
- Restore normal capability access as each lesson completes.
- Avoid one broad global lock for the whole prologue.
- Keep the gating centralized in a prologue/tutorial flow controller, not scattered through inventory, combat, pickup, or UI scripts.

Recommended capabilities:

| Capability | Starts As | Unlock Beat |
| --- | --- | --- |
| Movement | Enabled after opening story cards | Spawn / movement prompt |
| Bow attack | Disabled | Wolf reveal / shooting lesson |
| Pickup | Disabled unless needed by world defaults | Wolf death / loot lesson |
| Inventory open | Disabled | Pickup or inventory lesson |
| Equipment changes | Disabled or unguided until inventory lesson | Training bow lesson |
| Stats panel | Hidden/unguided until equipment lesson | After equipping tutorial bow |
| Potion slots / potion hotkeys | Disabled or unguided until potion lesson | Potion slot lesson |
| Unrelated menus | Disabled during prologue lessons | After inventory lesson completes or on Safe Haven entry |

### Beat-By-Beat Flow

| Beat | Player Experience | Tutorial Behavior | Capability State |
| --- | --- | --- | --- |
| Spawn | Player appears after the opening cards. | Fade in a small world-space prompt above the player: `WASD Move`. Fade it out once movement input is detected. | Movement enabled; combat, inventory, potion hotkeys, and unrelated menus gated. |
| Explore Path | Player can move around and settle into controls. | No heavy overlay. Let the player walk naturally toward the authored wolf area. | Movement remains enabled. |
| Wolf Reveal | Wolf becomes visible or the player enters the encounter area. | Show `Hold LMB to shoot`. | Bow attack enabled; inventory and unrelated menus still gated. |
| First Underdraw | Player releases too early for the first time, during this fight or later combat. | Pause and show tip: underdrawing fails the shot because the bow was not ready. Resume after `Space` or `Enter`. | Combat is held while the tip is open; the tip remains eligible until triggered once. |
| First Overdraw | Player holds too long for the first time, during this fight or later combat. | Cancel the held draw, pause, and explain that overdraw makes the shot unstable. Resume after `Space` or `Enter`. | Combat is held while the tip is open; the tip remains eligible until triggered once. |
| Wolf Death | Wolf dies and drops fixed tutorial loot. | Pause with a spotlight on Level and XP, explaining that enemies grant XP and gold. Resume when the player dismisses it. | Gameplay held while the reward callout is open. |
| Loot Pickup | The rewards remain on the ground after the callout. | Show `E Pick Up` above the player until both lesson items are collected. | Pickup enabled; menus still gated. |
| Inventory Open | Player has picked up the loot. | Darken the screen and spotlight the HUD menu toggle; after it is clicked, spotlight the visible Inventory button. If the HUD action panel is already open, skip directly to its visible Inventory button; if Inventory is already open, continue directly to the bow. Do not rely on teaching its shortcut first. | Gameplay paused; only the highlighted UI route advances the lesson. |
| Equip Training Bow | Wolf drop includes a predetermined `Training Bow`. | Spotlight that inventory item and wait until the player right-clicks it into the weapon slot. | Equipment changes enabled for the tutorial item. Implemented. |
| Stats Button | After equipping, point to the inventory stats button. | Explain that gear changes visible stats and builds can improve over time. | Implemented. |
| Potion Slots | Wolf also drops a simple potion. | Explain potion slots, ask the player to drag the potion into one, then spotlight the HUD `1` / `2` hotkeys. | Implemented. |
| Path Unblocked | Inventory lesson completes. | Disable the authored `prologueBlocker` scene object. | Implemented. |
| Safe Haven Approach | Player continues along the path. | No more mechanical teaching unless something unexpected needs a small reminder. | Normal prologue play. |
| Arrival Cards | Player reaches the exit trigger. | Arrival story cards play, then transition into Safe Haven. | Story overlay owns input until the transition starts. |

### Tutorial Loot

The Prologue wolf uses predetermined tutorial loot instead of normal random loot:

| Drop | Purpose |
| --- | --- |
| Training Bow | Teaches pickup, inventory, equipment, and visible stat changes. |
| Minor Healing Potion | Teaches potion slots and consumable preparation. |

The tutorial loot should be low value and clearly introductory. Its job is to teach interaction flow, not to become a long-term reward.

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
11. Pause and spotlight the Level/XP HUD, explaining the XP and gold reward.
12. Resume for `E Pick Up`, then pause again to guide the player through the HUD menu button into Inventory.

### Tutorial-Off Flow

- No instructional overlay.
- Same enemy, same path, same story.
- The player learns organically.

## Rewards

The first reward should be visible but not economically important.

Recommended reward:

- Small XP amount.
- Small gold amount.
- Guaranteed `Training Bow` and `Minor Healing Potion` lesson drops.

The first authored reward-to-inventory handoff is implemented: reward spotlight, grounded pickup, HUD menu toggle, Inventory selection, equipping the Training Bow, opening Stats, preparing the potion slot, and introducing HUD potion hotkeys.

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
- Optional one-shot reactive tips for first dry release and first overdraw. Implemented across gameplay scenes through shared UI wiring, with player-controlled dismissal.

### Needed Gameplay Signals

Use existing events where possible:

- Movement detected from `PlayerInputReaderSO.Move`.
- Bow draw from `PlayerBowController.DrawStarted`.
- Bow ready from `PlayerBowController.ShootReady`.
- Early release from `PlayerBowController.UnderdrawReleased`.
- Generic dry release / animation cancel from `PlayerBowController.DryReleased`.
- Shot fired from `PlayerBowController.ShotFired`.
- Overdraw / over-hold from `PlayerBowController.OverdrawStarted`.
- `PlayerBowEventsSO` carries underdraw and overdraw occurrences across gameplay scenes to the shared UI-side reactive tutorial presenter.
- Enemy killed from the enemy/death event path, if one exists.
- Tutorial loot collected by observing `UIInventoryEventsSO.OnInventoryUpdated` and confirming both authored items are present in the backpack.
- Reward explanation shown from the wolf death signal against registered HUD Level/XP bounds.
- HUD menu and Inventory selections advanced through registered click-through tutorial anchors.
- Inventory lesson opened from `UIEventsSO.OnScreenOpen` for `ScreenType.Inventory`.
- Safe Haven entered from a transition/trigger event.

These should be bridged into tutorial/prologue events without adding tutorial-specific logic into player or enemy controllers. The bow now reports neutral facts only; the UI-side tutorial presenter decides whether a saved tip is still due.

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
| `prologue.inventory.equip_training_bow` | Equip the first weapon inside Inventory |
| `prologue.inventory.stats_toggle` | Open Stats after equipping the first weapon |
| `prologue.inventory.stats_panel` | Explain the visible build stats |
| `prologue.inventory.potion_slots` | Explain prepared potion slots |
| `prologue.inventory.assign_potion` | Drag the first potion into a potion slot |
| `prologue.hud.potion_hotkeys` | Explain the HUD `1` / `2` potion hotkeys |
| `prologue.safe_haven_arrival` | Arrival title or short note |
| `smith.forge_tab.intro` | Existing Smith contextual tip |

## Resolved Authoring Decisions

- Story intro screens play for every new save.
- Story cards are player-paced with a continue/forward input.
- Separate prologue scene is recommended, then transition to `MainArea`.
- The current first gameplay enemy is the prologue minion wolf variant.
- Do not force a failed shot. Teach it after the player fails naturally.
- Teach overdraw only after the player overdraws naturally.
- Player walks to the Guide and interacts for now.

## Remaining Authoring Decisions

1. Should the first enemy drop a physical item, or only grant XP/gold?
2. What exact XP/gold reward should the first kill grant?
3. Should story cards be skippable after first viewing on a save slot?
4. Should the prologue wolf stay as the final first enemy, or be replaced by a more story-specific enemy later?
5. What background images and music/SFX should the story cards use?
