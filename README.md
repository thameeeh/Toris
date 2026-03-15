# Outland Haven

**Outland Haven** is a 2D isometric survival and resource management game blending Action RPG and Rogue-Lite elements. Developed as a system-driven bachelor's project, the game challenges players to balance the tension between safety and risk through a compelling town-building and exploration loop.

Cast out from a larger kingdom into an abandoned, ruined settlement, you must brave the monster-infested Overworld to gather resources, upgrade your equipment, and transform your desolate refuge into a thriving community.

## 🎯 Core Concept & Gameplay Loop

The game is structured around a central **Hub-and-Expedition** dynamic:

- **The Town (Safe Haven):** A permanent progression anchor where players manage their Main Inventory, craft equipment, and upgrade their character. Upgrades and town infrastructure persist between runs.
- **The Overworld (Danger Zone):** Procedurally generated, hostile territories filled with enemies and loot. Players must embark on expeditions to gather essential resources.
- **Risk vs. Reward:** The Overworld introduces meaningful stakes. Failing an expedition results in a partial loss of the resources carried in your limited Personal Stash. Players must constantly weigh the temptation of pushing deeper against the need to extract safely.

## ✨ Key Features

- **Strategic Bow-Centric Combat:** Master a highly mobile combat system focused on positioning, timing, and an evasive dash with invulnerability frames. Utilize specialized weapon tiers, including elemental variants like Lightning, Fire, and Poison bows, each offering unique area-of-effect and crowd-control abilities.
- **Town Building & Infrastructure:** Develop your safe haven by establishing Farms for food production, a Smith for crafting and repairing weapons, and a Gear Enhancer for permanent statistical upgrades. As your town grows, displaced NPCs will arrive to offer their services.
- **Deep Economic System:** Manage a closed-loop economy featuring four core resources:
  - **Food:** Ensures town sustainability and fuels operations.
  - **Money:** Used for crafting, repairing, and upgrading.
  - **Materials:** Gathered from enemies and used for equipment progression.
  - **Knowledge (XP):** Earned through combat to secure permanent character enhancements.
- **Minimalist Pixel Art:** A clean, 2D isometric art style prioritizing mechanical clarity, readable combat states, and cohesive environmental design.

## 🛠️ Technical Architecture

Outland Haven is built in **Unity (C#)** with a strong emphasis on robust, scalable architecture:

- **Data-Driven Design:** Extensive use of `ScriptableObjects` for flyweight items, crafting recipes, and game data.
- **MVP UI Pattern:** Strict separation of concerns where UI Views are purely visual and all logic resides in C# Presenters and Managers.
- **Event-Driven Systems:** Observer patterns and Event Buses decouple logic, preventing hard references and god objects.
- **Composition Over Inheritance:** Modular, component-based logic to handle complex inventory transactions and entity behaviors.

---
*Developed by Rimvydas Medimas & Karolis Nagys — Dongseo University*
