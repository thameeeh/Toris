/*
Naming guidelines. The core principle is [EnemyType][Behavior][ComponentType].

1. State Scripts (.cs files)

    This is for the state logic itself. The pattern is [EnemyType][Behavior]State.cs.

    ===================================================================================
    || Examples:                                                                     ||
    || WolfIdleState.cs                                                              ||
    || WolfChaseState.cs                                                             ||
    || WolfHowlState.cs                                                              ||
    || BadgerBurrowState.cs                                                          ||
    || BadgerAttackState.cs                                                          ||
    ===================================================================================

    Why: This is clean and groups all states for a specific enemy together
    alphabetically in your file explorer.

2. ScriptableObject Scripts (.cs files)

    This is for the C# code that defines your ScriptableObject's data and logic.

    A. Generic Base Scripts

        These are the core templates.

        ===================================================================================
        || EnemyBehaviorSO.cs (The new grand-base class)                                 ||
        ||                                                                               ||
        || IdleSOBase.cs (Inherits from EnemyBehaviorSO<T>)                              ||
        || ChaseSOBase.cs                                                                ||
        || AttackSOBase.cs                                                               ||
        ===================================================================================

    B. Concrete Implementation Scripts

        This is the script that contains the specific logic for one enemy's behavior.
        The pattern is [EnemyType][Behavior]SO.cs.

        ===================================================================================
        || Examples:                                                                     ||
        || WolfIdleSO.cs                                                                 ||
        || WolfChaseSO.cs                                                                ||
        || BadgerIdleSO.cs                                                               ||
        ===================================================================================

    Why: This clearly links the SO's logic to the enemy it's designed for.
    There's no ambiguity about what WolfIdleSO.cs is meant to do.

3. ScriptableObject Assets (.asset files)

    These are the actual assets you create in the Unity Editor (Create > Enemy > ...).
    These names are what your designers (or you) will see and work with most often,
    so they need to be very descriptive.
    The pattern is [EnemyType]_[Behavior]_[Variant].asset.
    The "Variant" is crucial for creating different versions of the same enemy.

    ===================================================================================
    || Examples:                                                                     ||
    || Wolf_Idle_Wander.asset                                                        ||
    || Wolf_Idle_Patrol.asset                                                        ||
    || Wolf_Chase_Standard.asset                                                     ||
    || Wolf_Attack_QuickBite.asset                                                   ||
    || Badger_Attack_ClawSwipe.asset                                                 ||
    || Boar_Chase_Charge.asset                                                       ||
    ===================================================================================

    Why: This makes it incredibly easy to find and assign the correct behavior in the Inspector. A designer can simply type "Wolf_" in the asset search bar to see all available behaviors for the wolf.
*/