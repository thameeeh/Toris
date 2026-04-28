# Quest Documentation

## Purpose

This document defines how quests, dialogue, jobs, rewards, and story progression should work in Toris.

The quest system is the backbone for adding content to the game.

The goal is not to make one working quest.

The goal is to make a content pipeline where quests can be authored mostly through Pixel Crushers, while Toris gameplay systems report facts, grant rewards, and open game UI when asked.

## North Star

Adding story content should feel like authoring, not rebuilding systems.

The long-term workflow should be:

- write dialogue in Pixel Crushers
- define the quest in Pixel Crushers
- configure available quest sources in the scene or asset
- configure which gameplay facts progress the quest
- configure rewards and unlocks
- test the flow in-game

If adding one normal quest requires editing many unrelated scripts or scene objects, the system is drifting in the wrong direction.

## Core Quest Goals

Quests should:

- teach the player about the world
- tell the main story
- teach basic gameplay without explaining every mechanic forever
- give rewards
- create progression
- make the existing combat, world, NPC, item, crafting, and shop systems feel purposeful

The main story is not fully written yet.

The current story direction is:

- the Kingdom casts people away for unknown reasons
- the player is one of the people cast out
- the Safe Haven is the player's foothold
- the Overworld is dangerous and strange for reasons the player should uncover
- finding out why the Kingdom does this, and why the Overworld is like this, is the larger story direction

The starting cast for the first serious storyline pass is:

- Player
- GuideNPC
- SmithNPC

Additional NPCs can exist later, but the first backbone should prove story flow with these two NPC anchors before expanding the cast.

## Ownership

Pixel Crushers owns:

- dialogue authoring
- conversation nodes
- dialogue choices
- quest definitions
- quest entries / objectives
- quest state
- story branches
- quest tracker / quest book UI where useful
- persistence for dialogue and quest state

Toris owns:

- player interaction
- enemy death reporting
- item pickup reporting
- NPC interaction reporting
- biome / scene / site reporting
- inventory changes
- shop opening
- crafting / reforging actions
- gold, XP, stats, unlocks, and rewards
- world-state changes triggered by quest completion

Toris should not become a second quest runtime.

Toris should provide reusable glue.

## Strategic Extensions

These are not required before the current quest journal and job flow is hardened.

They are important enough to keep in the architecture plan.

### World-State Integration

Quests should eventually be able to change the physical world.

Examples:

- clear a blockage
- open a gate
- activate a portal
- reveal a site
- disable a blocker after a quest state changes

Preferred direction:

- Pixel Crushers owns the quest state
- Toris owns the world objects
- a Toris bridge component observes Pixel Crushers quest state
- the bridge enables, disables, or swaps world objects based on that state

This should be generic.

Avoid one-off scripts like `OpenSpecificGateAfterGuideQuest`.

Prefer a reusable component such as `QuestStateWorldObjectActivator`.

### Dynamic NPC Presence

NPCs may eventually appear, disappear, or move based on quest state.

This is useful, but should be handled carefully.

Do not make `PixelCrushersConversationInteractable` own NPC visibility.

Preferred direction:

- keep conversation starting separate from NPC visibility
- use a separate component for quest-state or Lua-condition visibility
- let that component enable, disable, or swap NPC objects

Possible future component:

- `PixelCrushersConditionVisibility`

This can use a Pixel Crushers Lua condition later, but it is not urgent yet.

### Convention-Based Progress Mapping

Manual `QuestFactProgressRuleSetSO` assets are useful for explicit control.

However, creating a separate rule asset for every simple objective can become a bottleneck.

Preferred direction:

- keep explicit rule sets for complex quests
- add convention-based progress for simple quest objectives
- let the progress mapper attempt standard variable names before requiring manual setup

Example convention:

- fact: `Kill / LeaderWolf`
- quest: `Guide_Cull_Wolves`
- variable: `Guide_Cull_Wolves_Kill_LeaderWolf`
- required amount can come from quest fields, entry fields, or a configured convention rule

This should be a hybrid system, not a full replacement.

Use conventions for boring repeated cases.

Use explicit rule assets when the quest needs special behavior.

## Quest Source Rules

Quests can come from:

- Guide NPC
- other Safe Haven NPCs
- job boards
- world objects
- world encounters
- special sites such as graves, dens, gates, and future structures

The Guide is not the permanent owner of the story.

The Guide is the introduction carry.

The story backbone is the wider NPC cast inside Safe Haven.

Main quest ownership can move from NPC to NPC.

Side quests should not expire.

Side quests can always remain available unless the story specifically removes or completes their source.

## Quest Acceptance Rules

The player should initiate quests by choice.

Valid acceptance methods:

- dialogue choice
- choosing a quest from an NPC's available jobs view
- choosing a quest from a job board
- choosing an action on a world object

Quests should not silently begin without player intent, except for lightweight polish events such as a temporary encounter objective.

Job board flow:

- player walks up to a job board
- player interacts
- quest journal / available jobs view opens in all-source accept mode
- player chooses any currently grantable public job
- the job board does not need deep dialogue
- the player still returns to the relevant NPC/source for dialogue and turn-in when the quest requires it

Example polish objective:

- player awakens a Necromancer from a grave
- the game shows `Defeat the Necromancer`
- this does not need to behave like a normal reward quest

## Quest Type Rules

Supported quest types for the backbone:

- kill
- talk
- collect
- deliver
- explore
- clear site
- buy / sell
- level up
- enter biome
- interact with NPC
- interact with world object

The system should stay generic enough that new fact types can be added without rewriting every quest.

Quest objectives should be explicit.

Hidden objectives are not a priority.

## Main Quests

Main quests should:

- move the story forward
- reveal the world gradually
- introduce important NPCs
- introduce the Overworld
- create reasons to use existing gameplay systems
- unlock future story beats
- unlock batches of side quests when appropriate

Acts or chapters are acceptable and likely useful.

The main questline may eventually be required to finish the game, but the exact ending structure is not designed yet.

## Side Quests And Jobs

Side quests should mostly feel like jobs.

Some side quests can add lore.

Side quests should:

- be optional
- remain available
- give rewards
- give the player reasons to revisit combat, exploration, items, and crafting
- be available from NPCs or job boards

The player can have multiple quests active overall.

The player should only have one quest active from a specific source at a time.

Examples:

- one active Guide quest
- one active Smith quest
- one active job board quest
- not two active Guide jobs at once

Repeatable quests are allowed.

Repeatable quests should use cooldowns.

Repeatable quests should remember cooldowns and completion counts if needed.

Cooldown values should be configurable per quest.

Cooldowns can start simple and be adjusted through Inspector/content values rather than hardcoded constants.

## Dialogue Rules

Not every quest needs deep dialogue.

Story quests should use dialogue heavily.

Simple jobs can be much lighter.

NPCs should have idle lines when they have no active quest relevance.

NPC dialogue should change enough to avoid feeling broken.

Minimum NPC quest-state dialogue:

- no quest available
- quest available
- quest active reminder
- quest ready to turn in
- quest completed / idle again

Dialogue choices are mostly flavor for now.

Later, dialogue choices may matter for endings or branching outcomes.

Shop NPCs can mix dialogue and shop opening in the same conversation.

Example:

- NPC says welcome
- player chooses `Open shop`
- dialogue calls Toris command to open shop UI
- player can leave

Do not auto-open shop UI from normal story lines.

Smith story intro currently stays dialogue-only.

Shop opening should be an explicit player choice or a separate shop interactable.

Current Smith setup:

- `SmithNPC` is the canonical Smith scene/prefab object
- `SmithNPC` owns the Smith shop `InventoryManager`
- `SmithNPC` uses `PixelCrushersConversationInteractable` for story dialogue
- the Smith collider uses `DialogueNpcProximity` so the normal `PlayerInteractor` path starts dialogue
- the old scene-only `SmithStoryNPC` is deprecated and inactive
- dialogue can open the real Smith shop inventory with `TorisOpenScreen("SmithShop")`
- after the introduction, `Smith_AfterIntro` behaves like a small hub
- Smith hub choices can open the shop, open Smith-specific available jobs, or leave
- Smith-specific available jobs use Pixel Crushers quest group `SmithJobs`
- Smith's current prototype job is `Smith_Check_Forge`
- `Smith_Check_Forge` proves a talk/check-in job source outside the Guide flow

## Quest UI Rules

Needed quest UI:

- available jobs view
- active quest log
- completed quest history
- tracked quest HUD

Available jobs and active quests can live in the same larger quest-book style UI if clearly separated.

The preferred quest UI direction is to use Pixel Crushers UI wherever possible.

The current target prefab is:

- `Assets/Scripts/Quest/Prefabs/Standard UI Prefabs/Templates/Basic/Basic Standard UI Quest Log Window.prefab`

This prefab already supports quest groups through `useGroups`.

Important limitation:

- Quest groups organize quests visually.
- The stock Basic Standard UI Quest Log Window shows active quests and completed quests by default.
- Toris extends the Pixel Crushers quest log with `PixelCrushersQuestJournalWindow`.
- `PixelCrushersQuestJournalWindow` adds an Available Jobs view for Pixel Crushers quests in the `Grantable` state.
- `PixelCrushersDialogueCommandBridge` exposes `TorisOpenQuestJournal("Available")`, `TorisOpenQuestJournal("Active")`, and `TorisOpenQuestJournal("Completed")` to dialogue scripts.
- `TorisOpenQuestJournal("Available")` shows all grantable jobs.
- `TorisOpenQuestJournal("Available:GuideJobs")` shows only grantable quests whose Pixel Crushers `Group` is `GuideJobs`.
- `TorisOpenQuestJournal("Available:SmithJobs")` shows only grantable quests whose Pixel Crushers `Group` is `SmithJobs`.
- `TorisOpenQuestJournal("Available:All")` or `TorisOpenQuestJournal("Available:*")` is the intended pattern for a job board that lets the player accept every currently grantable job.
- `TorisOpenQuestJournal("Available:JobBoardJobs")` remains available if a board should show only board-specific jobs.
- the global `J` input opens the quest book in Active mode for inspection.
- global quest book inspection can show available jobs, but available jobs are read-only there.
- accepting available jobs should happen only when the journal is opened by a source such as Guide, Smith, or a job board.
- If no scene quest journal is assigned, the bridge can instantiate the configured quest journal prefab under a runtime overlay Canvas.
- The current journal extension creates an `Available Jobs` button when the prefab does not already provide one.
- The final direction should still feel like Pixel Crushers UI, not a separate Toris quest UI.

The Guide should open available jobs through dialogue.

The quest-log hotkey is:

- `J` opens the personal quest book / journal.

Minimum quest information:

- title
- description

Nice-to-have quest information:

- objectives
- rewards
- source NPC / board
- relevant location

Pixel Crushers quest book / quest tracker presets should be used where they fit.

Toris custom UI should only be added when Pixel Crushers does not provide the needed behavior.

If custom behavior is needed, prefer extending or wrapping Pixel Crushers UI over building a disconnected quest UI.

## Current Available Jobs Flow

The currently validated flow is:

- Guide reaches hub state after the Smith introduction quest
- player talks to Guide
- Guide starts `Guide_Hub`
- player chooses `Show me the extra jobs`
- dialogue calls `TorisOpenQuestJournal("Available:GuideJobs")`
- Toris opens the Pixel Crushers quest journal in Available Jobs mode
- the journal shows Pixel Crushers quests in the `Grantable` state
- selecting a grantable job shows its details
- the details panel shows an `Accept Job` button
- accepting the job changes the quest from `Grantable` to `Active`
- accepting the job activates the configured first quest entry
- the journal switches to Active quests and selects the accepted job
- jobs use the Pixel Crushers `Group` field as their source bucket
- if another quest in the same group is already active, the accept button is disabled
- the generated `Available Jobs` button preserves the source scope after viewing Active or Completed quests
- the same journal can switch between Available Jobs, Active quests, and Completed quests

The first-time post-Smith side-work route is also validated:

- player completes `Guide_Talk_To_Smith`
- player returns to Guide
- player chooses `Look at side work`
- the dialogue marks `Guide_Talk_To_Smith` as `success`
- the dialogue unlocks side work with `SafeHavenSideWorkUnlocked`
- the dialogue sets `Guide_Cull_Wolves` to `grantable` if it is still unassigned
- the dialogue calls `TorisOpenQuestJournal("Available:GuideJobs")`
- the journal opens immediately without requiring the player to restart the conversation

This proves the direction.

The previous temporary `PixelCrushersQuestOfferWindow` popup is no longer the preferred flow.

It can remain briefly as fallback while the journal is being hardened, but the target direction is the Pixel Crushers quest journal flow.

## Gameplay Input Lock Rules

Quest and dialogue UI should freeze gameplay input without disabling UI input.

Current behavior:

- Pixel Crushers conversations request a gameplay input lock through `UIEventsSO`
- the Pixel Crushers quest journal requests a gameplay input lock while open
- the temporary quest offer popup requests a gameplay input lock while open
- `InputManager` stores named gameplay locks in a set
- movement, interaction, dash, and combat are suppressed while any gameplay lock exists
- UI actions remain available so dialogue choices, continue buttons, journal tabs, and quest acceptance still work
- overlapping locks are safe because each system releases only its own lock id
- opening the personal quest book with `J` also uses the quest journal lock because the journal window owns the lock

Current lock ids:

- `PixelCrushersDialogue`
- `PixelCrushersQuestJournal`
- `PixelCrushersQuestOffers`

Rule for future quest-related UI:

- do not hard-reference the player controller
- do not add one-off movement flags to dialogue scripts
- request `UIEventsSO.OnGameplayInputLockRequested` when the UI opens
- request `UIEventsSO.OnGameplayInputUnlockRequested` when the UI closes or disables
- use a stable, descriptive lock id such as `JobBoardQuestJournal`

This keeps dialogue, quest journals, job boards, and future Pixel Crushers windows from fighting over player control.

## How To Add An NPC Job Source

Use this pattern for Guide, Smith, and later NPCs.

- create or choose a Pixel Crushers quest group, such as `GuideJobs` or `SmithJobs`
- set that group on every job quest offered by that source
- give the group a readable display name in Pixel Crushers
- add dialogue that marks the job quest `Grantable` when it becomes available
- open the journal with `TorisOpenQuestJournal("Available:GroupName")`
- keep the quest progress generic through facts and progress rules
- route active and turn-in conversations through `PixelCrushersConversationInteractable`
- configure rewards in `PixelCrushersQuestRewardSetSO`
- test that another active quest from the same group blocks accepting a second one

Example Smith command:

```lua
if CurrentQuestState("Smith_Check_Forge") == "unassigned" then
    SetQuestState("Smith_Check_Forge", "grantable")
end
TorisOpenQuestJournal("Available:SmithJobs")
```

Example Guide command:

```lua
if CurrentQuestState("Guide_Cull_Wolves") == "unassigned" then
    SetQuestState("Guide_Cull_Wolves", "grantable")
end
TorisOpenQuestJournal("Available:GuideJobs")
```

## Fact Reporting Rules

Gameplay systems report facts.

Facts should be generic.

A fact should describe what happened without knowing which quest needs it.

Examples:

- `Kill / LeaderWolf`
- `InteractNpc / SmithNPC`
- `PickUp / ItemId`
- `EnterBiome / Plains`
- `VisitSite / Grave`
- `ClearSite / WolfDen`
- `BuyItem / ItemId`
- `SellItem / ItemId`
- `LevelReached / Player`

Quest progress rules decide which facts matter.

Progress should count only while the quest is active unless explicitly configured otherwise.

Retroactive progress can exist, but it should be explicit.

One fact may progress multiple quests if the configured rules allow it.

## Reward Rules

Rewards can include:

- gold
- XP
- items
- unlocks
- dialogue changes
- shop unlocks
- world changes
- future quest unlocks

World-state rewards and world-state changes should stay minimal for now.

Use world changes only when the change is simple and clearly worth it.

Avoid building a large world-state simulation before the story backbone works.

Rewards should be shown before accepting when possible.

Rewards should be granted when turning in unless a quest is designed to auto-complete.

Rewards should not disappear if the inventory is full.

If the inventory is full:

- grant whatever can be granted safely
- do not delete rewards that cannot fit
- show an `Inventory full!` style message
- keep the quest reward claim available until the missing rewards can be claimed
- do not exhaust the claim button while required rewards are still blocked

## Quest Completion Rules

Some quests require returning to an NPC.

Some quests may auto-complete.

Turn-in quests should grant rewards at turn-in.

Auto-complete quests should grant rewards at completion only if that quest type is configured to do so.

Abandoning quests should be possible.

Abandoning should have a penalty.

Abandon penalty should be simple for now.

Penalty options:

- lose a percentage of XP
- lose a percentage of gold

The percentage should depend on quest difficulty.

Penalty values should be configurable per quest.

Penalty values can start as simple Inspector/content values and be tuned later.

## Persistence Rules

The following must save:

- quest states
- objective counts
- accepted jobs
- completed jobs
- abandoned jobs
- dialogue choices
- claimed rewards
- repeatable quest cooldowns / completion counts
- NPC dialogue state
- scene transitions between Safe Haven and Overworld

Save/load must not duplicate rewards.

Save/load must not reset active quest progress.

Save/load must not break NPC routing.

## Debugging Rules

Debugging should be useful but not flood the console.

Keep debug flags available on bridge components.

Editor debug logs should stay behind `#if UNITY_EDITOR`.

Useful debug output:

- conversation selected
- quest offer group opened
- quest accepted
- fact reported
- progress rule matched
- quest state changed
- reward granted
- reward blocked

Debug output should be turned on while testing broken flows and turned off when it becomes noise.

## Fragility Rules

The system is considered fragile if:

- adding one normal quest requires touching many unrelated places
- story content requires C# changes
- dialogue content is hardcoded into Toris scripts
- quest state routing is impossible to understand
- rewards can be duplicated
- save/load loses quest progress
- available jobs and active quests become separate confusing systems
- future teammates cannot follow the setup steps

The system is considered healthy if:

- most quest work happens in Pixel Crushers
- Toris scripts stay generic
- facts are reusable
- rewards are reusable
- job sources are reusable
- the same flow works for Guide, Smith, and job boards
- documentation explains how to add content

## Current Building Blocks

Current Toris-side bridge components:

- `PixelCrushersConversationInteractable`
- `PixelCrushersDialogueCommandBridge`
- `PixelCrushersQuestOfferWindow`
- `PixelCrushersQuestBridge`
- `PixelCrushersQuestFactReporter`
- `PixelCrushersQuestJournalWindow`
- `PixelCrushersQuestProgressMapper`
- `PixelCrushersQuestRewardAdapter`
- `QuestFact`
- `QuestFactType`
- `QuestFactProgressRuleSetSO`
- `PixelCrushersQuestRewardSetSO`
- `QuestFactSceneReporter`
- `QuestFactTriggerReporter`
- `DialogueNpcProximity`

These must remain generic.

No component should become `GuideOnlyQuestThing`.

## Massive Checklist

### Phase 1 - Make The Current Backbone Safe

- Verify Guide intro quest still works from fresh save
- Verify Smith talk quest still works from fresh save
- Verify Guide job popup opens only after the intended story beat
- Verify accepting `Guide_Cull_Wolves` starts the quest correctly
- Verify active Guide job reminder works
- Verify Guide job turn-in works
- Verify Guide returns to hub after job completion
- Verify rewards grant once only
- Verify completed jobs do not appear as available again unless marked repeatable
- Verify console logs are useful and not spammy
- Verify Save/Load keeps quest state and counters
- Verify Safe Haven to Overworld transition keeps quest state
- Verify Overworld to Safe Haven transition keeps quest state
- Verify player movement is locked during Pixel Crushers dialogue
- Verify player movement is locked while the quest journal is open
- Verify `J` opens the quest book in Active mode
- Verify available jobs in the global quest book cannot be accepted away from their source
- Verify source-opened available jobs can still be accepted
- Verify source-opened available jobs stay source-filtered after switching tabs
- Verify future job board all-source mode can accept every currently grantable job
- Verify dialogue choices and quest journal buttons still work while gameplay is locked

### Phase 2 - Turn Job Offers Into A Reusable Source System

- Replace prototype runtime popup visuals with a Pixel Crushers quest journal flow
- Use the Basic Standard UI Quest Log Window prefab as the first target journal UI
- Show `Grantable` quests as available jobs
- Add an Available Jobs tab/button to the Pixel Crushers quest journal
- Open the quest journal from dialogue with `TorisOpenQuestJournal`
- Accept `Grantable` quests from the journal details panel
- Support multiple offer groups such as `GuideJobs`, `SmithJobs`, `JobBoardJobs`
- Enforce one active quest per source through the Pixel Crushers quest `Group` field
- Open source-filtered available jobs with `TorisOpenQuestJournal("Available:GroupName")`
- Open all-source job board available jobs with `TorisOpenQuestJournal("Available:All")`
- Prove Smith has his own job source using `SmithJobs`
- Hide unavailable jobs based on quest state
- Show accepted active job from that source
- Show completed job state correctly
- Add support for repeatable jobs
- Add support for repeatable job cooldowns
- Add support for job unlock conditions
- Add support for reward preview
- Add support for partial reward granting and `Inventory full!` feedback
- Remove the temporary Toris runtime job popup after the journal replacement works

### Phase 3 - Expand Generic Fact Reporting

- Confirm enemy kill facts are stable
- Add NPC interaction facts
- Add item pickup facts
- Add item delivery facts
- Add biome reached facts
- Add site visited facts
- Add site cleared facts
- Add buy item facts
- Add sell item facts
- Add level reached facts
- Add world object interaction facts
- Document fact naming conventions
- Document how to add a new fact source

### Phase 4 - Expand Quest Progress Rules

- Support kill objectives
- Support talk objectives
- Support collect objectives
- Support deliver objectives
- Support explore objectives
- Support clear site objectives
- Support buy/sell objectives
- Support level-up objectives
- Support multi-step quest chains
- Support quests with multiple active objectives
- Support facts progressing multiple active quests
- Support retroactive progress only when explicitly configured
- Add convention-based progress mapping for simple repeated objectives
- Keep explicit rule sets for complex or special-case quest behavior

### Phase 5 - Rewards And Unlocks

- Verify gold rewards
- Verify XP rewards
- Verify item rewards
- Handle full inventory rewards safely
- Add unlock rewards
- Add shop unlock rewards
- Add dialogue unlock rewards
- Add future quest unlock rewards
- Add simple world-state unlock rewards if feasible
- Add reusable world-state activators for quest-state-driven world changes
- Avoid one-off world unlock scripts tied to specific quests
- Ensure rewards cannot duplicate after save/load

### Phase 6 - Dialogue Authoring Workflow

- Define NPC idle dialogue pattern
- Define quest available dialogue pattern
- Define quest active reminder pattern
- Define quest turn-in pattern
- Define quest completed pattern
- Define shop dialogue pattern
- Define job board dialogue pattern
- Define main story beat pattern
- Document how to call Toris commands from dialogue
- Document how to avoid hardcoding quest flow in C#

### Phase 7 - Opening Story Prototype

- Create a clean opening storyline from fresh game
- Introduce the player being cast away
- Introduce Safe Haven
- Introduce Guide
- Introduce basic movement naturally
- Introduce basic combat naturally
- Introduce the Smith
- Introduce the Overworld mysteriously
- Introduce first job source
- Introduce at least one side job
- Add 3-5 meaningful story beats
- Avoid rushing the main story in a few throwaway lines
- Prove NPC-to-NPC story ownership transfer works
- Consider dynamic NPC presence only after the basic story and job source flow is stable

### Phase 8 - Documentation For Future Content

- Write `How To Add A Story Quest`
- Write `How To Add A Side Job`
- Write `How To Add A Job Board Quest`
- Write `How To Add A Quest Fact`
- Write `How To Add Quest Rewards`
- Write `How To Debug A Broken Quest`
- Write `Naming Conventions`
- Write `Common Mistakes`

## Content Validation Slice

The smallest slice that proves the backbone is good:

- fresh game starts cleanly
- Guide introduces the world
- Guide gives first main quest
- first main quest teaches combat or Overworld entry
- Smith is introduced through a talk quest
- Guide unlocks available jobs
- Guide job popup works
- Smith has his own job source
- job board has its own job source
- one kill job completes
- one talk job completes
- one collect or deliver job completes
- one explore or biome quest completes
- at least one reward preview is visible
- at least one reward grants correctly
- saving and loading keeps everything intact

If this slice works, the project can move from system-building into content and polish with confidence.

## Opening Story Beats Draft

This is not final dialogue.

This is the first structure to prove the quest/story backbone.

### Beat 1 - Cast Away

Purpose:

- establish that the player was cast out by the Kingdom
- establish that the Safe Haven exists because others were cast out too
- introduce GuideNPC as the first stabilizing NPC

Gameplay proof:

- GuideNPC starts the opening conversation
- player can choose a flavor response about what happened
- choice is saved for later reference

### Beat 2 - Learn To Survive

Purpose:

- teach the player basic movement and combat without overexplaining
- frame the Overworld as dangerous but necessary

Gameplay proof:

- GuideNPC sends player to handle an immediate outside threat
- kill quest or simple combat objective completes through generic kill facts
- player returns to GuideNPC for turn-in

### Beat 3 - Safe Haven Has People

Purpose:

- prove the Safe Haven is not just a menu hub
- introduce SmithNPC as another story anchor
- transfer story attention from GuideNPC to another NPC

Gameplay proof:

- GuideNPC gives a talk quest
- player talks to SmithNPC
- SmithNPC reports an interaction fact
- player returns to GuideNPC

### Beat 4 - The Overworld Is Wrong

Purpose:

- GuideNPC explains that the Overworld is not normal wilderness
- hint that the Kingdom knows more than it says
- push the player toward exploration

Gameplay proof:

- player receives an explore / enter-biome / visit-site style objective
- biome or site fact completes the objective
- player returns to Safe Haven

### Beat 5 - Work Opens Up

Purpose:

- unlock optional jobs
- show that NPCs and job boards can provide repeatable or optional work
- separate main story from side content

Gameplay proof:

- GuideNPC opens available jobs through dialogue
- SmithNPC has his own job source
- job board can be interacted with directly
- at least one available job becomes active, completes, and turns in

### Beat 6 - First Real Mystery Hook

Purpose:

- establish the next story question
- move beyond tutorial structure
- give the player a reason to keep exploring

Gameplay proof:

- a new main quest starts from either GuideNPC or SmithNPC
- the quest points toward a grave, den, gate, or other world site
- this sets up the next content chunk without requiring the whole story to be finished now
