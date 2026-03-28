# Outland Haven - 2D Action RPG & Survival Management

**Outland Haven** is a 2D isometric survival and resource management game blending Action RPG and Rogue-Lite elements. My 4th-year graduation project, it features a compelling "Hub-and-Expedition" gameplay loop, contrasting the safety of a Town hub with the unpredictable danger of the Overworld. *(The game and all systems are currently in active development)*

**Source Code on GitHub** - [https://github.com/thameeeh/Toris.git](https://github.com/thameeeh/Toris.git)

As the lead systems programmer for the game's core interactive systems, I completely re-architected the UI and Item management foundations. My focus has been on building highly scalable, decoupled, and event-driven architectures that support complex interactions like crafting, cross-container inventory transfers, and dynamic item states.

---

## Core Technical Contributions

### 1. Event-Driven Inventory & Dynamic Item Architecture

![Insert GIF/Video of Inventory Interactions Here]()

I designed and implemented a robust, strictly decoupled inventory system capable of handling complex item states (like durability) without cluttering the core data structures.

* **Dynamic State Generation:** Utilized the `[SerializeReference]` attribute to allow an `ItemInstance` to store polymorphic `ItemComponentState` objects dynamically at runtime. This factory-driven approach ensures that hardcoded properties (e.g., durability, item level) are decoupled from the stateless `InventoryItemSO` blueprints.
* **Stateless Managers:** Created `InventoryManager` MonoBehaviours that strictly use `InventoryContainerSO` assets as blueprints, ensuring flexible instantiation for player, NPC, and chest inventories.
* **Centralized Transfer Arbitration:** Developed the `InventoryTransferManagerSO` to impartially assess rules and handle cross-container item transfers and partial-stack merges. This global arbitrator prevents direct coupling between separate `InventoryManager` instances.
* **Data-Driven Event Flow:** Implemented an event-driven flow (`UIInventoryEventsSO`) where UI interactions do not mutate state directly; instead, they fire global requests that the `InventoryManager` validates before broadcasting state updates to listeners.

### 2. Scalable UI Toolkit Architecture

![Insert GIF/Video of UI Toolkit Interactions & Drag-and-Drop Here]()

To support the game's complex hub interfaces (Smithing, Magic, Crafting, Salvaging), I built a flexible UI framework utilizing Unity's modern UI Toolkit.

* **Model-View-Presenter (MVP) Pattern:** Architected complex UI interactions, such as crafting and salvaging, using the MVP pattern. Logic (e.g., requirement validation against player inventory) is strictly maintained in Manager/Presenter classes (like `CraftingManagerSO`), preventing the UI Views from holding any game state.
* **Zone-Based Mutual Exclusivity:** Developed a custom `UIManager` that enforces view exclusivity based on `ScreenZone` regions (e.g., opening the Smith view automatically closes other main-zone views, while the HUD remains untouched).
* **Advanced Drag-and-Drop Implementation:** Engineered a seamless UI drag-and-drop system. To prevent clipping, I implemented a `UIDragManager` that instantiates a temporary "ghost" icon within a dedicated `#Drag_Layer` at the root of the UI document, maintaining proper standard click functionality via pointer drag thresholds.
* **BEM Methodology & Templating:** Applied strict BEM (Block__Element--Modifier) naming conventions to all UXML and USS definitions to prevent C# query collisions. Leveraged dynamic templating for repeating elements (like grid slots) instead of hardcoding UI instances.

### 3. Professional Engineering Practices

![Insert Screenshot of Documentation or Test Runner Here]()

Beyond feature implementation, I established rigorous development standards to ensure the project remains maintainable and bug-free as it scales.

* **Unit Testing Pipeline:** Integrated NUnit testing for both logic and ScriptableObjects (`PlayerDataSO`, pooling managers), using reflection to isolate MonoBehaviour behaviors and verify internal states without requiring a full Unity runtime setup.
* **Custom Editor Tooling:** Wrote custom Editor scripts (e.g., `InventoryItemSOEditor`) to bypass Unity inspector limitations. These tools provide Generic Menus via Reflection, allowing designers to easily select and instantiate concrete subclasses into abstract `[SerializeReference]` lists.
* **Architectural Documentation:** Maintained comprehensive Markdown documentation (e.g., `script dependency documentation.md`, `Inventory_Event_System_Documentation.md`), utilizing sequential dependency chains to clearly map core UI, event flow, and system manager architectures for the team.
* **Strict Namespace Organization:** Enforced rigid codebase boundaries (e.g., `OutlandHaven.UIToolkit`, `OutlandHaven.Inventory`) to prevent spaghetti code and ensure developers consciously declare dependencies across system domains.
