using TestTask.Bootstrap;
using TestTask.Config;
using TestTask.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TestTask.Editor
{
    public static class SceneReferenceBuilder
    {
        private const string ConfigPath = "Assets/_Project/Config/GameConfig.asset";

        [MenuItem("Tools/TestTask/Scene/Create Mobile HUD Canvas")]
        public static void CreateMobileHudCanvas()
        {
            var existing = Object.FindFirstObjectByType<HudSceneReferences>(FindObjectsInactive.Include);
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                Debug.Log("HUD canvas already exists in the scene.");
                return;
            }

            EnsureEventSystem();

            var canvasGo = new GameObject("[UI] HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            var refs = canvasGo.AddComponent<HudSceneReferences>();

            var safeArea = CreatePanel(canvasGo.transform, "SafeArea", Color.clear);
            StretchFull(safeArea.rectTransform);

            var coins = CreateText(safeArea.transform, "CoinsText", "0", 32, TextAnchor.UpperLeft);
            Pin(coins.rectTransform, new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(260f, 80f));

            var level = CreateText(safeArea.transform, "LevelText", "LVL 1", 30, TextAnchor.UpperRight);
            Pin(level.rectTransform, new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(260f, 80f));

            var message = CreateText(safeArea.transform, "MessageText", "Tap to start", 46, TextAnchor.MiddleCenter);
            Pin(message.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 260f));

            var hpFill = CreateBar(safeArea.transform, "CarHpBar", new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(430f, 34f), new Color(0.93f, 0.08f, 0.06f));
            var carHealthBar = hpFill.transform.parent.gameObject.AddComponent<HealthBarView>();
            ConfigureHealthBar(carHealthBar, hpFill);
            var xpFill = CreateBar(safeArea.transform, "XpBar", new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(540f, 26f), new Color(0.1f, 0.56f, 1f));
            var distanceFill = CreateBar(safeArea.transform, "DistanceBar", new Vector2(1f, 0.5f), new Vector2(-42f, 0f), new Vector2(24f, 420f), new Color(1f, 0.78f, 0.12f));
            var distanceMarker = CreateDistanceMarker(distanceFill.transform.parent);

            var upgradeRoot = CreatePanel(safeArea.transform, "UpgradePanel", new Color(0f, 0f, 0f, 0.62f));
            Pin(upgradeRoot.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 460f));

            var buttons = new Button[3];
            var texts = new TMP_Text[3];
            var icons = new Image[3];
            for (var i = 0; i < 3; i++)
            {
                var buttonGo = CreatePanel(upgradeRoot.transform, $"UpgradeButton_{i + 1}", new Color(0.12f, 0.16f, 0.2f, 0.96f));
                buttons[i] = buttonGo.gameObject.AddComponent<Button>();
                Pin(buttonGo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 280f, -20f), new Vector2(250f, 270f));

                icons[i] = CreatePanel(buttonGo.transform, "Icon", Color.white);
                icons[i].preserveAspect = true;
                icons[i].raycastTarget = false;
                icons[i].enabled = false;
                Pin(icons[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 48f), new Vector2(108f, 108f));

                texts[i] = CreateText(buttonGo.transform, "Text", string.Empty, 26, TextAnchor.MiddleCenter);
                Pin(texts[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -82f), new Vector2(210f, 92f));
            }

            upgradeRoot.gameObject.SetActive(false);
            AssignHudReferences(refs, coins, level, message, carHealthBar, xpFill, distanceFill, distanceMarker.root, distanceMarker.icon, distanceMarker.text, upgradeRoot.gameObject, buttons, texts, icons);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeObject = canvasGo;
        }

        [MenuItem("Tools/TestTask/Scene/Create Scene Reference From Current Config")]
        public static void CreateSceneReferenceFromCurrentConfig()
        {
            var config = Selection.activeObject as GameConfigAsset;
            if (config == null)
                config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(ConfigPath);

            if (config == null)
            {
                Debug.LogError("GameConfig.asset was not found. Select a GameConfigAsset or create one manually.");
                return;
            }

            var sceneReference = Object.FindFirstObjectByType<GameSceneReference>(FindObjectsInactive.Include);
            if (sceneReference == null)
                sceneReference = new GameObject("[SceneReference] Game").AddComponent<GameSceneReference>();

            var serializedReference = new SerializedObject(sceneReference);
            serializedReference.FindProperty("config").objectReferenceValue = config;
            serializedReference.ApplyModifiedPropertiesWithoutUndo();

            var bootstrap = Object.FindFirstObjectByType<GameBootstrapper>(FindObjectsInactive.Include);
            if (bootstrap == null)
                bootstrap = new GameObject("[Bootstrap] Game Entry Point").AddComponent<GameBootstrapper>();

            var serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("config").objectReferenceValue = null;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeObject = sceneReference.gameObject;
        }

        private static void AssignHudReferences(HudSceneReferences refs, TMP_Text coins, TMP_Text level, TMP_Text message, HealthBarView carHealthBar, Image xp, Image distanceFill, RectTransform distanceMarkerRoot, Image distanceMarkerIcon, TMP_Text distanceMarkerText, GameObject upgradeRoot, Button[] buttons, TMP_Text[] texts, Image[] icons)
        {
            var serialized = new SerializedObject(refs);
            serialized.FindProperty("coinsText").objectReferenceValue = coins;
            serialized.FindProperty("levelText").objectReferenceValue = level;
            serialized.FindProperty("distanceText").objectReferenceValue = null;
            serialized.FindProperty("messageText").objectReferenceValue = message;
            serialized.FindProperty("carHealthBar").objectReferenceValue = carHealthBar;
            serialized.FindProperty("xpFill").objectReferenceValue = xp;
            serialized.FindProperty("distanceFill").objectReferenceValue = distanceFill;
            serialized.FindProperty("distanceMarkerRoot").objectReferenceValue = distanceMarkerRoot;
            serialized.FindProperty("distanceMarkerIcon").objectReferenceValue = distanceMarkerIcon;
            serialized.FindProperty("distanceMarkerText").objectReferenceValue = distanceMarkerText;
            serialized.FindProperty("upgradeRoot").objectReferenceValue = upgradeRoot;
            AssignArray(serialized.FindProperty("upgradeButtons"), buttons);
            AssignArray(serialized.FindProperty("upgradeTexts"), texts);
            AssignArray(serialized.FindProperty("upgradeIconImages"), icons);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static Image CreateBar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var bg = CreatePanel(parent, name, new Color(0.02f, 0.03f, 0.05f, 0.82f));
            Pin(bg.rectTransform, anchor, position, size);

            var fill = CreatePanel(bg.transform, "Fill", color);
            fill.type = Image.Type.Simple;
            fill.fillMethod = size.y > size.x ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.pivot = size.y > size.x ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
            fill.rectTransform.offsetMin = new Vector2(4f, 4f);
            fill.rectTransform.offsetMax = new Vector2(-4f, -4f);
            return fill;
        }

        private static void ConfigureHealthBar(HealthBarView healthBar, Image fill)
        {
            var serialized = new SerializedObject(healthBar);
            serialized.FindProperty("root").objectReferenceValue = fill.transform.parent;
            serialized.FindProperty("fill").objectReferenceValue = fill;
            serialized.FindProperty("delayFill").objectReferenceValue = null;
            serialized.FindProperty("alwaysVisible").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static (RectTransform root, Image icon, TMP_Text text) CreateDistanceMarker(Transform parent)
        {
            var rootGo = new GameObject("DistanceMarker");
            rootGo.transform.SetParent(parent, false);
            var root = rootGo.AddComponent<RectTransform>();
            Pin(root, new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(96f, 34f));

            var text = CreateText(rootGo.transform, "DistanceMarkerText", "0m", 22, TextAnchor.MiddleCenter);
            text.alignment = TextAlignmentOptions.MidlineRight;
            Pin(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-20f, 0f), new Vector2(64f, 28f));
            text.rectTransform.pivot = new Vector2(1f, 0.5f);

            var icon = CreatePanel(rootGo.transform, "DistanceMarkerIcon", new Color(1f, 0.86f, 0.12f, 1f));
            Pin(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
            icon.raycastTarget = false;
            return (root, icon, text);
        }

        private static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = ToTmpAlignment(alignment);
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
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

        private static void Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
