# Pixel Crushers Quest And Dialogue System Plan

## Purpose

This document defines the quest and dialogue system direction for Toris.

Quests are a progression system.

They give the player purpose, guide the experience, carry story context, and connect gameplay actions to world progression.

Pixel Crushers Dialogue System is the quest and dialogue authority.

Toris gameplay systems report facts into Pixel Crushers and apply gameplay rewards through Toris-owned systems.

## Current Validated Slice

The first working quest slice is complete.

Implemented and verified flow:

1. Guide NPC opens Pixel Crushers dialogue through Toris interaction
2. Guide NPC starts `Kill_3_Leader_Wolves`
3. Leader Wolf deaths are reported from Toris gameplay
4. `LeaderWolfKills` increments from `0` to `3`
5. quest transitions to `returnToNPC`
6. Guide NPC routes to turn-in dialogue
7. turn-in dialogue sets quest state to `success`
8. Toris reward adapter grants rewards once
9. Guide NPC routes to post-quest dialogue

This proves the integration direction is valid.

The current implementation is still a vertical slice, not the complete reusable quest system.

## Ownership Split

Pixel Crushers owns:

- dialogue databases
- actors and conversations
- quest definitions
- quest entries / ordered stages
- quest state
- dialogue branching
- quest tracker / quest log UI for the first pass

Toris owns:

- combat outcomes
- item pickup
- NPC interaction
- scene travel
- site and encounter state
- stable gameplay IDs
- player inventory
- player gold, XP, stats, and progression
- reward application

No parallel Toris quest runtime should be introduced.

Toris-side quest code should be adapter and integration code.

## Quest Meaning

Quests are not only tasks.

They represent progression, story direction, player guidance, and purposeful world interaction.

Quests can be simple, staged, or part of a larger questline.

Examples:

- kill a specific enemy
- visit a location
- explore a site
- talk to an NPC
- kill an enemy and then visit a location
- clear a world encounter and return to an NPC
- progress through a main story chain

## Quest Categories

V1 supports three quest categories:

## 1. Tutorial Quests

Purpose:

- teach movement
- teach interaction
- teach combat
- teach hub-to-world flow
- introduce quest tracking and turn-in behavior

Tutorial quests are progression onboarding.

## 2. Main Quests

Purpose:

- carry the story of the world
- guide the player's main progression
- introduce major locations, systems, and threats
- unlock or point toward important game milestones

Only one main quest should be active at a time.

## 3. Side Quests

Purpose:

- provide optional goals
- add world context
- support exploration
- reward combat, collection, or site completion

Multiple side quests can be active at the same time.

## Active Quest Rules

V1 active quest model:

- one active main quest
- many active side quests
- tutorial quests are allowed during onboarding and can coexist where needed

This keeps story progression readable while still allowing optional content.

## Quest Structure

V1 quests are built from ordered stages.

In Pixel Crushers, quest entries are the stage model.

A stage can represent:

- one objective
- a group of related objectives
- a handoff to the next stage
- a return-to-NPC step

Supported quest shapes:

- one-step quest
- ordered multi-stage quest
- questline made of multiple quests
- follow-up branch that activates another quest

V1 does not support:

- quest failure
- quest abandonment
- repeatable quests
- fully divergent world-state quest arcs

## Branching Model

V1 branching means follow-up quest activation.

Branching does not mean fully divergent save-state or world-state systems yet.

Allowed v1 branching:

- dialogue choice activates one follow-up quest
- completed quest unlocks the next quest in a questline
- completed stage points toward a different next quest

Deferred branching:

- mutually exclusive world states
- permanent faction lockouts
- failed quest recovery
- repeatable quest reset logic

## Shared Fact Reporting System

Toris gameplay systems should report facts.

They should not contain quest logic.

Required shape:

- fact producers
- one shared fact reporting layer
- one quest progress mapping layer

Fact producers include:

- enemies
- items
- NPCs
- scene transition systems
- world sites
- encounters

The shared reporting layer receives standardized fact payloads.

Quest progress mapping translates those facts into Pixel Crushers variable updates, quest entry states, and quest states.

Separate reporting systems should not be created for each fact type.

## Fact Model

The shared fact model should support:

- fact category
- exact target ID
- type or tag target
- amount
- context ID

Required v1 fact categories:

- `Kill`
- `PickUp`
- `EnterScene`
- `VisitSite`
- `ClearSite`
- `InteractNpc`

Targeting rules:

- exact IDs are used for named story targets
- type/tag targets are used for procedural or generic objectives
- context IDs can restrict objectives to a scene, site, biome, or encounter if needed

## Stable ID Model

Reported facts require stable IDs.

IDs must be explicit fields on the relevant object or asset.

Do not infer quest identity from Unity object names.

Required ID surfaces:

- enemy exact ID
- enemy type/tag
- item exact ID
- item type/tag
- NPC exact ID
- site exact ID
- site type/tag
- encounter exact ID
- scene ID when needed

Current first-pass ID:

- `Enemy.questEnemyId`

## Quest Progress Mapping

Quest progress mapping is centralized.

Gameplay producers report facts.

The mapping layer decides what those facts mean for Pixel Crushers.

Current first-slice mapping:

- fact: `Kill`
- target ID: `LeaderWolf`
- variable: `LeaderWolfKills`
- threshold: `3`
- quest: `Kill_3_Leader_Wolves`
- entry result: entry `1` becomes `success`
- quest result: quest becomes `returnToNPC`

Current systemized direction:

- gameplay producers report `QuestFact`
- `PixelCrushersQuestFactReporter` is the shared reporting entry point
- `PixelCrushersQuestProgressMapper` maps configured facts into Pixel Crushers progress
- `PixelCrushersQuestBridge` stays a thin API wrapper around Pixel Crushers

Current `Kill_3_Leader_Wolves` mapper rule:

- fact type: `Kill`
- exact ID: `LeaderWolf`
- quest name: `Kill_3_Leader_Wolves`
- progress variable: `LeaderWolfKills`
- required amount: `3`
- entry number: `1`
- entry complete state: `Success`
- quest complete state: `ReturnToNPC`

Future mapping should avoid hardcoding every quest in bridge code.

The second and third quests should be cheaper to add than the first one.

## Dialogue Routing

NPC conversation start is owned by Toris interaction.

Current reusable component:

- `PixelCrushersConversationInteractable`

Current routing pattern:

- `unassigned` opens offer / intro dialogue
- `active` opens reminder dialogue
- `returnToNPC` opens turn-in dialogue
- `success` opens post-quest dialogue

Pixel Crushers trigger components are allowed for temporary bootstrap tests only.

The long-term NPC path should use:

- Toris `PlayerInteractor`
- Toris `IInteractable`
- Pixel Crushers conversation start through adapter code

## Rewards

Pixel Crushers decides when a quest is successful.

Toris decides how rewards are applied.

Current reusable component:

- `PixelCrushersQuestRewardAdapter`

Current reward types:

- gold
- XP
- item

Reward application must use Toris gameplay systems:

- `PlayerProgression.AddGold(...)`
- `PlayerProgression.AddExperience(...)`
- `InventoryManager.AddItem(...)`

Rewards must be guarded against duplicate payout.

Future reward types:

- skill unlock
- ability unlock
- NPC/shop unlock
- world-state unlock

## Quest Metadata

Pixel Crushers owns quest truth.

Toris may still keep non-authoritative quest metadata keyed by Pixel Crushers quest name.

Allowed metadata:

- quest category: `Tutorial`, `Main`, `Side`
- questline ID
- reward config
- optional UI ordering
- optional routing/mapping configuration

Do not duplicate Pixel Crushers dialogue, descriptions, or authoritative quest state in Toris metadata.

## Authoring Workflow

Every quest should follow one setup workflow.

1. Create the Pixel Crushers quest
2. Add ordered quest entries for stages
3. Add required Pixel Crushers variables
4. Create dialogue conversations for offer, active, turn-in, and post-completion states as needed
5. Configure Toris NPC dialogue routing
6. Configure stable IDs on gameplay objects or assets
7. Add or reuse fact mapping rules
8. Configure reward data
9. Test accept, progress, turn-in, success, and reward payout

Current first-slice setup note:

- add `PixelCrushersQuestProgressMapper` to a persistent quest/dialogue object
- configure one rule with the `Kill_3_Leader_Wolves` values listed in `Quest Progress Mapping`
- keep `Enemy.questEnemyId = LeaderWolf` on the Leader Wolf prefab

## Implementation Priorities

## Priority 1. Replace First-Slice Hardcoding

Move first-slice quest progress logic out of one-off bridge code and into a reusable mapping model.

Target result:

- new kill-style quests do not require custom bridge methods
- enemy death remains only a fact producer

## Priority 2. Define Shared Fact Payload

Create the shared fact data type used by all producers.

Minimum fields:

- fact category
- exact ID
- type/tag
- amount
- context ID

## Priority 3. Create Fact Reporting Entry Point

Create one Toris-side API for gameplay systems to report facts.

Example intent:

- `ReportFact(fact)`

This replaces feature-specific calls such as enemy-only reporting methods as the system matures.

## Priority 4. Create Mapping Configuration

Create a reusable way to map facts to Pixel Crushers progress.

The mapping should support:

- incrementing variables
- checking thresholds
- setting quest entry state
- setting quest state

## Priority 5. Standardize Rewards

Move reward setup toward one repeatable configuration pattern keyed by quest name.

Target result:

- one reward coordinator can handle multiple quests
- reward payout remains once-only

## Priority 6. Add A Second Producer

After the shared fact layer exists, connect a second producer.

Best candidates:

- item pickup
- scene travel

This confirms the system is not specific to enemy kills.

## Test Requirements

Core tests:

- tutorial quest can start from dialogue
- main quest can progress through ordered stages
- side quest can be active while a main quest is active
- fact reporting increments quest progress
- exact ID target only matches the intended target
- type/tag target matches valid procedural targets
- quest can transition to `returnToNPC`
- turn-in can transition to `success`
- rewards are granted once
- post-success dialogue routing works

Scene flow tests:

- quest state survives `MainArea` to `ProceduralTiles`
- quest state survives `ProceduralTiles` to `MainArea`
- reward payout does not duplicate after scene travel

## Current Status

Done:

- dialogue bootstrap
- Toris-driven NPC conversation start
- first Pixel Crushers quest
- first enemy kill fact producer
- shared fact payload
- shared fact reporting entry point
- first reusable progress mapper
- first turn-in flow
- first reward payout

Next:

- document the strict authoring workflow
- add a second producer after the shared fact layer exists

## Rules

- Pixel Crushers is quest and dialogue authority
- Toris reports gameplay facts
- Toris applies gameplay rewards
- no parallel Toris quest runtime
- no per-feature reporting systems
- no quest logic inside unrelated gameplay scripts
- use explicit stable IDs
- use one shared fact reporting layer
- keep progress mapping centralized
