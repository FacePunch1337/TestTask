using System.Collections;
using TestTask.UI;
using UnityEngine;

namespace TestTask.Gameplay.Presentation
{
    public sealed class BarrelPresenter : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private HealthBarView healthBar;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.09f;

        private Renderer[] _renderers;
        private Material[][] _baseMaterials;
        private Material _flashMaterial;
        private float _flashTimer;
        private int _lastHitEvent;
        private bool _deathStarted;

        public static BarrelPresenter Create(GameObject prefab, Material material, Material flashMaterial, float flashDuration)
        {
            var go = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "BarrelPresenter";
            if (prefab == null)
                go.transform.localScale = new Vector3(0.9f, 0.65f, 0.9f);

            var presenter = go.GetComponent<BarrelPresenter>();
            if (presenter == null)
                presenter = go.AddComponent<BarrelPresenter>();

            presenter.Construct(material, flashMaterial, flashDuration);
            return presenter;
        }

        public void Construct(Material material, Material flashMaterial, float configuredFlashDuration)
        {
            _flashMaterial = flashMaterial;
            flashDuration = Mathf.Max(0.01f, configuredFlashDuration);
            if (visual == null)
                visual = transform;

            _renderers = visual.GetComponentsInChildren<Renderer>(true);
            _baseMaterials = new Material[_renderers.Length][];
            for (var i = 0; i < _renderers.Length; i++)
            {
                _baseMaterials[i] = _renderers[i].sharedMaterials;
                if (material == null)
                    continue;

                var materials = _renderers[i].sharedMaterials;
                for (var j = 0; j < materials.Length; j++)
                    materials[j] = material;
                _renderers[i].sharedMaterials = materials;
                _baseMaterials[i] = materials;
            }

            EnsureHealthBar();
            healthBar.Hide();
        }

        public void Apply(Vector3 position, Quaternion rotation, float hp01, int hitEvent, float deltaTime)
        {
            if (_deathStarted)
                return;

            transform.SetPositionAndRotation(position, rotation);
            if (hitEvent != _lastHitEvent)
            {
                _lastHitEvent = hitEvent;
                _flashTimer = flashDuration;
                healthBar.Reveal(1.25f);
            }

            _flashTimer = Mathf.Max(0f, _flashTimer - deltaTime);
            ApplyFlash();
            healthBar.Tick(hp01, deltaTime, true);
        }

        public void BeginDeath(Vector3 position)
        {
            if (_deathStarted)
                return;

            _deathStarted = true;
            transform.position = position;
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            _flashTimer = flashDuration;
            ApplyFlash();
            yield return healthBar.PlayDeath(flashDuration);

            Destroy(gameObject);
        }

        private void ApplyFlash()
        {
            if (_flashMaterial == null || _renderers == null)
                return;

            var useFlash = _flashTimer > 0f;
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                if (!useFlash)
                {
                    if (_baseMaterials != null && i < _baseMaterials.Length)
                        renderer.sharedMaterials = _baseMaterials[i];
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
                healthBar.ConfigureFallback(transform, "BarrelHpBar", new Vector3(0f, 1.45f, 0f), new Vector2(0.85f, 0.12f));
            }

            healthBar.Initialize();
        }
    }
}
