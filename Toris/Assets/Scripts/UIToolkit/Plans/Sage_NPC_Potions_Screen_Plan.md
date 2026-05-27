# Implementation Plan: Sage NPC, Generic Service Interactable, and Custom Potions Screen

> [!NOTE]
> **CURRENT STATUS: COMPLETED (INTEGRATED)**
> Instead of a separate standalone green/purple panel, this plan was successfully unified with the Sage Upgrade interface under the amethyst/gold theme. Potion brewing was implemented as the dedicated **Brewing Tab** within `ScreenType.SageUpgrade` using `BrewSubView_Sage.cs` and `BrewSubView_Sage.uxml`, dynamically referencing alchemical potion recipes via `CraftingManagerSO`.

This plan details the addition of **Eldrin the Sage**, a new NPC responsible for potions, bow upgrades, and skills. We will implement a highly reusable interaction component to avoid duplicate NPC logic and create a gorgeous, custom-themed green/purple **Potions & Upgrades** screen by duplicating and tailoring the Smith's UI Toolkit architecture.

---

## 1. Reusable Interaction Architecture

We will create a highly generic script that handles NPC service routing dynamically, allowing us to configure the target screen, quest variable, and router conversation in the Inspector. This allows us to retire old hardcoded behaviors and instantly support any future specialized NPCs.

### [NEW] PixelCrushersNPCServiceInteractable.cs
A highly reusable interactable component:
*   **Properties**:
    *   `ScreenType TargetScreen` (e.g., `Smith`, `Potions`, `Skills`)
    *   `string QuestVariable` (e.g., `isSmithHaveQuest`, `isSageHaveQuest`)
    *   `string QuestSelectionConversation` (e.g., `Smith_Quest_Or_Shop`, `Sage_Quest_Or_Brew`)
    *   `InventoryManager ShopInventory` (caches local components automatically)
*   **Behavior**:
    *   Checks if the configured `QuestVariable` is true in the Dialogue database.
    *   If true, triggers `QuestSelectionConversation`.
    *   If false, fires the `OnRequestOpen` event channel with `TargetScreen` and the cached `ShopInventory`.

---

## 2. Potions & Upgrades UI Screen

We will duplicate the tabbed structure of the Smith screen and completely re-theme it into a premium **Sage's Alchemical Lab** (accented in vibrant magical greens/purples). We will map the Smith's forge/shop/salvage tabs into **Brewing**, **Bow Upgrades**, and **Ingredient Shop**.

### New Layout and Stylesheets:
*   **`Potions.uxml`**: The main layout UXML template for the Potions screen, adjusting titles to `"SAGE ALCHEMY"` and referencing potions styles.
*   **`Potions.uss`**: Master stylesheet for the Potions panel. Changes the Smith's gold variables to vibrant alchemical highlights:
    *   Primary Accent: Magic purple (`#9b59b6` / HSL tuned).
    *   Secondary Accent: Alchemical green (`#2ecc71` / HSL tuned).
*   **Subview UXML Templates**:
    *   `BrewSubView_Potions.uxml`: Customized version of the forge view for brewing recipes.
    *   `UpgradeSubView_Potions.uxml`: Customized version of the forge/upgrade view for bow upgrades.
    *   `Potions_Shop.uss` / `Potions_Brew.uss`: Tailored stylesheet files for the individual panels.

### View & Controller Layer:
*   **`PotionsView.cs`** (under `Assets/Scripts/UIToolkit/UI/UIViews/Potions/`): Controls tab transitions, button binds, and manages sub-views:
    *   `BrewSubView` (Potion crafting)
    *   `UpgradeSubView` (Bow upgrades)
    *   `ShopSubView` (Ingredient merchant)
*   **`PotionsScreenController.cs`** (under `Assets/Scripts/UIToolkit/UI/Controllers/`): Registers to the `UIManager` on `ScreenType.Potions`, binds inventory events, and resolves local shops.

---

## 3. Dialogue Database (Quest Variables)

We will configure Eldrin the Sage in the Dialogue System database:
1.  **Dialogue Variable**: Create `isSageHaveQuest` (Boolean, defaults to `false`).
2.  **Conversation**: Create `Sage_Quest_Or_Brew` conversation presenting options:
    *   *Choice 1 (Quest)*: visible under custom quest conditions.
    *   *Choice 2 (Brewing/Upgrades)*: calls `TorisOpenScreen("Potions")` (or Lua command equivalent).

---

## 4. Verification & Testing

*   **Compilation**: Build verification using `dotnet build Assembly-CSharp.csproj`.
*   **Manual Sandbox Testing**: Place a new NPC GameObject, attach `PixelCrushersNPCServiceInteractable.cs` set to `TargetScreen: Potions`, and verify that interacting either opens the conversation (if `isSageHaveQuest` is true) or slides out the beautiful green/purple Potions screen directly (if false).
