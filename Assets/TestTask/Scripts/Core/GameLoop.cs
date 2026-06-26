using TestTask.Config;
using TestTask.ECS.Bridge;
using TestTask.Gameplay.Camera;
using TestTask.Gameplay.Road;
using TestTask.Gameplay.Turret;
using TestTask.Gameplay.Vehicle;
using TestTask.UI;
using UnityEngine;

namespace TestTask.Core
{
    public sealed class GameLoop
    {
        private readonly GameConfigAsset _config;
        private readonly RunStats _runStats;
        private readonly GameSession _session;
        private readonly InputService _input;
        private readonly HudView _hud;
        private readonly VehicleController _vehicle;
        private readonly TurretController _turret;
        private readonly EnemySpawner _spawner;
        private readonly CombatEcsBridge _combat;
        private readonly CameraFollow _camera;
        private readonly RoadLooper _road;
        private readonly UpgradeService _upgrades;
        private bool _restartArmed;

        public GameLoop(GameConfigAsset config, RunStats runStats, GameSession session, InputService input, HudView hud, VehicleController vehicle, TurretController turret, EnemySpawner spawner, CombatEcsBridge combat, CameraFollow camera, RoadLooper road, UpgradeService upgrades)
        {
            _config = config;
            _runStats = runStats;
            _session = session;
            _input = input;
            _hud = hud;
            _vehicle = vehicle;
            _turret = turret;
            _spawner = spawner;
            _combat = combat;
            _camera = camera;
            _road = road;
            _upgrades = upgrades;
            ResetRun();
        }

        public void Tick(float deltaTime)
        {
            var bonusEnded = _runStats.Tick(deltaTime);
            if (bonusEnded)
                _combat.KillEnemiesInRadius(_vehicle.transform.position, _config.BonusEndKillRadius);
            _combat.UpdateRuntime(_vehicle.transform.position, _session.State == GameState.Running, _session.State == GameState.Lose, deltaTime);

            if (_session.State == GameState.WaitingForStart && _input.TapStarted())
                _session.StartRun();

            if (_session.State == GameState.Running)
            {
                _vehicle.Tick(deltaTime);
                _turret.Tick(deltaTime);
                _turret.TryShoot();
                _session.AddDistance(_config.CarSpeed * _runStats.SpeedMultiplier * deltaTime);
                _road.Tick(_vehicle.transform.position.z);
                _spawner.Tick(_session.Distance, _vehicle.transform.position);

                if (_session.Distance >= _config.LevelLength)
                    _session.CompleteLevel();
            }

            var carHpBeforeEvents = _session.CarHp;
            _combat.ConsumeEvents();
            if (_session.CarHp < carHpBeforeEvents)
                _vehicle.PlayDamageFeedback();
            _combat.SyncPresentation(deltaTime);
            _camera.Tick(deltaTime, _vehicle.transform);
            _hud.Tick(_session);

            if (_session.HasPendingUpgrade && _session.State == GameState.Running)
                ShowUpgradeAsync();

            if (_session.State is GameState.Win or GameState.Lose)
                TickEndState();
        }

        private async void ShowUpgradeAsync()
        {
            _session.PauseForUpgrade();
            Time.timeScale = 0f;
            var selected = await _hud.ShowUpgradeChoices(_upgrades.RollChoices());
            _upgrades.Apply(selected);
            Time.timeScale = 1f;
            _session.ResumeAfterUpgrade();
        }

        private void TickEndState()
        {
            _hud.ShowResult(_session.State == GameState.Win, _session.Stars());

            if (!_restartArmed)
            {
                _restartArmed = true;
                return;
            }

            if (_input.TapStarted())
                ResetRun();
        }

        private void ResetRun()
        {
            Time.timeScale = 1f;
            _restartArmed = false;
            _runStats.Reset(_config);
            _upgrades.Reset();
            _session.Reset();
            _vehicle.ResetVehicle();
            _turret.ResetTurret();
            _camera.SnapToTarget(_vehicle.transform);
            _combat.ResetWorld();
            _spawner.Reset();
            _road.ResetRoad();
            _hud.HideResult();
            _hud.Tick(_session);
        }
    }
}
