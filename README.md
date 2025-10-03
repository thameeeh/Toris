# Documentation

- Game Project

graph TD;
    subgraph "Sensor Triggers"
        AggroCheck[EnemyAggroCheck]
        StrikeCheck[EnemyStrikingDistanceCheck]
    end

    subgraph "Core Enemy Logic"
        Enemy --> StateMachine[EnemyStateMachine];
        StateMachine -- Manages --> IdleState[EnemyIdleState];
        StateMachine -- Manages --> ChaseState[EnemyChaseState];
        StateMachine -- Manages --> AttackState[EnemyAttackState];
    end

    subgraph "Behavior Modules (ScriptableObjects)"
        IdleState -- Uses --> IdleSO[EnemyIdleSOBase];
        ChaseState -- Uses --> ChaseSO[EnemyChaseSOBase];
        AttackState -- Uses --> AttackSO[EnemyAttackSOBase];
    end

    %% --- Connections ---
    AggroCheck -- "Calls SetAggroStatus()" --> Enemy;
    StrikeCheck -- "Calls SetStrikingDistanceBool()" --> Enemy;
    IdleState -- "Transitions based on IsAggroed" --> ChaseState;
    ChaseState -- "Transitions based on IsWithinStrikingDistance" --> AttackState;
    AttackState -- "Transitions if player leaves range" --> ChaseState;
