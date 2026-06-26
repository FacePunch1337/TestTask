using System.Collections.Generic;
using TestTask.ECS.Components;

namespace TestTask.ECS.Bridge
{
    public sealed class CombatEcsWorld
    {
        private int _nextEntityId = 1;

        public readonly Dictionary<int, EcsTransform> Transforms = new();
        public readonly Dictionary<int, EnemyData> Enemies = new();
        public readonly Dictionary<int, BarrelData> Barrels = new();
        public readonly Dictionary<int, EnemyTypeData> EnemyTypes = new();
        public readonly Dictionary<int, BulletData> Bullets = new();
        public readonly Dictionary<int, PickupData> Pickups = new();
        public readonly List<CarDamageEvent> CarDamageEvents = new();
        public readonly List<RewardEvent> RewardEvents = new();
        public readonly List<int> DestroyedBullets = new();
        public readonly List<int> DestroyedPickups = new();
        public readonly List<int> RemovedEnemies = new();

        public int CreateEntity()
        {
            var entity = _nextEntityId;
            _nextEntityId++;
            return entity;
        }

        public void Clear()
        {
            _nextEntityId = 1;
            Transforms.Clear();
            Enemies.Clear();
            Barrels.Clear();
            EnemyTypes.Clear();
            Bullets.Clear();
            Pickups.Clear();
            CarDamageEvents.Clear();
            RewardEvents.Clear();
            DestroyedBullets.Clear();
            DestroyedPickups.Clear();
            RemovedEnemies.Clear();
        }

        public void DestroyBullet(int entity)
        {
            Bullets.Remove(entity);
            Transforms.Remove(entity);
            DestroyedBullets.Add(entity);
        }

        public void DestroyPickup(int entity)
        {
            Pickups.Remove(entity);
            Transforms.Remove(entity);
            DestroyedPickups.Add(entity);
        }

        public void DestroyEnemy(int entity)
        {
            Enemies.Remove(entity);
            EnemyTypes.Remove(entity);
            Transforms.Remove(entity);
        }

        public void DestroyBarrel(int entity)
        {
            Barrels.Remove(entity);
            Transforms.Remove(entity);
        }

        public void RemoveEnemySilently(int entity)
        {
            DestroyEnemy(entity);
            RemovedEnemies.Add(entity);
        }
    }
}
