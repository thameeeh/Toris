/// <summary>
/// ====================================================================================
/// AI DESIGN DOCUMENT: ENEMY SYSTEM
/// ====================================================================================
///
/// 1. CORE ARCHITECTURE OVERVIEW
/// -----------------------------
/// This project uses a flexible State Machine Pattern for enemy AI.
///
/// - Enemy.cs (Abstract Base Class): Contains shared logic for ALL enemies (health, Rigidbody).
///   It is not intended to be used directly on a prefab.
///
/// - Specialized Enemy Scripts (e.g., Wolf.cs): Inherit from Enemy and add unique behaviors,
///   states, and properties specific to that enemy type.
///
/// - EnemyState<T> (Generic Base Class): A generic state that is aware of the specific
///   enemy type it works with (e.g., EnemyState<Wolf>). This ensures type-safety and
///   eliminates the need for casting in derived states.
///
/// - EnemyStateMachine.cs: Manages state transitions (ChangeState) and holds a
///   reference to the CurrentEnemyState.
///
/// - ScriptableObjects (SOs): Used to configure parameters for each state (e.g., moveSpeed,
///   attackDamage, attackCooldown). This allows for easy tweaking of AI behavior in the
///   Unity Inspector without changing code.
///
/// ====================================================================================
#region Wolf
/// 2. ENEMY TYPE: WOLF
/// -----------------------------
///
/// OVERVIEW:
/// The Wolf is a pack-oriented hunter. Its primary behavior revolves around alerting
/// nearby allies with a howl before initiating a coordinated chase and attack.
///
/// --- STATES ---
///
/// >>> WolfIdleState <<<
/// - Purpose: Default state when the Wolf is not aware of the player. Involves wandering.
/// - Entry Conditions:
///   - On spawn (initial state).
///   - When the player is lost after a chase.
/// - Core Logic:
///   1. Pick a random point within a defined "wander radius".
///   2. COLLISION CHECK: Before moving, ensure the path to the new point is not blocked.
///      If blocked, pick a new point.
///   3. Move towards the target point.
///   4. On arrival, wait for a short duration, then repeat.
/// - Exit Conditions:
///   - Player enters Aggro Trigger Zone -> Transition to WolfHowlState.
///   - Receives an "alert" event from another Wolf -> Transition to WolfHowlState.
///
/// >>> WolfHowlState <<<
/// - Purpose: To alert other wolves and provide a "tell" for the player before the chase.
/// - Entry Conditions:
///   - From WolfIdleState when the player is first detected.
/// - Core Logic:
///   1. Play "Howl" animation and sound. Set a 'hasAlerted' flag to 'true'.
///   2. ALERT MECHANISM: Use Physics.OverlapSphere to find other Wolf instances
///      within an "alert radius".
///   3. Call an OnAlerted() method on nearby idle wolves, causing them to also enter this state.
/// - Exit Conditions:
///   - Howl duration (from SO) has passed -> Transition to WolfChaseState.
///
/// >>> WolfChaseState <<<
/// - Purpose: To close the distance to the player.
/// - Entry Conditions:
///   - From WolfHowlState after the howl is complete.
///   - From WolfAttackState after an attack's cooldown.
/// - Core Logic:
///   1. Continuously calculate the vector towards the player's position.
///   2. Use the MoveEnemy() function to move towards the player.
///   3. Constantly check distance to the player.
/// - Exit Conditions:
///   - Player is within striking distance -> Transition to WolfAttackState.
///   - Player is lost (out of range/sight) -> Transition to WolfIdleState.
///
/// >>> WolfAttackState <<<
/// - Purpose: To perform a melee attack on the player.
/// - Entry Conditions:
///   - From WolfChaseState when the player is in attack range.
/// - Core Logic:
///   1. Stop movement.
///   2. Trigger the attack animation.
///   3. On a specific animation frame (using an Animation Event), apply damage.
///   4. Start an attackCooldown timer (from SO).
/// - Exit Conditions:
///   - Animation is complete and cooldown has passed -> Transition to WolfChaseState.
#endregion
/// ====================================================================================
/// 3. FUTURE DEVELOPMENT & TODOs
/// -----------------------------
///
/// >>> FleeState (Future State) <<<
/// - Purpose: Allow enemies to retreat when overwhelmed.
/// - Logic: When health drops below a threshold (e.g., 25%), the enemy will move *away*
///   from the player's position until it reaches a safe distance.
///
/// >>> PatrolState (Future State) <<<
/// - Purpose: A more structured alternative to the wandering IdleState.
/// - Logic: The enemy follows a predefined path of waypoints. Useful for guards.
///
/// --- ACTIONABLE TODO LIST ---
///
/// 1. [ ] Implement Alert System: Code the OverlapSphere check in WolfHowlState and the
///      corresponding OnAlerted() method in the Wolf class. Use a 'hasAlerted' flag
///      to prevent alert loops.
///
/// 2. [ ] Add Attack Cooldown: Implement the cooldown timer in WolfAttackState to
///      prevent attack spamming.
///
/// 3. [ ] Refine "Wander" Logic: Implement the collision check in WolfIdleState to
///      prevent wolves from getting stuck on geometry.
///
/// 4. [ ] Health-Based Behavior: Add logic to check CurrentHealth to potentially trigger
///      the FleeState when it is implemented.
///
/// ====================================================================================
/// </summary>