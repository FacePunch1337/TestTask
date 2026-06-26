using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TestTask.UI
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private Image fill;
        [SerializeField] private Image delayFill;
        [SerializeField] private bool alwaysVisible;
        [SerializeField] private Color damageDelayColor = new(1f, 0.82f, 0.08f, 0.95f);
        [SerializeField] private Color healDelayColor = new(0.2f, 1f, 0.25f, 0.95f);
        [SerializeField, Min(0.1f)] private float visibleDuration = 1.4f;
        [SerializeField, Min(0f)] private float delayStart = 0.18f;
        [SerializeField, Min(0.01f)] private float delayCatchupSpeed = 2.8f;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private bool billboardToCamera = true;

        private float _visibleTimer;
        private float _delayTimer;
        private float _lastHp01 = 1f;
        private float _displayHp01 = 1f;
        private float _delayHp01 = 1f;
        private HealthBarDelayMode _delayMode;
        private bool _initialized;
        private Graphic[] _graphics;
        private Color[] _baseGraphicColors;

        public void Initialize()
        {
            if (root == null)
                root = transform;

            if (!_initialized)
            {
                localOffset = root.localPosition;
                CacheGraphics();
                _initialized = true;
            }

            if (fill == null)
                fill = transform.Find("HPFill")?.GetComponent<Image>() ?? transform.Find("Fill")?.GetComponent<Image>();

            if (delayFill == null)
                delayFill = transform.Find("DelayFill")?.GetComponent<Image>();

            SetVisible(alwaysVisible);
        }

        public void Tick(float hp01, float deltaTime, bool revealOnDamage)
        {
            Initialize();
            hp01 = Mathf.Clamp01(hp01);
            if (hp01 < _lastHp01)
            {
                _delayTimer = delayStart;
                _displayHp01 = hp01;
                _delayHp01 = Mathf.Max(_delayHp01, _lastHp01);
                _delayMode = HealthBarDelayMode.Damage;
                ApplyDelayColor();
                if (revealOnDamage)
                    _visibleTimer = visibleDuration;
            }
            else if (hp01 > _lastHp01)
            {
                _delayTimer = delayStart;
                _displayHp01 = Mathf.Min(_displayHp01, _lastHp01);
                _delayHp01 = hp01;
                _delayMode = HealthBarDelayMode.Heal;
                ApplyDelayColor();
                if (revealOnDamage)
                    _visibleTimer = visibleDuration;
            }

            _lastHp01 = hp01;
            _visibleTimer = Mathf.Max(0f, _visibleTimer - deltaTime);
            ApplyFill(hp01, deltaTime);
            SetVisible(alwaysVisible || (_visibleTimer > 0f && hp01 > 0f && hp01 < 0.999f));
            TickTransform();
        }

        public void Reveal(float duration)
        {
            _visibleTimer = Mathf.Max(_visibleTimer, duration);
            SetVisible(true);
        }

        public IEnumerator PlayDeath(float flashDuration)
        {
            Initialize();
            if (fill != null)
                fill.fillAmount = 0f;
            if (delayFill != null)
                delayFill.fillAmount = Mathf.Max(_delayHp01, _lastHp01);

            SetVisible(true);
            TickTransform();

            var waitTimer = Mathf.Max(flashDuration, delayStart);
            while (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                yield return null;
            }

            while (delayFill != null && delayFill.fillAmount > 0.01f)
            {
                delayFill.fillAmount = Mathf.MoveTowards(delayFill.fillAmount, 0f, delayCatchupSpeed * Time.deltaTime);
                yield return null;
            }

            if (delayFill != null)
                delayFill.fillAmount = 0f;
        }

        public void Hide()
        {
            if (!alwaysVisible)
                SetVisible(false);
        }

        public void SetBlink(bool active, float time)
        {
            Initialize();
            if (_graphics == null || _baseGraphicColors == null)
                return;

            var pulse = active ? 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(time * 14f)) : 0f;
            for (var i = 0; i < _graphics.Length; i++)
            {
                var graphic = _graphics[i];
                if (graphic == null)
                    continue;

                graphic.color = active
                    ? Color.Lerp(_baseGraphicColors[i], Color.white, pulse * 0.65f)
                    : _baseGraphicColors[i];
            }
        }

        public void ConfigureFallback(Transform parent, string name, Vector3 offset, Vector2 size)
        {
            if (root != null && fill != null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            root = go.transform;
            localOffset = offset;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = UnityEngine.Camera.main;
            go.AddComponent<CanvasScaler>();
            go.GetComponent<RectTransform>().sizeDelta = size;

            CreateImage(go.transform, "BG", new Color(0.04f, 0.04f, 0.04f, 0.75f), false);
            delayFill = CreateImage(go.transform, "DelayFill", damageDelayColor, true);
            fill = CreateImage(go.transform, "HPFill", new Color(0.95f, 0.08f, 0.04f, 0.95f), true);
        }

        private void ApplyFill(float hp01, float deltaTime)
        {
            if (delayFill == null)
            {
                if (fill != null)
                    fill.fillAmount = hp01;
                return;
            }

            if (_delayTimer > 0f)
            {
                _delayTimer = Mathf.Max(0f, _delayTimer - deltaTime);
            }
            else
            {
                if (_delayMode == HealthBarDelayMode.Heal)
                    _displayHp01 = Mathf.MoveTowards(_displayHp01, hp01, delayCatchupSpeed * deltaTime);
                else
                    _delayHp01 = Mathf.MoveTowards(_delayHp01, hp01, delayCatchupSpeed * deltaTime);
            }

            if (fill != null)
                fill.fillAmount = _delayMode == HealthBarDelayMode.Heal ? _displayHp01 : hp01;

            delayFill.fillAmount = _delayMode == HealthBarDelayMode.Heal
                ? hp01
                : Mathf.Max(hp01, _delayHp01);

            var isCaughtUp = _delayMode == HealthBarDelayMode.Heal
                ? Mathf.Abs(_displayHp01 - hp01) <= 0.001f
                : Mathf.Abs(_delayHp01 - hp01) <= 0.001f;

            if (isCaughtUp)
            {
                _displayHp01 = hp01;
                _delayHp01 = hp01;
                if (_delayMode == HealthBarDelayMode.Heal)
                    CompleteDelay(hp01);
            }
        }

        private void CompleteDelay(float hp01)
        {
            _displayHp01 = hp01;
            _delayHp01 = hp01;
            delayFill.fillAmount = hp01;
            delayFill.color = damageDelayColor;
            _delayMode = HealthBarDelayMode.Damage;
            CacheGraphics();
        }

        private void ApplyDelayColor()
        {
            if (delayFill == null)
                return;

            delayFill.color = _delayMode == HealthBarDelayMode.Heal ? healDelayColor : damageDelayColor;
            CacheGraphics();
        }

        private void TickTransform()
        {
            if (root == null)
                return;

            root.localPosition = localOffset;
            var camera = UnityEngine.Camera.main;
            if (billboardToCamera && camera != null && root.gameObject.activeSelf)
                root.rotation = Quaternion.LookRotation(root.position - camera.transform.position, Vector3.up);
        }

        private void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }

        private static Image CreateImage(Transform parent, string name, Color color, bool filled)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            if (filled)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                image.fillAmount = 1f;
            }

            return image;
        }

        private void CacheGraphics()
        {
            _graphics = root != null ? root.GetComponentsInChildren<Graphic>(true) : GetComponentsInChildren<Graphic>(true);
            _baseGraphicColors = new Color[_graphics.Length];
            for (var i = 0; i < _graphics.Length; i++)
                _baseGraphicColors[i] = _graphics[i] != null ? _graphics[i].color : Color.white;
        }

        private enum HealthBarDelayMode
        {
            Damage,
            Heal
        }
    }
}
