# Runner Shooter Prototype

## Entry Point

`Assets/_Project/Scripts/Bootstrap/GameBootstrapper.cs` is the runtime entry point.

Put `GameBootstrapper` on a scene object and assign `GameConfigAsset` in the Inspector.

For quick setup use:

`Tools/TestTask/Setup Current Scene`

This creates:

- `Assets/_Project/Config/GameConfig.asset`
- prototype prefabs under `Assets/_Project/Prefabs`
- a scene object named `[Bootstrap] Game Entry Point`
- an assigned config reference on `GameBootstrapper`

If there is no scene bootstrap object, the project still creates a fallback runtime bootstrap automatically.

## Architecture

Folders are split by responsibility:

- `Bootstrap` - entry point and composition root.
- `Config` - ScriptableObject game balance, enemy type definitions, prefab references.
- `Core` - state machine, progression, upgrades, spawning.
- `Gameplay` - MonoBehaviour presentation and player-facing components.
- `ECS` - data components, simulation systems, and ECS/GameObject bridge.
- `UI` - HUD and upgrade overlay.
- `Utils` - scene/model/material factory helpers.

## ECS Layer

Mass gameplay entities are stored as ECS data:

- `EnemyData`, `EnemyTypeData`
- `BulletData`
- `PickupData`
- `GameRuntimeData`
- `CarDamageEvent`, `RewardEvent`

Systems:

- `EnemyMovementSystem` handles idle/run/attack transitions and car damage events.
- `BulletCollisionSystem` moves bullets, applies damage, hit events and death reward events.
- `PickupMagnetSystem` attracts money/XP pickups to the car in the configured radius.

`CombatEcsBridge` is the boundary between ECS and GameObjects. It spawns ECS entities, syncs lightweight presenters, and consumes ECS events into `GameSession`.

## Enemy Types

`GameConfigAsset.EnemyTypes` supports different enemy types with:

- HP
- speed
- damage
- XP reward
- coin reward
- color/presentation
- run animation speed
- attack animation speed
- hit pose strength
- optional presenter prefab override

`EnemyPresenter` supports two modes:

- Animator mode: assign `Animator` and set state names for run, attack and hit.
- Procedural fallback: leave Animator empty and the presenter animates run/attack/hit placeholders in code.

Death particles and visual root can also be assigned directly on the enemy presenter prefab.

## Gameplay Requirements Covered

- Tap to start.
- Car follows an automatic snake-like forward path.
- Player controls turret angle with finger/mouse.
- Turret auto-snaps to enemies inside the aimed cone and fires automatically.
- Enemies spawn inside road width.
- Enemies idle, then run to the car, then attack.
- Enemies flash white on hit.
- Enemies emit particles on death.
- Money and XP pickups drop from enemies and magnetize to the car.
- Car HP, XP bar, coins and distance UI.
- Level-up offers 1 of 3 upgrades.
- Win/lose overlay and tap restart.
- Star result: 3 for no damage, 2 for moderate damage, 1 for heavy damage.

Approximate implementation time: 4-5 hours for this prototype pass.
