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