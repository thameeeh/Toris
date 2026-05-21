# SFX Implementation Map

This map turns the current rough sound pile into authoring families. Use it as the working queue before creating definitions, rules, or new gameplay hooks.

## Naming Rules

- Use lowercase IDs with underscores.
- Prefer one ID per gameplay intention, not one ID per source file.
- Put alternate takes in the same `SfxDefinition.clips` array when they should randomize.
- Use one-shots for short actions and loops for ambience, portals, and sustained states.

## First Batch

| Priority | Family | Proposed SFX ID | Source Clips | Trigger | Hook Status | Notes |
|---|---|---|---|---|---|---|
| 1 | Menu hover | `ui_menu_hover` | `SFX_Menu_Hover` | UI Toolkit button hover | `GameView` emits through `UIEventsSO`; `UISfxEventBridge` plays it | 2D, quiet, short cooldown. |
| 1 | Menu confirm | `ui_menu_confirm` | `SFX_menu_confirm` | UI Toolkit button click | `GameView` emits through `UIEventsSO`; `UISfxEventBridge` plays it | 2D, slightly louder than hover. |
| 1 | Inventory open | `ui_inventory_open` | `single_ziper`, optional `Cloth` layer | `ScreenType.Inventory` opened | Wired through `UISfxEventBridge` default inventory screen SFX | One-shot. |
| 1 | Inventory close | `ui_inventory_close` | `single_ziper` reversed/edited or `Cloth 2` | `ScreenType.Inventory` closed | Wired through `UISfxEventBridge` default inventory screen SFX | One-shot. |
| 1 | Coin pickup | `item_coin_pickup` | `Coin` quiet edit | Enemy immediate gold reward | Wired through `EnemyLootTableSO.goldRewardSfxId` | Keep short and quiet. |
| 1 | Coin purchase | `ui_coin_purchase` | `Coin` louder edit | Shop buy/sell success | Wired through `ShopManagerSO.coinTransactionSfxId` | Good for purchase confirmation. |
| 1 | Portal enter | `world_portal_enter` | `SFX_Portal_Enter` | Player enters portal trigger | Needs portal trigger hook | 3D at portal position. |
| 1 | Portal loop | `world_portal_loop` | `SFX_portal_sound` | Portal active while spawned | Needs portal loop owner | Loop, 3D, fade in/out. |

## Player And Inventory

| Family | Proposed SFX ID | Source Clips | Trigger | Hook Status | Notes |
|---|---|---|---|---|---|
| Potion drink | `potion_healdrink` | Existing potion clip | `TimedConsumableUsed` or `HealthConsumableUsed` | Existing player SFX rule path | Use `TimedConsumableUsed` for HoT potion drink. |
| Equip armor | `ui_equip_armor` | Equip/unequip armor edit | Equipment success | Wired through `InventoryActionController.equipSfxId` | Shared with unequip for now. |
| Unequip armor | `ui_equip_armor` | Equip/unequip armor edit | Unequip success | Wired through `InventoryActionController.equipSfxId` | Shared with equip for now. |
| Move item | `ui_item_move` | `Cloth` quiet edit | Inventory drag/drop success | Needs inventory transfer event | Keep subtle. |

## Crafting, Smithing, And Salvage

| Family | Proposed SFX ID | Source Clips | Trigger | Hook Status | Notes |
|---|---|---|---|---|---|
| Forge hit | `craft_forge_hit` | `Forge_Smack`, `Smith_Forge` | Forge/craft success | Wired through `CraftingManagerSO.forgeSuccessSfxId` | Plays after craft output succeeds. |
| Gear upgrade | `craft_gear_upgrade` | `SFX_Gear_Upgrade`, `SFX_Gear_Upgrade_v2` | Gear upgrade success | Needs upgrade event | Could layer with forge hit. |
| Salvage | `craft_salvage` | `Hoover`, `Outlet` edits | Salvage success | Wired through `SalvageManagerSO.salvageSuccessSfxId` | Plays after salvage rewards succeed. |

## Movement

| Family | Proposed SFX ID | Source Clips | Trigger | Hook Status | Notes |
|---|---|---|---|---|---|
| Dirt footsteps | `player_footstep_dirt` | `Footsteps_Dirt` | Walking on dirt | Needs terrain routing | Existing generic footstep loop can be expanded later. |
| Dirt run | `player_footstep_dirt_run` | `Footsteps_Dirt_Run` | Running on dirt | Needs speed/terrain routing | May use same family with pitch if edited. |
| Leaf footsteps | `player_footstep_leaf` | `Footsteps_Leaf` | Walking on leaves/grass | Needs terrain routing | Candidate replacement for dirt if it reads better. |
| Leaf run | `player_footstep_leaf_run` | `Footsteps_Leaf_Run` | Running on leaves/grass | Needs speed/terrain routing | Keep as later system pass. |
| Sand footsteps | `player_footstep_sand` | `Footsteps_Sand` | Walking on sand | Needs terrain routing | Later system pass. |
| Sand run | `player_footstep_sand_run` | `Footsteps_Sand_Run` | Running on sand | Needs terrain routing | Later system pass. |
| Wood run | `player_footstep_wood_run` | `Footsteps_Wood_Run` | Running on wood | Needs terrain routing | Need slow/walk version eventually. |

## World And Ambience

| Family | Proposed ID | Source Clips | Trigger | Hook Status | Notes |
|---|---|---|---|---|---|
| Forest ambience | `amb_forest` | `AMB_forest` | Forest biome active | Needs ambience owner | Loop with fade. |
| Water ambience | `amb_water` | `AMB_Water` | Water/beach biome or proximity | Needs ambience owner | Loop with fade. |
| Wind ambience | `amb_wind` | `AMB_Wind` | Wind layer/weather | Needs ambience owner | Loop with fade. |

## Enemies And Combat

| Family | Proposed SFX ID | Source Clips | Trigger | Hook Status | Notes |
|---|---|---|---|---|---|
| Wolf attack growl | `enemy_wolf_attack_growl` | `Wolf_Growl` | Wolf attack state starts | Wired through `WolfAttackSO.attackGrowlSfxId` | Plays on attack commit, even if the bite misses. |
| Wolf bite hit | `enemy_wolf_bite_hit` | `Wolf_Bite` | Wolf attack hit frame accepts a target | Wired through `WolfAttackSO.biteHitSfxId` | Plays only when the hit path is accepted. |
| Wolf death | `enemy_wolf_death` | Wolf death variants | Wolf death | Wired through `EnemySfxModule_WolfDeath` on wolf prefabs | Randomizes variants in one definition. |
| Deer/boar death | `enemy_deer_boar_death` | `Deer_Exhale` | Deer or boar death | Wired through `EnemySfxModule_DeerBoarDeath` on deer/boar prefabs | Shared temporary death family. |
| Den death | `world_den_death` | `Den_Death`, optional rock layer | Den destroyed | Wired through `WolfDen.clearedSfxId` | Specific one-shot. |
| Arrow impact | `enemy_impacthit` | Existing `ArrowImpact` | Arrow hits enemies, walls, or world objects | Enemy hits use `EnemySfxModule_ImpactHit`; non-enemy/world impacts use `ArrowProjectile.impactSfxId` | Shared impact sound for now. |

## Progression And Music

| Family | Proposed ID | Source Clips | Trigger | Hook Status | Notes |
|---|---|---|---|---|---|
| Level up | `ui_level_up` | `SFX_Ability_Upgrade` | Player level up | Needs progression event | Good alternate use if ability upgrade changes. |
| Ability upgrade | `ui_ability_upgrade` | `SFX_Ability_Upgrade` | Ability unlocked/upgraded | Needs skill event | Decide after level-up test. |
| Main menu music | `music_main_menu` | `BMG_MenuMusic` | Main menu scene | Needs music definition + scene play hook | Use `MusicDefinition`, not `SfxDefinition`. |

## Import Checklist

1. Import edited clips under `Assets/Scripts/AudioManager/Content/`.
2. Create or update one `SfxDefinition` per proposed SFX ID.
3. Add each definition to `SfxLibrary.asset`.
4. For music, create a `MusicDefinition` and add it to `MusicLibrary.asset`.
5. Add bridge components or gameplay rules for the matching hook status.
6. Test in-game for volume, cooldown, pitch randomization, and loop stopping.
