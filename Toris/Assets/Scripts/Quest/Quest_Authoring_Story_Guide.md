# Quest Authoring And Story Growth Guide

This guide explains how to create quests in the current Pixel Crushers + Toris setup, and how to grow the opening questline into a full game story over time.

Use this document when you are authoring content.

Use `Quest_Documentation.md` when you need technical details about the quest system itself.

## Current Quest Philosophy

Quests should do at least one of these things:

- teach the player how to play
- teach the player about the world
- move the main story forward
- give useful rewards or progression
- make the world feel reactive

Main quests should carry the story.

Side jobs should mostly be work, but they can still add small pieces of lore.

Job boards should be a fast route to accept available work.

NPCs should remain important for personality, turn-ins, and story context.

## System Ownership

Pixel Crushers owns:

- quest names
- quest states
- quest entries
- dialogue
- quest journal UI
- job availability groups

Toris owns:

- gameplay facts
- inventory
- rewards
- XP and gold
- shop actions
- enemy kills
- world/object triggers

The rule is:

Pixel Crushers says what the quest means.

Toris says what happened.

## Basic Quest Creation

For a simple quest:

1. Create the quest in the Pixel Crushers dialogue database.
2. Add one or more quest entries.
3. Decide the quest group, such as `GuideJobs`, `SmithJobs`, or a future story group.
4. Create any needed Pixel Crushers variables.
5. Add dialogue that starts or unlocks the quest.
6. Add reward data in the Toris reward set.
7. Add abandon/cooldown data only if the quest needs it.
8. Test the quest from start to completion.
9. Run the Quest Authoring Validator.

## Objective Authoring

For simple objectives, prefer convention-based progress variables.

Convention format:

```text
QuestName_FactType_Target
QuestName_FactType_Target_Required_#
```

Examples:

```text
Guide_Cull_Wolves_Kill_LeaderWolf_Required_3
Guide_Buy_Ore_BuyItem_Ore_Prog
Find_Silent_Gate_Explore_SilentGate
Reach_Level_5_LevelReached_Level_5
```

Use `Required_#` when the player must do something more than once.

Use explicit `QuestFactProgressRuleSetSO` rules when the quest needs special behavior.

## Common Objective Types

Use these fact types for normal quest authoring:

- `Kill`
- `InteractNpc`
- `PickUp`
- `Collect`
- `Deliver`
- `BuyItem`
- `SellItem`
- `VisitSite`
- `ClearSite`
- `Explore`
- `BiomeReached`
- `LevelReached`
- `InteractWorldObject`

If no fact source exists yet, either add a generic reporter component or ask for a small Toris-side fact source.

Do not add quest-specific gameplay scripts unless there is no generic path.

## NPC Job Flow

NPC jobs should work like this:

1. Player talks to NPC.
2. NPC dialogue offers work.
3. Dialogue opens `TorisOpenQuestJournal("Available:GroupName")`.
4. Player accepts a job in the Pixel Crushers quest journal.
5. Player completes the objective.
6. Player returns to the NPC if the quest is `returnToNPC`.
7. Rewards are granted or claimed through the journal if inventory blocks them.

Example groups:

- Guide uses `GuideJobs`
- Smith uses `SmithJobs`
- job boards can use `Available:All`

## Story Quest Flow

Story quests should usually be more authored than side jobs.

A good story quest has:

- a reason the player cares
- a clear objective
- dialogue before the objective
- gameplay action in the world
- dialogue or world feedback after the objective
- a new question or next lead

Example:

```text
Quest: Find_Silent_Gate
Entry: Find the sealed gate beyond Safe Haven.
Fact: Explore / SilentGate
Variable: Find_Silent_Gate_Explore_SilentGate
Result: Return to Guide.
Story: The gate proves the overworld was changed deliberately.
```

This is the kind of quest that can carry the mystery of why people are cast away and why the overworld is broken.

## Growing From Basic To Full Game

Start with one clean opening arc.

Then expand in layers.

### Layer 1 - Opening Tutorial Arc

Goal:

- introduce Safe Haven
- introduce the Guide
- introduce the Smith
- teach talking, fighting, shopping, jobs, rewards, and the quest journal

Example beats:

- wake up as a castaway
- speak to the Guide
- kill Leader Wolves
- meet the Smith
- unlock side work
- discover the first strange overworld sign

### Layer 2 - First Mystery Arc

Goal:

- prove the overworld is not just randomly dangerous
- give the player a mystery site to find

Example beats:

- find a silent gate
- inspect a broken kingdom marker
- discover a grave that should not exist
- return with evidence

### Layer 3 - Regional Story Arc

Goal:

- make each major overworld region teach one part of the world

Example region questions:

- why are the dead restless?
- why are beasts mutated?
- why are roads broken?
- why do kingdom relics appear outside the Kingdom?

Each region should have:

- one story quest chain
- one job board set
- one NPC or lore source
- one mystery clue

### Layer 4 - Kingdom Truth Arc

Goal:

- reveal why people are cast away
- reveal why the overworld is damaged
- connect Safe Haven to the Kingdom

Good questions to answer slowly:

- who decides who gets cast out?
- what does the Kingdom fear?
- is the overworld punishment, accident, or cover-up?
- who benefits from keeping the truth hidden?

### Layer 5 - Endgame Arc

Goal:

- let the player act on the truth

Possible endings:

- expose the Kingdom
- break the exile system
- restore a path through the overworld
- choose Safe Haven over the Kingdom
- discover the Kingdom was protecting people from something worse

Do not decide the final answer too early.

Leave room for better ideas as the world develops.

## Side Jobs

Side jobs should be simple, repeatable, and readable.

Good side jobs:

- kill dangerous enemies
- buy or deliver supplies
- clear a site
- collect materials
- scout a location
- talk to someone

Side jobs can include lore, but they should not carry too much main story weight.

If a side job reveals something major, consider turning it into a story quest.

## When To Use Manual Rules

Use manual progress rules when:

- one fact should progress unusual quest states
- one objective affects multiple entries in a custom order
- the quest needs a non-default final state
- the objective should not use entry `1`
- the target matching is unusual
- the quest has multiple active objectives at the same time

Use conventions when:

- the quest is active
- one fact increments one variable
- entry `1` should complete
- the quest can move to the mapper's default convention completion state

## Testing Checklist

For each new quest, test:

- quest can be accepted only from the intended source
- quest appears in the correct journal mode
- objective progress increments
- objective completes at the correct amount
- quest moves to the correct final state
- rewards grant correctly
- full inventory reward behavior works
- abandon penalties work if configured
- cooldown works if configured
- save/load preserves quest state and progress
- Quest Authoring Validator reports no unexpected errors

## Recommended Story Building Rhythm

Build story in small slices:

1. Add one story beat.
2. Add one gameplay objective.
3. Add one reward.
4. Add one follow-up question.
5. Test the whole flow.
6. Only then add the next beat.

Avoid writing the entire full-game story before the opening works.

The opening is the foundation.

If the opening feels good, the rest of the game has somewhere solid to grow from.
