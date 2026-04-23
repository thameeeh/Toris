Identifier: OutlandHaven.UIToolkit.Tests.InventorySlotTests : [NUnit Test Fixture]

Architectural Role: Unit Test Suite

Core Logic (The 'Contract'):
- Abstract/Virtual Methods: None
- Public API:
  - SetItem_WithValidItemAndPositiveAmount_SetsPropertiesCorrectly(): Verifies normal item insertion logic.
  - SetItem_WithNullItem_SetsHeldItemToNullAndRetainsAmount(): Verifies edge case behavior when a null item is passed with a residual count.
  - SetItem_WithNegativeAmount_SetsAmountCorrectly(): Verifies negative amounts are successfully tracked (if expected behavior) or mapped.

Dependency Graph (Crucial for Scaling):
- Upstream: Depends on NUnit.Framework, OutlandHaven.UIToolkit.InventorySlot, OutlandHaven.Inventory.ItemInstance.
- Downstream: None.

Data Schema:
- None. Only contains local test variables.

Side Effects & Lifecycle:
- Instantiates raw `InventorySlot` and `ItemInstance` objects locally on the managed heap without Unity MonoBehavior overhead. Asserts directly on object state.
