# Drag and Drop System Architecture

This document details the newly refactored event-driven Drag and Drop system implemented within Outland Haven, moving away from rigid singleton-based visual management to a highly decoupled and scalable pipeline.

## Overview

The core shift in the drag-and-drop architecture replaces the use of rigid singletons (such as `UIDragManager.Instance`) within individual visual slots. Instead, the architecture relies on an event-driven pattern and controller delegation. Visual slots do not handle logic or directly command the visual hierarchy; they only emit state changes, allowing higher-level controllers or managers to act on those intentions.

## Architectural Shifts

### 1. Event-Driven Modularity
Previously, UI views were tightly coupled to global singletons, making them difficult to reuse across different systems (like a hotbar or a skill tree) or environments where the singleton might be absent.

**The Solution:**
`InventorySlotView` and other interactive visual elements are now completely "dumb" and isolated. They translate low-level UI Toolkit hardware events into generic semantic events and broadcast them.

For instance, when a drag gesture is detected:
*   Instead of calling `UIDragManager.Instance.StartDrag()`, the view fires an event or defers to an injected controller pattern.
*   This ensures that dropping the script into another environment or project will not throw null reference exceptions looking for an inventory-specific drag manager. It greatly improves the stability of scene transitions.

### 2. Targeted Updates over "Nuke" Redraws
When an item completes a drag-and-drop operation (a transfer or swap), the original prototype logic forced a complete UI rebuild by broadcasting a parameterless `OnInventoryUpdated` event.

**The Solution:**
The system now broadcasts a highly specific `OnSpecificSlotsUpdated(sourceSlot, targetSlot)`. The active UI view uses a Dictionary map to rebuild only the visual representation of the exact two slots involved in the transaction. This eliminates GC spikes and allows the inventory grid to scale massively without dropping frames. It also enables future potential for targeted animations.

### 3. Quantity-Based Transactions
Transfers have been upgraded from basic binary (all-or-nothing) actions to fully featured quantity-based transactions.

**The Solution:**
An `amountToMove` integer is injected into the entire pipeline.
*   **User Intent:** Input listeners (e.g., detecting the Shift key during a drag) determine the requested quantity.
*   **The Global Event:** The request event `OnRequestMoveItem` passes the source, target, and the requested `amountToMove`.
*   **The Arbitration Layer:** The centralized arbitrator (`InventoryTransferManagerSO`) processes the rules. It uses `Mathf.Min(amountToMove, targetSpace, sourceQuantity)` to calculate the safe bounds of the transfer, gracefully handling stack splitting.
*   **Safety Net:** It explicitly blocks partial-stack swaps (trying to move 5 arrows onto a stack of 2 swords) to protect the underlying game economy from duplication or deletion bugs.

## Technical Flow Summary

1.  **Input:** User clicks and drags an item in an `InventorySlotView`.
2.  **Threshold:** The `PointerMoveEvent` verifies a pixel threshold has been met before confirming the action as a drag.
3.  **Visual Layer:** Through an injected controller or an event broadcast, a "Ghost" visual icon is spawned in a dedicated, root-level `#Drag_Layer` (with `pickingMode = PickingMode.Ignore`).
4.  **Drop Intention:** The user releases the pointer over a valid target. An event like `OnRequestMoveItem(source, target, amount)` is dispatched.
5.  **Execution:** The authoritative logic manager calculates the `Mathf.Min` valid transfer and mutates the data.
6.  **Targeted Redraw:** The data container dispatches `OnSpecificSlotsUpdated(source, target)`. The UI view looks up those specific slots in its mapping Dictionary and updates only their visual state.