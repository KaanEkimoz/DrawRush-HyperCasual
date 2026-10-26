using DrawRush.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// Top-of-screen progress bar for free drawing: how much of the shape is painted. Builds its
    /// own bar at runtime (background, fill, percentage) so it needs no authored prefab; flashes
    /// red briefly when an enemy hit wipes fresh paint. Gains pop up under the bar as "+N%"
    /// (the Paper.io pull of seeing each bit of progress land).
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
        [SerializeField] private Vector2 size = new(560f, 40f);
        [Tooltip("Gap below the safe-area top, in canvas units.")]
        [SerializeField] private float topOffset = 185f;
        [Tooltip("How fast the shown value catches up with the real one (per second).")]
        [SerializeField] private float catchUp = 4f;

        [Header("+N% popups")]
        [Tooltip("Gains are summed for this many seconds, then shown as one popup.")]
        [SerializeField] private float popupEvery = 0.5f;
        [Tooltip("Smallest gain (0-1) worth a popup.")]
        [SerializeField] private float popupMinGain = 0.01f;
        [Tooltip("Seconds a popup rises and fades.")]
        [SerializeField] private float popupSeconds = 0.9f;
        [Tooltip("How far (canvas units) a popup rises.")]
        [SerializeField] private float popupRise = 70f;
        [SerializeField] private Color popupColor = new(1f, 0.95f, 0.4f, 1f);

        private Image _fill;
        private TextMeshProUGUI _label;
        private float _shown, _target, _lossFlash;
        private float _gain, _gainTimer;

        private struct Popup { public TextMeshProUGUI Text; public float Age; public float X; }
        private readonly System.Collections.Generic.List<Popup> _popups = new();

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
            if (value > _target && _target >= 0f) _gain += value - _target;
            if (value < _target) _gain = 0f;   // a new level, or paint lost: start counting again
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
            TickPopups();
        }

        private void TickPopups()
        {
            float dt = Time.deltaTime;   // scaled: no popups pile up while the game is paused
            _gainTimer += dt;
            if (_gainTimer >= popupEvery)
            {
                _gainTimer = 0f;
                if (_gain >= popupMinGain && _target < 1f) Spawn(Mathf.RoundToInt(_gain * 100f));
                if (_gain >= popupMinGain) _gain = 0f;
            }
            for (int i = 0; i < _popups.Count; i++)
            {
                Popup p = _popups[i];
                if (!p.Text.gameObject.activeSelf) continue;
                p.Age += dt;
                float k = p.Age / popupSeconds;
                if (k >= 1f) { p.Text.gameObject.SetActive(false); _popups[i] = p; continue; }
                var rt = (RectTransform)p.Text.transform;
                rt.anchoredPosition = new Vector2(p.X, -size.y - 20f - popupRise * (1f - (1f - k) * (1f - k)));
                rt.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, (k - 0.15f) / 0.85f));
                var c = popupColor; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                p.Text.color = c;
                _popups[i] = p;
            }
        }

        private void Spawn(int percent)
        {
            if (percent <= 0) return;
            int slot = _popups.FindIndex(p => !p.Text.gameObject.activeSelf);
            if (slot < 0)
            {
                var go = new GameObject("Gain", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(240f, size.y * 1.6f);
                var t = go.GetComponent<TextMeshProUGUI>();
                if (font != null) t.font = font;
                t.alignment = TextAlignmentOptions.Center;
                t.fontSize = size.y * 1.1f;
                t.fontStyle = FontStyles.Bold;
                t.raycastTarget = false;
                _popups.Add(new Popup { Text = t });
                slot = _popups.Count - 1;
            }
            Popup p = _popups[slot];
            p.Age = 0f;
            p.X = Random.Range(-size.x * 0.25f, size.x * 0.25f);
            p.Text.text = "+" + percent + "%";
            p.Text.gameObject.SetActive(true);
            _popups[slot] = p;
        }
    }
}
