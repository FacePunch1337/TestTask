using Unity.Mathematics;

namespace TestTask.ECS.Components
{
    public enum EnemyEcsState : byte
    {
        Idle,
        Walking,
        Running,
        Attacking,
        Hit,
        Dead
    }

    public enum PickupEcsType : byte
    {
        Coin,
        Experience,
        Gear,
        Magnet,
        Bonus
    }

    public enum BarrelEcsState : byte
    {
        Alive,
        Dead
    }

    public struct EcsTransform
    {
        public float3 Position;
        public quaternion Rotation;
    }

    public struct GameRuntimeData
    {
        public float3 CarPosition;
        public float CarBodyRadius;
        public float RoadWidth;
        public float EnemyWakeDistance;
        public float EnemyAttackRange;
        public float EnemyAttackInterval;
        public float EnemyHitCapsuleRadius;
        public float EnemyHitCapsuleHeight;
        public float EnemyAimHeight;
        public float EnemyDespawnBehindDistance;
        public float EnemyWanderChancePerSecond;
        public float EnemyWanderMinDistance;
        public float EnemyWanderMaxDistance;
        public float EnemyWanderSpeedMultiplier;
        public float PickupMagnetRadius;
        public float PickupSpeed;
        public float BulletDamage;
        public float BulletSpeed;
        public float BulletLifetime;
        public float BulletHitRadius;
        public float BarrelCollisionDamage;
        public float DeltaTime;
        public bool IsRunning;
        public bool AllowPassiveEnemyMovement;
    }

    public struct BarrelData
    {
        public float Hp;
        public float MaxHp;
        public float Radius;
        public float Height;
        public int HitEvent;
        public int DeathEvent;
        public BarrelEcsState State;
    }

    public struct EnemyTypeData
    {
        public int TypeIndex;
        public float MaxHp;
        public float Speed;
        public float DamagePerSecond;
        public int XpReward;
        public float XpDropChance;
        public int CoinReward;
        public float CoinDropChance;
        public float GearDropChance;
        public float GearHealAmount;
    }

    public struct EnemyData
    {
        public float Hp;
        public EnemyEcsState State;
        public float HitTimer;
        public float AttackCooldown;
        public float WanderCooldown;
        public float3 WanderTarget;
        public bool HasWanderTarget;
        public int DeathEvent;
        public int HitEvent;
    }

    public struct BulletData
    {
        public float3 Direction;
        public float3 PreviousPosition;
        public float Damage;
        public float Speed;
        public float Life;
    }

    public struct PickupData
    {
        public PickupEcsType Type;
        public int Amount;
    }

    public struct CarDamageEvent
    {
        public float Amount;
    }

    public struct RewardEvent
    {
        public int Coins;
        public int Xp;
        public float Repair;
        public int Magnet;
        public int Bonus;
        public float3 Position;
        public bool SpawnPickups;
    }
}
