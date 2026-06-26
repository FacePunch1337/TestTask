using System.Collections.Generic;
using TestTask.ECS.Bridge;
using TestTask.ECS.Components;
using Unity.Mathematics;

namespace TestTask.ECS.Systems
{
    public sealed class BulletCollisionSystem
    {
        private readonly List<int> _bullets = new();
        private readonly List<int> _enemies = new();
        private readonly List<int> _barrels = new();

        public void Tick(CombatEcsWorld world, GameRuntimeData runtime)
        {
            if (!runtime.IsRunning)
                return;

            _bullets.Clear();
            _enemies.Clear();
            _barrels.Clear();
            _bullets.AddRange(world.Bullets.Keys);
            _enemies.AddRange(world.Enemies.Keys);
            _barrels.AddRange(world.Barrels.Keys);

            foreach (var bulletEntity in _bullets)
            {
                if (!world.Bullets.TryGetValue(bulletEntity, out var bullet))
                    continue;

                var bulletTransform = world.Transforms[bulletEntity];
                bullet.PreviousPosition = bulletTransform.Position;
                bulletTransform.Position += bullet.Direction * bullet.Speed * runtime.DeltaTime;
                bullet.Life -= runtime.DeltaTime;

                var consumed = bullet.Life <= 0f;
                foreach (var enemyEntity in _enemies)
                {
                    if (consumed)
                        break;

                    var enemy = world.Enemies[enemyEntity];
                    if (enemy.State == EnemyEcsState.Dead)
                        continue;

                    var enemyTransform = world.Transforms[enemyEntity];
                    var hitRadius = math.max(0.1f, runtime.EnemyHitCapsuleRadius + runtime.BulletHitRadius);
                    if (DistanceSqBulletSegmentToEnemyCapsule(bullet.PreviousPosition, bulletTransform.Position, enemyTransform.Position, runtime.EnemyHitCapsuleHeight) > hitRadius * hitRadius)
                        continue;

                    enemy.Hp -= bullet.Damage;
                    enemy.HitTimer = 0.12f;
                    enemy.HitEvent++;

                    if (enemy.Hp <= 0f)
                    {
                        enemy.State = EnemyEcsState.Dead;
                        enemy.DeathEvent++;
                        var enemyType = world.EnemyTypes[enemyEntity];
                        world.RewardEvents.Add(EnemyRewardUtility.CreateDrop(enemyType, enemyTransform.Position));
                    }

                    world.Enemies[enemyEntity] = enemy;
                    consumed = true;
                }

                foreach (var barrelEntity in _barrels)
                {
                    if (consumed)
                        break;

                    if (!world.Barrels.TryGetValue(barrelEntity, out var barrel) || barrel.State == BarrelEcsState.Dead)
                        continue;

                    var barrelTransform = world.Transforms[barrelEntity];
                    var hitRadius = math.max(0.1f, barrel.Radius + runtime.BulletHitRadius);
                    if (DistanceSqBulletSegmentToEnemyCapsule(bullet.PreviousPosition, bulletTransform.Position, barrelTransform.Position, barrel.Height) > hitRadius * hitRadius)
                        continue;

                    barrel.Hp -= bullet.Damage;
                    barrel.HitEvent++;
                    if (barrel.Hp <= 0f)
                    {
                        barrel.State = BarrelEcsState.Dead;
                        barrel.DeathEvent++;
                    }

                    world.Barrels[barrelEntity] = barrel;
                    consumed = true;
                }

                if (consumed)
                    world.DestroyBullet(bulletEntity);
                else
                {
                    world.Bullets[bulletEntity] = bullet;
                    world.Transforms[bulletEntity] = bulletTransform;
                }
            }
        }

        private static float DistanceSqBulletSegmentToEnemyCapsule(float3 bulletStart, float3 bulletEnd, float3 enemyPosition, float height)
        {
            var capsuleHeight = math.max(0.1f, height);
            var bottom = enemyPosition + new float3(0f, 0.15f, 0f);
            var top = enemyPosition + new float3(0f, capsuleHeight, 0f);
            return SegmentSegmentDistanceSq(bulletStart, bulletEnd, bottom, top);
        }

        private static float SegmentSegmentDistanceSq(float3 p1, float3 q1, float3 p2, float3 q2)
        {
            var d1 = q1 - p1;
            var d2 = q2 - p2;
            var r = p1 - p2;
            var a = math.dot(d1, d1);
            var e = math.dot(d2, d2);
            var f = math.dot(d2, r);
            float s;
            float t;

            if (a <= 0.0001f && e <= 0.0001f)
                return math.distancesq(p1, p2);

            if (a <= 0.0001f)
            {
                s = 0f;
                t = math.saturate(f / e);
            }
            else
            {
                var c = math.dot(d1, r);
                if (e <= 0.0001f)
                {
                    t = 0f;
                    s = math.saturate(-c / a);
                }
                else
                {
                    var b = math.dot(d1, d2);
                    var denom = a * e - b * b;
                    s = denom != 0f ? math.saturate((b * f - c * e) / denom) : 0f;
                    t = (b * s + f) / e;

                    if (t < 0f)
                    {
                        t = 0f;
                        s = math.saturate(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = math.saturate((b - c) / a);
                    }
                }
            }

            var closest1 = p1 + d1 * s;
            var closest2 = p2 + d2 * t;
            return math.distancesq(closest1, closest2);
        }
    }
}
