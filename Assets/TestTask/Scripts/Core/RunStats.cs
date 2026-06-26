using TestTask.Config;
using UnityEngine;
namespace TestTask.Core
{
    public sealed class RunStats
    {
        public float CarMaxHp { get; private set; }
        public float FireRate { get; private set; }
        public float BulletDamage { get; private set; }
        public float BulletSpeed { get; private set; }
        public float TurretYawLimit { get; private set; }
        public float PickupMagnetRadius { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public bool IsInvulnerable => _bonusTimer > 0f;

        private float _magnetTimer;
        private float _magnetMultiplier = 1f;
        private float _bonusTimer;
        private float _bonusSpeedMultiplier = 1f;

        public RunStats(GameConfigAsset config) => Reset(config);

        public void Reset(GameConfigAsset config)
        {
            CarMaxHp = config.CarMaxHp;
            FireRate = config.FireRate;
            BulletDamage = config.BulletDamage;
            BulletSpeed = config.BulletSpeed;
            TurretYawLimit = config.TurretYawLimit;
            PickupMagnetRadius = config.PickupMagnetRadius;
            SpeedMultiplier = 1f;
            _magnetTimer = 0f;
            _magnetMultiplier = 1f;
            _bonusTimer = 0f;
            _bonusSpeedMultiplier = 1f;
        }

        public bool Tick(float deltaTime)
        {
            var bonusWasActive = _bonusTimer > 0f;
            if (_magnetTimer > 0f)
            {
                _magnetTimer = Mathf.Max(0f, _magnetTimer - deltaTime);
                if (_magnetTimer <= 0f)
                    _magnetMultiplier = 1f;
            }

            if (_bonusTimer > 0f)
            {
                _bonusTimer = Mathf.Max(0f, _bonusTimer - deltaTime);
                if (_bonusTimer <= 0f)
                    _bonusSpeedMultiplier = 1f;
            }

            SpeedMultiplier = _bonusSpeedMultiplier;
            return bonusWasActive && _bonusTimer <= 0f;
        }

        public void ActivateMagnet(float duration, float radiusMultiplier)
        {
            _magnetTimer = Mathf.Max(_magnetTimer, duration);
            _magnetMultiplier = Mathf.Max(_magnetMultiplier, radiusMultiplier);
        }

        public void ActivateBonus(float duration, float speedMultiplier)
        {
            _bonusTimer = Mathf.Max(_bonusTimer, duration);
            _bonusSpeedMultiplier = Mathf.Max(_bonusSpeedMultiplier, speedMultiplier);
            SpeedMultiplier = _bonusSpeedMultiplier;
        }

        public void AddFireRate(float amount) => FireRate += amount;
        public void AddBulletDamage(float amount) => BulletDamage += amount;
        public void AddPickupMagnetRadius(float amount) => PickupMagnetRadius += amount;
        public float EffectivePickupMagnetRadius => PickupMagnetRadius * _magnetMultiplier;
        public void AddCarMaxHp(float amount) => CarMaxHp += amount;
    }
}
