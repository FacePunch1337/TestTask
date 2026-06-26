using System.Collections.Generic;
using TestTask.Config;
using TestTask.Core;
using TestTask.ECS.Components;
using TestTask.ECS.Systems;
using TestTask.Gameplay.Presentation;
using TestTask.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace TestTask.ECS.Bridge
{
    public sealed class CombatEcsBridge
    {
        private readonly GameConfigAsset _config;
        private readonly RunStats _runStats;
        private readonly GameSession _session;
        private readonly CombatEcsWorld _world = new();
        private readonly EnemyMovementSystem _enemyMovement = new();
        private readonly BulletCollisionSystem _bulletCollision = new();
        private readonly BarrelCollisionSystem _barrelCollision = new();
        private readonly PickupMagnetSystem _pickupMagnet = new();
        private readonly Dictionary<int, EnemyPresenter> _enemyPresenters = new();
        private readonly Dictionary<int, GameObject> _enemyPresenterOverrides = new();
        private readonly Dictionary<int, BarrelPresenter> _barrelPresenters = new();
        private readonly Dictionary<int, BulletPresenter> _bulletPresenters = new();
        private readonly Dictionary<int, PickupPresenter> _pickupPresenters = new();
        private readonly List<int> _enemySyncBuffer = new();
        private readonly List<int> _deadEnemyBuffer = new();
        private readonly List<int> _barrelSyncBuffer = new();
        private readonly List<int> _deadBarrelBuffer = new();
        private GameRuntimeData _runtime;

        public CombatEcsBridge(GameConfigAsset config, RunStats runStats, GameSession session)
        {
            _config = config;
            _runStats = runStats;
            _session = session;
        }

        public void ResetWorld()
        {
            _world.Clear();
            DestroyAll(_enemyPresenters.Values);
            DestroyAll(_barrelPresenters.Values);
            DestroyAll(_bulletPresenters.Values);
            DestroyAll(_pickupPresenters.Values);
            _enemyPresenters.Clear();
            _barrelPresenters.Clear();
            _bulletPresenters.Clear();
            _pickupPresenters.Clear();
            _enemyPresenterOverrides.Clear();
        }

        public void UpdateRuntime(Vector3 carPosition, bool running, bool allowPassiveEnemyMovement, float deltaTime)
        {
            _runtime = new GameRuntimeData
            {
                CarPosition = new float3(carPosition.x, carPosition.y, carPosition.z),
                CarBodyRadius = _config.CarBodyRadius,
                RoadWidth = _config.RoadWidth,
                EnemyWakeDistance = _config.EnemyWakeDistance,
                EnemyAttackRange = _config.EnemyAttackRange,
                EnemyAttackInterval = _config.EnemyAttackInterval,
                EnemyHitCapsuleRadius = _config.EnemyHitCapsuleRadius,
                EnemyHitCapsuleHeight = _config.EnemyHitCapsuleHeight,
                EnemyAimHeight = _config.EnemyAimHeight,
                EnemyDespawnBehindDistance = _config.EnemyDespawnBehindDistance,
                EnemyWanderChancePerSecond = _config.EnemyWanderChancePerSecond,
                EnemyWanderMinDistance = _config.EnemyWanderMinDistance,
                EnemyWanderMaxDistance = _config.EnemyWanderMaxDistance,
                EnemyWanderSpeedMultiplier = _config.EnemyWanderSpeedMultiplier,
                PickupMagnetRadius = _runStats.EffectivePickupMagnetRadius,
                PickupSpeed = _config.PickupSpeed,
                BulletDamage = _runStats.BulletDamage,
                BulletSpeed = _runStats.BulletSpeed,
                BulletLifetime = _config.BulletLifetime,
                BulletHitRadius = _config.BulletHitRadius,
                BarrelCollisionDamage = _config.BarrelCollisionDamage,
                DeltaTime = deltaTime,
                IsRunning = running,
                AllowPassiveEnemyMovement = allowPassiveEnemyMovement
            };

            _enemyMovement.Tick(_world, _runtime);
            _barrelCollision.Tick(_world, _runtime);
            _bulletCollision.Tick(_world, _runtime);
            _pickupMagnet.Tick(_world, _runtime);
        }

        public int SpawnEnemy(float3 position, int typeIndex, float difficultyMultiplier, GameObject presenterPrefabOverride = null)
        {
            typeIndex = math.clamp(typeIndex, 0, _config.EnemyTypes.Length - 1);
            var type = _config.EnemyTypes[typeIndex];
            var entity = _world.CreateEntity();
            var randomYaw = UnityEngine.Random.Range(0f, 360f);
            _world.Transforms[entity] = new EcsTransform { Position = position, Rotation = quaternion.RotateY(math.radians(randomYaw)) };
            _world.Enemies[entity] = new EnemyData { Hp = type.Hp * difficultyMultiplier, State = EnemyEcsState.Idle };
            _world.EnemyTypes[entity] = new EnemyTypeData
            {
                TypeIndex = typeIndex,
                MaxHp = type.Hp * difficultyMultiplier,
                Speed = type.Speed,
                DamagePerSecond = type.DamagePerSecond,
                XpReward = type.XpReward,
                XpDropChance = type.XpDropChance,
                CoinReward = type.CoinReward,
                CoinDropChance = type.CoinDropChance,
                GearDropChance = type.GearDropChance,
                GearHealAmount = _config.GearHealAmount
            };

            if (presenterPrefabOverride != null)
                _enemyPresenterOverrides[entity] = presenterPrefabOverride;
            EnsureEnemyPresenter(entity, type);
            return entity;
        }

        public void SpawnBullet(float3 position, float3 direction)
        {
            var entity = _world.CreateEntity();
            _world.Transforms[entity] = new EcsTransform { Position = position, Rotation = quaternion.identity };
            _world.Bullets[entity] = new BulletData
            {
                Direction = math.normalizesafe(direction, new float3(0f, 0f, 1f)),
                PreviousPosition = position,
                Damage = _runStats.BulletDamage,
                Speed = _runStats.BulletSpeed,
                Life = _config.BulletLifetime
            };
        }

        public void KillEnemiesInRadius(Vector3 center, float radius)
        {
            if (radius <= 0f)
                return;

            var center3 = new float3(center.x, center.y, center.z);
            var radiusSq = radius * radius;
            _enemySyncBuffer.Clear();
            _enemySyncBuffer.AddRange(_world.Enemies.Keys);
            foreach (var entity in _enemySyncBuffer)
            {
                if (!_world.Enemies.TryGetValue(entity, out var enemy) || enemy.State == EnemyEcsState.Dead)
                    continue;

                var transform = _world.Transforms[entity];
                var delta = transform.Position - center3;
                delta.y = 0f;
                if (math.lengthsq(delta) > radiusSq)
                    continue;

                enemy.Hp = 0f;
                enemy.State = EnemyEcsState.Dead;
                enemy.DeathEvent++;
                _world.Enemies[entity] = enemy;

                if (_world.EnemyTypes.TryGetValue(entity, out var enemyType))
                    _world.RewardEvents.Add(EnemyRewardUtility.CreateDrop(enemyType, transform.Position));
            }
        }

        public int SpawnBarrel(float3 position)
        {
            var entity = _world.CreateEntity();
            var yaw = UnityEngine.Random.Range(0f, 360f);
            _world.Transforms[entity] = new EcsTransform { Position = position, Rotation = quaternion.RotateY(math.radians(yaw)) };
            _world.Barrels[entity] = new BarrelData
            {
                Hp = _config.BarrelHp,
                MaxHp = _config.BarrelHp,
                Radius = _config.BarrelHitRadius,
                Height = _config.BarrelHitHeight,
                State = BarrelEcsState.Alive
            };
            EnsureBarrelPresenter(entity);
            return entity;
        }

        public bool TryFindTargetInCone(Vector3 carPosition, float inputYaw, float coneDegrees, out float3 target)
        {
            target = default;
            var bestScore = float.MaxValue;
            var found = false;

            foreach (var pair in _world.Enemies)
            {
                var enemy = pair.Value;
                if (enemy.State == EnemyEcsState.Dead)
                    continue;

                var position = _world.Transforms[pair.Key].Position;
                if (position.z < carPosition.z - 1f)
                    continue;

                var offset = ToVector3(position) - carPosition;
                var yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                var yawDelta = Mathf.Abs(Mathf.DeltaAngle(inputYaw, yaw));
                if (yawDelta > coneDegrees)
                    continue;

                var score = yawDelta * 2f + offset.sqrMagnitude * 0.012f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                target = position + new float3(0f, _config.EnemyAimHeight, 0f);
                found = true;
            }

            foreach (var pair in _world.Barrels)
            {
                var barrel = pair.Value;
                if (barrel.State == BarrelEcsState.Dead)
                    continue;

                var position = _world.Transforms[pair.Key].Position;
                if (position.z < carPosition.z - 1f)
                    continue;

                var offset = ToVector3(position) - carPosition;
                var yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                var yawDelta = Mathf.Abs(Mathf.DeltaAngle(inputYaw, yaw));
                if (yawDelta > coneDegrees)
                    continue;

                var score = yawDelta * 2f + offset.sqrMagnitude * 0.012f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                target = position + new float3(0f, barrel.Height * 0.5f, 0f);
                found = true;
            }

            return found;
        }

        public void ConsumeEvents()
        {
            for (var i = 0; i < _world.CarDamageEvents.Count; i++)
                _session.ApplyCarDamage(_world.CarDamageEvents[i].Amount);
            _world.CarDamageEvents.Clear();

            for (var i = 0; i < _world.RewardEvents.Count; i++)
            {
                var reward = _world.RewardEvents[i];
                if (reward.SpawnPickups)
                {
                    SpawnPickup(reward.Position, PickupEcsType.Coin, reward.Coins, new float3(-0.28f, 0.45f, 0f));
                    SpawnPickup(reward.Position, PickupEcsType.Experience, reward.Xp, new float3(0.28f, 0.55f, 0f));
                    SpawnPickup(reward.Position, PickupEcsType.Gear, Mathf.RoundToInt(reward.Repair), new float3(0f, 0.65f, -0.2f));
                    SpawnPickup(reward.Position, PickupEcsType.Magnet, reward.Magnet, new float3(-0.18f, 0.75f, -0.24f));
                    SpawnPickup(reward.Position, PickupEcsType.Bonus, reward.Bonus, new float3(0.18f, 0.8f, -0.24f));
                }
                else
                {
                    _session.AddReward(reward.Coins, reward.Xp);
                    if (reward.Repair > 0f)
                        _session.RepairCar(reward.Repair);
                    if (reward.Magnet > 0)
                        _runStats.ActivateMagnet(_config.MagnetPickupDuration, _config.MagnetPickupRadiusMultiplier);
                    if (reward.Bonus > 0)
                        _runStats.ActivateBonus(_config.BonusPickupDuration, _config.BonusPickupSpeedMultiplier);
                }
            }
            _world.RewardEvents.Clear();
        }

        public void SyncPresentation(float deltaTime)
        {
            SyncEnemies(deltaTime);
            SyncBarrels(deltaTime);
            SyncBullets();
            SyncPickups();
        }

        private void SpawnPickup(float3 position, PickupEcsType type, int amount, float3 offset)
        {
            if (amount <= 0)
                return;

            var entity = _world.CreateEntity();
            _world.Transforms[entity] = new EcsTransform { Position = position + offset, Rotation = quaternion.identity };
            _world.Pickups[entity] = new PickupData { Type = type, Amount = amount };
        }

        private void SyncEnemies(float deltaTime)
        {
            for (var i = 0; i < _world.RemovedEnemies.Count; i++)
            {
                var entity = _world.RemovedEnemies[i];
                if (_enemyPresenters.TryGetValue(entity, out var removedPresenter))
                    Object.Destroy(removedPresenter.gameObject);
                _enemyPresenters.Remove(entity);
                _enemyPresenterOverrides.Remove(entity);
            }
            _world.RemovedEnemies.Clear();

            _enemySyncBuffer.Clear();
            _enemySyncBuffer.AddRange(_world.Enemies.Keys);
            _deadEnemyBuffer.Clear();

            foreach (var entity in _enemySyncBuffer)
            {
                if (!_world.Enemies.TryGetValue(entity, out var enemy))
                    continue;

                var typeData = _world.EnemyTypes[entity];
                var type = _config.EnemyTypes[math.clamp(typeData.TypeIndex, 0, _config.EnemyTypes.Length - 1)];
                var transform = _world.Transforms[entity];
                var presenter = EnsureEnemyPresenter(entity, type);

                if (enemy.State == EnemyEcsState.Dead)
                {
                    presenter.BeginDeath(ToVector3(transform.Position));
                    _deadEnemyBuffer.Add(entity);
                    continue;
                }

                presenter.Show();
                var hp01 = typeData.MaxHp > 0f ? Mathf.Clamp01(enemy.Hp / typeData.MaxHp) : 0f;
                presenter.ApplyState(ToVector3(transform.Position), ToQuaternion(transform.Rotation), enemy.State, enemy.HitEvent, enemy.DeathEvent, hp01, deltaTime);
            }

            for (var i = 0; i < _deadEnemyBuffer.Count; i++)
            {
                var entity = _deadEnemyBuffer[i];
                _world.DestroyEnemy(entity);
                _enemyPresenters.Remove(entity);
                _enemyPresenterOverrides.Remove(entity);
            }
        }

        private void SyncBullets()
        {
            foreach (var entity in _world.DestroyedBullets)
            {
                if (_bulletPresenters.TryGetValue(entity, out var presenter))
                    presenter.gameObject.SetActive(false);
            }
            _world.DestroyedBullets.Clear();

            foreach (var pair in _world.Bullets)
            {
                if (!_bulletPresenters.TryGetValue(pair.Key, out var presenter))
                {
                    presenter = BulletPresenter.Create(
                        _config.Prefabs.BulletPresenterPrefab,
                        _config.Prefabs.BulletMaterial,
                        _config.Prefabs.BulletTrailMaterial,
                        _config.Prefabs.BulletTrailLength,
                        _config.Prefabs.BulletTrailStartWidth,
                        _config.Prefabs.BulletTrailEndWidth,
                        _config.Prefabs.BulletTrailTailColor,
                        _config.Prefabs.BulletTrailHeadColor);
                    _bulletPresenters.Add(pair.Key, presenter);
                }

                presenter.Apply(ToVector3(_world.Transforms[pair.Key].Position));
            }
        }

        private void SyncBarrels(float deltaTime)
        {
            _barrelSyncBuffer.Clear();
            _barrelSyncBuffer.AddRange(_world.Barrels.Keys);
            _deadBarrelBuffer.Clear();

            foreach (var entity in _barrelSyncBuffer)
            {
                if (!_world.Barrels.TryGetValue(entity, out var barrel))
                    continue;

                var transform = _world.Transforms[entity];
                var presenter = EnsureBarrelPresenter(entity);
                if (transform.Position.z < _runtime.CarPosition.z - _runtime.EnemyDespawnBehindDistance)
                {
                    Object.Destroy(presenter.gameObject);
                    _deadBarrelBuffer.Add(entity);
                    continue;
                }

                if (barrel.State == BarrelEcsState.Dead)
                {
                    presenter.BeginDeath(ToVector3(transform.Position));
                    SpawnBarrelDrop(transform.Position);
                    _deadBarrelBuffer.Add(entity);
                    continue;
                }

                var hp01 = barrel.MaxHp > 0f ? Mathf.Clamp01(barrel.Hp / barrel.MaxHp) : 0f;
                presenter.Apply(ToVector3(transform.Position), ToQuaternion(transform.Rotation), hp01, barrel.HitEvent, deltaTime);
            }

            for (var i = 0; i < _deadBarrelBuffer.Count; i++)
            {
                var entity = _deadBarrelBuffer[i];
                _world.DestroyBarrel(entity);
                _barrelPresenters.Remove(entity);
            }
        }

        private void SpawnBarrelDrop(float3 position)
        {
            var drops = _config.BarrelDrops;
            if (drops == null || drops.Length == 0)
                return;

            var totalWeight = 0f;
            for (var i = 0; i < drops.Length; i++)
                totalWeight += Mathf.Max(0f, drops[i].Weight);

            if (totalWeight <= 0f)
                return;

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            for (var i = 0; i < drops.Length; i++)
            {
                var drop = drops[i];
                roll -= Mathf.Max(0f, drop.Weight);
                if (roll > 0f)
                    continue;

                var reward = new RewardEvent { Position = position, SpawnPickups = true };
                switch (drop.Type)
                {
                    case PickupDropType.Coin:
                        reward.Coins = drop.Amount;
                        break;
                    case PickupDropType.Experience:
                        reward.Xp = drop.Amount;
                        break;
                    case PickupDropType.Gear:
                        reward.Repair = drop.Amount;
                        break;
                    case PickupDropType.Magnet:
                        reward.Magnet = drop.Amount;
                        break;
                    case PickupDropType.Bonus:
                        reward.Bonus = drop.Amount;
                        break;
                }

                _world.RewardEvents.Add(reward);
                return;
            }
        }

        private void SyncPickups()
        {
            foreach (var entity in _world.DestroyedPickups)
            {
                if (_pickupPresenters.TryGetValue(entity, out var presenter))
                    presenter.gameObject.SetActive(false);
            }
            _world.DestroyedPickups.Clear();

            foreach (var pair in _world.Pickups)
            {
                if (!_pickupPresenters.TryGetValue(pair.Key, out var presenter))
                {
                    var prefab = pair.Value.Type switch
                    {
                        PickupEcsType.Coin => _config.Prefabs.CoinPickupPrefab,
                        PickupEcsType.Experience => _config.Prefabs.ExperiencePickupPrefab,
                        PickupEcsType.Gear => _config.Prefabs.GearPickupPrefab,
                        PickupEcsType.Magnet => _config.Prefabs.MagnetPickupPrefab,
                        PickupEcsType.Bonus => _config.Prefabs.BonusPickupPrefab,
                        _ => null
                    };
                    var material = pair.Value.Type switch
                    {
                        PickupEcsType.Coin => _config.Prefabs.CoinMaterial,
                        PickupEcsType.Experience => _config.Prefabs.ExperienceMaterial,
                        PickupEcsType.Gear => _config.Prefabs.GearMaterial,
                        PickupEcsType.Magnet => _config.Prefabs.MagnetMaterial,
                        PickupEcsType.Bonus => _config.Prefabs.BonusMaterial,
                        _ => null
                    };
                    presenter = PickupPresenter.Create(prefab, material);
                    _pickupPresenters.Add(pair.Key, presenter);
                }

                presenter.Apply(ToVector3(_world.Transforms[pair.Key].Position), pair.Value.Type);
            }
        }

        private BarrelPresenter EnsureBarrelPresenter(int entity)
        {
            if (_barrelPresenters.TryGetValue(entity, out var presenter))
                return presenter;

            presenter = BarrelPresenter.Create(
                _config.Prefabs.BarrelPresenterPrefab,
                _config.Prefabs.BarrelMaterial,
                _config.Prefabs.BarrelDamageFlashMaterial != null ? _config.Prefabs.BarrelDamageFlashMaterial : _config.Prefabs.EnemyDamageFlashMaterial,
                _config.BarrelDamageFlashDuration);
            presenter.gameObject.SetActive(true);
            _barrelPresenters.Add(entity, presenter);
            return presenter;
        }

        private EnemyPresenter EnsureEnemyPresenter(int entity, EnemyTypeConfig type)
        {
            if (_enemyPresenters.TryGetValue(entity, out var presenter))
                return presenter;

            var prefab = _enemyPresenterOverrides.TryGetValue(entity, out var presenterOverride) && presenterOverride != null
                ? presenterOverride
                : type.PresenterPrefabOverride != null
                ? type.PresenterPrefabOverride
                : _config.Prefabs.EnemyPresenterPrefab;

            var root = prefab != null
                ? Object.Instantiate(prefab)
                : new GameObject($"EnemyPresenter_{type.Id}");

            root.name = $"EnemyPresenter_{type.Id}";
            presenter = root.GetComponent<EnemyPresenter>();
            if (presenter == null)
                presenter = root.AddComponent<EnemyPresenter>();

            presenter.Construct(
                type,
                _config.Prefabs.EnemyMaterial,
                _config.Prefabs.EnemyDamageFlashMaterial,
                _config.Prefabs.EnemyDamageFlashDuration,
                _config.Prefabs.EnemyDeathParticleColor);
            presenter.Hide();
            _enemyPresenters.Add(entity, presenter);
            return presenter;
        }

        private static void DestroyAll<T>(IEnumerable<T> presenters) where T : Component
        {
            foreach (var presenter in presenters)
                Object.Destroy(presenter.gameObject);
        }

        private static Vector3 ToVector3(float3 value) => new(value.x, value.y, value.z);

        private static Quaternion ToQuaternion(quaternion value) => new(value.value.x, value.value.y, value.value.z, value.value.w);
    }
}
