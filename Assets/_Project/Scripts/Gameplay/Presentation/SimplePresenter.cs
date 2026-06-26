using TestTask.ECS.Components;
using TestTask.Utils;
using UnityEngine;

namespace TestTask.Gameplay.Presentation
{
    public sealed class BulletPresenter : MonoBehaviour
    {
        private LineRenderer _trail;
        private Vector3 _lastPosition;
        private float _trailLength = 1.65f;
        private float _currentTrailLength;
        private bool _hasLastPosition;

        private Color _trailTailColor;
        private Color _trailHeadColor;

        public static BulletPresenter Create(GameObject prefab, Material material, Material trailMaterial, float trailLength, float trailStartWidth, float trailEndWidth, Color trailTailColor, Color trailHeadColor)
        {
            var go = prefab != null ? Object.Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "BulletPresenter";
            if (prefab == null)
                go.transform.localScale = Vector3.one * 0.22f;

            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material = material != null
                    ? material
                    : SceneFactory.MakeMaterial("Bullet_Mat", new Color(1f, 0.72f, 0.08f));
            }

            var presenter = go.GetComponent<BulletPresenter>();
            if (presenter == null)
                presenter = go.AddComponent<BulletPresenter>();

            presenter.ConfigureTrail(trailMaterial, trailLength, trailStartWidth, trailEndWidth, trailTailColor, trailHeadColor);
            return presenter;
        }

        public void Apply(Vector3 position)
        {
            gameObject.SetActive(true);
            transform.position = position;
            UpdateTrail(position);
        }

        private void OnDisable()
        {
            _hasLastPosition = false;
            _currentTrailLength = 0f;
            if (_trail != null)
                _trail.enabled = false;
        }

        private void ConfigureTrail(Material trailMaterial, float trailLength, float trailStartWidth, float trailEndWidth, Color trailTailColor, Color trailHeadColor)
        {
            _trailLength = Mathf.Max(0.01f, trailLength);
            _trailTailColor = trailTailColor;
            _trailHeadColor = trailHeadColor;
            _trail = GetComponent<LineRenderer>();
            if (_trail == null)
                _trail = gameObject.AddComponent<LineRenderer>();

            _trail.useWorldSpace = true;
            _trail.positionCount = 2;
            _trail.textureMode = LineTextureMode.Stretch;
            _trail.alignment = LineAlignment.View;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.startWidth = Mathf.Max(0.005f, trailStartWidth);
            _trail.endWidth = Mathf.Max(0.005f, trailEndWidth);
            _trail.colorGradient = CreateTrailGradient();
            _trail.material = trailMaterial != null ? trailMaterial : CreateDefaultTrailMaterial();
            _trail.enabled = false;
        }

        private void UpdateTrail(Vector3 position)
        {
            if (_trail == null)
                return;

            if (!_hasLastPosition)
            {
                _lastPosition = position;
                _hasLastPosition = true;
                _currentTrailLength = 0f;
                _trail.SetPosition(0, position);
                _trail.SetPosition(1, position);
                _trail.enabled = true;
                return;
            }

            var movement = position - _lastPosition;
            var direction = movement.sqrMagnitude > 0.0001f ? movement.normalized : -transform.forward;
            _currentTrailLength = Mathf.Min(_trailLength, _currentTrailLength + movement.magnitude);
            var tail = position - direction * _currentTrailLength;

            _trail.SetPosition(0, tail);
            _trail.SetPosition(1, position);
            _trail.enabled = true;
            _lastPosition = position;
        }

        private Gradient CreateTrailGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(_trailTailColor, 0f),
                    new GradientColorKey(_trailHeadColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(_trailTailColor.a, 0f),
                    new GradientAlphaKey(_trailHeadColor.a, 1f)
                });
            return gradient;
        }

        private static Material CreateDefaultTrailMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            return new Material(shader)
            {
                name = "BulletTrail_Runtime_Mat"
            };
        }
    }

    public sealed class PickupPresenter : MonoBehaviour
    {
        private Renderer _renderer;
        private bool _usesPrefab;

        public static PickupPresenter Create(GameObject prefab, Material material)
        {
            var usesPrefab = prefab != null;
            var go = usesPrefab ? Object.Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PickupPresenter";
            var presenter = go.GetComponent<PickupPresenter>();
            if (presenter == null)
                presenter = go.AddComponent<PickupPresenter>();

            presenter._usesPrefab = usesPrefab;
            presenter._renderer = go.GetComponentInChildren<Renderer>();
            if (!usesPrefab && presenter._renderer != null)
                presenter._renderer.sharedMaterial = material != null ? material : SceneFactory.MakeMaterial("Pickup_Mat", GetFallbackColor(PickupEcsType.Coin));

            return presenter;
        }

        public void Apply(Vector3 position, PickupEcsType type)
        {
            gameObject.SetActive(true);
            transform.position = position;
            if (!_usesPrefab)
                transform.localScale = GetFallbackScale(type);
            transform.Rotate(Vector3.up, 280f * Time.deltaTime, Space.World);
        }

        private static Vector3 GetFallbackScale(PickupEcsType type)
        {
            return type switch
            {
                PickupEcsType.Coin => Vector3.one * 0.32f,
                PickupEcsType.Experience => Vector3.one * 0.25f,
                PickupEcsType.Gear => Vector3.one * 0.34f,
                PickupEcsType.Magnet => Vector3.one * 0.36f,
                PickupEcsType.Bonus => Vector3.one * 0.4f,
                _ => Vector3.one * 0.25f
            };
        }

        private static Color GetFallbackColor(PickupEcsType type)
        {
            return type switch
            {
                PickupEcsType.Coin => new Color(1f, 0.78f, 0.06f),
                PickupEcsType.Experience => new Color(0.18f, 0.8f, 1f),
                PickupEcsType.Gear => new Color(0.36f, 1f, 0.36f),
                PickupEcsType.Magnet => new Color(0.1f, 0.95f, 1f),
                PickupEcsType.Bonus => new Color(1f, 0.18f, 0.9f),
                _ => Color.white
            };
        }
    }
}
