# Necromancer Enemy Setup

## 2026-04-07 - Visual Setup

Current scope:
- Standalone hostile enemy setup only.
- Visual and animation contract first.
- First-pass form/movement/cast-readiness logic is implemented.
- Projectile logic, VFX, pooling registration, and POI/world integration are not implemented yet.

Current animation folder:
- `Necromancer.controller`
- `Necromancer_Human_Idle.anim`
- `Necromancer_Floater_Idle.anim`
- `Necromancer_Human_To_Floater.anim`
- `Necromancer_Floater_To_Human.anim`
- `Necromancer_Run.anim`
- `Necromancer_Attack.anim` legacy/old combined attack clip
- `Necromancer_Air_Slash.anim` panic swing clip
- `Necromancer_Projectile.anim` spell-cast/projectile clip
- `Necromancer_Summon.anim` summon animation hook clip
- `Necromancer_Human_Dead.anim`
- `Necromancer_Floater_Dead.anim`
- `Necromancer_Attack 1.png`: only attack source texture, sliced into 11 frames.
- `Necromancer_Switch_Idle 1.png`: only idle/switch source texture, sliced into 19 frames.
- `Necromancer_Walk.png`: only movement source texture, sliced into 4 frames.
- `ShadowUnderCharacter.png`: small shadow texture only.
- `OLD/` contains the discarded UI showcase-based animation assets.
- Project scan found no other usable Necromancer sprite sheets or gameplay animation clips.

Animator contract to support future Wolf-style enemy logic:
- `DirectionX` float
- `DirectionY` float
- `IsMoving` bool
- `Attack` trigger, deprecated legacy trigger from the old combined attack state
- `Dead` trigger
- `SpellCast` trigger
- `PanicSwing` trigger
- `Summon` trigger

Expected controller states:
- `Human_Idle` using `Necromancer_Human_Idle.anim`
- `Human_To_Floater` using `Necromancer_Human_To_Floater.anim`
- `Floater_Idle` using `Necromancer_Floater_Idle.anim`
- `Floater_To_Human` using `Necromancer_Floater_To_Human.anim`
- `Run` using `Necromancer_Run.anim`, which is human form
- `Projectile` using `Necromancer_Projectile.anim`, which is floater spell-cast form
- `Air_Slash` using `Necromancer_Air_Slash.anim`, which is floater panic-swing form
- `Summon` using `Necromancer_Summon.anim`, which is a first-pass summon animation hook
- `Attack` using `Necromancer_Attack.anim`, legacy/old combined attack state
- `Human_Dead` using `Necromancer_Human_Dead.anim`
- `Floater_Dead` using `Necromancer_Floater_Dead.anim`

Expected clip settings:
- `Necromancer_Human_Idle.anim`: loop enabled
- `Necromancer_Floater_Idle.anim`: loop enabled
- `Necromancer_Human_To_Floater.anim`: loop disabled
- `Necromancer_Floater_To_Human.anim`: loop disabled
- `Necromancer_Run.anim`: loop enabled
- `Necromancer_Attack.anim`: loop disabled
- `Necromancer_Air_Slash.anim`: loop disabled
- `Necromancer_Projectile.anim`: loop disabled
- `Necromancer_Summon.anim`: loop disabled
- `Necromancer_Human_Dead.anim`: loop disabled
- `Necromancer_Floater_Dead.anim`: loop disabled

Required animation events:
- `Necromancer_Attack.anim`: `Anim_AttackHit()` on the projectile release frame
- `Necromancer_Attack.anim`: `Anim_AttackFinished()` near the end of the cast animation
- `Necromancer_Air_Slash.anim`: `Anim_AttackHit()` and `Anim_AttackFinished()`
- `Necromancer_Projectile.anim`: `Anim_AttackHit()` and `Anim_AttackFinished()`
- `Necromancer_Summon.anim`: `Anim_AttackHit()` and `Anim_AttackFinished()`
- `Necromancer_Human_Dead.anim`: `Anim_Despawn()` near the end of the death animation
- `Necromancer_Floater_Dead.anim`: `Anim_Despawn()` near the end of the death animation

Verified current setup:
- `Necromancer.controller` has `Necromancer_Human_Idle`, `Necromancer_Human_To_Floater`, `Necromancer_Floater_Idle`, `Necromancer_Floater_To_Human`, `Necromancer_Run`, `Necromancer_Attack`, `Necromancer_Human_Dead`, and `Necromancer_Floater_Dead` states.
- `Necromancer.controller` has `DirectionX`, `DirectionY`, `IsMoving`, `Attack`, `Dead`, `BecomeFloater`, and `BecomeHuman` parameters.
- `Necromancer.controller` has `SpellCast`, `PanicSwing`, and `Summon` triggers for the split attack clips.
- `Necromancer_Human_Idle.anim` loops.
- `Necromancer_Floater_Idle.anim` loops.
- `Necromancer_Human_To_Floater.anim` does not loop.
- `Necromancer_Floater_To_Human.anim` does not loop.
- `Necromancer_Run.anim` loops.
- `Necromancer_Attack.anim` does not loop.
- `Necromancer_Air_Slash.anim`, `Necromancer_Projectile.anim`, and `Necromancer_Summon.anim` do not loop.
- `Necromancer_Human_Dead.anim` does not loop.
- `Necromancer_Floater_Dead.anim` does not loop.
- `Necromancer_Attack.anim` calls `Anim_AttackHit()` at `0.36666667`.
- `Necromancer_Attack.anim` calls `Anim_AttackFinished()` at `1.2833333`.
- The split attack clips call `Anim_AttackHit()` and `Anim_AttackFinished()` so the existing `EnemyAnimationEventRelay` and `NecromancerAttackSO` completion flow still works.
- `Necromancer_Human_Dead.anim` and `Necromancer_Floater_Dead.anim` call `Anim_Despawn()`.
- `Human_Idle -> Human_To_Floater` uses `BecomeFloater`.
- `Floater_Idle -> Floater_To_Human` uses `BecomeHuman`.
- `Human_Idle -> Run` uses `IsMoving == true`.
- `Run -> Human_Idle` uses `IsMoving == false`.
- `Floater_Idle -> Attack` uses `Attack`.
- `Floater_Idle -> Projectile` uses `SpellCast`.
- `Floater_Idle -> Air_Slash` uses `PanicSwing`.
- `Floater_Idle -> Summon` uses `Summon`.
- `Projectile`, `Air_Slash`, and `Summon` return to `Floater_Idle` by exit time and can transition to `Floater_Dead` on `Dead`.
- No `Any State` transitions are currently present.

Prefab skeleton progress:
- Unity scene/object hierarchy started with root `Necromancer`.
- Expected child objects are `Animator`, `AggroCheck`, `CastingRange`, `StrikingDist`, and `CastPoint`.
- `CastingRange` is a child object between `AggroCheck` and `StrikingDist`.
- The `Animator` child should own `SpriteRenderer`, `Animator`, and `EnemyAnimationEventRelay`.
- Root physics and trigger shell setup completed in Unity scene/object form.
- Gameplay prefab asset saved at `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer.prefab`.
- Verified root `Necromancer` object uses tag `Enemy` and layer `EnemyHurtBox`.
- Verified root has `Rigidbody2D` and a non-trigger `CapsuleCollider2D`.
- Verified child `Animator` uses `Necromancer.controller` and has `EnemyAnimationEventRelay`.
- Current active clips now target `SpriteRenderer.sprite` directly on the `Animator` object.
- Verified the rebuilt prefab removed the obsolete nested `Animator/Anim_Ref/Enemy` visual path.
- Verified the rebuilt prefab has a single active `SpriteRenderer` on the `Animator` object.
- Verified `Run` is assigned to `Necromancer_Run.anim`.
- Verified child `AggroCheck` uses layer `EnemyAggro`, trigger `CircleCollider2D`, radius `8`, and `EnemyAggroCheck`.
- Verified child `CastingRange` uses layer `EnemyStrikingbox`, trigger `CircleCollider2D`, radius `6`, and `NecromancerCastingRangeCheck`.
- Verified child `StrikingDist` uses layer `EnemyStrikingbox`, trigger `CircleCollider2D`, radius `2`, and `EnemyStrikingDistanceCheck`.
- `StrikingDist` is now the panic swing range and should stay smaller than the middle casting range.
- `CastingRange` should stay between outer aggro and close panic swing range; first radius is `6`.
- Verified child `CastPoint` exists.
- Verified root `Necromancer` has the `Necromancer` component assigned.
- Verified root `Necromancer` has `GridPathAgent` assigned.
- Verified the `Necromancer` component has `rb` assigned to the root `Rigidbody2D`.
- Verified the `Necromancer` component has `CastPoint` assigned.
- Verified `Necromancer_Idle_Stand.asset`, `Necromancer_Chase_CastRange.asset`, `Necromancer_Attack_BoltCast.asset`, and `Necromancer_Dead_Final.asset` exist and are assigned on the prefab.

Future implementation notes:
- `Necromancer : Enemy` skeleton has been added.
- Wolf-style state/SO split has been added with `Idle`, `Chase`, `Attack`, and `Dead`.
- Idle is being split visually into human form, human-to-floater transition, floater form, and floater-to-human transition while keeping one gameplay idle state.
- Idle should not auto-cycle between human and floater forms. The Necromancer remains human by default and only becomes a floater when behavior explicitly triggers the transformation.
- Human form uses `Run` while moving.
- Floater form can still move/reposition, but visually stays in floater idle rather than transitioning to `Run`.
- Attack should route only from floater form; human form should not transition directly to `Attack`.
- Code now gates attack entry on `Necromancer_Floater_Idle` via `Necromancer.IsReadyToCastAnimation`, so the enemy waits for floater form before entering the attack state.
- Chase no longer force-plays `Run` on state entry; movement visuals are driven by `IsMoving` and the controller's human-form movement transitions.
- Temporary editor-only animation debug logs are enabled on the prefab through `debugAnimationTransitions`.
- Debug logs are prefixed with `[NecromancerAnim:<object name>]` and include gameplay state changes, Animator state changes, attack gating reasons, and attack/death animation events.
- Death is form-specific: human form routes to `Human_Dead`, and floater form routes to `Floater_Dead`.
- Chase now runs its movement/animation logic before checking whether it can cast, mirroring the Wolf chase flow more closely.
- `AggroCheck` radius is `8`, `CastingRange` is `6`, and `StrikingDist` is `2`.
- If the player starts inside `StrikingDist`, the Necromancer is expected to prefer panic swing over spell casting.
- Use a child `EnemyAnimationEventRelay` on the animator object so clip events can reach the enemy state machine.
- Spawn the ranged projectile from `NecromancerAttackSO` only when `Anim_AttackHit()` fires.
- Projectile and VFX logic are still intentionally not implemented.

## 2026-04-08 - Behavior Direction

Current behavior concept:
- Coding approach: keep logic KISS/DRY, avoid magic numbers, and expose tuning values as serialized fields so behavior can be adjusted as a design problem rather than by code edits.
- Idle with no player: the Necromancer wanders in human form. It can become a floater only when no player is in detectable range and it has been idling for a randomized 10-15 second window.
- Idle floater duration: default `10` seconds, exposed as a serialized tuning value.
- Aggro with player far: human form alerts and runs toward the player; floater form floats toward the player to get into casting range.
- Aggro with player in casting range: human form begins transforming into floater form to cast; floater form casts immediately if cooldown/attack rules allow.
- Combat spacing: the Necromancer should try to maintain a safe distance of `6` units, back away if the player is closer than `4` units, and always reposition after a spellcast.
- Repositioning while floater: float backward away from the player.

Planned future abilities:
- Spell casting attack is the first implementation target.
- Melee panic swing is implemented as an animation/selection hook when the player is inside `StrikingDist`.
- Summon is implemented as a phase-two animation/selection hook at the serialized half-health threshold; actual minion spawning is still future work.
- Current split attack behavior: `StrikingDist` selects `PanicSwing`, `CastingRange` selects `SpellCast`, and after health is at or below the serialized summon threshold, casting range can select recurring `Summon` on its own cooldown.

## 2026-04-08 - Necromancer Brain

Core identity:
- The Necromancer is a two-form enemy: human form moves with `Run`, while floater form casts, panic-swings, summons, and floats without using the run animation.
- The Necromancer is hostile, but presents a human facade while roaming the world.
- Human form is the default roaming disguise.
- Floater form is behavior-triggered, not an automatic idle cycle, and reveals the monster behind the human facade.
- Combat should feel like a ranged caster trying to maintain space, with a close-range panic response when the player breaks in.
- The player should read the transformation as the moment the disguised hostile becomes an active monster encounter.

Form responsibilities:
- Human form owns grounded movement, roaming, and the possible rescue/non-hostile variant.
- Floater form owns spell casting, panic swing, summon hook, retreating, and post-cast repositioning.
- `IsMoving` is treated as the human run animation flag only.
- Floater movement still moves the Rigidbody, but leaves `IsMoving` false so the animator stays visually in floater idle unless an attack/summon/death state is triggered.

Range responsibilities:
- `AggroCheck` is the outer transition/awareness radius.
- `CastingRange` is the middle spell/summon decision radius.
- `StrikingDist` is the close panic swing radius.
- Current first-pass radii are `AggroCheck = 8`, `CastingRange = 6`, and `StrikingDist = 2`.
- `preferredDistance = 6` is still used by chase spacing and post-cast reposition completion.
- `retreatDistance = 4` is the combat spacing threshold that makes floater form move backward if the player is too close but not necessarily inside panic swing range.

Idle brain:
- If `IsAggroed` becomes true, `NecromancerIdleState` resolves the aggro transition first, then switches to `ChaseState`.
- If no player is detected, `NecromancerIdleSO` controls roaming and no-combat form timing.
- Human no-player roam picks wander targets around the current roam center.
- After a randomized `10-15` second no-player window, human form can request `BecomeFloater`.
- Floater no-player duration is `10` seconds, then it requests `BecomeHuman`.
- If a form transition starts, idle roaming stops for that frame so `BecomeFloater`/`BecomeHuman` does not compete with movement transitions.

Aggro transition brain:
- On first aggro per spawn, `Necromancer.ResolveAggroTransition()` rolls the serialized `humanRescueVariantChance`.
- If the rescue variant wins, the Necromancer remains human, cannot become floater, and is invincible while `humanRescueVariantInvincible` is enabled.
- The rescue variant currently only moves away in chase behavior; the actual NPC/rescue flow is future work.
- If the rescue variant does not win, the Necromancer behaves as a hostile caster.

Chase brain priority:
- If there is no player transform, stop.
- If the Necromancer is the human rescue variant, move away from the player and skip hostile attack selection.
- If the Necromancer is changing form, stop and face the player.
- If post-cast reposition is required, move away until the reposition timer has elapsed and distance is back at or beyond preferred distance.
- If the player is inside `StrikingDist`, choose the panic swing branch.
- If the player is closer than `retreatDistance`, transform to floater if still human; otherwise float backward.
- If the player is inside `CastingRange`, stop and face the player.
- If inside `CastingRange` while human, request `BecomeFloater`.
- If inside `CastingRange` while floater, choose `Summon` when phase two is unlocked and summon cooldown is ready; otherwise choose `SpellCast`.
- If none of the above applies, move toward the player.

Attack selection:
- `NecromancerChaseSO` chooses the next `NecromancerAttackType`.
- `PanicSwing` is selected from `StrikingDist`.
- `SpellCast` is the default casting range attack.
- Phase two begins when health reaches the serialized summon threshold.
- In phase two, the Necromancer gains access to `Summon` on top of projectile casting.
- `Summon` uses its own cooldown, so phase two can alternate between projectile casting and summon opportunities instead of consuming summon once.
- `NecromancerChaseState` only enters `AttackState` when cooldown is ready and the animator is in `Necromancer_Floater_Idle`.
- `NecromancerAttackState` is still one gameplay state; the selected attack type determines which animator trigger is fired.

Attack animation mapping:
- `SpellCast` trigger plays `Necromancer_Projectile`.
- `PanicSwing` trigger plays `Necromancer_Air_Slash`.
- `Summon` trigger plays `Necromancer_Summon`.
- The old `Attack` trigger and `Necromancer_Attack` state are deprecated and should be removed once the controller is cleaned up.
- All active split attack clips must call `Anim_AttackHit()` and `Anim_AttackFinished()`.

Attack completion:
- `NecromancerAttackSO` stops movement and faces the player throughout the attack.
- `Anim_AttackHit()` starts the cooldown for the selected attack type.
- `Anim_AttackFinished()` marks the attack complete and returns gameplay to chase.
- `SpellCast` requires post-cast reposition after finishing.
- `PanicSwing` represents a close-range 360-degree response around the Necromancer, but actual damage payload is not implemented yet.
- `Summon` is intended to summon Blood Mages in phase two, but Blood Mages are a future enemy/system and are not implemented yet.
- Current code starts a summon cooldown when the summon animation hit event fires, then allows future summons once that cooldown is ready again.

Death brain:
- Death is form-specific through animator transitions.
- Human form routes to `Necromancer_Human_Dead`.
- Floater/attack/summon form routes to `Necromancer_Floater_Dead`.
- `Necromancer.Die()` stops movement before entering `DeadState`, so velocity and `IsMoving` are cleared before the dying animation trigger is requested.
- `NecromancerDeadSO` stops movement and requests the `Dead` trigger.
- Death clips call `Anim_Despawn()`.

Current tuning knobs:
- `Necromancer.AttackDamage`
- `Necromancer.MovementSpeed`
- `Necromancer.hideShadowInHumanForm`
- `Necromancer.flipSpriteHorizontally`
- `Necromancer.humanRescueVariantChance`
- `Necromancer.humanRescueVariantInvincible`
- `Necromancer.summonHealthThreshold`
- `NecromancerIdleSO.roamRadius`
- `NecromancerIdleSO.idleMoveSpeedMultiplier`
- `NecromancerIdleSO.minHumanIdleBeforeFloat`
- `NecromancerIdleSO.maxHumanIdleBeforeFloat`
- `NecromancerIdleSO.floaterIdleDuration`
- `NecromancerChaseSO.preferredDistance`
- `NecromancerChaseSO.retreatDistance`
- `NecromancerChaseSO.floaterMoveSpeedMultiplier`
- `NecromancerChaseSO.postCastRepositionDuration`
- `NecromancerAttackSO.castCooldown`
- `NecromancerAttackSO.panicSwingCooldown`
- `NecromancerAttackSO.summonCooldown`

Known incomplete brain pieces:
- `SpellCast` has animation/cooldown/reposition behavior, but no projectile prefab spawn yet.
- `PanicSwing` has animation/cooldown selection, but no 360-degree damage payload yet.
- `Summon` has animation/cooldown/recurring phase-two selection, but no Blood Mage spawn behavior yet.
- Rescue variant has a first-pass invincible/non-floater behavior, but no friendly NPC conversion or rescue reward flow yet.

Script skeleton files:
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer.cs`
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer States/NecromancerIdleState.cs`
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer States/NecromancerChaseState.cs`
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer States/NecromancerAttackState.cs`
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer States/NecromancerDeadState.cs`
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer Behaviour/Idle/NecromancerIdleSO.cs`
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer Behaviour/Chase/NecromancerChaseSO.cs`
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer Behaviour/Attack/NecromancerAttackSO.cs`
- `Assets/Scripts/Enemy/Enemy Types/Necromancer/Necromancer Behaviour/Dead/NecromancerDeadSO.cs`

Implemented first-pass behavior split:
- `Necromancer.cs` owns shared form/runtime animation requests: human/floater form checks, `BecomeFloater`, `BecomeHuman`, `Attack`, `Dead`, post-cast reposition flag, and editor-only debug logging.
- `NecromancerIdleSO` owns no-player roaming and idle form timing. Human form roams with `Run`; after a randomized 10-15 second no-player window it requests floater form, then floater form lasts 10 seconds before requesting human form again.
- `NecromancerIdleState` checks `IsAggroed` before running idle SO logic so no-player floater timing cannot trigger after the player has entered detection.
- `NecromancerChaseSO` owns combat spacing. Preferred casting distance is 6 units, retreat threshold is 4 units, floater movement uses a slower serialized multiplier, and post-cast repositioning is required before another cast can happen.
- `NecromancerCastingRangeCheck` owns the middle casting range trigger and sets `Necromancer.IsWithinCastingRange`.
- `AggroCheck` is the outer transition point. On first aggro, `Necromancer` can roll the serialized human rescue variant chance; if successful, it remains human and invincible for the current first-pass behavior.
- `StrikingDist` is now treated as panic swing range, not projectile cast range.
- `NecromancerChaseSO` stops movement for the frame when requesting a form change, preventing `BecomeFloater` from competing with `IsMoving`/`Run` transitions.
- `NecromancerAttackSO` owns cast cooldown and marks post-cast reposition when `Anim_AttackFinished()` fires.
- `NecromancerAttackSO` now supports separate cooldowns for spell cast, panic swing, and recurring phase-two summon animation hooks.
- `NecromancerDeadSO` owns death movement shutdown and requests the form-specific death trigger through `Necromancer`.
- `IsMoving` is now treated as the human run animation flag. Floater movement still moves the Rigidbody but leaves the animator in floater idle visuals.
- `Enemy.UpdateAnimationDirection` is virtual so Necromancer can add sprite flipping without changing Wolf behavior.
- `Necromancer` auto-caches the body `SpriteRenderer` from the `Animator` object and the shadow `SpriteRenderer` from `Animator/Shadow`.
- Shadow visibility is driven by form state: hidden in human form, visible while transforming/floating/attacking/dead unless `hideShadowInHumanForm` is disabled.
- Left/right facing is driven by horizontal movement/facing direction through `SpriteRenderer.flipX`; vertical-only movement keeps the previous horizontal facing.
