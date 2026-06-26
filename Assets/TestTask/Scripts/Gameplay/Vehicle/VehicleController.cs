using TestTask.Config;
using UnityEngine;

namespace TestTask.Gameplay.Vehicle
{
    public sealed class VehicleController : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Material damageFlashMaterial;
        [SerializeField] private Material invulnerabilityFlashMaterial;
        [SerializeField] private Color invulnerabilityColorA = new(0.15f, 0.85f, 1f, 1f);
        [SerializeField] private Color invulnerabilityColorB = new(1f, 0.2f, 0.95f, 1f);
        [SerializeField, Min(0.01f)] private float damageFlashDuration = 0.09f;
        [SerializeField, Min(0.01f)] private float damagePulseDuration = 0.16f;
        [SerializeField, Range(0.01f, 0.35f)] private float damagePulseScale = 0.08f;

        [Header("Wheels")]
        [SerializeField] private Transform[] wheels;
        [SerializeField, Min(0.05f)] private float wheelRadius = 0.34f;
        [SerializeField] private Vector3 wheelRotationAxis = Vector3.right;

        private GameConfigAsset _config;
        private TestTask.Core.RunStats _runStats;
        private Renderer[] _renderers;
        private Material[][] _baseRendererMaterials;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _visualBaseScale = Vector3.one;
        private float _phase;
        private float _flashTimer;
        private float _pulseTimer;
        private bool _invulnerabilityFlashApplied;

        public void Construct(GameConfigAsset config, TestTask.Core.RunStats runStats = null)
        {
            _config = config;
            _runStats = runStats;
            if (damageFlashMaterial == null)
                damageFlashMaterial = config.Prefabs.VehicleDamageFlashMaterial != null
                    ? config.Prefabs.VehicleDamageFlashMaterial
                    : config.Prefabs.EnemyDamageFlashMaterial;

            if (invulnerabilityFlashMaterial == null)
                invulnerabilityFlashMaterial = config.Prefabs.VehicleInvulnerabilityFlashMaterial != null
                    ? config.Prefabs.VehicleInvulnerabilityFlashMaterial
                    : damageFlashMaterial;

            if (Mathf.Approximately(damageFlashDuration, 0.09f))
                damageFlashDuration = Mathf.Max(0.01f, config.Prefabs.VehicleDamageFlashDuration);
            if (Mathf.Approximately(damagePulseDuration, 0.16f))
                damagePulseDuration = Mathf.Max(0.01f, config.Prefabs.VehicleDamagePulseDuration);
            if (Mathf.Approximately(damagePulseScale, 0.08f))
                damagePulseScale = Mathf.Max(0.01f, config.Prefabs.VehicleDamagePulseScale);
            CacheVisuals();
        }

        public void ResetVehicle()
        {
            _phase = 0f;
            _flashTimer = 0f;
            _pulseTimer = 0f;
            _invulnerabilityFlashApplied = false;
            ClearMaterialPropertyBlocks();
            RestoreBaseMaterials();
            if (visualRoot != null)
                visualRoot.localScale = _visualBaseScale;
            transform.SetPositionAndRotation(new Vector3(0f, 0.35f, 0f), Quaternion.identity);
        }

        public void Tick(float deltaTime)
        {
            var previousPosition = transform.position;
            _phase += deltaTime * _config.CarPathFrequency;
            var speedMultiplier = _runStats != null ? _runStats.SpeedMultiplier : 1f;
            var nextZ = transform.position.z + _config.CarSpeed * speedMultiplier * deltaTime;
            var nextX = Mathf.Sin(_phase) * _config.CarPathPrimaryAmplitude +
                        Mathf.Sin(_phase * 0.47f) * _config.CarPathSecondaryAmplitude;
            var nextPosition = new Vector3(nextX, 0.35f, nextZ);
            var movement = nextPosition - previousPosition;
            var yaw = movement.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg * _config.CarSteerYawMultiplier
                : transform.eulerAngles.y;
            var rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, yaw, 0f), 1f - Mathf.Exp(-deltaTime * _config.CarSteerSharpness));
            transform.SetPositionAndRotation(nextPosition, rotation);

            TickWheels(movement.magnitude);
            TickDamageFeedback(deltaTime);
            TickInvulnerabilityFeedback();
        }

        public void PlayDamageFeedback()
        {
            _flashTimer = damageFlashDuration;
            _pulseTimer = damagePulseDuration;
            ApplyFlashMaterial(damageFlashMaterial, true);
        }

        private void CacheVisuals()
        {
            if (visualRoot == null)
                visualRoot = FindVisualRoot();

            if (visualRoot != null)
                _visualBaseScale = visualRoot.localScale;

            _renderers = visualRoot != null
                ? visualRoot.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);

            _baseRendererMaterials = new Material[_renderers.Length][];
            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _baseRendererMaterials[i] = _renderers[i].sharedMaterials;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
        }

        private Transform FindVisualRoot()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.GetComponentInChildren<Renderer>(true) != null)
                    return child;
            }

            return transform;
        }

        private void TickWheels(float distance)
        {
            if (wheels == null || wheels.Length == 0 || distance <= 0f)
                return;

            var angle = distance / Mathf.Max(0.05f, wheelRadius) * Mathf.Rad2Deg;
            var axis = wheelRotationAxis.sqrMagnitude > 0.0001f ? wheelRotationAxis.normalized : Vector3.right;
            for (var i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                    wheels[i].Rotate(axis, angle, Space.Self);
            }
        }

        private void TickDamageFeedback(float deltaTime)
        {
            if (_flashTimer > 0f)
            {
                _flashTimer = Mathf.Max(0f, _flashTimer - deltaTime);
                if (_flashTimer <= 0f)
                    ApplyDamageFlash(false);
            }

            if (visualRoot == null || _pulseTimer <= 0f)
                return;

            _pulseTimer = Mathf.Max(0f, _pulseTimer - deltaTime);
            var t = 1f - _pulseTimer / Mathf.Max(0.01f, damagePulseDuration);
            var wave = Mathf.Sin(t * Mathf.PI);
            var scale = 1f - damagePulseScale * wave;
            visualRoot.localScale = _visualBaseScale * scale;

            if (_pulseTimer <= 0f)
                visualRoot.localScale = _visualBaseScale;
        }

        private void TickInvulnerabilityFeedback()
        {
            if (_runStats == null || !_runStats.IsInvulnerable)
            {
                if (_invulnerabilityFlashApplied && _flashTimer <= 0f)
                {
                    _invulnerabilityFlashApplied = false;
                    ApplyDamageFlash(false);
                }
                return;
            }

            var useFlash = Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
            if (useFlash == _invulnerabilityFlashApplied)
            {
                if (useFlash)
                    ApplyInvulnerabilityColor();
                return;
            }

            _invulnerabilityFlashApplied = useFlash;
            ApplyFlashMaterial(invulnerabilityFlashMaterial, useFlash);
            if (useFlash)
                ApplyInvulnerabilityColor();
        }

        private void ApplyDamageFlash(bool useFlash)
        {
            ApplyFlashMaterial(damageFlashMaterial, useFlash);
        }

        private void ApplyFlashMaterial(Material flashMaterial, bool useFlash)
        {
            if (flashMaterial == null || _renderers == null || _baseRendererMaterials == null)
                return;

            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                if (!useFlash)
                {
                    if (i < _baseRendererMaterials.Length && _baseRendererMaterials[i] != null)
                        renderer.sharedMaterials = _baseRendererMaterials[i];
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                var materials = renderer.sharedMaterials;
                for (var j = 0; j < materials.Length; j++)
                    materials[j] = flashMaterial;
                renderer.sharedMaterials = materials;
            }
        }

        private void ApplyInvulnerabilityColor()
        {
            if (_renderers == null)
                return;

            var t = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
            var color = Color.Lerp(invulnerabilityColorA, invulnerabilityColorB, t);
            _propertyBlock ??= new MaterialPropertyBlock();
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", color);
                _propertyBlock.SetColor("_Color", color);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ClearMaterialPropertyBlocks()
        {
            if (_renderers == null)
                return;

            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].SetPropertyBlock(null);
            }
        }

        private void RestoreBaseMaterials()
        {
            ApplyDamageFlash(false);
        }
    }
}
