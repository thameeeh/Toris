# Ability UI and Skill System Implementation Plan

## Overview
This document outlines the architectural plan for integrating Player Abilities with the UI Toolkit-based HUD. The goal is a high-performance, event-driven system where the UI reacts to state changes in the player's ability runtimes without polling.

## Changes Made So Far

### 1. Extended `UISkillEventsSO.cs`
- **Purpose**: Acts as the decoupled communication bridge between the `PlayerAbilityController` and the UI.
- **New Events**:
    - `OnAbilitySlotPressed(int slotIndex)`: Notifies the UI the moment a key is pressed, allowing for immediate visual feedback (e.g., button press animation) even if the ability is on cooldown.
    - `OnAbilityCooldownStarted(int slotIndex, float duration)`: Broadcasts the start of a recharge cycle along with its total duration.
    - `OnAbilityReady(int slotIndex)`: Signals that an ability has finished its cooldown and is ready for use.

### 2. Refactored `PlayerAbilityController.cs`
- **State Tracking**: Added a `_wasOnCooldown` bit-array/bool-array to monitor transitions.
- **Logic Integration**:
    - The `Update` loop now compares the current `IsOnCooldown` state with the previous frame's state.
    - Successfully triggers the corresponding events in `UISkillEventsSO` only during the frame the state changes, ensuring zero redundant event calls.
- **Dependency Injection**: Added a serialized field for `UISkillEventsSO` to allow the player system to push data to the UI layer.

---

## Future Implementation Plan

### 1. Ability HUD View (UI Toolkit)
- **Template Creation**: Design a reusable `AbilitySlot.uxml` and `AbilitySlot.uss`.
- **Visual Features**:
    - **Icon Display**: Show the icon defined in the `PlayerAbilitySO`.
    - **Cooldown Overlay**: A semi-transparent radial fill element that shrinks as the cooldown progresses.
    - **Timer Text**: (Optional) A numeric countdown for longer cooldowns.
    - **"Ready" Animation**: A visual glow or "pulse" triggered when `OnAbilityReady` is received.

### 2. Ability HUD Controller
- **Lifecycle**: Create an `AbilityHUDController.cs` that registers with the `UIManager`.
- **Dynamic Population**: At start, query the `PlayerAbilityController` for the number of active slots and instantiate the UI templates accordingly.
- **Event Subscription**:
    - Subscribe to `OnAbilityCooldownStarted` to begin local UI animations (using Tweens or Coroutines).
    - Subscribe to `OnAbilityReady` to stop animations and play the "Ready" visual effect.
    - Subscribe to `OnAbilitySlotPressed` to trigger a "click" or "bounce" visual feedback on the UI slot.

### 3. Ability Tree / Mage Screen Integration
- Extend the `MageScreenController` to allow players to drag and drop unlocked abilities into the 5 available slots.
- Ensure that updating the slots in the UI correctly updates the `PlayerAbilityController` and triggers the `CaptureTransferredState` logic in `GameSessionSO` for persistence.
