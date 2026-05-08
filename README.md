# Outland Haven

**Outland Haven** is a 2D isometric survival and resource management game blending Action RPG and Rogue-Lite elements. Developed as a system-driven bachelor's project, the game challenges players to balance the tension between safety and risk through a compelling exploration and progression loop.

Cast out from a larger kingdom into an abandoned, ruined settlement, you must brave the monster-infested Overworld to gather resources, upgrade your equipment, and establish yourself in this desolate refuge.

## Core Concept & Gameplay Loop

The game is structured around a central **Hub-and-Expedition** dynamic:

- **The Hub (Preparation):** A permanent base where players manage their inventory, craft equipment, and perform character upgrades. It acts as a safe staging area to prepare for upcoming expeditions.
- **The Overworld (Danger Zone):** Procedurally generated, hostile territories filled with enemies and loot. Players must embark on expeditions to gather essential resources.
- **Risk vs. Reward:** The Overworld introduces meaningful stakes. Failing an expedition results in a partial loss of the resources carried in your limited Personal Stash. Players must constantly weigh the temptation of pushing deeper against the need to extract safely.

## Key Features

- **Strategic Bow-Centric Combat:** Master a highly mobile combat system focused on positioning, timing, and an evasive dash with invulnerability frames. Utilize specialized weapon tiers, including elemental variants, offering unique crowd-control and area-of-effect abilities.
- **Progression & Crafting:** Utilize gathered materials to upgrade your gear. Interact with specialized NPCs like the Smith to craft new weapons and enhance your character's capabilities using Gold and XP.
- **Quest & Dialogue System:** Engage with the world's inhabitants through a robust dialogue and quest system. Complete objectives to unlock new opportunities and advance your standing in the Outlands.
- **Economic System:** Manage a straightforward economy based on items and progression milestones:
  - **Gold:** Used for purchasing items and NPC services.
  - **XP (Knowledge):** Earned through combat to unlock new character skills.
  - **Item-Based Resources:** Instead of abstract counters, Food and Materials exist as physical items in the inventory. Food is consumed to restore health, while Material items are used as crafting components for gear upgrades.
- **Minimalist Pixel Art:** A clean, 2D isometric art style prioritizing mechanical clarity and readable combat states.

## Technical Architecture

Outland Haven is built in **Unity (C#)** with a strong emphasis on robust, scalable architecture:

- **Data-Driven Design:** Extensive use of `ScriptableObjects` for flyweight items, crafting recipes, and game data.
- **MVP UI Pattern:** Strict separation of concerns where UI Views are purely visual and all logic resides in C# Presenters and Managers.
- **Event-Driven Systems:** Observer patterns and Event Buses decouple logic, preventing hard references and god objects.
- **Composition Over Inheritance:** Modular, component-based logic to handle complex inventory and entity behaviors.

---
*Developed by Rimvydas Medimas & Karolis Nagys — Dongseo University*
