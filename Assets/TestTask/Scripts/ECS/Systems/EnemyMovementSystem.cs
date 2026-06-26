using System.Collections.Generic;
using TestTask.ECS.Bridge;
using TestTask.ECS.Components;
using Unity.Mathematics;

namespace TestTask.ECS.Systems
{
    public sealed class EnemyMovementSystem
    {
        private readonly List<int> _entities = new();

        public void Tick(CombatEcsWorld world, GameRuntimeData runtime)
        {
            if (!runtime.IsRunning && !runtime.AllowPassiveEnemyMovement)
                return;

            _entities.Clear();
            _entities.AddRange(world.Enemies.Keys);

            foreach (var entity in _entities)
            {
                var enemy = world.Enemies[entity];
                if (enemy.State == EnemyEcsState.Dead)
                    continue;

                var transform = world.Transforms[entity];
                var type = world.EnemyTypes[entity];

                if (transform.Position.z < runtime.CarPosition.z - runtime.EnemyDespawnBehindDistance)
                {
                    world.RemoveEnemySilently(entity);
                    continue;
                }

                if (enemy.HitTimer > 0f)
                {
                    enemy.HitTimer -= runtime.DeltaTime;
                    enemy.State = EnemyEcsState.Hit;
                    world.Enemies[entity] = enemy;
                    continue;
                }

                if (!runtime.IsRunning)
                {
                    enemy.AttackCooldown = 0f;
                    TickWander(ref enemy, ref transform, type.Speed, runtime);
                    world.Transforms[entity] = transform;
                    world.Enemies[entity] = enemy;
                    continue;
                }

                var toCar = runtime.CarPosition - transform.Position;
                toCar.y = 0f;
                var distance = math.length(toCar);
                var contactDistance = math.max(0f, distance - runtime.CarBodyRadius);

                if (distance <= runtime.CarBodyRadius)
                {
                    enemy.State = EnemyEcsState.Dead;
                    enemy.DeathEvent++;
                    world.CarDamageEvents.Add(new CarDamageEvent { Amount = type.DamagePerSecond });
                    world.RewardEvents.Add(EnemyRewardUtility.CreateDrop(type, transform.Position));
                    world.Enemies[entity] = enemy;
                    continue;
                }

                if (contactDistance <= runtime.EnemyAttackRange)
                {
                    enemy.State = EnemyEcsState.Attacking;
                    enemy.AttackCooldown -= runtime.DeltaTime;
                    if (enemy.AttackCooldown <= 0f)
                    {
                        var attackInterval = math.max(0.05f, runtime.EnemyAttackInterval);
                        world.CarDamageEvents.Add(new CarDamageEvent { Amount = type.DamagePerSecond * attackInterval });
                        enemy.AttackCooldown = attackInterval;
                    }
                    world.Enemies[entity] = enemy;
                    continue;
                }

                enemy.AttackCooldown = 0f;

                if (distance <= runtime.EnemyWakeDistance && distance > 0.001f)
                {
                    var direction = toCar / distance;
                    var moveDistance = math.min(type.Speed * runtime.DeltaTime, math.max(0f, contactDistance - runtime.EnemyAttackRange));
                    transform.Position += direction * moveDistance;
                    transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
                    enemy.State = EnemyEcsState.Running;
                }
                else
                {
                    TickWander(ref enemy, ref transform, type.Speed, runtime);
                }

                world.Transforms[entity] = transform;
                world.Enemies[entity] = enemy;
            }
        }

        private static void TickWander(ref EnemyData enemy, ref EcsTransform transform, float baseSpeed, GameRuntimeData runtime)
        {
            if (enemy.HasWanderTarget)
            {
                var toTarget = enemy.WanderTarget - transform.Position;
                toTarget.y = 0f;
                var distance = math.length(toTarget);
                if (distance <= 0.1f)
                {
                    enemy.HasWanderTarget = false;
                    enemy.State = EnemyEcsState.Idle;
                    return;
                }

                var direction = toTarget / math.max(0.001f, distance);
                var moveDistance = math.min(baseSpeed * runtime.EnemyWanderSpeedMultiplier * runtime.DeltaTime, distance);
                transform.Position += direction * moveDistance;
                transform.Position.x = math.clamp(transform.Position.x, -runtime.RoadWidth * 0.45f, runtime.RoadWidth * 0.45f);
                transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
                enemy.State = EnemyEcsState.Walking;
                return;
            }

            enemy.State = EnemyEcsState.Idle;
            enemy.WanderCooldown -= runtime.DeltaTime;
            if (enemy.WanderCooldown > 0f || UnityEngine.Random.value > runtime.EnemyWanderChancePerSecond * runtime.DeltaTime)
                return;

            var angle = UnityEngine.Random.Range(0f, math.PI * 2f);
            var wanderDistance = UnityEngine.Random.Range(runtime.EnemyWanderMinDistance, runtime.EnemyWanderMaxDistance);
            var offset = new float3(math.sin(angle), 0f, math.cos(angle)) * wanderDistance;
            enemy.WanderTarget = transform.Position + offset;
            enemy.WanderTarget.x = math.clamp(enemy.WanderTarget.x, -runtime.RoadWidth * 0.45f, runtime.RoadWidth * 0.45f);
            enemy.HasWanderTarget = true;
            enemy.WanderCooldown = UnityEngine.Random.Range(0.25f, 1.2f);
        }
    }
}
