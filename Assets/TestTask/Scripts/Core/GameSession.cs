using TestTask.Config;
using UnityEngine;

namespace TestTask.Core
{
    public enum GameState
    {
        WaitingForStart,
        Running,
        Upgrade,
        Win,
        Lose
    }

    public sealed class GameSession
    {
        private readonly GameConfigAsset _config;
        private readonly RunStats _runStats;

        public GameSession(GameConfigAsset config, RunStats runStats)
        {
            _config = config;
            _runStats = runStats;
        }

        public GameState State { get; private set; }
        public float Distance { get; private set; }
        public float CarHp { get; private set; }
        public float DamageTaken { get; private set; }
        public int Coins { get; private set; }
        public int Xp { get; private set; }
        public int PlayerLevel { get; private set; } = 1;
        public bool HasPendingUpgrade { get; private set; }

        public float Hp01 => Mathf.Clamp01(CarHp / _runStats.CarMaxHp);
        public int CurrentXpRequirement => Mathf.Max(1, Mathf.RoundToInt(_config.XpToLevel * Mathf.Pow(Mathf.Max(1f, _config.XpRequirementMultiplier), PlayerLevel - 1)));
        public float Xp01 => Mathf.Clamp01((float)Xp / CurrentXpRequirement);
        public float Distance01 => Mathf.Clamp01(Distance / _config.LevelLength);
        public float LevelLength => _config.LevelLength;
        public bool IsCarInvulnerable => _runStats.IsInvulnerable;

        public void Reset()
        {
            State = GameState.WaitingForStart;
            Distance = 0f;
            CarHp = _runStats.CarMaxHp;
            DamageTaken = 0f;
            Coins = 0;
            Xp = 0;
            PlayerLevel = 1;
            HasPendingUpgrade = false;
        }

        public void StartRun() => State = GameState.Running;
        public void PauseForUpgrade() => State = GameState.Upgrade;

        public void ResumeAfterUpgrade()
        {
            HasPendingUpgrade = false;
            State = GameState.Running;
        }

        public void AddDistance(float amount) => Distance = Mathf.Min(_config.LevelLength, Distance + amount);

        public void ApplyCarDamage(float damage)
        {
            if (State != GameState.Running)
                return;

            if (_runStats.IsInvulnerable)
                return;

            CarHp = Mathf.Max(0f, CarHp - damage);
            DamageTaken += damage;
            if (CarHp <= 0f)
                State = GameState.Lose;
        }

        public void RepairCar(float amount)
        {
            if (State != GameState.Running)
                return;

            CarHp = Mathf.Min(_runStats.CarMaxHp, CarHp + amount);
        }

        public void AddReward(int coins, int xp)
        {
            Coins += coins;
            Xp += xp;

            while (Xp >= CurrentXpRequirement)
            {
                Xp -= CurrentXpRequirement;
                PlayerLevel++;
                HasPendingUpgrade = true;
            }
        }

        public void CompleteLevel() => State = GameState.Win;

        public int Stars()
        {
            var damageRatio = DamageTaken / _runStats.CarMaxHp;
            if (damageRatio <= 0.001f)
                return 3;
            return damageRatio <= 0.35f ? 2 : 1;
        }
    }
}
