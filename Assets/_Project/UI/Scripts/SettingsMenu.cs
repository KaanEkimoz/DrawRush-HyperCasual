using DrawRush.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// A small settings menu for the two player-facing control options the redesign research
    /// asked for: vibration on/off and floating vs fixed joystick. A round gear button sits under
    /// the music button; the panel pauses the game while open, like the shop. Built at runtime so
    /// it needs no authored prefab.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Gear button position from the top-left corner (canvas units), under the music button.")]
        [SerializeField] private Vector2 buttonOffset = new(30f, -286f);
        [SerializeField] private float buttonSize = 110f;
        [SerializeField] private Sprite buttonIcon;
        [SerializeField] private Color cardColor = new(0.16f, 0.17f, 0.24f, 0.97f);
        [SerializeField] private Color onColor = new(0.25f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color offColor = new(0.35f, 0.35f, 0.4f, 1f);

        private GameObject _panel;
        private TextMeshProUGUI _vibrationLabel, _joystickLabel, _adsLabel;
        private Image _vibrationButton, _joystickButton, _adsButton;
        private GameObject _consentPanel;
        private System.Action<bool> _onConsent;

        private void Awake()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            BuildMenuButton();
            BuildPanel();
            _panel.SetActive(false);
            BuildConsentPanel();
            _consentPanel.SetActive(false);
        }

        private void BuildMenuButton()
        {
            var go = NewUi("SettingsButton", transform, typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = Vector2.one * buttonSize;
            rt.anchoredPosition = buttonOffset;
            var img = go.GetComponent<Image>();
            img.sprite = buttonIcon != null ? buttonIcon : Circle(64);
            go.GetComponent<Button>().onClick.AddListener(Open);
            go.AddComponent<ButtonJuice>();
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
            crt.sizeDelta = new Vector2(820f, 880f);
            card.GetComponent<Image>().color = cardColor;

            Label(card.transform, "SETTINGS", new Vector2(0f, 360f), new Vector2(760f, 100f), 72f);
            _vibrationButton = Toggle(card.transform, new Vector2(0f, 220f), ToggleVibration, out _vibrationLabel);
            _joystickButton = Toggle(card.transform, new Vector2(0f, 90f), ToggleJoystick, out _joystickLabel);
            _adsButton = Toggle(card.transform, new Vector2(0f, -40f), ToggleAdConsent, out _adsLabel);
            Toggle(card.transform, new Vector2(0f, -170f), OpenPrivacyPolicy, out TextMeshProUGUI policy).color = offColor;
            policy.text = "PRIVACY POLICY";

            var close = NewUi("Close", card.transform, typeof(Image), typeof(Button));
            var clrt = (RectTransform)close.transform;
            clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0.5f);
            clrt.sizeDelta = new Vector2(400f, 120f);
            clrt.anchoredPosition = new Vector2(0f, -330f);
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

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Open()
        {
            if (_panel == null || IsOpen) return;   // MenuPause counts opens: never open twice
            MenuPause.Open();
            Refresh();
            _panel.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _panel.SetActive(false);
            MenuPause.Close();
        }

        // First-launch consent card (GDPR regions only, see AdConsent). Both answers get the same
        // size and weight: a lopsided "Accept" button is exactly what regulators object to.
        private void BuildConsentPanel()
        {
            _consentPanel = NewUi("ConsentPanel", transform, typeof(Image));
            var prt = (RectTransform)_consentPanel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = prt.offsetMax = Vector2.zero;
            _consentPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var card = NewUi("Card", _consentPanel.transform, typeof(Image));
            var crt = (RectTransform)card.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(880f, 1000f);
            card.GetComponent<Image>().color = cardColor;

            Label(card.transform, "YOUR PRIVACY", new Vector2(0f, 400f), new Vector2(800f, 100f), 66f);
            var body = Label(card.transform,
                "Draw Rush is free thanks to ads.\n\nWith your OK, our ad partner (Unity Ads) may use your device's " +
                "advertising ID to show ads that suit you. Say no and you still see ads, just less relevant ones." +
                "\n\nYou can change this any time in Settings.",
                new Vector2(0f, 90f), new Vector2(780f, 500f), 38f);
            body.textWrappingMode = TextWrappingModes.Normal;

            ConsentButton(card.transform, "ACCEPT", new Vector2(0f, -250f), true);
            ConsentButton(card.transform, "NO, THANKS", new Vector2(0f, -380f), false);

            var link = NewUi("Policy", card.transform, typeof(Image), typeof(Button));
            var lrt = (RectTransform)link.transform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(500f, 70f);
            lrt.anchoredPosition = new Vector2(0f, -465f);
            link.GetComponent<Image>().color = Color.clear;
            link.GetComponent<Button>().onClick.AddListener(OpenPrivacyPolicy);
            var linkLabel = Label(link.transform, "<u>Privacy Policy</u>", Vector2.zero, new Vector2(500f, 70f), 34f);
            linkLabel.color = new Color(0.75f, 0.85f, 1f, 1f);
        }

        private void ConsentButton(Transform parent, string text, Vector2 pos, bool granted)
        {
            var go = NewUi(text, parent, typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 110f);
            rt.anchoredPosition = pos;
            go.GetComponent<Image>().color = onColor;
            go.GetComponent<Button>().onClick.AddListener(() => AnswerConsent(granted));
            go.AddComponent<ButtonJuice>();
            Label(go.transform, text, Vector2.zero, new Vector2(620f, 110f), 50f);
        }

        /// <summary>Show the first-launch consent card; <paramref name="onAnswer"/> gets the choice.
        /// The game stays paused behind it.</summary>
        public void AskConsent(System.Action<bool> onAnswer)
        {
            if (_consentPanel == null) { onAnswer?.Invoke(false); return; }
            _onConsent = onAnswer;
            if (!_consentPanel.activeSelf) { MenuPause.Open(); _consentPanel.SetActive(true); }
        }

        private void AnswerConsent(bool granted)
        {
            if (_consentPanel == null || !_consentPanel.activeSelf) return;
            _consentPanel.SetActive(false);
            MenuPause.Close();
            var answer = _onConsent;
            _onConsent = null;
            answer?.Invoke(granted);
        }

        private void ToggleAdConsent()
        {
            DrawRush.Ads.AdConsent.Set(!DrawRush.Ads.AdConsent.Personalised);
            Refresh();
        }

        private static void OpenPrivacyPolicy() => Application.OpenURL(DrawRush.Ads.AdConsent.PrivacyPolicyUrl);

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
            bool personal = DrawRush.Ads.AdConsent.Personalised;
            _adsLabel.text = personal ? "PERSONALISED ADS: ON" : "PERSONALISED ADS: OFF";
            _adsButton.color = personal ? onColor : offColor;
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
