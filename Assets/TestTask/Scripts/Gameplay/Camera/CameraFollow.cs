using UnityEngine;
using TestTask.Config;

namespace TestTask.Gameplay.Camera
{
    public sealed class CameraFollow
    {
        private readonly GameConfigAsset _config;
        private readonly UnityEngine.Camera _camera;

        public CameraFollow(GameConfigAsset config, UnityEngine.Camera camera)
        {
            _config = config;
            _camera = camera;
            _camera.fieldOfView = 48f;
        }

        public void SnapToTarget(Transform target)
        {
            _camera.transform.position = target.position + _config.CameraOffset;
            _camera.transform.rotation = Quaternion.Euler(_config.CameraEulerAngles);
        }

        public void Tick(float deltaTime, Transform target)
        {
            var t = 1f - Mathf.Exp(-deltaTime * _config.CameraFollowSharpness);
            var targetPosition = target.position + _config.CameraOffset;
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetPosition, t);
            _camera.transform.rotation = Quaternion.Lerp(_camera.transform.rotation, Quaternion.Euler(_config.CameraEulerAngles), t);
        }
    }
}
