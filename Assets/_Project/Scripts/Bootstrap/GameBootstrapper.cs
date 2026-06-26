using TestTask.Core;
using TestTask.Config;
using TestTask.DI;
using UnityEngine;

namespace TestTask.Bootstrap
{
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private GameConfigAsset config;

        private GameLoop _gameLoop;

        private void Awake()
        {
            if (config == null)
            {
                var sceneReference = FindFirstObjectByType<GameSceneReference>();
                if (sceneReference != null)
                    config = sceneReference.Config;
            }

            var container = new SimpleContainer();
            new GameInstaller(container, config).Install();
            _gameLoop = container.Resolve<GameLoop>();
        }

        private void Update() => _gameLoop?.Tick(Time.deltaTime);
    }
}
