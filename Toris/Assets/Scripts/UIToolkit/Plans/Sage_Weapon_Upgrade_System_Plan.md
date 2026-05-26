# Implementation Plan: Sage Weapon Upgrade System (Scenario A)

This implementation plan details the addition of a standard weapon upgrade screen (+1, +2, +3 levels) at the new NPC **Eldrin the Sage**. It leverages the existing event-driven UI, composition-based item architecture (`UpgradeableComponent`/`UpgradeableState`), and database-driven stats calculation in `EquippedItemStatCalculator`.

---

## 1. Architectural Strategy

We will utilize the existing modular item system to perform weapon level-ups:
* **Item State Mutator**: When upgrading an item, we retrieve its active `UpgradeableState` and increment `CurrentLevel` (up to `UpgradeableComponent.MaxLevel`).
* **Stat Re-Calculation**: Since `EquippedItemStatCalculator.cs` already parses `UpgradeableState` to dynamically compute `UpgradeDamageBonus = Mathf.Max(0, UpgradeLevel - 1) * 2f;`, any item level-up will automatically reflect instantly across player combat, damage calculations, and presentation screens.
* **No Database Polling**: The screen will be fully event-driven, binding directly to UI Toolkit button events.

---

## 2. Proposed Changes

### Component: NPC Interaction Bridge

We will introduce a script similar to the Smith Interactable for the Sage NPC to handle branching dialogues or screen open requests.

#### [NEW] [PixelCrushersSageInteractable.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/Quest/Dialogue/PixelCrushersSageInteractable.cs)
- Implements `IInteractable`.
- Checks Dialogue System variables (e.g. `isSageHaveQuest`) to determine whether to launch a conversation or trigger a direct screen transition.
- Invokes `UIEvents.OnRequestOpen?.Invoke(ScreenType.SageUpgrade, null);` when launching the upgrade menu.

---

### Component: Core Progression Upgrades

#### [MODIFY] [UpgradeableModule.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/Items/Entity%20Modules/UpgradeableModule.cs)
- Add upgrade helper methods inside `UpgradeableState`:
  ```csharp
  public bool CanUpgrade(int maxLevel) => CurrentLevel < maxLevel;
  public void Upgrade() => CurrentLevel++;
  ```

---

### Component: UI Visual Layout (`SageUpgrade.uxml` & `SageUpgrade.uss`)

We will create a beautiful, gold/mystical themed UI Toolkit screen.

#### [NEW] [SageUpgrade.uxml](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/UI_Toolkit/UXMLs/SageUpgrade.uxml)
- **Title Block**: Header showing "Eldrin the Sage - Forge & Alchymy".
- **Item Selection Panel**: A scroll view listing the player's current weapons from their inventory that possess the `UpgradeableComponent`.
- **Active Upgrade Zone**: 
  - **Before vs After Stats**: Text labels demonstrating the damage scaling (e.g. `Damage: 12 ➔ 14`).
  - **Upgrade Button**: A gold, pulsing button centered in the display.
  - **Cost Indicator**: Shows materials/gold required for the next upgrade step.

#### [NEW] [SageUpgrade.uss](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/UI_Toolkit/USS/SageUpgrade.uss)
- Custom styles utilizing rich, mystical themes (glassmorphism panels, deep amethyst backgrounds, glowing golden runes/accents).
- Pulsing glow keyframe animations for the main "Upgrade Weapon" button when active.

---

### Component: UI Logic Bindings (`SageUpgradeView` & `SageUpgradeController`)

We will wire up the UI elements to the game's upgrade state.

#### [NEW] [SageUpgradeView.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/UIToolkit/UI/UIViews/SageUpgradeView.cs)
- Binds all buttons, lists, and status overlays.
- Listens to inventory selection clicks.
- Renders before/after stats using `EquippedItemStatCalculator.CalculateWeapon`.
- Dispatches upgrade click events to the controller.

#### [NEW] [SageUpgradeController.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/UIToolkit/UI/Controllers/SageUpgradeController.cs)
- Orchestrates data flow between the `GameSessionSO` active inventories and the View.
- Validates that the player has enough gold/materials before allowing the upgrade action.
- Deducts upgrade costs, increments the weapon level, and refreshes the display.
- Requests the `SaveDataOrchestrator` automatically to ensure newly upgraded weapon values are persisted.

---

## 3. Verification Plan

### Manual Verification Steps
1. Interact with Eldrin the Sage and open the Upgrade screen.
2. Verify that only upgradeable items (possessing `UpgradeableComponent`) are populated in the selection list.
3. Select an active weapon (e.g. Starter Sword).
4. Verify before/after stats reflect a `+2` base damage increase based on the level increment.
5. Click "Upgrade Weapon".
6. Observe that the level increases (e.g., `Sword +1` becomes `Sword +2`) and currency/materials are correctly deducted.
7. Open the Pause screen or Character Stats screen to verify that outgoing final damage reflects the upgraded weapon stats perfectly.
