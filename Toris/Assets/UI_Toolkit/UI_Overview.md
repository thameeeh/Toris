# UI Toolkit Overview

This document provides a detailed summary of the `UI_Toolkit` UI implementation in OutlandHaven, detailing every `.uss` and `.uxml` file and explaining the overall architectural design of the user interface.

## Architectural Overview

The UI implementation for OutlandHaven relies exclusively on Unity's UI Toolkit (UXML for structure, USS for styling), eschewing traditional Unity UI (Canvas/UGUI) components.

The architecture follows a strict separation of concerns:
1. **Master Layout System:** The application utilizes a global `MasterLayout.uxml` which dictates where different types of UI can appear (Left Zone, Right Zone, HUD Layer, FullScreen Zone).
2. **Screens and SubViews:** Major interfaces (like `PlayerInventory`, `Smith`, `Mage`, `SkillScreen`) are designed as high-level Screens. Auxiliary or tabbed content (like the Smith's Forge or Shop) are separated into reusable SubViews (`ForgeSubView_Smith.uxml`, `ShopSubView.uxml`).
3. **Component-Based Styling:** Styling is heavily componentized. Global variables (colors, fonts) are defined in `theme-variables.uss`. Reusable atomic components (like item slots, buttons, tabs) are defined in `Components.uss` and applied globally. Screen-specific stylesheets (like `Inventory.uss` or `SkillScreen.uss`) are reserved only for complex layout positioning unique to that screen.
4. **Decoupled Data Binding:** The UI elements (slots, buttons, bars) serve as pure view representations. The actual data binding and event propagation happen via external C# controllers mapping to specific UXML `VisualElement` names or classes, maintaining a "dumb UI" pattern that only listens to and displays data.

---

## USS Files Summary

### `theme-variables.uss`
Acts as the central source of truth for the UI's design system. It defines CSS custom properties (variables) for global colors (e.g., `--color-bg-dark`, `--color-health`, `--color-border-highlight`) and standard typography sizes. All other stylesheets reference these variables to maintain visual consistency.

### `Components.uss`
Contains styles for generic, reusable UI atoms and molecules across the entire game. Key components include:
- `.standard-button`: Base styles and hover states for UI buttons.
- `.item-slot`: Base dimensions, borders, and hover effects for inventory and equipment slots.
- `.panel-tab`: Styling for tabbed navigation interfaces (like the Smith menu), including active and hover states.
- Equipment slot positioning classes (e.g., `.equip-slot--head`, `.equip-slot--weapon`) mapping directly to visual avatar layouts.

### `HUD.uss`
Specific styling for the persistent Heads-Up Display. It defines:
- Layout containers for the bottom section (left/right distribution).
- Progress bar styling (`.unity-progress-bar`), completely overriding Unity's default aesthetics to create rounded, custom-colored fills mapped to XP, Health, and Mana variables.
- HUD typography (`.HUD`) including text outlines for readability over the game world.

### `Inventory.uss`
Styles specifically for the `PlayerInventory` screen layout. It controls the flexbox distribution of the Stats panel, the visual background for the Player Equipment panel, and the grid wrapper for the inventory slots.

### `Shop.uss`
Contains layout and structural styles for the merchant interface, specifically the `.shop-window` container, the item grid (`.shop-grid`), and the header sections.

### `SkillScreen.uss`
Handles the complex visual layout for the Skill Tree interface. Includes:
- A massive virtual canvas layout (`.skill-tree__container`) for freeform node placement.
- Node visual states (`.skill-node--unlocked`, `--available`, `--locked`) with scaling hover transitions.
- Layout and typography for the detailed `.skill-info-panel` that appears alongside the tree.

---

## UXML Files Summary

### Screens (Root Layouts)

- **`MasterLayout.uxml`**
  The foundation of the UI hierarchy. It defines absolute-positioned overlay layers: `Layer_Windows` (with `Left_Zone` and `Right_Zone` for side-by-side menus), `Layer_HUD` (bottom 20%), and a `FullScreen_Zone`.

- **`HUD.uxml`**
  The actual Heads-Up Display structure. Includes a menu toggle button block and the bottom section containing the player's Level, XP, Health, and Mana bars, along with a Gold label.

- **`PlayerInventory.uxml`**
  The main character screen. It features a flex layout dividing the screen into a Top Section (containing a scrolling Stats Panel and a visually mapped Equipment Panel) and a Bottom Section (the Inventory Slots grid).

- **`Smith.uxml`**
  The root screen for the Blacksmith NPC interaction. It provides a tabbed header navigation (Forge, Market, Salvage) and a generic middle panel (`Smith-middle__panel`) where specific SubViews are dynamically injected.

- **`Mage.uxml`**
  Similar to the Smith screen, this acts as the root for Mage NPC interactions, starting with an 'Enchant' tab and a dynamic middle panel.

- **`SkillScreen.uxml`**
  The dedicated skill tree UI. It contains a hidden-scrollbar `ScrollView` panning over the massive skill tree canvas, and a fixed right-side info panel showing skill details, states, and the unlock button.

### UXML Templates (Reusable Sub-components)

- **`Slot.uxml`**
  The atomic inventory item slot. Contains an icon image element and a quantity label. Included dynamically wherever items are displayed.

- **`HUDMenuButtonTemplate.uxml`**
  A structural template for the side-menu buttons appearing in the HUD, providing a standardized layout for an icon, label, and keyboard shortcut hint.

- **`ShopSubView.uxml`**
  The merchant inventory view injected into NPC screens. It contains a header showing the player's gold and a scrollable grid of shop items.

- **`ForgeSubView_Smith.uxml`**
  The crafting sub-view. It explicitly layouts an input section with two `.item-slot` elements separated by a "+" label, a "Forge Items" execution button, and a designated result output slot.

- **`SalvageSubView_Smith.uxml`**
  The item destruction/salvage sub-view. It provides a single input slot for the item to be dismantled, and presents the user with two side-by-side options: converting the item into Gold, or converting it into crafting Materials.
