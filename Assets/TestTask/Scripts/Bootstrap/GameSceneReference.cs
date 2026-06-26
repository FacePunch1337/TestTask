using TestTask.Config;
using UnityEngine;

namespace TestTask.Bootstrap
{
    public sealed class GameSceneReference : MonoBehaviour
    {
        [SerializeField] private GameConfigAsset config;

        public GameConfigAsset Config => config;
    }
}
