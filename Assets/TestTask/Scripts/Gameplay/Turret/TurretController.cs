using TestTask.Config;
using TestTask.Core;
using TestTask.ECS.Bridge;
using TestTask.Gameplay.Vehicle;
using Unity.Mathematics;
using UnityEngine;

namespace TestTask.Gameplay.Turret
{
    public sealed class TurretController : MonoBehaviour
    {
        [Header("Prefab Setup")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private Vector3 fallbackMuzzleLocalPosition = new(0f, 0.55f, 1.15f);

        private GameConfigAsset _config;
        private RunStats _runStats;
        private InputService _input;
        private VehicleController _vehicle;
        private CombatEcsBridge _combat;
        private float _lastShotTime;
        private float _manualYaw;
        private float _desiredYaw;

        public Vector3 FirePosition => muzzlePoint != null ? muzzlePoint.position : transform.position;
        public Vector3 FireDirection => transform.forward;

        public void Construct(GameConfigAsset config, RunStats runStats, InputService input, VehicleController vehicle, CombatEcsBridge combat)
        {
            _config = config;
            _runStats = runStats;
            _input = input;
            _vehicle = vehicle;
            _combat = combat;
            EnsureMuzzlePoint();
            EnsureAimLine();
        }

        public void ResetTurret()
        {
            _lastShotTime = 0f;
            _manualYaw = 0f;
            _desiredYaw = 0f;
            transform.localRotation = Quaternion.identity;
            UpdateAimLine();
        }

        public void Tick(float deltaTime)
        {
            _manualYaw = Mathf.Clamp(
                _manualYaw + _input.HorizontalAimDelta() * _config.TurretDragSensitivity,
                -_runStats.TurretYawLimit,
                _runStats.TurretYawLimit);

            if (_combat.TryFindTargetInCone(_vehicle.transform.position, _manualYaw, _config.TurretTargetCone, out var target))
            {
                var targetPosition = new Vector3(target.x, target.y, target.z);
                var localDirection = transform.parent.InverseTransformDirection(targetPosition - transform.position);
                var targetYaw = Mathf.Clamp(Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg, -_runStats.TurretYawLimit, _runStats.TurretYawLimit);
                var correction = Mathf.Clamp(Mathf.DeltaAngle(_manualYaw, targetYaw), -_config.TurretAimAssistMaxCorrection, _config.TurretAimAssistMaxCorrection);
                _desiredYaw = Mathf.Clamp(_manualYaw + correction * _config.TurretAimAssistStrength, -_runStats.TurretYawLimit, _runStats.TurretYawLimit);
            }
            else
            {
                _desiredYaw = _manualYaw;
            }

            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(0f, _desiredYaw, 0f), 1f - Mathf.Exp(-deltaTime * _config.TurretRotationSharpness));
            UpdateAimLine();
        }

        public void TryShoot()
        {
            if (Time.time < _lastShotTime + 1f / _runStats.FireRate)
                return;

            _lastShotTime = Time.time;
            _combat.SpawnBullet((float3)FirePosition, (float3)FireDirection);
        }

        private void EnsureAimLine()
        {
            if (aimLine == null)
                aimLine = GetComponent<LineRenderer>();
            if (aimLine == null)
                aimLine = gameObject.AddComponent<LineRenderer>();

            aimLine.useWorldSpace = true;
            aimLine.positionCount = 2;
            aimLine.textureMode = LineTextureMode.Stretch;
            aimLine.alignment = LineAlignment.View;
            aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            aimLine.receiveShadows = false;
            aimLine.startWidth = Mathf.Max(0.005f, _config.TurretAimLineWidth);
            aimLine.endWidth = Mathf.Max(0.005f, _config.TurretAimLineWidth * 0.25f);
            aimLine.colorGradient = CreateAimGradient();
            aimLine.material = _config.Prefabs.TurretAimLineMaterial != null
                ? _config.Prefabs.TurretAimLineMaterial
                : CreateDefaultAimMaterial();
        }

        private void UpdateAimLine()
        {
            if (aimLine == null)
                return;

            var start = FirePosition;
            var end = start + FireDirection * _config.TurretAimLineLength;
            aimLine.SetPosition(0, start);
            aimLine.SetPosition(1, end);
            aimLine.enabled = true;
        }

        private void EnsureMuzzlePoint()
        {
            if (muzzlePoint != null)
                return;

            var existing = transform.Find("MuzzlePoint");
            if (existing != null)
            {
                muzzlePoint = existing;
                return;
            }

            var go = new GameObject("MuzzlePoint");
            muzzlePoint = go.transform;
            muzzlePoint.SetParent(transform, false);
            muzzlePoint.localPosition = fallbackMuzzleLocalPosition;
        }

        private Gradient CreateAimGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(_config.TurretAimLineStartColor, 0f),
                    new GradientColorKey(_config.TurretAimLineEndColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(_config.TurretAimLineStartColor.a, 0f),
                    new GradientAlphaKey(_config.TurretAimLineEndColor.a, 1f)
                });
            return gradient;
        }

        private static Material CreateDefaultAimMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            return new Material(shader)
            {
                name = "TurretAimLine_Runtime_Mat"
            };
        }
    }
}
