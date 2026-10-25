using DrawRush.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// Top-of-screen progress bar for free drawing: how much of the shape is painted. Builds its
    /// own bar at runtime (background, fill, percentage) so it needs no authored prefab; flashes
    /// red briefly when an enemy hit wipes fresh paint.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CoverageHud : MonoBehaviour
    {
        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Color barColor = new(0.2f, 0.95f, 0.6f, 1f);
        [SerializeField] private Color backColor = new(0f, 0f, 0f, 0.35f);
        [SerializeField] private Color lossColor = new(1f, 0.3f, 0.3f, 1f);
        [Tooltip("Bar size in canvas units.")]
        [SerializeField] private Vector2 size = new(560f, 34f);
        [Tooltip("Gap below the safe-area top, in canvas units.")]
        [SerializeField] private float topOffset = 150f;
        [Tooltip("How fast the shown value catches up with the real one (per second).")]
        [SerializeField] private float catchUp = 4f;

        private Image _fill;
        private TextMeshProUGUI _label;
        private float _shown, _target, _lossFlash;

        private void Awake()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(0f, -topOffset);

            var back = gameObject.AddComponent<Image>();
            back.color = backColor;
            back.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(transform, false);
            var frt = (RectTransform)fillGo.transform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = new Vector2(4f, 4f); frt.offsetMax = new Vector2(-4f, -4f);
            _fill = fillGo.GetComponent<Image>();
            _fill.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.color = barColor;
            _fill.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(transform, false);
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            _label = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) _label.font = font;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = size.y * 0.8f;
            _label.color = Color.white;
            _label.raycastTarget = false;
        }

        private void OnEnable()
        {
            if (painter == null) painter = FindFirstObjectByType<FreeDrawPainter>();
            if (painter == null) return;
            painter.CoverageChanged += OnCoverage;
            painter.PaintLost += OnPaintLost;
            _shown = _target = painter.Coverage;
        }

        private void OnDisable()
        {
            if (painter == null) return;
            painter.CoverageChanged -= OnCoverage;
            painter.PaintLost -= OnPaintLost;
        }

        private void OnCoverage(float value)
        {
            _target = value;
            if (value < _shown - 0.2f) _shown = value;   // a new level starts from zero, no slide down
        }

        private void OnPaintLost() => _lossFlash = 1f;

        private void Update()
        {
            _shown = Mathf.MoveTowards(_shown, _target, catchUp * Time.unscaledDeltaTime * Mathf.Max(0.05f, Mathf.Abs(_target - _shown)) * 4f);
            _lossFlash = Mathf.MoveTowards(_lossFlash, 0f, Time.unscaledDeltaTime * 2.5f);
            _fill.fillAmount = _shown;
            _fill.color = Color.Lerp(barColor, lossColor, _lossFlash);
            _label.text = Mathf.FloorToInt(_shown * 100f + 0.001f) + "%";
        }
    }
}
