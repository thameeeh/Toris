# Implementation Plan: Sage Weapon Upgrade System (Corrected)

> [!NOTE]
> **CURRENT STATUS: COMPLETED**
> This system has been fully implemented, registered, and compiled. We built the specialized `InfusionSubView_Sage.cs` presenter and successfully integrated it inside the tabbed coordinator shell `SageUpgradeView.cs`. It runs weapon upgrades seamlessly via the `UpgradeSalvageManagerSO` database.

This plan adds a dedicated weapon upgrade screen at the **Sage NPC (Eldrin)**. The player selects a weapon from their inventory, previews the stat boost, and spends gold to level it up (+1 → +2 → +3, etc.).

> [!IMPORTANT]
> This plan was validated against the live codebase. All class names, method signatures, enum values, and data flows have been confirmed by reading the actual source files.

---

## 1. Architectural Strategy

The upgrade system is already fully implemented in the backend:

| Concern | Existing Code | What It Does |
|---|---|---|
| Upgrade Logic | `UpgradeSalvageManagerSO.TryUpgradeItem(InventorySlot)` | Validates max level, deducts gold, increments `UpgradeableState.CurrentLevel`, fires `NotifyStateChanged()` |
| Cost Formula | `UpgradeSalvageManagerSO.CalculateUpgradeCost(ItemInstance)` | Returns `UpgradeBaseGoldCost × CurrentLevel` |
| Stat Calculation | `EquippedItemStatCalculator.CalculateWeapon(ItemInstance)` | Computes `UpgradeDamageBonus = Max(0, UpgradeLevel - 1) × 2f` |
| Item State (DTO) | `UpgradeableState` in `UpgradeableModule.cs` | Pure DTO with `CurrentLevel` — **do NOT modify** |
| Blueprint Config | `UpgradeableComponent` on `InventoryItemSO` | Static `MaxLevel = 5` per item |

**We only need to build the UI layer.** No backend or DTO changes are required.

### Key Decisions
- **DO NOT** add methods to `UpgradeableState` — it is a pure DTO (Rule 3.5 in `How_To_Properly_Create_Item_Module.md`).
- **Delegate all upgrade logic** to `UpgradeSalvageManagerSO.TryUpgradeItem()`.
- **Follow the Smith pattern exactly**: `Controller (MonoBehaviour)` → constructs `View (GameView)` → registers with `UIManager`.
- **Simpler than Smith**: No tabs needed — just a single weapon-list + upgrade panel.

---

## 2. Proposed Changes (Step by Step)

Implementation should follow this exact order to satisfy dependency chains.

---

### Step 1: Add `SageUpgrade` to the `ScreenType` Enum

#### [MODIFY] [ScreenTypes.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/UIToolkit/UI/Events/ScreenTypes.cs)

Add `SageUpgrade` to the `ScreenType` enum:

```diff
         Mage,
         Potions,
+        SageUpgrade,

         PlayerEquipment,
```

**Why first?** Every other file depends on this enum value compiling.

---

### Step 2: Add `SageUpgrade` to `InventoryInteractionContext`

#### [MODIFY] [InventoryInteractionContext.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/UIToolkit/UI/Events/InventoryInteractionContext.cs)

```diff
         Forge,
-        Salvage
+        Salvage,
+        SageUpgrade
```

**Why?** The Inventory panel uses this context to know what clicking an item should do. In `SageUpgrade` context, clicking a weapon should select it for upgrading (not equip/sell/forge). The inventory view already listens to `OnInteractionContextChanged` to adjust behavior.

---

### Step 3: Add `OnRequestUpgrade` Event to `UIInventoryEventsSO`

#### [MODIFY] [UIInventoryEventsSO.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/UIToolkit/UI/Events/UIInventoryEventsSO.cs)

Add a new event under the `[Header("Crafting Events")]` section:

```diff
         public UnityAction<CraftingRecipeSO> OnRequestCraftRecipe;
+
+        [Header("Sage Upgrade Events")]
+        public UnityAction<InventorySlot> OnRequestSageUpgrade;
```

**What this does:** The `SageUpgradeView` will invoke this event when the player clicks the "Upgrade" button. The `SageUpgradeScreenController` will listen and delegate to `UpgradeSalvageManagerSO.TryUpgradeItem()`.

---

### Step 4: Create the UXML Layout

#### [NEW] `Assets/UI_Toolkit/UXMLs/SageUpgrade.uxml`

A single-panel UI with no tabs (simpler than the Smith). Layout hierarchy:

```
<SageUpgrade-root>
  ├── <SageUpgrade-header>           → Title: "Eldrin — Weapon Forge"
  ├── <SageUpgrade-body>
  │   ├── <SageUpgrade-weapon-list>  → ScrollView listing upgradeable weapons
  │   └── <SageUpgrade-detail>       → Selected weapon's upgrade preview
  │       ├── <weapon-icon>          → Large weapon sprite
  │       ├── <weapon-name-label>    → "Iron Sword +2"
  │       ├── <stat-before-label>    → "Damage: 14"
  │       ├── <stat-arrow>           → "➔"
  │       ├── <stat-after-label>     → "Damage: 16"  (highlighted green)
  │       ├── <upgrade-cost-label>   → "Cost: 100g"
  │       ├── <player-gold-label>    → "Your Gold: 350"
  │       └── <btn-upgrade>          → "Upgrade Weapon" button
  └── <SageUpgrade-footer>           → (optional) flavor text
```

**Weapon list items:** Each row shows the weapon icon, name, and current level badge (e.g., "+2"). Only items with an `UpgradeableComponent` appear. Items already at max level show a "MAX" badge and are grayed out.

---

### Step 5: Create the USS Stylesheet

#### [NEW] `Assets/UI_Toolkit/USS/SageUpgrade.uss`

Premium mystical theme inspired by the Sage's alchemical nature:
- **Background**: Deep amethyst gradient (`#1a0a2e` → `#2d1b69`)
- **Accent color**: Golden rune glow (`#f4c430` / `#d4a017`)
- **Font**: Match existing game font (from Smith USS)
- **Weapon list rows**: Semi-transparent glass panels with hover glow
- **Upgrade button**: Gold background with a subtle pulsing `@keyframes` glow animation when active, grayed out + disabled when insufficient gold or at max level
- **Stat change arrows**: Green (`#2ecc71`) for the "after" value to visually communicate improvement

---

### Step 6: Create the `SageUpgradeView` (GameView)

#### [NEW] `Assets/Scripts/UIToolkit/UI/UIViews/SageUpgrade/SageUpgradeView.cs`

```
namespace OutlandHaven.UIToolkit
{
    public class SageUpgradeView : GameView
    {
        public override ScreenType ID => ScreenType.SageUpgrade;
```

**Constructor parameters** (mirror the Smith pattern):
- `VisualElement topElement`
- `VisualTreeAsset slotTemplate` (for rendering weapon icons in the list)
- `UIEventsSO uiEvents`
- `UIInventoryEventsSO uiInventoryEvents`
- `GameSessionSO gameSession`
- `UpgradeSalvageManagerSO upgradeManager`

**Key responsibilities:**

| Method | What It Does |
|---|---|
| `SetVisualElements()` | Queries all named elements from the UXML (`btn-upgrade`, `weapon-name-label`, `stat-before-label`, `stat-after-label`, `upgrade-cost-label`, `player-gold-label`, `SageUpgrade-weapon-list`) |
| `Setup(object payload)` | Calls `BuildWeaponList()` to populate the ScrollView with upgradeable weapons from `_gameSession.ActivePlayerInventory` |
| `Show()` | Calls `base.Show()`, sets `OnInteractionContextChanged(InventoryInteractionContext.SageUpgrade)`, subscribes to `OnItemClicked` |
| `Hide()` | Calls `base.Hide()`, resets context to `InventoryInteractionContext.Normal`, unsubscribes from `OnItemClicked` |
| `BuildWeaponList()` | Iterates `_gameSession.ActivePlayerInventory.Slots`, filters to items with `UpgradeableComponent`, creates a clickable row for each |
| `SelectWeapon(InventorySlot)` | Sets the selected weapon, calls `RefreshUpgradePreview()` |
| `RefreshUpgradePreview()` | Uses `EquippedItemStatCalculator.CalculateWeapon()` for current stats; simulates +1 level for the "after" column; shows gold cost via `_upgradeManager.CalculateUpgradeCost()` |
| `OnUpgradeClicked()` | Invokes `_uiInventoryEvents.OnRequestSageUpgrade(selectedSlot)` |

**Stat preview formula** (to display before/after):
```csharp
// Current stats
WeaponComputedStats current = EquippedItemStatCalculator.CalculateWeapon(selectedItem);

// Preview: temporarily increment level for display only
float previewUpgradeBonus = Mathf.Max(0, current.UpgradeLevel) * 2f;
float previewFinalDamage = current.BaseDamage + previewUpgradeBonus 
                          + current.AwakenedDamageBonus + current.StrengthDamageBonus;
```

> [!NOTE]
> We do NOT mutate the actual `UpgradeableState` for the preview. We calculate the "after" values arithmetically using the known formula: `(CurrentLevel) × 2` (since after upgrade, `UpgradeLevel - 1` becomes `CurrentLevel`).

---

### Step 7: Create the `SageUpgradeScreenController` (MonoBehaviour)

#### [NEW] `Assets/Scripts/UIToolkit/UI/Controllers/SageUpgradeScreenController.cs`

Follow the exact same lifecycle pattern as [SmithScreenController.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/UIToolkit/UI/Controllers/SmithScreenController.cs):

```
namespace OutlandHaven.UIToolkit
{
    public class SageUpgradeScreenController : MonoBehaviour
    {
```

**Inspector fields:**
- `[SerializeField] VisualTreeAsset _sageUpgradeTemplate;` — Drag `SageUpgrade.uxml`
- `[SerializeField] VisualTreeAsset _slotTemplate;` — Drag `Slot.uxml`
- `[SerializeField] UIEventsSO _uiEvents;`
- `[SerializeField] UIInventoryEventsSO _uiInventoryEvents;`
- `[SerializeField] GameSessionSO _gameSession;`
- `[SerializeField] UpgradeSalvageManagerSO _upgradeManager;` — **Reuses the existing SO asset**

**Lifecycle (mirroring Smith):**

| MonoBehaviour | What Happens |
|---|---|
| `Awake()` | `_uiManager = FindFirstObjectByType<UIManager>()` |
| `OnEnable()` | Subscribe to `_uiEvents.OnRequestOpen += HandleRequestOpen`, `_uiEvents.OnScreenOpen += HandleScreenOpen`, `_uiInventoryEvents.OnRequestSageUpgrade += HandleUpgradeRequest` |
| `OnDisable()` | Unsubscribe from all events |
| `Start()` | Instantiate `_sageUpgradeTemplate`, create `new SageUpgradeView(...)`, call `_view.Initialize()`, call `_uiManager.RegisterView(_view, ScreenZone.Left)` |

**Key handler methods:**

```csharp
private void HandleRequestOpen(ScreenType screenType, object payload)
{
    if (screenType != ScreenType.SageUpgrade) return;
    _view?.Setup(null);
}

private void HandleScreenOpen(ScreenType screenType)
{
    if (screenType != ScreenType.SageUpgrade) return;
    EnsureInventoryVisible(); // Opens Inventory panel if not already open
}

private void HandleUpgradeRequest(InventorySlot slot)
{
    if (_upgradeManager == null || slot == null) return;

    bool success = _upgradeManager.TryUpgradeItem(slot);
    if (success)
    {
        // Refresh the view to show updated stats
        _view?.Setup(null);
        _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
    }
}
```

> [!IMPORTANT]
> **Zero backend changes.** The controller delegates 100% to `UpgradeSalvageManagerSO.TryUpgradeItem()`, which handles: max-level check, gold validation, gold deduction, `CurrentLevel++`, and `NotifyStateChanged()`. We don't touch the DTO.

---

### Step 8: Create the Sage NPC Interactable

#### [NEW] `Assets/Scripts/Quest/Dialogue/PixelCrushersSageInteractable.cs`

Clone the [PixelCrushersSmithInteractable.cs](file:///d:/GameDev/Unity/Game%20Project%20Toris/Toris/Toris/Assets/Scripts/Quest/Dialogue/PixelCrushersSmithInteractable.cs) pattern exactly:

```csharp
[DisallowMultipleComponent]
public class PixelCrushersSageInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private UIEventsSO _uiEvents;
    [SerializeField] private string _questVariable = "isSageHaveQuest";
    [ConversationPopup] [SerializeField] private string _questSelectionConversation = "Sage_Quest_Or_Upgrade";

    public void Interact(GameObject interactor)
    {
        bool hasQuest = DialogueManager.hasInstance 
                     && DialogueLua.GetVariable(_questVariable).asBool;

        if (hasQuest && !string.IsNullOrWhiteSpace(_questSelectionConversation))
        {
            PixelCrushersQuestBridge.StartConversation(
                _questSelectionConversation, interactor.transform, transform);
        }
        else
        {
            _uiEvents.OnRequestOpen?.Invoke(ScreenType.SageUpgrade, null);
        }
    }
}
```

**Differences from the Smith:**
- No `_shopInventory` needed (upgrade screen reads from `GameSessionSO.ActivePlayerInventory` directly)
- Opens `ScreenType.SageUpgrade` instead of `ScreenType.Smith`
- Different quest variable and conversation

> [!NOTE]
> **Future-proofing for Potions:** If the Sage later also hosts a Potions screen, change the conversation `Sage_Quest_Or_Upgrade` to offer dialogue choices like:
> - "Upgrade my weapons" → Lua: `TorisOpenScreen("SageUpgrade")`
> - "Show me your potions" → Lua: `TorisOpenScreen("Potions")`
> 
> The interactable itself would then always launch the conversation (removing the direct `OnRequestOpen` shortcut). This keeps the interactable script unchanged.

---

### Step 9: Dialogue Database Configuration

#### [MODIFY] Dialogue System Database (Unity Editor)

1. **Create Variable**: `isSageHaveQuest` → Boolean, default `false`
2. **Create Conversation**: `Sage_Quest_Or_Upgrade`
   - Start Node → "What can I do for you?"
   - Choice 1 (Quest): Visible when quest conditions are met
   - Choice 2 (Upgrade): Calls `TorisOpenScreen("SageUpgrade")` via Lua sequencer

---

### Step 10: Scene Setup

#### [MODIFY] Game Scene (Unity Editor)

1. **Create Sage NPC GameObject** in the world scene
2. Attach `PixelCrushersSageInteractable` component:
   - Drag the shared `UIEventsSO` asset
   - Set quest variable and conversation
3. Add child `InteractableProximity` with a `Collider2D` trigger for approach detection
4. **Create `SageUpgradeScreenController` GameObject** under the UI Canvas:
   - Drag `SageUpgrade.uxml`, `Slot.uxml` templates
   - Drag shared `UIEventsSO`, `UIInventoryEventsSO`, `GameSessionSO` assets
   - Drag the **existing** `UpgradeSalvageManagerSO` asset (same one the Smith uses)

---

## 3. File Summary

| # | Action | File | Purpose |
|---|---|---|---|
| 1 | MODIFY | `ScreenTypes.cs` | Add `SageUpgrade` enum value |
| 2 | MODIFY | `InventoryInteractionContext.cs` | Add `SageUpgrade` context |
| 3 | MODIFY | `UIInventoryEventsSO.cs` | Add `OnRequestSageUpgrade` event |
| 4 | NEW | `SageUpgrade.uxml` | UI layout template |
| 5 | NEW | `SageUpgrade.uss` | Premium mystical stylesheet |
| 6 | NEW | `SageUpgradeView.cs` | View (extends `GameView`) |
| 7 | NEW | `SageUpgradeScreenController.cs` | Controller (extends `MonoBehaviour`) |
| 8 | NEW | `PixelCrushersSageInteractable.cs` | NPC interaction bridge |
| 9 | MODIFY | Dialogue Database | Quest variable + conversation |
| 10 | MODIFY | Game Scene | Place NPC + wire controller |

**Zero modifications to:**
- ❌ `UpgradeableModule.cs` / `UpgradeableState` (pure DTO, untouched)
- ❌ `UpgradeSalvageManagerSO.cs` (reused as-is)
- ❌ `EquippedItemStatCalculator.cs` (read-only usage)

---

## 4. Verification Plan

### Manual Verification Steps
1. **Enum compilation**: Build the project. Verify no compilation errors after adding the new enum values.
2. **NPC interaction**: Walk up to Eldrin the Sage → verify `InteractableProximity` trigger → press E.
3. **Quest routing**: If `isSageHaveQuest == true`, verify the conversation launches. If false, verify the Sage Upgrade screen opens directly.
4. **Inventory opens alongside**: Verify that the Inventory panel auto-opens in the `Right` zone when the Sage Upgrade screen opens in the `Left` zone (same as Smith behavior).
5. **Weapon list filtering**: Verify only items with `UpgradeableComponent` appear in the list. Items at max level show "MAX" and are non-selectable.
6. **Stat preview accuracy**: Select a weapon at Level 1. Verify:
   - Before: `BaseDamage` (e.g., 10)
   - After: `BaseDamage + 2` (e.g., 12)
   - Cost: `UpgradeBaseGoldCost × 1` (e.g., 50g)
7. **Upgrade execution**: Click "Upgrade Weapon" → verify gold is deducted, weapon level increments, stats refresh instantly.
8. **Persistence**: Open the Pause/Character screen → verify final damage reflects the upgraded weapon. Save/load → verify the upgrade persists.
9. **Edge cases**:
   - Not enough gold → button stays disabled, upgrade does not fire
   - Already at max level → button shows "Max Level Reached", disabled
   - No upgradeable weapons → empty state message in the list
