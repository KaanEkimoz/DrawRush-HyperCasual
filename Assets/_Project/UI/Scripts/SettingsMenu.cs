using DrawRush.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// A small settings menu for the two player-facing control options the redesign research
    /// asked for: vibration on/off and floating vs fixed joystick. A round menu button sits under
    /// the mute button; the panel pauses the game while open, like the shop. Built at runtime so
    /// it needs no authored prefab.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Menu button position from the top-right corner (canvas units), under the mute button.")]
        [SerializeField] private Vector2 buttonOffset = new(-34f, -290f);
        [SerializeField] private float buttonSize = 104f;
        [SerializeField] private Color buttonColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color iconColor = new(0.2f, 0.22f, 0.3f, 1f);
        [SerializeField] private Color cardColor = new(0.16f, 0.17f, 0.24f, 0.97f);
        [SerializeField] private Color onColor = new(0.25f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color offColor = new(0.35f, 0.35f, 0.4f, 1f);

        private GameObject _panel;
        private TextMeshProUGUI _vibrationLabel, _joystickLabel;
        private Image _vibrationButton, _joystickButton;

        private void Awake()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            BuildMenuButton();
            BuildPanel();
            _panel.SetActive(false);
        }

        private void BuildMenuButton()
        {
            var go = NewUi("SettingsButton", transform, typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = Vector2.one * buttonSize;
            rt.anchoredPosition = buttonOffset;
            var img = go.GetComponent<Image>();
            img.sprite = Circle(64);
            img.color = buttonColor;
            go.GetComponent<Button>().onClick.AddListener(Open);
            // A three-bar "menu" icon: no glyph needed from the font.
            for (int i = 0; i < 3; i++)
            {
                var bar = NewUi("Bar" + i, go.transform, typeof(Image));
                var brt = (RectTransform)bar.transform;
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = new Vector2(buttonSize * 0.46f, buttonSize * 0.08f);
                brt.anchoredPosition = new Vector2(0f, (1 - i) * buttonSize * 0.16f);
                var bi = bar.GetComponent<Image>();
                bi.color = iconColor;
                bi.raycastTarget = false;
            }
        }

        private void BuildPanel()
        {
            _panel = NewUi("SettingsPanel", transform, typeof(Image));
            var prt = (RectTransform)_panel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = prt.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);   // dims the game, eats taps

            var card = NewUi("Card", _panel.transform, typeof(Image));
            var crt = (RectTransform)card.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(820f, 620f);
            card.GetComponent<Image>().color = cardColor;

            Label(card.transform, "SETTINGS", new Vector2(0f, 230f), new Vector2(760f, 100f), 72f);
            _vibrationButton = Toggle(card.transform, new Vector2(0f, 90f), ToggleVibration, out _vibrationLabel);
            _joystickButton = Toggle(card.transform, new Vector2(0f, -40f), ToggleJoystick, out _joystickLabel);

            var close = NewUi("Close", card.transform, typeof(Image), typeof(Button));
            var clrt = (RectTransform)close.transform;
            clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0.5f);
            clrt.sizeDelta = new Vector2(400f, 120f);
            clrt.anchoredPosition = new Vector2(0f, -210f);
            close.GetComponent<Image>().color = new Color(0.9f, 0.25f, 0.25f, 1f);
            close.GetComponent<Button>().onClick.AddListener(Close);
            Label(close.transform, "CLOSE", Vector2.zero, new Vector2(400f, 120f), 56f);
        }

        private Image Toggle(Transform parent, Vector2 pos, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI label)
        {
            var go = NewUi("Toggle", parent, typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, 110f);
            rt.anchoredPosition = pos;
            go.GetComponent<Button>().onClick.AddListener(onClick);
            label = Label(go.transform, "", Vector2.zero, new Vector2(700f, 110f), 50f);
            return go.GetComponent<Image>();
        }

        public void Open()
        {
            MenuPause.Open();
            Refresh();
            _panel.SetActive(true);
        }

        public void Close()
        {
            _panel.SetActive(false);
            MenuPause.Close();
        }

        private void ToggleVibration() { Haptics.Enabled = !Haptics.Enabled; Refresh(); if (Haptics.Enabled) Haptics.Light(); }
        private void ToggleJoystick() { FloatingJoystick.FixedMode = !FloatingJoystick.FixedMode; Refresh(); }

        private void Refresh()
        {
            bool vib = Haptics.Enabled;
            _vibrationLabel.text = vib ? "VIBRATION: ON" : "VIBRATION: OFF";
            _vibrationButton.color = vib ? onColor : offColor;
            bool fixedStick = FloatingJoystick.FixedMode;
            _joystickLabel.text = fixedStick ? "JOYSTICK: FIXED" : "JOYSTICK: FLOATING";
            _joystickButton.color = onColor;
        }

        private TextMeshProUGUI Label(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize)
        {
            var go = NewUi("Label", parent, typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.fontSize = fontSize; t.alignment = TextAlignmentOptions.Center; t.color = Color.white; t.raycastTarget = false;
            return t;
        }

        private static GameObject NewUi(string name, Transform parent, params System.Type[] components)
        {
            var types = new System.Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            components.CopyTo(types, 1);
            var go = new GameObject(name, types);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static Sprite Circle(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)))));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), Vector2.one * 0.5f);
        }
    }
}
