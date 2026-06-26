using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TestTask.UI
{
    public sealed class HudSceneReferences : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField, HideInInspector] private TMP_Text distanceText;
        [SerializeField] private TMP_Text messageText;

        [Header("Bars")]
        [SerializeField] private HealthBarView carHealthBar;
        [SerializeField] private Image xpFill;
        [SerializeField] private Image distanceFill;

        [Header("Distance Marker")]
        [Tooltip("Existing scene object that should move along the distance bar. Do not assign DistanceBar or Fill here.")]
        [SerializeField] private RectTransform distanceMarkerRoot;
        [Tooltip("Icon inside Distance Marker Root.")]
        [SerializeField] private Image distanceMarkerIcon;
        [Tooltip("TMP text inside Distance Marker Root.")]
        [SerializeField] private TMP_Text distanceMarkerText;

        [Header("Upgrade")]
        [SerializeField] private GameObject upgradeRoot;
        [SerializeField] private Button[] upgradeButtons;
        [SerializeField] private TMP_Text[] upgradeTexts;
        [FormerlySerializedAs("upgradeIcons")]
        [Tooltip("Image components inside upgrade buttons. Do not assign Sprite assets here; assign sprites in GameConfigAsset -> Upgrades -> Icon.")]
        [SerializeField] private Image[] upgradeIconImages;

        public HudView CreateView()
        {
            return new HudView(
                coinsText,
                levelText,
                messageText,
                carHealthBar,
                xpFill,
                distanceFill,
                distanceMarkerRoot,
                distanceMarkerIcon,
                distanceMarkerText,
                upgradeRoot,
                upgradeButtons,
                upgradeTexts,
                upgradeIconImages);
        }

        [ContextMenu("Auto Assign Distance Marker")]
        private void AutoAssignDistanceMarker()
        {
            if (distanceFill == null)
            {
                Debug.LogWarning("Assign Distance Fill first.", this);
                return;
            }

            var barRoot = distanceFill.transform.parent as RectTransform;
            if (barRoot == null)
            {
                Debug.LogWarning("Distance Fill must be a child of a RectTransform bar root.", this);
                return;
            }

            if (distanceMarkerRoot == null || distanceMarkerRoot == barRoot || distanceMarkerRoot == distanceFill.rectTransform)
                distanceMarkerRoot = FindMarkerRoot(barRoot);

            if (distanceMarkerRoot == null)
            {
                Debug.LogWarning("Could not find DistanceMarker. Create an existing scene RectTransform named 'DistanceMarker' near the DistanceBar, then run this again.", this);
                return;
            }

            if (distanceMarkerIcon == null)
                distanceMarkerIcon = distanceMarkerRoot.GetComponentInChildren<Image>(true);

            if (distanceMarkerText == null)
                distanceMarkerText = distanceMarkerRoot.GetComponentInChildren<TMP_Text>(true);

            Debug.Log("Distance marker references assigned.", this);
        }

        private RectTransform FindMarkerRoot(RectTransform barRoot)
        {
            var direct = barRoot.Find("DistanceMarker") as RectTransform;
            if (direct != null)
                return direct;

            var parent = barRoot.parent;
            if (parent == null)
                return null;

            var sibling = parent.Find("DistanceMarker") as RectTransform;
            if (sibling != null)
                return sibling;

            var allRects = parent.GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < allRects.Length; i++)
            {
                var rect = allRects[i];
                if (rect == barRoot || rect == distanceFill.rectTransform)
                    continue;

                if (rect.name.Contains("DistanceMarker"))
                    return rect;
            }

            return null;
        }
    }
}
