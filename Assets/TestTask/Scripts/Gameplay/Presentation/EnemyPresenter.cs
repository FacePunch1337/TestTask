using System.Collections;
using System.Collections.Generic;
using TestTask.Config;
using TestTask.ECS.Components;
using TestTask.UI;
using UnityEngine;

namespace TestTask.Gameplay.Presentation
{
    public sealed class EnemyPresenter : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private ParticleSystem deathParticles;
        [Header("Health Bar")]
        [SerializeField] private HealthBarView healthBar;

        private Renderer[] _renderers;
        private Material[][] _baseRendererMaterials;
        private Material _baseMaterialOverride;
        private Material _flashMaterial;
        private float _flashDuration = 0.09f;
        private Color _deathParticleColor = Color.red;
        private EnemyTypeConfig _type;
        private Vector3 _visualBaseLocalPosition;
        private Quaternion _visualBaseLocalRotation;
        private Transform[] _poseTransforms;
        private Vector3[] _bindLocalPositions;
        private Quaternion[] _bindLocalRotations;
        private Vector3[] _bindLocalScales;
        private EnemyEcsState _lastState;
        private float _stateTime;
        private float _flashTimer;
        private int _lastHitEvent;
        private bool _deathStarted;

#if UNITY_EDITOR
        private void Reset() => EnsureDeathParticlesInEditor();

        private void OnValidate()
        {
            if (!Application.isPlaying)
                EnsureDeathParticlesInEditor();
        }

        [ContextMenu("Ensure Death Particles")]
        private void EnsureDeathParticlesInEditor()
        {
            if (deathParticles != null)
                return;

            var existing = transform.Find("EnemyDeathParticles");
            if (existing != null)
            {
                deathParticles = existing.GetComponent<ParticleSystem>();
                if (deathParticles != null)
                    return;
            }

            var go = new GameObject("EnemyDeathParticles");
            go.transform.SetParent(transform, false);
            deathParticles = go.AddComponent<ParticleSystem>();
            ConfigureDeathParticles(deathParticles);
        }
#endif

        public void Construct(EnemyTypeConfig type, Material baseMaterialOverride = null, Material flashMaterial = null, float flashDuration = 0.09f, Color deathParticleColor = default)
        {
            _type = type;
            _baseMaterialOverride = baseMaterialOverride;
            _flashMaterial = flashMaterial;
            _flashDuration = Mathf.Max(0.01f, flashDuration);
            _deathParticleColor = deathParticleColor == default ? Color.red : deathParticleColor;

            if (visual == null)
                visual = FindVisualChild();

            if (visual == null)
                Debug.LogError($"EnemyPresenter for '{type.Id}' has no visual child. Put the enemy rig/model as a child of EnemyPresenterPrefab and assign it to Visual.", this);
            else
            {
                _visualBaseLocalPosition = visual.localPosition;
                _visualBaseLocalRotation = visual.localRotation;
                CacheBindPose();
            }

            if (deathParticles == null)
                deathParticles = CreateDeathParticles();

            CacheVisualRenderers();
            CacheRendererMaterials();
            ApplyBaseMaterials();

            EnsureHealthBar();
            healthBar.Hide();
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        public void ApplyState(Vector3 position, Quaternion rotation, EnemyEcsState state, int hitEvent, int deathEvent, float hp01, float deltaTime)
        {
            if (_deathStarted)
                return;

            if (state == EnemyEcsState.Dead)
            {
                BeginDeath(position);
                return;
            }

            if (hitEvent != _lastHitEvent)
            {
                _lastHitEvent = hitEvent;
                _flashTimer = _flashDuration;
                healthBar.Reveal(1.4f);
            }

            if (state != _lastState)
            {
                _lastState = state;
                _stateTime = 0f;
            }

            transform.SetPositionAndRotation(position, rotation);
            _stateTime += deltaTime;
            _flashTimer = Mathf.Max(0f, _flashTimer - deltaTime);

            ApplyDamageFlash();
            healthBar.Tick(hp01, deltaTime, true);
            ApplyClipState(state);
        }

        private Transform FindVisualChild()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.GetComponent<ParticleSystem>() == null)
                    return child;
            }

            return null;
        }

        private void CacheBindPose()
        {
            _poseTransforms = visual.GetComponentsInChildren<Transform>(true);
            _bindLocalPositions = new Vector3[_poseTransforms.Length];
            _bindLocalRotations = new Quaternion[_poseTransforms.Length];
            _bindLocalScales = new Vector3[_poseTransforms.Length];

            for (var i = 0; i < _poseTransforms.Length; i++)
            {
                var poseTransform = _poseTransforms[i];
                _bindLocalPositions[i] = poseTransform.localPosition;
                _bindLocalRotations[i] = poseTransform.localRotation;
                _bindLocalScales[i] = poseTransform.localScale;
            }
        }

        private void ApplyClipState(EnemyEcsState state)
        {
            if (visual == null)
                return;

            switch (state)
            {
                case EnemyEcsState.Idle:
                    SampleLoop(_type.Animations.Idle, _type.Animations.IdleSpeed);
                    break;
                case EnemyEcsState.Walking:
                    SampleLoop(_type.Animations.Walk, _type.Animations.WalkSpeed);
                    break;
                case EnemyEcsState.Running:
                    SampleLoop(_type.Animations.Run, _type.Animations.RunSpeed);
                    break;
                case EnemyEcsState.Attacking:
                    SampleLoop(_type.Animations.Attack, _type.Animations.AttackSpeed);
                    break;
                case EnemyEcsState.Hit:
                    SampleOnce(_type.Animations.Hit, _type.Animations.HitSpeed);
                    break;
                default:
                    RestoreBindPose();
                    break;
            }
        }

        private void SampleLoop(AnimationClip clip, float speed)
        {
            if (clip == null)
            {
                RestoreBindPose();
                return;
            }

            var sampleTime = Mathf.Repeat(_stateTime * Mathf.Max(0.01f, speed), Mathf.Max(0.01f, clip.length));
            SampleClip(clip, sampleTime);
        }

        private void SampleOnce(AnimationClip clip, float speed)
        {
            if (clip == null)
            {
                RestoreBindPose();
                return;
            }

            var sampleTime = Mathf.Min(_stateTime * Mathf.Max(0.01f, speed), Mathf.Max(0.01f, clip.length));
            SampleClip(clip, sampleTime);
        }

        private void SampleClip(AnimationClip clip, float time)
        {
            clip.SampleAnimation(visual.gameObject, time);

            visual.localPosition = _visualBaseLocalPosition;
            visual.localRotation = _visualBaseLocalRotation;
        }

        private void RestoreBindPose()
        {
            if (_poseTransforms == null)
                return;

            for (var i = 0; i < _poseTransforms.Length; i++)
            {
                var poseTransform = _poseTransforms[i];
                if (poseTransform == null)
                    continue;

                poseTransform.localPosition = _bindLocalPositions[i];
                poseTransform.localRotation = _bindLocalRotations[i];
                poseTransform.localScale = _bindLocalScales[i];
            }
        }

        private void CacheRendererMaterials()
        {
            if (_renderers == null || _renderers.Length == 0)
                CacheVisualRenderers();

            _baseRendererMaterials = new Material[_renderers.Length][];
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                _baseRendererMaterials[i] = renderer.sharedMaterials;
            }
        }

        private void CacheVisualRenderers()
        {
            var sourceRenderers = visual != null
                ? visual.GetComponentsInChildren<Renderer>(true)
                : GetComponentsInChildren<Renderer>(true);

            var filtered = new List<Renderer>(sourceRenderers.Length);
            for (var i = 0; i < sourceRenderers.Length; i++)
            {
                var renderer = sourceRenderers[i];
                if (renderer == null || ShouldIgnoreMaterialRenderer(renderer))
                    continue;

                filtered.Add(renderer);
            }

            _renderers = filtered.ToArray();
        }

        private bool ShouldIgnoreMaterialRenderer(Renderer renderer)
        {
            if (renderer is ParticleSystemRenderer)
                return true;

            if (deathParticles != null && renderer.transform.IsChildOf(deathParticles.transform))
                return true;

            if (healthBar != null && renderer.transform.IsChildOf(healthBar.transform))
                return true;

            return false;
        }

        private void ApplyBaseMaterials()
        {
            if (_baseMaterialOverride == null || _renderers == null)
                return;

            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                var materials = renderer.sharedMaterials;
                for (var j = 0; j < materials.Length; j++)
                    materials[j] = _baseMaterialOverride;
                renderer.sharedMaterials = materials;
            }

            CacheRendererMaterials();
        }

        private void ApplyDamageFlash()
        {
            if (_flashMaterial == null || _renderers == null || _baseRendererMaterials == null)
                return;

            var useFlash = _flashTimer > 0f;
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                if (!useFlash)
                {
                    if (i < _baseRendererMaterials.Length && _baseRendererMaterials[i] != null)
                        renderer.sharedMaterials = _baseRendererMaterials[i];
                    continue;
                }

                var materials = renderer.sharedMaterials;
                for (var j = 0; j < materials.Length; j++)
                    materials[j] = _flashMaterial;
                renderer.sharedMaterials = materials;
            }
        }

        private void EnsureHealthBar()
        {
            if (healthBar == null)
                healthBar = GetComponentInChildren<HealthBarView>(true);

            if (healthBar == null)
            {
                healthBar = gameObject.AddComponent<HealthBarView>();
                healthBar.ConfigureFallback(transform, "EnemyHpBar", new Vector3(0f, 2.35f, 0f), new Vector2(0.9f, 0.12f));
            }

            healthBar.Initialize();
        }

        public void BeginDeath(Vector3 position)
        {
            if (_deathStarted)
                return;

            _deathStarted = true;
            transform.position = position;
            StartCoroutine(DeathSequence(position));
        }

        private IEnumerator DeathSequence(Vector3 position)
        {
            if (visual != null)
                visual.gameObject.SetActive(true);

            _flashTimer = Mathf.Max(_flashDuration, 0.01f);
            ApplyDamageFlash();

            yield return healthBar.PlayDeath(_flashDuration);

            healthBar.Hide();
            if (visual != null)
                visual.gameObject.SetActive(false);

            PlayDeathParticles(position);
            Destroy(gameObject, DeathDestroyDelay());
        }

        private ParticleSystem CreateDeathParticles()
        {
            var go = new GameObject("EnemyDeathParticles");
            go.transform.SetParent(transform, false);
            var particles = go.AddComponent<ParticleSystem>();
            ConfigureDeathParticles(particles);
            return particles;
        }

        private static void ConfigureDeathParticles(ParticleSystem particles)
        {
            var main = particles.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 5f;
            main.startSize = 0.16f;
            main.startColor = Color.red;
            main.maxParticles = 48;
            main.loop = false;
            main.playOnAwake = false;
            var emission = particles.emission;
            emission.enabled = false;
        }

        private void PlayDeathParticles(Vector3 position)
        {
            if (deathParticles == null)
                return;

            if (deathParticles.transform.IsChildOf(transform))
                deathParticles.transform.SetParent(transform.parent, true);

            ApplyDeathParticleColor(deathParticles);
            deathParticles.transform.position = position + Vector3.up * 0.8f;
            deathParticles.gameObject.SetActive(true);
            deathParticles.Emit(36);
        }

        private void ApplyDeathParticleColor(ParticleSystem particles)
        {
            var main = particles.main;
            main.startColor = _deathParticleColor;
        }

        private float DeathDestroyDelay()
        {
            if (deathParticles == null)
                return 0.5f;

            var main = deathParticles.main;
            return Mathf.Max(main.duration, main.startLifetime.constantMax) + 0.1f;
        }
    }
}
