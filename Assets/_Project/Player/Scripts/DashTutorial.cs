using DrawRush.Boosters;
using DrawRush.Core;
using DrawRush.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.Player
{
    /// <summary>
    /// Teaching the dash, in two steps. Right after the first Dash purchase the shop shows
    /// <see cref="DashUnlockedPopup"/>: a card with the icon, an animated double-tap and what it is
    /// for. The next level then runs <see cref="DashPractice"/>: time slows, a prompt asks for a
    /// double-tap, and the first real dash is cheered before time comes back.
    /// </summary>
    public static class DashTutorial
    {
        private const string PracticeKey = "DashPracticePending";

        /// <summary>True when the next level should open with the dash practice.</summary>
        public static bool PracticePending
        {
            get => PlayerPrefs.GetInt(PracticeKey, 0) == 1;
            set { PlayerPrefs.SetInt(PracticeKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static string HowTo => Application.isMobilePlatform
            ? "Double-tap anywhere to dash"
            : "Double-tap or press Space to dash";

        internal static RectTransform Child(Transform parent, string name, params System.Type[] components)
        {
            var types = new System.Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            components.CopyTo(types, 1);
            var go = new GameObject(name, types);
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        internal static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, string text, float size, Color color)
        {
            var rt = Child(parent, "Text", typeof(TextMeshProUGUI));
            var t = rt.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A finger dot with two ripples: the double-tap, drawn in UI.</summary>
        internal static DoubleTapDemo Demo(Transform parent, Color color, float size)
        {
            var rt = Child(parent, "DoubleTap");
            rt.sizeDelta = new Vector2(size, size);
            var demo = rt.gameObject.AddComponent<DoubleTapDemo>();
            demo.Build(color, size);
            return demo;
        }
    }

    /// <summary>Loops a double-tap: two quick presses, each throwing a ring, then a rest.</summary>
    public sealed class DoubleTapDemo : MonoBehaviour
    {
        private const float Cycle = 1.4f;
        private RectTransform _dot;
        private Image _dotImg;
        private readonly RectTransform[] _rings = new RectTransform[2];
        private readonly Image[] _ringImgs = new Image[2];
        private float _size;

        public void Build(Color color, float size)
        {
            _size = size;
            for (int i = 0; i < 2; i++)
            {
                _rings[i] = DashTutorial.Child(transform, "Ring", typeof(Image));
                _ringImgs[i] = _rings[i].GetComponent<Image>();
                _ringImgs[i].sprite = RingSprite.Get();
                _ringImgs[i].color = color;
                _ringImgs[i].raycastTarget = false;
            }
            _dot = DashTutorial.Child(transform, "Finger", typeof(Image));
            _dotImg = _dot.GetComponent<Image>();
            RoundedSprite.Apply(_dotImg, Color.white, 4f);
            _dotImg.raycastTarget = false;
            _dot.sizeDelta = Vector2.one * size * 0.32f;
        }

        private void Update()
        {
            float t = Time.unscaledTime % Cycle;
            // Presses at 0 and 0.22 s.
            float press = Mathf.Max(Pulse(t, 0f), Pulse(t, 0.22f));
            _dot.localScale = Vector3.one * (1f - 0.25f * press);
            for (int i = 0; i < 2; i++)
            {
                float age = t - i * 0.22f;
                bool on = age >= 0f && age < 0.7f;
                float k = on ? age / 0.7f : 1f;
                _rings[i].sizeDelta = Vector2.one * _size * Mathf.Lerp(0.3f, 1f, k);
                Color c = _ringImgs[i].color; c.a = on ? 1f - k : 0f; _ringImgs[i].color = c;
            }
        }

        private static float Pulse(float t, float at)
        {
            float d = t - at;
            return d >= 0f && d < 0.12f ? Mathf.Sin(d / 0.12f * Mathf.PI) : 0f;
        }
    }

    /// <summary>A white ring sprite made in code (for the double-tap ripples).</summary>
    internal static class RingSprite
    {
        private static Sprite _sprite;
        public static Sprite Get()
        {
            if (_sprite != null) return _sprite;
            const int n = 128; const float outer = 62f, width = 7f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                float a = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - (outer - width) + 0.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px); tex.Apply(false, true);
            _sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            return _sprite;
        }
    }

    /// <summary>
    /// The card shown over the shop after the first Dash purchase. Tapping "Got it" closes it and
    /// arms the practice for the next level.
    /// </summary>
    public sealed class DashUnlockedPopup : MonoBehaviour
    {
        private RectTransform _card, _icon;
        private float _openedAt;

        public static void Show(Transform shop, TMP_FontAsset font, Sprite icon, Color accent)
        {
            DashTutorial.PracticePending = true;
            Canvas canvas = shop.GetComponentInParent<Canvas>();
            Transform root = canvas != null ? canvas.rootCanvas.transform : shop;

            var overlay = DashTutorial.Child(root, "DashUnlockedPopup", typeof(Image));
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.1f, 0.78f);   // blocks taps
            overlay.SetAsLastSibling();
            var popup = overlay.gameObject.AddComponent<DashUnlockedPopup>();
            popup.Build(font, icon, accent);
        }

        private void Build(TMP_FontAsset font, Sprite icon, Color accent)
        {
            _openedAt = Time.unscaledTime;
            _card = DashTutorial.Child(transform, "Card", typeof(Image));
            _card.sizeDelta = new Vector2(840f, 1000f);
            RoundedSprite.Apply(_card.GetComponent<Image>(), new Color(0.13f, 0.14f, 0.24f, 1f), 1.6f);

            var band = DashTutorial.Child(_card, "Band", typeof(Image));
            band.anchorMin = new Vector2(0f, 1f); band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(0.5f, 1f); band.sizeDelta = new Vector2(0f, 150f);
            RoundedSprite.Apply(band.GetComponent<Image>(), accent, 1.6f);

            var title = DashTutorial.Text(_card, font, "DASH UNLOCKED!", 84f, Color.white);
            title.rectTransform.anchorMin = new Vector2(0f, 1f); title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.sizeDelta = new Vector2(0f, 150f); title.rectTransform.anchoredPosition = new Vector2(0f, -75f);
            title.fontStyle = FontStyles.Bold;

            _icon = DashTutorial.Child(_card, "Icon", typeof(Image));
            _icon.sizeDelta = new Vector2(230f, 230f); _icon.anchoredPosition = new Vector2(-150f, 190f);
            var iconImg = _icon.GetComponent<Image>(); iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            if (icon != null) iconImg.sprite = icon; else RoundedSprite.Apply(iconImg, accent, 4f);

            var demo = DashTutorial.Demo(_card, accent, 230f);
            ((RectTransform)demo.transform).anchoredPosition = new Vector2(150f, 190f);

            var how = DashTutorial.Text(_card, font, DashTutorial.HowTo, 56f, Color.Lerp(accent, Color.white, 0.4f));
            how.rectTransform.sizeDelta = new Vector2(760f, 130f); how.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            how.fontStyle = FontStyles.Bold;

            var why = DashTutorial.Text(_card, font,
                "Dart past enemies, saws and spikes.\nYou can't be hurt mid-dash.\nUpgrades: further, more often.",
                42f, new Color(1f, 1f, 1f, 0.85f));
            why.rectTransform.sizeDelta = new Vector2(760f, 190f); why.rectTransform.anchoredPosition = new Vector2(0f, -195f);

            var btn = DashTutorial.Child(_card, "GotIt", typeof(Image), typeof(Button));
            btn.anchorMin = btn.anchorMax = new Vector2(0.5f, 0f);
            btn.sizeDelta = new Vector2(460f, 120f); btn.anchoredPosition = new Vector2(0f, 105f);
            RoundedSprite.Apply(btn.GetComponent<Image>(), new Color(0.25f, 0.8f, 0.4f, 1f), 1.2f);
            var label = DashTutorial.Text(btn, font, "GOT IT!", 60f, Color.white);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.fontStyle = FontStyles.Bold;
            btn.GetComponent<Button>().onClick.AddListener(() => Destroy(gameObject));
            Haptics.Medium();
        }

        private void Update()
        {
            float t = Time.unscaledTime - _openedAt;
            float pop = t < 0.25f ? Mathf.SmoothStep(0.6f, 1f, t / 0.25f) + Mathf.Sin(t / 0.25f * Mathf.PI) * 0.06f : 1f;
            _card.localScale = Vector3.one * pop;
            _icon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 2.4f) * 6f);
        }
    }

    /// <summary>
    /// The in-level practice after buying the dash: once the countdown is over, time slows and a
    /// prompt asks for a double-tap; the first dash (or a timeout) ends it.
    /// </summary>
    public sealed class DashPractice : MonoBehaviour
    {
        private const float SlowScale = 0.35f;
        private const float GiveUpSeconds = 8f;
        private GameObject _ui;
        private float _shownAt = -1f;
        private bool _done;

        public static void Begin(GameObject host) => host.AddComponent<DashPractice>();

        private void OnEnable() => PlayerDash.Dashed += OnDashed;
        private void OnDisable() => PlayerDash.Dashed -= OnDashed;

        private void Update()
        {
            if (_done) return;
            var state = GameServices.State;
            if (state != null && (state.IsGameWon || state.IsFinishing)) { Finish(false); return; }
            if (_shownAt < 0f)
            {
                // Wait for the 3-2-1 countdown (time stopped) and any open menu.
                if (Time.timeScale <= 0f || MenuPause.IsOpen) return;
                Show();
                return;
            }
            if (Time.unscaledTime - _shownAt > GiveUpSeconds) Finish(false);
        }

        private void Show()
        {
            _shownAt = Time.unscaledTime;
            var hud = FindFirstObjectByType<BoosterHud>();
            Transform canvas = hud != null && hud.transform.parent != null ? hud.transform.parent : null;
            if (canvas == null) { Finish(false); return; }
            TMP_FontAsset font = hud.Font;
            Color accent = new(0.55f, 0.45f, 1f, 1f);

            var root = DashTutorial.Child(canvas, "DashPractice");
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(0f, -330f);
            _ui = root.gameObject;

            var pill = DashTutorial.Child(root, "Pill", typeof(Image));
            pill.sizeDelta = new Vector2(860f, 300f);
            RoundedSprite.Apply(pill.GetComponent<Image>(), new Color(0.1f, 0.1f, 0.2f, 0.82f), 2.4f);
            pill.GetComponent<Image>().raycastTarget = false;

            var title = DashTutorial.Text(root, font, "TRY YOUR DASH!", 76f, Color.white);
            title.rectTransform.sizeDelta = new Vector2(820f, 100f); title.rectTransform.anchoredPosition = new Vector2(70f, 60f);
            title.fontStyle = FontStyles.Bold;
            var how = DashTutorial.Text(root, font, DashTutorial.HowTo, 46f, Color.Lerp(accent, Color.white, 0.45f));
            how.rectTransform.sizeDelta = new Vector2(620f, 70f); how.rectTransform.anchoredPosition = new Vector2(90f, -40f);

            var demo = DashTutorial.Demo(root, accent, 200f);
            ((RectTransform)demo.transform).anchoredPosition = new Vector2(-300f, -5f);

            MenuPause.SetGameTimeScale(SlowScale);
        }

        private void OnDashed()
        {
            if (_shownAt < 0f || _done) return;
            Finish(true);
        }

        private void Finish(bool dashed)
        {
            _done = true;
            DashTutorial.PracticePending = false;
            if (_ui != null) Destroy(_ui);
            if (_shownAt >= 0f && Mathf.Approximately(Time.timeScale, SlowScale)) MenuPause.SetGameTimeScale(1f);
            if (dashed)
            {
                var hud = FindFirstObjectByType<BoosterHud>();
                if (hud != null) hud.Toast("NICE DASH!", "Use it to escape danger", new Color(0.55f, 0.45f, 1f));
            }
            Destroy(this);
        }

        private void OnDestroy()
        {
            if (_ui != null) Destroy(_ui);
        }
    }
}
