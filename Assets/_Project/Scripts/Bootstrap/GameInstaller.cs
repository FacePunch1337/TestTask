using System;
using TestTask.Config;
using TestTask.Core;
using TestTask.DI;
using TestTask.ECS.Bridge;
using TestTask.Gameplay.Turret;
using TestTask.UI;
using TestTask.Utils;
using UnityEngine;

namespace TestTask.Bootstrap
{
    public sealed class GameInstaller
    {
        private readonly SimpleContainer _container;
        private readonly GameConfigAsset _config;

        public GameInstaller(SimpleContainer container, GameConfigAsset config)
        {
            _container = container;
            _config = config ?? throw new InvalidOperationException("GameBootstrapper requires an assigned GameConfigAsset. Run Tools/TestTask/Setup Current Scene once and assign the config manually if needed.");
        }

        public void Install()
        {
            var config = _config;
            var runStats = new RunStats(config);
            var session = new GameSession(config, runStats);
            var input = new InputService();
            var combat = new CombatEcsBridge(config, runStats, session);
            var scene = new SceneFactory(config);
            var hudReferences = UnityEngine.Object.FindFirstObjectByType<HudSceneReferences>(FindObjectsInactive.Include);
            var hud = hudReferences != null ? hudReferences.CreateView() : HudView.Create();
            var vehicle = scene.CreateVehicle();
            vehicle.Construct(config, runStats);
            var turretTransform = scene.CreateTurretTransform(vehicle.transform);
            var turret = turretTransform.GetComponent<TurretController>();
            if (turret == null)
                turret = turretTransform.gameObject.AddComponent<TurretController>();
            var camera = scene.CreateCamera();
            var road = scene.CreateRoad();
            var spawner = new EnemySpawner(config, combat);
            var upgrades = new UpgradeService(config, runStats);

            turret.Construct(config, runStats, input, vehicle, combat);

            _container.Bind(config);
            _container.Bind(runStats);
            _container.Bind(session);
            _container.Bind(input);
            _container.Bind(combat);
            _container.Bind(hud);
            _container.Bind(vehicle);
            _container.Bind(turret);
            _container.Bind(camera);
            _container.Bind(road);
            _container.Bind(spawner);
            _container.Bind(upgrades);
            _container.Bind(new GameLoop(config, runStats, session, input, hud, vehicle, turret, spawner, combat, camera, road, upgrades));
        }
    }
}
