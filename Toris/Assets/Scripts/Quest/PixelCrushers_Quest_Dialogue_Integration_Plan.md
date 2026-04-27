# Pixel Crushers Story Content Pipeline

## Purpose

This document defines how Toris should use Pixel Crushers Dialogue System to turn existing gameplay systems into authored story content.

The problem is not that Toris lacks gameplay systems.

The problem is that the systems need purposeful content wrapped around them.

Pixel Crushers is the authoring tool for Toris story content.

Toris provides gameplay facts, rewards, and interaction hooks.

## North Star

Adding a quest or NPC interaction should eventually feel like content authoring, not new systems programming.

The desired workflow is:

- create Pixel Crushers dialogue
- create Pixel Crushers quest entries
- configure Toris facts that can progress those entries
- configure Toris rewards or gameplay actions
- test the flow in-game

The first quest may be painful.

The second quest should be cheaper.

The third quest should feel mostly like authoring.

## What This Is

This is a content pipeline.

It connects:

- NPC dialogue
- quest state
- player gameplay
- world progression
- rewards
- future story branches

The Guide NPC is only the first validation case.

The system must not become Guide-specific.

## What This Is Not

This is not a custom Toris quest runtime.

This is not a replacement for Pixel Crushers quests.

This is not a hardcoded chain of:

- Guide talks
- quest 1 starts
- Guide talks again
- quest 2 starts

That kind of sequence can exist as content, but it must live mostly inside Pixel Crushers conversations and quests.

Toris C# should only provide reusable hooks.

## Ownership Split

Pixel Crushers owns:

- dialogue databases
- actors
- conversations
- quest definitions
- quest entries / ordered stages
- quest state
- dialogue branching
- quest tracker / quest log UI for the first pass
- authored story flow

Toris owns:

- player interaction input
- enemy deaths
- item pickups
- scene travel
- NPC interaction hooks
- site and encounter state
- stable gameplay IDs
- inventory changes
- gold, XP, stats, and rewards
- opening gameplay UI such as shops

No parallel Toris quest runtime should be introduced.

Toris quest-side code should stay adapter/integration code.

## Runtime Flow

The intended runtime flow is:

1. Player interacts with an NPC or world object.
2. Toris starts a Pixel Crushers conversation.
3. Pixel Crushers dialogue starts or advances a quest.
4. Player performs Toris gameplay.
5. Toris reports a generic gameplay fact.
6. The mapping layer translates that fact into Pixel Crushers quest progress.
7. Pixel Crushers updates quest state, tracker text, and available dialogue.
8. Pixel Crushers dialogue reaches a reward or gameplay action point.
9. Toris applies the reward or gameplay action.

Example:

```text
Guide NPC
-> starts Guide_Intro conversation
-> starts Kill_3_Leader_Wolves quest
-> player kills LeaderWolf enemies
-> Toris reports Kill / LeaderWolf facts
-> mapper increments LeaderWolfKills
-> quest becomes returnToNPC
-> Guide opens turn-in dialogue
-> quest becomes success
-> Toris grants reward
```

The example is content.

The pipeline is the reusable system.

## Current Validated Slice

The first working quest slice is complete.

Verified flow:

1. Guide NPC opens Pixel Crushers dialogue through Toris interaction.
2. Guide NPC starts `Kill_3_Leader_Wolves`.
3. Leader Wolf deaths are reported from Toris gameplay.
4. `LeaderWolfKills` increments from `0` to `3`.
5. quest transitions to `returnToNPC`.
6. Guide NPC routes to turn-in dialogue.
7. turn-in dialogue sets quest state to `success`.
8. Toris reward adapter grants rewards once.
9. Guide NPC routes to post-quest dialogue.

This proves the integration direction is valid.

It is still a vertical slice, not the finished authoring pipeline.

## Reusable Building Blocks

Current Toris-side integration code:

- `PixelCrushersConversationInteractable`
- `DialogueNpcProximity`
- `PixelCrushersDialogueCommandBridge`
- `PixelCrushersQuestBridge`
- `PixelCrushersQuestFactReporter`
- `PixelCrushersQuestProgressMapper`
- `PixelCrushersQuestRewardAdapter`
- `PixelCrushersQuestRewardSetSO`
- `QuestFact`
- `QuestFactType`
- `QuestFactProgressRuleSetSO`
- `QuestFactSceneReporter`
- `QuestFactTriggerReporter`

These should remain generic.

They should not contain story-specific logic.

`PixelCrushersConversationInteractable` may use an ordered list of quest conversation routes.

This lets one NPC, such as the Guide, serve several authored quest beats without adding Guide-specific C#.

Routes are the only quest-state conversation selection path.

The default conversation is only used when no route resolves.

Route order matters.

Put newer or higher-priority beats before older completed beats.

## Dialogue Authoring

Pixel Crushers conversations are the main story authoring surface.

Use conversations for:

- NPC greeting lines
- quest offers
- quest reminders
- turn-in dialogue
- post-quest dialogue
- simple choices
- shop prompts
- branching story choices

Simple NPC example:

```text
Shopkeeper
-> "Welcome to my shop."
-> choice: Open Shop
-> choice: Leave
```

The dialogue should decide what the player sees.

Toris should only provide the hook that opens the shop when the dialogue requests it.

Dialogue can call Toris gameplay hooks through one scene-level `PixelCrushersDialogueCommandBridge`.

Current commands:

- `TorisOpenScreen("Inventory")`
- `TorisOpenScreen("Skills")`
- `TorisOpenScreen("ConfiguredCommandId")`
- `TorisCloseScreen("Inventory")`
- `TorisReportFact("VisitSite", "SiteId", "SiteType", 1, "ContextId")`

Direct screen names open a `ScreenType` with no payload.

Configured command IDs are used when a screen needs extra Toris payload, such as a shop inventory.

Example:

```text
Dialogue choice: "Show me what you sell."
-> script: TorisOpenScreen("SmithShop")
-> command bridge maps SmithShop to ScreenType.Smith and the correct shop inventory payload
```

Dialogue commands should request gameplay actions.

They should not directly change inventory, stats, or world systems.

## Quest Authoring

Pixel Crushers quests are the authoritative quest records.

Use Pixel Crushers quests for:

- quest names
- quest descriptions
- quest entries / stages
- quest state
- tracker text
- quest progression visibility

Toris should not duplicate this data in another quest database.

Allowed Toris-side metadata:

- reward config
- fact mapping config
- optional quest category
- optional questline ID
- optional UI ordering

Do not duplicate authoritative dialogue, descriptions, or quest state.

## Quest Categories

V1 quest categories:

- Tutorial
- Main
- Side

Tutorial quests teach the player how to play.

Main quests carry the world story and forward progression.

Side quests add optional goals, rewards, and world context.

V1 active quest rule:

- one active main quest
- many active side quests
- tutorial quests can coexist where needed

## Quest Shapes

Supported v1 shapes:

- one-step quest
- ordered multi-stage quest
- questline made of multiple quests
- dialogue choice activates a follow-up quest
- completed quest unlocks the next quest

Deferred shapes:

- failure states
- abandonment
- repeatable quests
- permanent faction lockouts
- fully divergent world-state arcs

Branching v1 means follow-up quest activation and dialogue variation.

It does not mean a full divergent world-state system yet.

## Fact Reporting

Toris gameplay systems report facts.

They do not contain quest logic.

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

The mapping layer decides which facts matter to which Pixel Crushers quests.

Separate reporting systems should not be created for each fact type.

## Fact Model

The shared fact model supports:

- fact category
- exact target ID
- type/tag target
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
- type/tag targets are used for generic or procedural objectives
- context IDs can restrict objectives to a scene, site, biome, or encounter

Do not infer quest identity from Unity object names.

Stable IDs must be explicit fields on relevant objects or assets.

Current first-pass ID:

- `Enemy.questEnemyId`
- optional NPC fact ID on `PixelCrushersConversationInteractable`

Future ID surfaces:

- enemy type/tag
- item exact ID
- item type/tag
- NPC exact ID
- site exact ID
- site type/tag
- encounter exact ID
- scene ID

## Quest Progress Mapping

Quest progress mapping is centralized.

Gameplay producers report facts.

The mapping layer translates matching facts into Pixel Crushers updates.

Mapping rules should live in `QuestFactProgressRuleSetSO` assets.

Scene objects should reference rule sets instead of owning long inline rule lists.

`PixelCrushersQuestProgressMapper` intentionally does not expose inline rules anymore.

Rules should be authored in rule set assets so the scene component stays small and readable.

Current first-slice mapping:

- fact: `Kill`
- target ID: `LeaderWolf`
- variable: `LeaderWolfKills`
- threshold: `3`
- quest: `Kill_3_Leader_Wolves`
- entry result: entry `1` becomes `success`
- quest result: quest becomes `returnToNPC`

The second quest should not require new bridge methods.

If a new quest needs new C# code, check whether the code is genuinely reusable before adding it.

Current fact producers:

- enemy death reports `Kill`
- Pixel Crushers NPC interaction can optionally report `InteractNpc`
- world item pickup can optionally report `PickUp`
- `QuestFactSceneReporter` can report scene or area entry facts
- `QuestFactTriggerReporter` can report visit, clear, or generic trigger facts

`InteractNpc` should be used for objectives like:

- talk to the Guide
- introduce yourself to the Blacksmith
- return to an NPC after another stage

Do not create a new fact type for each NPC.

Use:

- fact type: `InteractNpc`
- exact ID: the specific NPC ID
- type/tag: optional NPC group such as `Shopkeeper` or `Guide`
- context ID: optional scene, hub, or story context

Runtime placement:

- `PixelCrushersQuestProgressMapper` can be placed on a scene object for authoring.
- `PixelCrushersQuestProgressMapper` reads one or more `QuestFactProgressRuleSetSO` assets.
- only enabled mapper components should install persistent runtime listeners.
- duplicated or disabled mapper components should not own runtime rules.
- when `_installPersistentRuntime` is enabled, it creates a small `DontDestroyOnLoad` runtime listener using the configured rules.
- this keeps quest fact mapping alive after `MainArea` loads `ProceduralTiles` with `LoadSceneMode.Single`.
- do not rely on the Guide NPC object itself staying alive across scene changes.

Current second validation beat:

- `Guide_Talk_To_Smith` uses `InteractNpc`
- exact ID: `SmithNPC`
- context ID: `MainArea`
- variable: `TalkedToSmith`
- threshold: `1`
- quest result: quest becomes `returnToNPC`

## Rewards

Pixel Crushers decides when a quest reaches success.

Toris decides how rewards are applied.

Current reusable component:

- `PixelCrushersQuestRewardAdapter`

Reward entries should live in `PixelCrushersQuestRewardSetSO` assets.

Scene reward adapters should reference reward sets instead of owning one quest reward directly.

`PixelCrushersQuestRewardAdapter` intentionally does not expose single-quest reward fields anymore.

Rewards should be authored in reward set assets so one adapter can handle all configured quest payouts.

Current reward types:

- gold
- XP
- item

Reward application must use Toris gameplay systems:

- `PlayerProgression.AddGold(...)`
- `PlayerProgression.AddExperience(...)`
- `InventoryManager.AddItem(...)`

Rewards must be guarded against duplicate payout.

Each reward entry must have a Pixel Crushers variable that marks payout as granted.

Future reward types:

- ability unlock
- shop unlock
- NPC unlock
- world-state unlock

## Persistence

There are two different persistence concerns.

In-session scene transition persistence:

- quest state and variables live in Pixel Crushers runtime data
- the progress mapper installs a `DontDestroyOnLoad` listener from scene-authored rule sets
- this keeps gameplay facts progressing quests after loading from `MainArea` into `ProceduralTiles`

Save/load persistence:

- Pixel Crushers should own dialogue and quest save data
- Toris should not create a second quest save format
- Pixel Crushers `SaveSystem` plus `DialogueSystemSaver` is the expected path for saving quest state and Lua variables
- reward-granted variables must be saved with the Pixel Crushers data so rewards do not pay out twice after loading

Current static validation:

- gameplay scenes were checked for Pixel Crushers save components
- no gameplay scene-level `SaveSystem` or `DialogueSystemSaver` setup was found yet
- therefore true disk save/load persistence for Pixel Crushers quest state should not be considered complete yet
- current validated behavior is runtime quest flow and scene-transition survival, not full save-slot restoration

Required persistence validation before calling the story pipeline complete:

- add or confirm one Pixel Crushers `SaveSystem` setup in the runtime bootstrap path
- add or confirm `DialogueSystemSaver` on the Dialogue Manager or equivalent Pixel Crushers object
- keep save encryption disabled and password empty
- start a quest, progress it, save, reload, and confirm quest state survives
- complete a reward quest, save, reload, and confirm rewards are not granted again
- transition from hub to overworld and back, then confirm quest variables and tracker state still match

## Dialogue Commands

Pixel Crushers dialogue can request Toris gameplay actions through `PixelCrushersDialogueCommandBridge`.

This bridge is for gameplay actions, not quest ownership.

Supported v1 commands:

- open a UI screen by `ScreenType` name
- open a configured screen command with optional inventory payload
- close a UI screen by `ScreenType` name
- report a generic quest fact from authored dialogue

Use configured screen commands when dialogue needs to open a vendor UI.

Example configured command:

- command ID: `SmithShop`
- screen type: `Smith`
- pass inventory payload: `true`
- inventory payload: the Smith shop inventory container

Example Pixel Crushers script:

```text
TorisOpenScreen("SmithShop")
```

Use direct screen names for payload-free screens.

Example Pixel Crushers script:

```text
TorisOpenScreen("Skills")
```

Use `TorisReportFact` only when the fact is caused by dialogue itself.

Example Pixel Crushers script:

```text
TorisReportFact("InteractNpc", "GuideNPC", "Guide", 1, "MainArea")
```

## Authoring Workflow

Every quest should follow one setup workflow.

1. Decide what story purpose the quest serves.
2. Create the Pixel Crushers quest.
3. Add ordered quest entries for stages.
4. Add required Pixel Crushers variables.
5. Create dialogue conversations for offer, active, turn-in, and post-completion states as needed.
6. Configure Toris NPC interaction to start the correct conversation.
7. Configure stable IDs on gameplay objects or assets.
8. Add or reuse fact mapping rules.
9. Configure reward data.
10. Test accept, progress, turn-in, success, and reward payout.

The workflow should be improved whenever adding content feels unnecessarily technical.

## Current Guide Slice Checklist

`Kill_3_Leader_Wolves` should have:

- Pixel Crushers quest named `Kill_3_Leader_Wolves`
- Pixel Crushers variable named `LeaderWolfKills`
- Guide intro dialogue script starts the quest, activates entry `1`, and resets `LeaderWolfKills` to `0`
- Guide active dialogue used while quest state is `active`
- Guide turn-in dialogue used while quest state is `returnToNPC`
- Guide turn-in dialogue sets the quest to `success`
- Guide post-quest dialogue used after `success` or `done`
- Leader Wolf prefab has `questEnemyId = LeaderWolf`
- `PixelCrushersQuestProgressMapper` has a `Kill / LeaderWolf` rule
- mapper rule requires quest active
- mapper rule increments `LeaderWolfKills`
- mapper rule completes entry `1` at `3`
- mapper rule sets quest state to `ReturnToNPC`
- `PixelCrushersQuestRewardAdapter` watches `Kill_3_Leader_Wolves`
- reward adapter uses a dedicated reward-granted variable

If any of these are missing, the slice is content-broken, not architecture-broken.

## Validation Strategy

The next validation target is not more architecture.

The next validation target is authored content.

Validation sequence:

1. Keep the existing Guide / Leader Wolf / reward slice working.
2. Turn it into a real first Guide story beat.
3. Add a second Guide beat using the same pipeline.
4. Add one non-Guide NPC interaction using the same pipeline.
5. Add one side quest using the same pipeline.

This proves the system is not Guide-specific.

## Near-Term Focus

Immediate focus:

- stabilize the current Guide quest slice
- document the exact authoring checklist
- keep Pixel Crushers as the content source of truth
- avoid new C# unless the authored content needs a reusable hook

Next useful reusable hooks:

- persistence validation for quest variables, reward-granted variables, and runtime scene transitions
- first authored shop dialogue using `TorisOpenScreen("ConfiguredCommandId")`
- first pickup or site objective authored through the new fact producers

Only add these when they support real content being authored.

## Guardrails

- Pixel Crushers is quest and dialogue authority.
- Toris reports gameplay facts.
- Toris applies gameplay rewards.
- Toris opens gameplay UI when dialogue requests it.
- No parallel Toris quest runtime.
- No per-feature reporting systems.
- No quest logic inside unrelated gameplay scripts.
- No Guide-specific C# quest chain.
- Use explicit stable IDs.
- Keep progress mapping centralized.
- Prefer authored Pixel Crushers content over hardcoded C# branching.

## Temporary Change Policy

Avoid touching unrelated legacy systems to prove quest or dialogue behavior.

If a temporary shortcut is genuinely needed, record it in this document before moving on.

Temporary notes must include:

- what was changed
- why it was changed
- what should replace it
- when it should be reverted or removed

Current temporary changes:

- none

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
- first Guide slice stabilization checklist
- persistent progress mapper runtime for scene transitions
- optional `InteractNpc` fact producer for Pixel Crushers NPC interactions
- ordered conversation routes for NPCs that serve multiple quest beats
- first non-Guide NPC interaction beat through `SmithStoryNPC`
- `Guide_Talk_To_Smith` talk-to-NPC quest slice
- reusable `QuestFactProgressRuleSetSO` asset for progress mapping rules
- `MainArea Quest Fact Progress Rules` asset owns the current wolf and Smith mapping rules
- reusable `PixelCrushersQuestRewardSetSO` asset for quest rewards
- `MainArea Quest Rewards` asset owns the current wolf quest payout
- reusable `PickUp` fact reporting on `WorldItem`
- reusable scene entry fact reporter
- reusable trigger fact reporter for visit, clear, or site-style objectives
- dialogue command bridge for opening Toris UI and reporting dialogue-authored facts
- MainArea has one command bridge instance registered with `UIEventsSO`
- no shop-specific dialogue screen command is configured yet
- removed prototype fallback paths from conversation routing, progress mapping, and reward payout components

Next:

- validate Pixel Crushers save/load behavior for quest state, quest variables, and reward-granted variables
- configure the first shop dialogue command when a real shop conversation is authored
