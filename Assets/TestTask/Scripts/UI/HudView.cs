using System.Threading.Tasks;
using TestTask.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TestTask.UI
{
    public sealed class HudView
    {
        private readonly TMP_Text _coins;
        private readonly TMP_Text _level;
        private readonly TMP_Text _message;
        private readonly HealthBarView _carHealthBar;
        private readonly Image _hp;
        private readonly Image _xp;
        private readonly Image _distanceFill;
        private RectTransform _distanceMarkerRoot;
        private Image _distanceMarkerIcon;
        private TMP_Text _distanceMarkerText;
        private bool _ownsDistanceMarker;
        private bool _canMoveAssignedDistanceMarker = true;
        private readonly bool _allowRuntimeDistanceMarkerCreation;
        private Vector2 _assignedDistanceMarkerStart;
        private readonly GameObject _upgradeRoot;
        private readonly Button[] _upgradeButtons;
        private readonly TMP_Text[] _upgradeTexts;
        private readonly Image[] _upgradeIcons;
        private readonly Color _hpBaseColor;

        public HudView(TMP_Text coins, TMP_Text level, TMP_Text message, HealthBarView carHealthBar, Image xp, Image distanceFill, RectTransform distanceMarkerRoot, Image distanceMarkerIcon, TMP_Text distanceMarkerText, GameObject upgradeRoot, Button[] upgradeButtons, TMP_Text[] upgradeTexts, Image[] upgradeIcons = null, Image hpFallback = null)
        {
            _coins = coins;
            _level = level;
            _message = message;
            _carHealthBar = carHealthBar;
            _hp = hpFallback;
            _xp = xp;
            _distanceFill = distanceFill;
            _distanceMarkerRoot = distanceMarkerRoot;
            _distanceMarkerIcon = distanceMarkerIcon;
            _distanceMarkerText = distanceMarkerText;
            _ownsDistanceMarker = distanceMarkerRoot == null;
            _allowRuntimeDistanceMarkerCreation = distanceMarkerRoot == null && distanceMarkerIcon == null && distanceMarkerText == null;
            _upgradeRoot = upgradeRoot;
            _upgradeButtons = upgradeButtons;
            _upgradeTexts = upgradeTexts;
            _upgradeIcons = upgradeIcons;
            _hpBaseColor = _hp != null ? _hp.color : Color.white;
            _carHealthBar?.Initialize();
            EnsureDistanceMarker();
            CacheDistanceMarkerLayout();
        }

        public void Tick(GameSession session)
        {
            _coins.text = session.Coins.ToString();
            _level.text = $"LVL {session.PlayerLevel}";
            var currentMeters = Mathf.FloorToInt(session.Distance);
            if (_carHealthBar != null)
                _carHealthBar.Tick(session.Hp01, Time.deltaTime, true);
            else
                SetBar01(_hp, session.Hp01);
            ApplyHpInvulnerabilityBlink(session.IsCarInvulnerable);
            SetBar01(_xp, session.Xp01);
            SetBar01(_distanceFill, session.Distance01);
            UpdateDistanceMarker(session.Distance01, currentMeters);

            if (session.State == GameState.WaitingForStart)
                _message.text = "Tap to start";
            else if (session.State == GameState.Running)
                _message.text = string.Empty;
        }

        public void ShowResult(bool win, int stars)
        {
            _message.text = win ? $"You win\n{new string('*', stars)}\nTap to restart" : "You lose\nTap to restart";
        }

        public void HideResult()
        {
            _upgradeRoot.SetActive(false);
            _message.text = "Tap to start";
        }

        public Task<UpgradeDefinition> ShowUpgradeChoices(UpgradeDefinition[] choices)
        {
            var completion = new TaskCompletionSource<UpgradeDefinition>();
            _upgradeRoot.SetActive(true);

            for (var i = 0; i < _upgradeButtons.Length; i++)
            {
                var index = i;
                if (_upgradeTexts != null && i < _upgradeTexts.Length && _upgradeTexts[i] != null)
                    _upgradeTexts[i].text = $"{choices[i].Title}\n{choices[i].Description}";
                ApplyUpgradeIcon(i, choices[i].Icon);
                _upgradeButtons[i].onClick.RemoveAllListeners();
                _upgradeButtons[i].onClick.AddListener(() =>
                {
                    _upgradeRoot.SetActive(false);
                    completion.TrySetResult(choices[index]);
                });
            }

            return completion.Task;
        }

        public static HudView Create()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var coins = CreateText(canvasGo.transform, "CoinsText", "0", new Vector2(18f, -18f), TextAnchor.UpperLeft, 28);
            var level = CreateText(canvasGo.transform, "LevelText", "LVL 1", new Vector2(-18f, -18f), TextAnchor.UpperRight, 26);
            var message = CreateText(canvasGo.transform, "MessageText", "Tap to start", Vector2.zero, TextAnchor.MiddleCenter, 42);
            Stretch(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(540f, 220f));

            var hp = CreateBar(canvasGo.transform, "CarHpBar", new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(260f, 24f), new Color(0.93f, 0.08f, 0.06f));
            var xp = CreateBar(canvasGo.transform, "XpBar", new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(330f, 18f), new Color(0.1f, 0.56f, 1f));
            var distanceFill = CreateBar(canvasGo.transform, "DistanceBar", new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(18f, 250f), new Color(1f, 0.78f, 0.12f));

            var upgradeRoot = new GameObject("UpgradePanel");
            upgradeRoot.transform.SetParent(canvasGo.transform, false);
            var bg = upgradeRoot.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.62f);
            Stretch(bg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(720f, 360f));

            var buttons = new Button[3];
            var texts = new TMP_Text[3];
            var icons = new Image[3];
            for (var i = 0; i < 3; i++)
            {
                var buttonGo = new GameObject($"UpgradeButton_{i + 1}");
                buttonGo.transform.SetParent(upgradeRoot.transform, false);
                var image = buttonGo.AddComponent<Image>();
                image.color = new Color(0.12f, 0.16f, 0.2f, 0.96f);
                buttons[i] = buttonGo.AddComponent<Button>();
                Stretch(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(200f, 210f));
                image.rectTransform.anchoredPosition = new Vector2((i - 1) * 230f, -20f);

                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(buttonGo.transform, false);
                icons[i] = iconGo.AddComponent<Image>();
                icons[i].preserveAspect = true;
                icons[i].raycastTarget = false;
                Stretch(icons[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(92f, 92f));
                icons[i].rectTransform.anchoredPosition = new Vector2(0f, 40f);

                texts[i] = CreateText(buttonGo.transform, "Text", string.Empty, new Vector2(0f, -58f), TextAnchor.MiddleCenter, 22);
                Stretch(texts[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(170f, 84f));
            }

            upgradeRoot.SetActive(false);
            return new HudView(coins, level, message, null, xp, distanceFill, null, null, null, upgradeRoot, buttons, texts, icons, hp);
        }

        private void ApplyUpgradeIcon(int index, Sprite icon)
        {
            if (_upgradeIcons == null || index >= _upgradeIcons.Length || _upgradeIcons[index] == null)
                return;

            _upgradeIcons[index].sprite = icon;
            _upgradeIcons[index].enabled = icon != null;
            _upgradeIcons[index].preserveAspect = true;
        }

        private void EnsureDistanceMarker()
        {
            if (_distanceFill == null)
                return;

            var barRoot = _distanceFill.transform.parent as RectTransform;
            if (barRoot == null)
                return;

            if (_distanceMarkerRoot == barRoot || _distanceMarkerRoot == _distanceFill.rectTransform)
                _distanceMarkerRoot = ResolveSceneDistanceMarkerRoot(barRoot);

            if (_distanceMarkerRoot != null)
            {
                _ownsDistanceMarker = _allowRuntimeDistanceMarkerCreation;
                return;
            }

            if (!_allowRuntimeDistanceMarkerCreation)
            {
                _canMoveAssignedDistanceMarker = false;
                Debug.LogError("Distance marker root is not assigned correctly. Use an existing scene RectTransform that contains the marker icon and text; do not assign DistanceBar or Fill.", _distanceFill);
                return;
            }

            CreateDistanceMarkerRoot(barRoot);
            EnsureDistanceMarkerTextAndIcon();
        }

        private void UpdateDistanceMarker(float progress, int currentMeters)
        {
            if (_distanceMarkerRoot == null || _distanceFill == null)
                return;

            progress = Mathf.Clamp01(progress);
            var barRoot = _distanceFill.transform.parent as RectTransform;
            if (barRoot == null)
                return;

            var vertical = barRoot.rect.height >= barRoot.rect.width;
            if (_ownsDistanceMarker)
                UpdateOwnedDistanceMarker(barRoot, vertical, progress);
            else
                UpdateAssignedDistanceMarker(barRoot, vertical, progress);

            if (_distanceMarkerText != null)
                _distanceMarkerText.text = $"{currentMeters}m";
        }

        private void UpdateOwnedDistanceMarker(RectTransform barRoot, bool vertical, float progress)
        {
            var width = barRoot.rect.width;
            var height = barRoot.rect.height;
            _distanceMarkerRoot.SetParent(barRoot, false);
            _distanceMarkerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _distanceMarkerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _distanceMarkerRoot.pivot = new Vector2(0.5f, 0.5f);
            _distanceMarkerRoot.localScale = Vector3.one;
            _distanceMarkerRoot.anchoredPosition = vertical
                ? new Vector2(0f, Mathf.Lerp(-height * 0.5f, height * 0.5f, progress))
                : new Vector2(Mathf.Lerp(-width * 0.5f, width * 0.5f, progress), 0f);
        }

        private void UpdateAssignedDistanceMarker(RectTransform barRoot, bool vertical, float progress)
        {
            if (!_canMoveAssignedDistanceMarker)
                return;

            var parent = _distanceMarkerRoot.parent as RectTransform;
            if (parent == null)
                return;

            var offset = GetBarProgressOffsetInParent(barRoot, parent, vertical, progress);
            _distanceMarkerRoot.anchoredPosition = _assignedDistanceMarkerStart + offset;
        }

        private void CacheDistanceMarkerLayout()
        {
            if (_distanceMarkerRoot == null || _distanceFill == null)
                return;

            _assignedDistanceMarkerStart = _distanceMarkerRoot.anchoredPosition;

            if (_ownsDistanceMarker)
                return;

            var barRoot = _distanceFill.transform.parent as RectTransform;
            if (barRoot == null)
                return;

            if (_distanceMarkerRoot == barRoot || _distanceMarkerRoot == _distanceFill.rectTransform)
            {
                _canMoveAssignedDistanceMarker = false;
                Debug.LogError("Distance marker root is assigned to DistanceBar or Fill. Assign the existing marker RectTransform from the scene instead.", _distanceMarkerRoot);
            }
        }

        private RectTransform ResolveSceneDistanceMarkerRoot(RectTransform barRoot)
        {
            var iconParent = _distanceMarkerIcon != null ? _distanceMarkerIcon.transform.parent as RectTransform : null;
            var textParent = _distanceMarkerText != null ? _distanceMarkerText.transform.parent as RectTransform : null;

            if (iconParent != null && iconParent == textParent && iconParent != barRoot && iconParent != _distanceFill.rectTransform)
                return iconParent;

            if (iconParent != null && iconParent != barRoot && iconParent != _distanceFill.rectTransform)
                return iconParent;

            if (textParent != null && textParent != barRoot && textParent != _distanceFill.rectTransform)
                return textParent;

            return null;
        }

        private void CreateDistanceMarkerRoot(RectTransform barRoot)
        {
            var markerGo = new GameObject("DistanceMarker");
            markerGo.transform.SetParent(barRoot, false);
            _ownsDistanceMarker = true;
            _canMoveAssignedDistanceMarker = true;
            _distanceMarkerRoot = markerGo.AddComponent<RectTransform>();
            _distanceMarkerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _distanceMarkerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _distanceMarkerRoot.pivot = new Vector2(0.5f, 0.5f);
            _distanceMarkerRoot.sizeDelta = new Vector2(96f, 34f);
            _distanceMarkerRoot.localScale = Vector3.one;
        }

        private void EnsureDistanceMarkerTextAndIcon()
        {
            if (_distanceMarkerText == null)
            {
                var text = CreateText(_distanceMarkerRoot, "DistanceMarkerText", "0m", new Vector2(-22f, 0f), TextAnchor.MiddleCenter, 18);
                text.rectTransform.sizeDelta = new Vector2(58f, 24f);
                text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                text.rectTransform.pivot = new Vector2(1f, 0.5f);
                text.alignment = TextAlignmentOptions.MidlineRight;
                _distanceMarkerText = text;
            }
            else if (!_distanceMarkerText.transform.IsChildOf(_distanceMarkerRoot))
            {
                _distanceMarkerText.transform.SetParent(_distanceMarkerRoot, true);
            }

            if (_distanceMarkerIcon == null)
            {
                var iconGo = new GameObject("DistanceMarkerIcon");
                iconGo.transform.SetParent(_distanceMarkerRoot, false);
                _distanceMarkerIcon = iconGo.AddComponent<Image>();
                _distanceMarkerIcon.color = new Color(1f, 0.86f, 0.12f, 1f);
                _distanceMarkerIcon.raycastTarget = false;
                var iconRect = _distanceMarkerIcon.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(24f, 24f);
            }
            else if (!_distanceMarkerIcon.transform.IsChildOf(_distanceMarkerRoot))
            {
                _distanceMarkerIcon.transform.SetParent(_distanceMarkerRoot, true);
            }
        }

        private static Vector2 GetBarProgressOffsetInParent(RectTransform barRoot, RectTransform markerParent, bool vertical, float progress)
        {
            var rect = barRoot.rect;
            var startLocal = vertical
                ? new Vector3(0f, rect.yMin, 0f)
                : new Vector3(rect.xMin, 0f, 0f);
            var endLocal = vertical
                ? new Vector3(0f, rect.yMax, 0f)
                : new Vector3(rect.xMax, 0f, 0f);

            var startInParent = markerParent.InverseTransformPoint(barRoot.TransformPoint(startLocal));
            var endInParent = markerParent.InverseTransformPoint(barRoot.TransformPoint(endLocal));
            var delta = endInParent - startInParent;
            return new Vector2(delta.x, delta.y) * progress;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, Vector2 position, TextAnchor alignment, int size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.alignment = ToTmpAlignment(alignment);
            text.fontSize = size;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.rectTransform.sizeDelta = new Vector2(270f, 84f);
            text.rectTransform.anchoredPosition = position;

            if (alignment == TextAnchor.UpperLeft)
            {
                text.rectTransform.anchorMin = new Vector2(0f, 1f);
                text.rectTransform.anchorMax = new Vector2(0f, 1f);
                text.rectTransform.pivot = new Vector2(0f, 1f);
            }
            else if (alignment == TextAnchor.UpperRight)
            {
                text.rectTransform.anchorMin = new Vector2(1f, 1f);
                text.rectTransform.anchorMax = new Vector2(1f, 1f);
                text.rectTransform.pivot = new Vector2(1f, 1f);
            }

            return text;
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                _ => TextAlignmentOptions.Center
            };
        }

        private static Image CreateBar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var bgGo = new GameObject(name);
            bgGo.transform.SetParent(parent, false);
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
            bg.rectTransform.anchorMin = anchor;
            bg.rectTransform.anchorMax = anchor;
            bg.rectTransform.pivot = anchor;
            bg.rectTransform.anchoredPosition = position;
            bg.rectTransform.sizeDelta = size;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Simple;
            fill.fillMethod = size.y > size.x ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.pivot = size.y > size.x ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = new Vector2(3f, 3f);
            fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
            return fill;
        }

        private static void SetBar01(Image fill, float value)
        {
            value = Mathf.Clamp01(value);
            var rect = fill.rectTransform;
            var vertical = rect.rect.height >= rect.rect.width;
            fill.type = Image.Type.Filled;
            fill.fillMethod = vertical ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
            fill.fillOrigin = vertical ? (int)Image.OriginVertical.Bottom : (int)Image.OriginHorizontal.Left;
            fill.fillAmount = value;
            rect.localScale = Vector3.one;
        }

        private void ApplyHpInvulnerabilityBlink(bool active)
        {
            if (_carHealthBar != null)
            {
                _carHealthBar.SetBlink(active, Time.unscaledTime);
                return;
            }

            if (_hp == null)
                return;

            if (!active)
            {
                _hp.color = _hpBaseColor;
                return;
            }

            var pulse = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 14f));
            _hp.color = Color.Lerp(_hpBaseColor, Color.white, pulse * 0.65f);
        }

        private static void Stretch(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
    }
}
