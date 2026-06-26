using System.Collections.Generic;
using TestTask.ECS.Bridge;
using TestTask.ECS.Components;
using Unity.Mathematics;

namespace TestTask.ECS.Systems
{
    public sealed class PickupMagnetSystem
    {
        private readonly List<int> _pickups = new();

        public void Tick(CombatEcsWorld world, GameRuntimeData runtime)
        {
            if (!runtime.IsRunning)
                return;

            _pickups.Clear();
            _pickups.AddRange(world.Pickups.Keys);
            var target = runtime.CarPosition + new float3(0f, 1f, 0f);

            foreach (var entity in _pickups)
            {
                if (!world.Pickups.TryGetValue(entity, out var pickup))
                    continue;

                var transform = world.Transforms[entity];
                var distance = math.distance(transform.Position, target);
                if (distance <= runtime.PickupMagnetRadius && distance > 0.001f)
                {
                    var direction = math.normalize(target - transform.Position);
                    transform.Position += direction * runtime.PickupSpeed * runtime.DeltaTime;
                }

                if (distance <= 0.85f)
                {
                    world.RewardEvents.Add(new RewardEvent
                    {
                        Coins = pickup.Type == PickupEcsType.Coin ? pickup.Amount : 0,
                        Xp = pickup.Type == PickupEcsType.Experience ? pickup.Amount : 0,
                        Repair = pickup.Type == PickupEcsType.Gear ? pickup.Amount : 0f,
                        Magnet = pickup.Type == PickupEcsType.Magnet ? pickup.Amount : 0,
                        Bonus = pickup.Type == PickupEcsType.Bonus ? pickup.Amount : 0,
                        Position = transform.Position,
                        SpawnPickups = false
                    });
                    world.DestroyPickup(entity);
                }
                else
                {
                    world.Transforms[entity] = transform;
                }
            }
        }
    }
}
