using System.Collections.Generic;
using TestTask.ECS.Bridge;
using TestTask.ECS.Components;
using Unity.Mathematics;

namespace TestTask.ECS.Systems
{
    public sealed class BarrelCollisionSystem
    {
        private readonly List<int> _entities = new();

        public void Tick(CombatEcsWorld world, GameRuntimeData runtime)
        {
            if (!runtime.IsRunning)
                return;

            _entities.Clear();
            _entities.AddRange(world.Barrels.Keys);

            foreach (var entity in _entities)
            {
                if (!world.Barrels.TryGetValue(entity, out var barrel) || barrel.State == BarrelEcsState.Dead)
                    continue;

                var transform = world.Transforms[entity];
                var delta = transform.Position - runtime.CarPosition;
                delta.y = 0f;

                var contactRadius = runtime.CarBodyRadius + barrel.Radius;
                if (math.lengthsq(delta) > contactRadius * contactRadius)
                    continue;

                barrel.Hp = 0f;
                barrel.State = BarrelEcsState.Dead;
                barrel.HitEvent++;
                barrel.DeathEvent++;
                world.Barrels[entity] = barrel;

                if (runtime.BarrelCollisionDamage > 0f)
                    world.CarDamageEvents.Add(new CarDamageEvent { Amount = runtime.BarrelCollisionDamage });
            }
        }
    }
}
