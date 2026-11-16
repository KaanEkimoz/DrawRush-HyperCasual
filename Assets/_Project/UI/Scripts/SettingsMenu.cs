using DrawRush.Core;
using DrawRush.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// The settings menu: a round gear button under the music button opens a card with grouped
    /// rows — language (English / Türkçe / Español), switches for vibration and personalised ads, then the privacy policy and a two-tap level reset. The panel pauses the
    /// game while open, like the shop. Built at runtime so it needs no authored prefab.
    ///
    /// Changing the language updates this card at once; the rest of the screen is rebuilt (scene
    /// reload) when the card closes, so every runtime-built label picks up the new language.
    /// Also owns the first-launch ad consent card.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Bold, roomy font for the settings card (the thin hand font read cramped).")]
        [SerializeField] private TMP_FontAsset cardFont;
        [Tooltip("Gear button position from the top-left corner (canvas units), under the music button.")]
        [SerializeField] private Vector2 buttonOffset = new(30f, -286f);
        [SerializeField] private float buttonSize = 110f;
        [SerializeField] private Sprite buttonIcon;
        [SerializeField] private Color cardColor = new(0.13f, 0.14f, 0.24f, 0.98f);
        [SerializeField] private Color rowColor = new(1f, 1f, 1f, 0.06f);
        [SerializeField] private Color onColor = new(0.25f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color offColor = new(0.35f, 0.35f, 0.42f, 1f);
        [SerializeField] private Color switchOffColor = new(0.07f, 0.07f, 0.13f, 1f);
        [SerializeField] private Color accentColor = new(0.55f, 0.45f, 1f, 1f);
        [SerializeField] private Color dangerColor = new(0.9f, 0.45f, 0.15f, 1f);

        private const float CardWidth = 860f, RowWidth = 760f, RowHeight = 104f;

        private GameObject _panel;
        private RectTransform _card;
        private float _openedAt;
        private Switch _vibration, _ads;
        private readonly Image[] _langChips = new Image[3];
        private readonly TextMeshProUGUI[] _langLabels = new TextMeshProUGUI[3];
        private TextMeshProUGUI _title, _resetLabel, _policyLabel, _closeLabel, _langHeader, _gameHeader, _moreHeader;
        private float _resetArmedUntil;
        private Language _languageOnOpen;
        private GameObject _consentPanel;
        private System.Action<bool> _onConsent;

        private sealed class Switch
        {
            public TextMeshProUGUI Label, State;
            public Image Track;
            public RectTransform Knob;
            public string English;
        }

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

        // ---------- the card ----------

        private void BuildPanel()
        {
            _panel = NewUi("SettingsPanel", transform, typeof(Image), typeof(Button));
            var prt = (RectTransform)_panel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = prt.offsetMax = Vector2.zero;
            _panel.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.08f, 0.72f);   // dims the game, eats taps
            _panel.GetComponent<Button>().transition = Selectable.Transition.None;
            _panel.GetComponent<Button>().onClick.AddListener(Close);                       // tap outside closes

            var card = NewUi("Card", _panel.transform, typeof(Image), typeof(Button));
            _card = (RectTransform)card.transform;
            _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.5f);
            _card.sizeDelta = new Vector2(CardWidth, 1180f);
            RoundedSprite.Apply(card.GetComponent<Image>(), cardColor, 2f);
            card.GetComponent<Button>().transition = Selectable.Transition.None;   // swallow taps on the card

            // Header band.
            var band = NewUi("Header", card.transform, typeof(Image));
            var brt = (RectTransform)band.transform;
            brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = Vector2.one; brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 150f);
            RoundedSprite.Apply(band.GetComponent<Image>(), accentColor, 2f);
            _title = Label(card.transform, "", new Vector2(0f, 515f), new Vector2(700f, 120f), 76f);
            _title.fontStyle = FontStyles.Bold;

            var x = NewUi("X", card.transform, typeof(Image), typeof(Button));
            var xrt = (RectTransform)x.transform;
            xrt.anchorMin = xrt.anchorMax = new Vector2(0.5f, 0.5f);
            xrt.sizeDelta = new Vector2(96f, 96f); xrt.anchoredPosition = new Vector2(CardWidth * 0.5f - 70f, 515f);
            RoundedSprite.Apply(x.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.22f), 4f);
            x.GetComponent<Button>().onClick.AddListener(Close);
            x.AddComponent<ButtonJuice>();
            Label(x.transform, "X", Vector2.zero, new Vector2(96f, 96f), 54f).fontStyle = FontStyles.Bold;

            float y = 380f;
            _langHeader = Section(card.transform, ref y);
            BuildLanguageRow(card.transform, ref y);

            _gameHeader = Section(card.transform, ref y);
            _vibration = SwitchRow(card.transform, "Vibration", ref y, ToggleVibration);
            _ads = SwitchRow(card.transform, "Personalised ads", ref y, ToggleAdConsent);

            _moreHeader = Section(card.transform, ref y);
            _policyLabel = ActionRow(card.transform, ref y, OpenPrivacyPolicy, rowColor);
            _resetLabel = ActionRow(card.transform, ref y, ResetLevels, new Color(dangerColor.r, dangerColor.g, dangerColor.b, 0.85f));

            var close = NewUi("Close", card.transform, typeof(Image), typeof(Button));
            var clrt = (RectTransform)close.transform;
            clrt.anchorMin = clrt.anchorMax = new Vector2(0.5f, 0.5f);
            clrt.sizeDelta = new Vector2(440f, 120f);
            clrt.anchoredPosition = new Vector2(0f, -490f);
            RoundedSprite.Apply(close.GetComponent<Image>(), onColor, 1.4f);
            close.GetComponent<Button>().onClick.AddListener(Close);
            close.AddComponent<ButtonJuice>();
            _closeLabel = Label(close.transform, "", Vector2.zero, new Vector2(440f, 120f), 58f);
            _closeLabel.fontStyle = FontStyles.Bold;

            var version = Label(card.transform, "v" + Application.version, new Vector2(0f, -560f), new Vector2(400f, 40f), 28f);
            version.color = new Color(1f, 1f, 1f, 0.35f);
        }

        private TextMeshProUGUI Section(Transform parent, ref float y)
        {
            var t = Label(parent, "", new Vector2(0f, y), new Vector2(RowWidth - 20f, 50f), 30f);
            t.alignment = TextAlignmentOptions.Left;
            t.characterSpacing = 8f;
            t.color = new Color(1f, 1f, 1f, 0.45f);
            y -= 62f;
            return t;
        }

        private void BuildLanguageRow(Transform parent, ref float y)
        {
            var row = Row(parent, y, rowColor);
            float w = (RowWidth - 16f * 4) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var lang = (Language)i;
                var chip = NewUi("Lang_" + lang, row, typeof(Image), typeof(Button));
                var rt = (RectTransform)chip.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(w, RowHeight - 22f);
                rt.anchoredPosition = new Vector2(16f + w * 0.5f + i * (w + 16f), 0f);
                _langChips[i] = chip.GetComponent<Image>();
                RoundedSprite.Apply(_langChips[i], offColor, 1.2f);
                chip.GetComponent<Button>().onClick.AddListener(() => SetLanguage(lang));
                chip.AddComponent<ButtonJuice>();
                _langLabels[i] = Label(chip.transform, Loc.NameOf(lang), Vector2.zero, rt.sizeDelta, 40f);
            }
            y -= RowHeight + 14f;
        }

        private Switch SwitchRow(Transform parent, string english, ref float y, UnityEngine.Events.UnityAction onClick)
        {
            var row = Row(parent, y, rowColor);
            row.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
            var s = new Switch { English = english };
            s.Label = Label(row, "", Vector2.zero, Vector2.zero, 42f);
            Stretch(s.Label.rectTransform, 34f, 200f);
            s.Label.enableAutoSizing = true; s.Label.fontSizeMin = 30f; s.Label.fontSizeMax = 42f;
            s.Label.alignment = TextAlignmentOptions.Left;

            var track = NewUi("Track", row, typeof(Image));
            var trt = (RectTransform)track.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(1f, 0.5f);
            trt.sizeDelta = new Vector2(140f, 70f); trt.anchoredPosition = new Vector2(-100f, 0f);
            s.Track = track.GetComponent<Image>();
            RoundedSprite.Apply(s.Track, offColor, 3f);
            var knob = NewUi("Knob", track.transform, typeof(Image));
            s.Knob = (RectTransform)knob.transform;
            s.Knob.anchorMin = s.Knob.anchorMax = new Vector2(0.5f, 0.5f);
            s.Knob.sizeDelta = new Vector2(56f, 56f);
            RoundedSprite.Apply(knob.GetComponent<Image>(), Color.white, 3f);
            y -= RowHeight + 14f;
            return s;
        }

        private TextMeshProUGUI ActionRow(Transform parent, ref float y, UnityEngine.Events.UnityAction onClick, Color color)
        {
            var row = Row(parent, y, color);
            row.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
            row.gameObject.AddComponent<ButtonJuice>();
            var t = Label(row, "", Vector2.zero, Vector2.zero, 42f);
            Stretch(t.rectTransform, 24f, 24f);
            y -= RowHeight + 14f;
            return t;
        }

        private RectTransform Row(Transform parent, float y, Color color)
        {
            var go = NewUi("Row", parent, typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(RowWidth, RowHeight);
            rt.anchoredPosition = new Vector2(0f, y);
            RoundedSprite.Apply(go.GetComponent<Image>(), color, 1.4f);
            return rt;
        }

        // ---------- open / close ----------

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public void Open()
        {
            if (_panel == null || IsOpen) return;   // MenuPause counts opens: never open twice
            MenuPause.Open();
            _languageOnOpen = Loc.Current;
            _openedAt = Time.unscaledTime;
            Refresh();
            _panel.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen) return;
            _panel.SetActive(false);
            MenuPause.Close();
            if (Loc.Current != _languageOnOpen) Reload();   // rebuild every label in the new language
        }

        private void Update()
        {
            if (!IsOpen) return;
            float t = Time.unscaledTime - _openedAt;
            _card.localScale = Vector3.one * (t < 0.2f ? Mathf.SmoothStep(0.85f, 1f, t / 0.2f) : 1f);
            if (_resetArmedUntil > 0f && Time.unscaledTime > _resetArmedUntil) { _resetArmedUntil = 0f; Refresh(); }
        }

        private static void Reload()
        {
            MenuPause.SetGameTimeScale(1f);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        // ---------- actions ----------

        private void SetLanguage(Language l) { Loc.Current = l; Refresh(); }

        private void ToggleAdConsent() { DrawRush.Ads.AdConsent.Set(!DrawRush.Ads.AdConsent.Personalised); Refresh(); }

        private static void OpenPrivacyPolicy() => Application.OpenURL(DrawRush.Ads.AdConsent.PrivacyPolicyUrl);

        private void ToggleVibration() { Haptics.Enabled = !Haptics.Enabled; Refresh(); if (Haptics.Enabled) Haptics.Light(); }

        // Two taps (the second within 3 s): back to level 1 and reload, for tests and clean videos.
        private void ResetLevels()
        {
            if (Time.unscaledTime > _resetArmedUntil)
            {
                _resetArmedUntil = Time.unscaledTime + 3f;
                _resetLabel.text = Loc.T("Tap again to reset");
                return;
            }
            PlayerProgress.ResetLevelProgress();
            _panel.SetActive(false);
            MenuPause.Close();
            Reload();
        }

        private void Refresh()
        {
            _title.text = Loc.Upper(Loc.T("Settings"));
            _langHeader.text = Loc.Upper(Loc.T("Language"));
            _gameHeader.text = Loc.Upper(Loc.T("Game"));
            _moreHeader.text = Loc.Upper(Loc.T("More"));
            for (int i = 0; i < 3; i++)
            {
                bool on = (int)Loc.Current == i;
                _langChips[i].color = on ? accentColor : offColor;
                _langLabels[i].fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
            }
            SetSwitch(_vibration, Haptics.Enabled);
            SetSwitch(_ads, DrawRush.Ads.AdConsent.Personalised);
            _policyLabel.text = Loc.T("Privacy Policy");
            if (_resetArmedUntil <= 0f) _resetLabel.text = Loc.T("Reset levels");
            _closeLabel.text = Loc.Upper(Loc.T("Done"));
        }

        private void SetSwitch(Switch s, bool on)
        {
            s.Label.text = Loc.T(s.English);
            s.Track.color = on ? onColor : switchOffColor;
            s.Knob.anchoredPosition = new Vector2(on ? 34f : -34f, 0f);
        }

        // ---------- first-launch consent card ----------

        // GDPR regions only, see AdConsent. Both answers get the same size and weight: a lopsided
        // "Accept" button is exactly what regulators object to.
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
            RoundedSprite.Apply(card.GetComponent<Image>(), cardColor, 2f);

            Label(card.transform, Loc.Upper(Loc.T("Your privacy")), new Vector2(0f, 400f), new Vector2(800f, 100f), 66f);
            var body = Label(card.transform,
                Loc.T("Draw Rush is free thanks to ads.\n\nWith your OK, our ad partner (Unity Ads) may use your device's advertising ID to show ads that suit you. Say no and you still see ads, just less relevant ones.\n\nYou can change this any time in Settings."),
                new Vector2(0f, 90f), new Vector2(780f, 500f), 38f);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 6f;

            ConsentButton(card.transform, Loc.Upper(Loc.T("Accept")), new Vector2(0f, -250f), true);
            ConsentButton(card.transform, Loc.Upper(Loc.T("No, thanks")), new Vector2(0f, -380f), false);

            var link = NewUi("Policy", card.transform, typeof(Image), typeof(Button));
            var lrt = (RectTransform)link.transform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(500f, 70f);
            lrt.anchoredPosition = new Vector2(0f, -465f);
            link.GetComponent<Image>().color = Color.clear;
            link.GetComponent<Button>().onClick.AddListener(OpenPrivacyPolicy);
            var linkLabel = Label(link.transform, "<u>" + Loc.T("Privacy Policy") + "</u>", Vector2.zero, new Vector2(500f, 70f), 34f);
            linkLabel.color = new Color(0.75f, 0.85f, 1f, 1f);
        }

        private void ConsentButton(Transform parent, string text, Vector2 pos, bool granted)
        {
            var go = NewUi(text, parent, typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 110f);
            rt.anchoredPosition = pos;
            RoundedSprite.Apply(go.GetComponent<Image>(), onColor, 1.4f);
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

        // ---------- helpers ----------

        private static void Stretch(RectTransform rt, float left, float right)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, 0f); rt.offsetMax = new Vector2(-right, 0f);
        }

        private TextMeshProUGUI Label(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize)
        {
            var go = NewUi("Label", parent, typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var t = go.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset f = cardFont != null ? cardFont : font;
            if (f != null) t.font = f;
            t.text = text; t.fontSize = fontSize; t.alignment = TextAlignmentOptions.Center; t.color = Color.white; t.raycastTarget = false;
            t.characterSpacing = 2f;                       // a little air between letters
            t.textWrappingMode = TextWrappingModes.NoWrap;
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
