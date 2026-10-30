using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.Boosters
{
    /// <summary>
    /// Top-centre rings for the running boosters: one radial timer per booster in its colour, with
    /// its initial inside and its name under it, draining as it runs out. Collecting a booster
    /// also flashes its name and what it does in the middle of the screen, so a new player learns
    /// each one the first time they grab it. Built at runtime; sits under the coverage bar.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoosterHud : MonoBehaviour
    {
        [SerializeField] private PlayerBoosts boosts;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private float ringSize = 84f;
        [SerializeField] private float spacing = 18f;
        [Tooltip("Gap below the safe-area top, in canvas units.")]
        [SerializeField] private float topOffset = 250f;

        [Header("Pickup announcement")]
        [Tooltip("Height of the announcement from the screen centre (canvas units).")]
        [SerializeField] private float toastY = 520f;
        [SerializeField] private float toastSeconds = 2.2f;

        [Tooltip("Text colour: ballpoint ink, readable on the paper ground.")]
        [SerializeField] private Color inkColor = new(0.12f, 0.16f, 0.42f, 1f);

        private sealed class Ring { public GameObject Root; public Image Fill; public TextMeshProUGUI Label, Title; public Image Icon; }
        private readonly List<Ring> _rings = new();
        private Sprite _circle;
        private TextMeshProUGUI _toastTitle, _toastHint;
        private float _toastUntil;

        private void Awake()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topOffset);
            rt.sizeDelta = new Vector2(ringSize * 3f, ringSize);
            _circle = MakeCircle(64);
            BuildToast();
        }

        private void OnEnable() { if (boosts != null) boosts.Granted += Announce; }
        private void OnDisable() { if (boosts != null) boosts.Granted -= Announce; }

        private void Announce(BoosterType type)
            => Toast(boosts.Config.TitleOf(type), boosts.Config.HintOf(type), boosts.Config.ColorOf(type));

        /// <summary>Big mid-screen announcement (boosters, hazards, the boss).</summary>
        public void Toast(string title, string hint, Color color)
        {
            _toastTitle.text = title;
            _toastTitle.color = color;
            _toastHint.text = hint;
            _toastUntil = Time.unscaledTime + toastSeconds;
        }

        private void BuildToast()
        {
            // Lives on the canvas root so it can sit mid-screen while this HUD hugs the top.
            Transform canvas = transform.parent != null ? transform.parent : transform;
            _toastTitle = MakeText(canvas, "BoosterToastTitle", 96f, new Vector2(0f, toastY));
            _toastHint = MakeText(canvas, "BoosterToastHint", 52f, new Vector2(0f, toastY - 100f));
            _toastTitle.alpha = _toastHint.alpha = 0f;
        }

        private TextMeshProUGUI MakeText(Transform parent, string name, float size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000f, size * 1.3f);
            rt.anchoredPosition = pos;
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.color = inkColor; t.raycastTarget = false;
            return t;
        }

        private void LateUpdate()
        {
            if (boosts == null) return;
            float left = _toastUntil - Time.unscaledTime;
            float fade = Mathf.Clamp01(left / 0.4f);   // hold, then fade over the last 0.4 s
            float pop = 1f + 0.25f * Mathf.Clamp01((left - (toastSeconds - 0.2f)) / 0.2f);   // quick pop-in
            _toastTitle.alpha = _toastHint.alpha = fade;
            _toastTitle.transform.localScale = Vector3.one * pop;
            var running = boosts.Running;
            while (_rings.Count < running.Count) _rings.Add(MakeRing());
            float width = running.Count * ringSize + Mathf.Max(0, running.Count - 1) * spacing;
            for (int i = 0; i < _rings.Count; i++)
            {
                Ring ring = _rings[i];
                bool on = i < running.Count;
                ring.Root.SetActive(on);
                if (!on) continue;
                var a = running[i];
                Color c = boosts.Config.ColorOf(a.Type);
                ring.Fill.color = c;
                ring.Fill.fillAmount = a.Length > 0f ? Mathf.Clamp01((a.Ends - Time.time) / a.Length) : 0f;
                Sprite icon = boosts.Config.IconOf(a.Type);
                ring.Icon.sprite = icon;
                ring.Icon.enabled = icon != null;
                ring.Label.text = icon != null ? "" : Initial(a.Type);
                ring.Title.text = boosts.Config.TitleOf(a.Type);
                ((RectTransform)ring.Root.transform).anchoredPosition = new Vector2(-width * 0.5f + ringSize * 0.5f + i * (ringSize + spacing), -ringSize * 0.5f);
            }
        }

        private static string Initial(BoosterType t) => t switch
        {
            BoosterType.Shield => "S", BoosterType.Freeze => "F", BoosterType.Speed => "H", _ => "W",
        };

        private Ring MakeRing()
        {
            var root = new GameObject("BoosterRing", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = Vector2.one * ringSize;
            var back = root.GetComponent<Image>();
            back.sprite = _circle; back.color = new Color(inkColor.r, inkColor.g, inkColor.b, 0.2f); back.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(root.transform, false);
            var frt = (RectTransform)fillGo.transform; frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = _circle; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top; fill.fillClockwise = false; fill.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(root.transform, false);
            var lrt = (RectTransform)labelGo.transform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.alignment = TextAlignmentOptions.Center; label.fontSize = ringSize * 0.5f; label.color = Color.white; label.raycastTarget = false;
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            var irt = (RectTransform)iconGo.transform; irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = irt.offsetMax = Vector2.one * (ringSize * 0.1f); irt.offsetMax = -irt.offsetMin;
            var icon = iconGo.GetComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(root.transform, false);
            var trt = (RectTransform)titleGo.transform;
            trt.anchorMin = new Vector2(0.5f, 0f); trt.anchorMax = new Vector2(0.5f, 0f); trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(ringSize * 2f, ringSize * 0.4f); trt.anchoredPosition = new Vector2(0f, -4f);
            var title = titleGo.GetComponent<TextMeshProUGUI>();
            if (font != null) title.font = font;
            title.alignment = TextAlignmentOptions.Center; title.fontSize = ringSize * 0.26f; title.color = inkColor; title.raycastTarget = false;
            return new Ring { Root = root, Fill = fill, Label = label, Title = title, Icon = icon };
        }

        private static Sprite MakeCircle(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), Vector2.one * 0.5f);
        }
    }
}
