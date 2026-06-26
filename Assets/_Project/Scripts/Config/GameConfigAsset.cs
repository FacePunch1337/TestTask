using System;
using UnityEngine;

namespace TestTask.Config
{
    [CreateAssetMenu(menuName = "TestTask/Game Config", fileName = "GameConfigAsset")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [Header("Level")]
        [Min(40f)] public float LevelLength = 260f;
        [Min(2f)] public float RoadWidth = 7.2f;
        [Min(1f)] public float RoadSegmentLength = 32f;
        [Min(3)] public int RoadSegmentCount = 12;
        [Min(1f)] public float CarSpeed = 11f;
        [Min(1f)] public float CarMaxHp = 140f;
        [Min(0f)] public float CarBodyRadius = 2.1f;
        [Min(1)] public int BaseEnemyCount = 58;

        [Header("Enemy Spawning")]
        [Min(1f)] public float EnemySpawnSpacing = 4.5f;
        [Min(0f)] public float EnemySpawnSpacingRandomness = 1.25f;
        [Min(5f)] public float EnemySpawnAheadDistance = 70f;
        [Min(0f)] public float EnemyFirstSpawnDistance = 24f;

        [Header("Enemy Behaviour")]
        [Min(1f)] public float EnemyWakeDistance = 34f;
        [Min(0.25f)] public float EnemyAttackRange = 2.05f;
        [Min(0.05f)] public float EnemyAttackInterval = 0.65f;
        [Min(0.1f)] public float EnemyHitCapsuleRadius = 0.75f;
        [Min(0.1f)] public float EnemyHitCapsuleHeight = 2.2f;
        [Min(0f)] public float EnemyAimHeight = 1.15f;
        [Min(0f)] public float EnemyDespawnBehindDistance = 12f;
        [Range(0f, 1f)] public float EnemyWanderChancePerSecond = 0.18f;
        [Min(0f)] public float EnemyWanderMinDistance = 1.5f;
        [Min(0f)] public float EnemyWanderMaxDistance = 5f;
        [Min(0.01f)] public float EnemyWanderSpeedMultiplier = 0.45f;
        public EnemyWaveConfig[] EnemyWaves =
        {
            new()
        };

        [Header("Turret")]
        [Min(0.1f)] public float FireRate = 8f;
        [Min(0.1f)] public float BulletDamage = 12f;
        [Min(1f)] public float BulletSpeed = 42f;
        [Min(0.1f)] public float BulletLifetime = 1.55f;
        [Min(0f)] public float BulletHitRadius = 0.45f;
        [Range(5f, 90f)] public float TurretYawLimit = 58f;
        [Range(5f, 60f)] public float TurretTargetCone = 24f;
        [Range(0f, 1f)] public float TurretAimAssistStrength = 0.35f;
        [Min(0f)] public float TurretAimAssistMaxCorrection = 22f;
        [Min(10f)] public float TurretDragSensitivity = 160f;
        [Min(1f)] public float TurretRotationSharpness = 28f;
        [Min(1f)] public float TurretAimLineLength = 34f;
        [Min(0.005f)] public float TurretAimLineWidth = 0.035f;
        public Color TurretAimLineStartColor = new(1f, 0.92f, 0.12f, 0.78f);
        public Color TurretAimLineEndColor = new(1f, 0.18f, 0.04f, 0.18f);

        [Header("Vehicle Path")]
        [Min(0.1f)] public float CarPathFrequency = 1.1f;
        [Min(0f)] public float CarPathPrimaryAmplitude = 1.65f;
        [Min(0f)] public float CarPathSecondaryAmplitude = 0.65f;
        [Min(0f)] public float CarSteerYawMultiplier = 1f;
        [Min(1f)] public float CarSteerSharpness = 14f;

        [Header("Pickups")]
        [Min(0.5f)] public float PickupMagnetRadius = 7f;
        [Min(1f)] public float PickupSpeed = 16f;
        [Min(1)] public int XpToLevel = 60;
        [Min(1f)] public float XpRequirementMultiplier = 1.25f;
        [Min(0f)] public float GearHealAmount = 25f;
        [Min(0.1f)] public float MagnetPickupDuration = 5f;
        [Min(1f)] public float MagnetPickupRadiusMultiplier = 2.5f;
        [Min(0.1f)] public float BonusPickupDuration = 4f;
        [Min(1f)] public float BonusPickupSpeedMultiplier = 1.35f;
        [Min(0f)] public float BonusEndKillRadius = 5f;

        [Header("Barrels")]
        [Range(0f, 1f)] public float BarrelSpawnChance = 0.35f;
        [Min(1f)] public float BarrelSpawnSpacing = 12f;
        [Min(0f)] public float BarrelSpawnSpacingRandomness = 3f;
        [Min(0f)] public float BarrelFirstSpawnDistance = 18f;
        [Min(1f)] public float BarrelSpawnAheadDistance = 70f;
        [Min(1f)] public float BarrelHp = 35f;
        [Min(0f)] public float BarrelCollisionDamage = 15f;
        [Min(0.1f)] public float BarrelHitRadius = 0.7f;
        [Min(0.1f)] public float BarrelHitHeight = 1.25f;
        [Min(0.01f)] public float BarrelDamageFlashDuration = 0.09f;
        public BarrelDropConfig[] BarrelDrops =
        {
            new()
        };

        [Header("Upgrades")]
        public UpgradeStatConfig FireRateUpgrade = new("FireRate", 10f, 1f);
        public UpgradeStatConfig DamageUpgrade = new("Damage", 10f, 1f);
        public UpgradeStatConfig PickUpRadiusUpgrade = new("PickUpRadius", 10f, 1f);

        [Header("Camera")]
        public Vector3 CameraOffset = new(0f, 7.4f, -7.2f);
        public Vector3 CameraEulerAngles = new(58f, 0f, 0f);
        [Min(0.1f)] public float CameraFollowSharpness = 9f;

        [Header("Prefabs")]
        public PrefabReferences Prefabs = new();

        [Header("Enemy Types")]
        public EnemyTypeConfig[] EnemyTypes =
        {
            new("Runner", 22f, 5.2f, 10f, 9, 2, new Color(1f, 0.08f, 0.04f), 9f, 12f, 10f),
            new("Bruiser", 48f, 3.2f, 19f, 15, 5, new Color(0.55f, 0.08f, 0.95f), 6.5f, 8f, 18f),
            new("Stinger", 16f, 6.7f, 7f, 7, 1, new Color(1f, 0.48f, 0.05f), 12f, 15f, 7f)
        };

        public static GameConfigAsset CreateRuntimeDefault()
        {
            var asset = CreateInstance<GameConfigAsset>();
            asset.name = "RuntimeGameConfig";
            return asset;
        }
    }

    [Serializable]
    public sealed class BarrelDropConfig
    {
        public PickupDropType Type = PickupDropType.Coin;
        [Min(1)] public int Amount = 1;
        [Min(0f)] public float Weight = 1f;
    }

    public enum PickupDropType
    {
        Coin,
        Experience,
        Gear,
        Magnet,
        Bonus
    }

    [Serializable]
    public sealed class UpgradeStatConfig
    {
        public string Title;
        [Tooltip("Sprite shown on the upgrade choice button. Imported textures must use Texture Type: Sprite (2D and UI).")]
        public Sprite Icon;
        public float BaseValue;
        [Min(0f)] public float ValueMultiplier = 1f;

        public UpgradeStatConfig(string title, float baseValue, float valueMultiplier)
        {
            Title = title;
            BaseValue = baseValue;
            ValueMultiplier = valueMultiplier;
        }
    }

    [Serializable]
    public sealed class PrefabReferences
    {
        [Tooltip("Root prefab for the car. VehicleController will be added if it is missing.")]
        public GameObject VehiclePrefab;

        [Tooltip("Turret visual/root prefab. TurretController will be added at runtime.")]
        public GameObject TurretPrefab;

        [Tooltip("Road segment prefab. If empty, ground.fbx/fallback cube is used.")]
        public GameObject RoadSegmentPrefab;

        [Tooltip("Enemy presenter prefab with EnemyPresenter component and optional visual/particles references.")]
        public GameObject EnemyPresenterPrefab;

        [Tooltip("Bullet presenter prefab. BulletPresenter will be added if missing.")]
        public GameObject BulletPresenterPrefab;

        [Tooltip("Coin pickup presenter prefab. PickupPresenter will be added if missing.")]
        public GameObject CoinPickupPrefab;

        [Tooltip("Experience pickup presenter prefab. PickupPresenter will be added if missing.")]
        public GameObject ExperiencePickupPrefab;

        [Tooltip("Gear repair pickup presenter prefab. PickupPresenter will be added if missing.")]
        public GameObject GearPickupPrefab;
        public GameObject MagnetPickupPrefab;
        public GameObject BonusPickupPrefab;
        public GameObject BarrelPresenterPrefab;

        [Header("Optional Materials")]
        public Material BulletMaterial;
        public Material BulletTrailMaterial;
        [Min(0.01f)] public float BulletTrailLength = 1.65f;
        [Min(0.005f)] public float BulletTrailStartWidth = 0.12f;
        [Min(0.005f)] public float BulletTrailEndWidth = 0.015f;
        public Color BulletTrailTailColor = new(1f, 0.08f, 0.02f, 0f);
        public Color BulletTrailHeadColor = new(1f, 0.95f, 0.08f, 1f);
        public Material CoinMaterial;
        public Material ExperienceMaterial;
        public Material GearMaterial;
        public Material MagnetMaterial;
        public Material BonusMaterial;
        public Material BarrelMaterial;
        public Material BarrelDamageFlashMaterial;
        public Material RoadSideMaterial;
        public Material TurretAimLineMaterial;

        [Header("Enemy Materials")]
        public Material EnemyMaterial;
        public Material EnemyDamageFlashMaterial;
        [Min(0.01f)] public float EnemyDamageFlashDuration = 0.09f;
        public Color EnemyDeathParticleColor = Color.red;

        [Header("Vehicle Feedback")]
        public Material VehicleDamageFlashMaterial;
        public Material VehicleInvulnerabilityFlashMaterial;
        [Min(0.01f)] public float VehicleDamageFlashDuration = 0.09f;
        [Min(0.01f)] public float VehicleDamagePulseDuration = 0.16f;
        [Range(0.01f, 0.35f)] public float VehicleDamagePulseScale = 0.08f;
    }

    [Serializable]
    public sealed class EnemyTypeConfig
    {
        public string Id;
        public float Hp;
        public float Speed;
        public float DamagePerSecond;
        public int XpReward;
        [Range(0f, 1f)] public float XpDropChance = 1f;
        public int CoinReward;
        [Range(0f, 1f)] public float CoinDropChance = 1f;
        [Range(0f, 1f)] public float GearDropChance = 0.08f;
        public Color Color;

        [HideInInspector]
        public float RunAnimationSpeed;

        [HideInInspector]
        public float AttackAnimationSpeed;

        [HideInInspector]
        public float HitTilt;

        [Header("Optional Overrides")]
        public GameObject PresenterPrefabOverride;
        public EnemyAnimationSet Animations = new();

        public EnemyTypeConfig(string id, float hp, float speed, float damagePerSecond, int xpReward, int coinReward, Color color, float runAnimationSpeed, float attackAnimationSpeed, float hitTilt)
        {
            Id = id;
            Hp = hp;
            Speed = speed;
            DamagePerSecond = damagePerSecond;
            XpReward = xpReward;
            CoinReward = coinReward;
            Color = color;
            RunAnimationSpeed = runAnimationSpeed;
            AttackAnimationSpeed = attackAnimationSpeed;
            HitTilt = hitTilt;
        }
    }

    [Serializable]
    public sealed class EnemyAnimationSet
    {
        public AnimationClip Run;
        public AnimationClip Idle;
        public AnimationClip Walk;
        public AnimationClip Attack;
        public AnimationClip Hit;

        [Min(0.01f)] public float IdleSpeed = 1f;
        [Min(0.01f)] public float WalkSpeed = 1f;
        [Min(0.01f)] public float RunSpeed = 1f;
        [Min(0.01f)] public float AttackSpeed = 1f;
        [Min(0.01f)] public float HitSpeed = 1f;
    }

    [Serializable]
    public sealed class EnemyWaveConfig
    {
        [Min(0f)] public float StartDistance;
        [Tooltip("If greater than 0, the wave spawns exactly this many enemies across its distance segment. If 0, SpawnSpacing is used.")]
        [Min(0)] public int SpawnCount;
        [Min(1f)] public float SpawnSpacing = 4.5f;
        [Min(0f)] public float SpawnSpacingRandomness = 1.25f;
        [Min(5f)] public float SpawnAheadDistance = 70f;
        [Min(0f)] public float DifficultyMultiplier = 1f;
        public EnemySpawnChoice[] Choices =
        {
            new()
        };
    }

    [Serializable]
    public sealed class EnemySpawnChoice
    {
        [Min(0)] public int EnemyTypeIndex;
        [Min(0f)] public float Weight = 1f;
        public GameObject PresenterPrefabOverride;
    }
}
