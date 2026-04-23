# Pixel Crushers-First Quest And Dialogue Integration Plan

This document defines the new quest and dialogue direction for Toris.

The earlier custom Toris quest-runtime direction is no longer the active path.

The new rule is:

- Pixel Crushers Dialogue System is the quest and dialogue authority.
- Toris gameplay systems report meaningful gameplay facts into Pixel Crushers.
- Toris still owns combat, inventory, rewards, progression, world generation, enemies, and site logic.

The goal is not to build two systems and awkwardly merge them together.

The goal is to fully integrate the purchased Dialogue System asset into Toris and connect it to the game cleanly.

## Locked Decisions

The current implementation plan is locked to these choices:

- NPC conversations start through Toris `PlayerInteractor` and `IInteractable`
- Pixel Crushers trigger components are allowed only as temporary bootstrap tools, not as the long-term NPC interaction standard
- the first dialogue UI pass uses the stock Pixel Crushers dialogue UI
- the first quest UI pass uses the stock Pixel Crushers tracker / log if it is good enough
- first-pass persistence is only for `MainArea` <-> `ProceduralTiles` during the current play session
- the first real quest slice is `Kill_3_Leader_Wolves`
- that quest requires turn-in at the Guide NPC after the kills are done

## Core Ownership Split

Pixel Crushers owns:

- dialogue databases
- actors and conversations
- branching dialogue
- quest definitions
- quest state
- quest offer / active / success / completion flow
- quest log / quest tracker UI, at least for the first pass

Toris owns:

- enemy deaths
- item pickups
- scene travel
- site and encounter clearing
- player inventory
- player gold / XP / progression
- combat and stats
- world generation and world sites

The integration layer connects those two areas.

It should stay small and practical.

## What We Should Stop Doing

Do not continue building a separate Toris quest authority.

Avoid adding:

- custom quest definition ScriptableObjects as the main quest data
- a custom Toris `QuestManager` as the main quest authority
- a parallel custom quest state model
- a second custom quest log that competes with Pixel Crushers

If Toris needs code in `Assets/Scripts/Quest`, it should mostly be adapter code that helps gameplay systems talk to Pixel Crushers.

## Correct Mental Model

Pixel Crushers answers:

- what quest is active?
- what dialogue should play?
- what branch is valid?
- is the quest successful?
- is the quest complete?
- what should the player see in dialogue / quest UI?

Toris answers:

- did a Leader Wolf die?
- did the player pick up this item?
- did the player enter this scene?
- did this world site get cleared?
- can the player receive this reward?
- how are gold, XP, and inventory actually modified?

## First Vertical Slice

The first real slice should be:

1. Guide NPC says hello through Pixel Crushers dialogue
2. Guide NPC offers `Kill_3_Leader_Wolves`
3. player accepts the quest through dialogue
4. player travels to `ProceduralTiles`
5. player kills 3 Leader Wolves
6. Toris reports those kills into Pixel Crushers
7. Pixel Crushers marks the quest as `returnToNPC`
8. player returns to `MainArea`
9. Guide NPC plays completion dialogue
10. Pixel Crushers completes the quest
11. Toris reward adapter grants gold / XP / item reward

This proves:

- dialogue works
- Pixel Crushers quest authoring works
- Toris gameplay reporting works
- scene-to-scene quest state works
- rewards can still be applied through Toris gameplay systems

## Integration Subsystems

## 1. Dialogue Bootstrap

Purpose:

- get Dialogue System running inside Toris scenes
- create a Toris dialogue database
- create the first Guide NPC conversation

First proof:

- guide NPC says one line in `MainArea`
- conversation opens correctly
- player input / UI does not break while dialogue is open

Important setup:

- use the package `Dialogue Manager` prefab
- replace the demo database with a Toris dialogue database
- add the Dialogue Manager to both `MainArea` and `ProceduralTiles` for development safety
- keep the manager's single-instance / persistence behavior
- enable 2D physics support through Pixel Crushers settings if needed

## 2. Toris-Driven NPC Conversation Start

Purpose:

- keep one consistent interact model across the game
- avoid fragmenting NPC interaction into a separate trigger-only path

Direction:

- use Toris `PlayerInteractor`
- use Toris `IInteractable`
- add a dedicated Pixel Crushers conversation interactable
- add a proximity helper that sets / clears the current interactable through `PlayerInteractor`

First-pass behavior:

- player presses the normal interact input
- Toris interactable selects the correct conversation title
- Pixel Crushers starts that conversation

## 3. Quest Authoring In Pixel Crushers

Purpose:

- author quests inside Pixel Crushers instead of Toris custom quest assets

First proof:

- create one quest: `Kill_3_Leader_Wolves`
- define the states the dialogue uses:
  - `unassigned`
  - `active`
  - `returnToNPC`
  - `success`
- define a kill counter variable:
  - `LeaderWolfKills`

The exact database field workflow should be documented as we use it.

## 4. Gameplay Event Reporting

Purpose:

- let Toris gameplay tell Pixel Crushers what happened

Examples:

- `LeaderWolf` killed
- item picked up
- scene entered
- site visited
- wolf den cleared
- necromancer grave cleared

This reporting layer should be reusable.

It should not know about every individual quest.

Good direction:

- enemy owns a canonical enemy ID
- item owns a canonical item ID
- site owns a canonical site / encounter ID
- adapters report those IDs to Pixel Crushers

Current first-pass implementation:

- `Enemy` exposes a serialized `questEnemyId`
- enemy death reports that ID through `PixelCrushersQuestBridge.ReportEnemyKilled(...)`
- the bridge currently maps `LeaderWolf` into the `Kill_3_Leader_Wolves` tutorial quest flow

## 5. Enemy Quest Reporting

Purpose:

- enemy deaths update Pixel Crushers quests

Example:

- Leader Wolf dies
- Toris reports `LeaderWolf` kill
- Pixel Crushers kill counter increments
- when the counter reaches `3`, the quest moves to `returnToNPC`

This should connect through the enemy death pipeline, not by writing custom code for each quest.

## 6. Reward Application

Purpose:

- Pixel Crushers decides that a quest is complete
- Toris applies the actual gameplay rewards

Rewards should be applied by Toris because Toris owns:

- inventory
- gold
- XP
- progression
- unlocks

First reward types:

- gold
- XP
- item

Important rule:

- do not let the dialogue asset directly mutate Toris inventory in fragile ways
- use a small reward adapter that calls the existing Toris gameplay systems

Current first-pass implementation:

- `PixelCrushersQuestRewardAdapter` watches one quest for a transition into `success`
- rewards are granted through Toris systems:
  - `PlayerProgression.AddGold(...)`
  - `PlayerProgression.AddExperience(...)`
  - `InventoryManager.AddItem(...)`
- a Pixel Crushers int variable guards against duplicate reward payout

## 7. Dialogue / Quest UI

First pass:

- use Pixel Crushers dialogue UI
- use Pixel Crushers quest tracker / log if it works well enough

Later:

- replace or restyle UI only if it clashes with Toris visuals or UI Toolkit architecture

Do not build custom Toris quest UI before proving the asset UI is insufficient.

## 8. Persistence

First pass:

- rely on Pixel Crushers runtime quest state during the current play session
- keep the Dialogue Manager alive across `MainArea` and `ProceduralTiles`
- verify quest state survives scene transitions

Later:

- decide whether full save/load is owned by Pixel Crushers save tools, Toris save tools, or a deliberate bridge between them

Important rule:

- do not create two independent sources of quest truth

## 9. Debugging / Diagnostics

We still need visibility while integrating.

Useful debug support:

- log when Toris reports a gameplay event to Pixel Crushers
- log when a Pixel Crushers quest state changes
- expose a test button or editor-only helper to reset tutorial quest state
- verify quest state after scene travel

Debug logs should be wrapped in `#if UNITY_EDITOR`.

## Strict Implementation Order

## Phase 1: Dialogue Bootstrap

Build / configure:

- Dialogue Manager in `MainArea`
- Toris dialogue database
- `Player` actor
- `GuideNPC` actor
- one `Guide_Intro` conversation
- one placeholder Guide NPC that can start the conversation

Goal:

- prove dialogue works before adding quests

## Phase 2: Toris-Driven Conversation Start

Build:

- `PixelCrushersConversationInteractable`
- `DialogueNpcProximity`

Goal:

- start Pixel Crushers conversations through Toris interaction instead of stock Pixel triggers

## Phase 3: Pixel Crushers Quest Bootstrap

Build / configure:

- one Pixel Crushers quest
- dialogue that can offer / complete that quest
- basic quest state transitions using the asset's intended workflow

Goal:

- prove the asset can own quest state cleanly

## Phase 4: Toris Gameplay Reporting Adapter

Build:

- enemy kill report adapter
- item pickup report adapter later if needed
- scene travel report adapter later if needed

Goal:

- Toris gameplay facts update Pixel Crushers quest state

## Phase 5: Reward Adapter

Build:

- quest completion reward adapter
- gold reward
- XP reward
- item reward

Goal:

- Pixel Crushers completes the quest, Toris grants the reward

## Phase 6: UI And Persistence Review

Evaluate:

- Pixel Crushers dialogue UI
- Pixel Crushers quest tracker / log
- quest state across `MainArea` and `ProceduralTiles`

Goal:

- decide what is good enough and what needs Toris-specific polish

## Rules Going Forward

- Pixel Crushers is quest / dialogue authority
- Toris reports gameplay facts
- Toris applies gameplay rewards
- do not rebuild a parallel quest runtime
- do not wire individual quests directly into enemy / item scripts
- prefer stable IDs for enemies, items, NPCs, scenes, sites, and encounters
- document every new integration assumption as we discover how the asset behaves
