using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Player;
using DrawRush.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.Boosters
{
    /// <summary>
    /// The upgrade half of the coin shop, under the skin grid: one card per hero stat and the
    /// Dash skill. Each card shows the stat's doodle icon, what it gives now, a pip per level and a
    /// coin button with the next price (green when affordable, grey when not, gold MAX at the top).
    /// A purchase pops the card and its icon. The booster and helper rows are parked behind
    /// <see cref="showBoosters"/>; what was already bought keeps working.
    ///
    /// Buying the Dash for the first time opens <see cref="DashUnlockedPopup"/>, the dash tutorial.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UpgradeShop : MonoBehaviour
    {
        [SerializeField] private BoosterConfig config;
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Also list the booster, pet and buddy upgrades under the stats.")]
        [SerializeField] private bool showBoosters;
        [SerializeField] private Color speedColor = new(1f, 0.75f, 0.2f, 1f);
        [SerializeField] private Color healthColor = new(0.95f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color drawAreaColor = new(0.35f, 0.7f, 1f, 1f);
        [SerializeField] private Color dashColor = new(0.55f, 0.45f, 1f, 1f);
        [Header("Icons")]
        [SerializeField] private Sprite speedIcon;
        [SerializeField] private Sprite healthIcon;
        [SerializeField] private Sprite drawAreaIcon;
        [SerializeField] private Sprite dashIcon;
        [SerializeField] private Sprite coinIcon;
        [Tooltip("List area inside the shop panel, from its centre (canvas units): between the " +
                 "skin grid (bottom at -250) and the Close button (top near -770).")]
        [SerializeField] private Vector2 areaPosition = new(0f, -470f);
        [SerializeField] private Vector2 areaSize = new(900f, 430f);
        [SerializeField] private float rowHeight = 108f;
        [SerializeField] private Color rowColor = new(0.14f, 0.15f, 0.24f, 0.96f);
        [SerializeField] private Color buyColor = new(0.25f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color cantColor = new(0.33f, 0.34f, 0.42f, 1f);
        [SerializeField] private Color maxColor = new(1f, 0.78f, 0.25f, 1f);
        [SerializeField] private Color pipOffColor = new(1f, 1f, 1f, 0.16f);
        [SerializeField] private Color petColor = new(1f, 0.55f, 0.75f, 1f);
        [SerializeField] private Color buddyColor = new(0.3f, 0.9f, 0.85f, 1f);

        private sealed class Row
        {
            public RectTransform Card, Icon;
            public TextMeshProUGUI Value, Price;
            public Image Button, CoinImage;
            public Color Accent;
            public readonly List<Image> Pips = new();
            public System.Func<string> ValueText;
            public System.Func<int> Level, Price01;
            public System.Func<bool> Buy;
            public float PopUntil;
        }

        private const float PopSeconds = 0.28f;
        private readonly List<Row> _rows = new();
        private RectTransform _area;

        private void Awake()
        {
            var go = new GameObject("Upgrades", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _area = (RectTransform)go.transform;
            _area.anchorMin = _area.anchorMax = new Vector2(0.5f, 0.5f);
            _area.sizeDelta = areaSize;
            _area.anchoredPosition = areaPosition;

            AddStat("Speed", speedColor, speedIcon, PlayerStat.Speed);
            AddStat("Health", healthColor, healthIcon, PlayerStat.Health);
            AddStat("Draw Area", drawAreaColor, drawAreaIcon, PlayerStat.DrawArea);
            AddRow("Dash", dashColor, dashIcon, DashSkill.Describe, () => DashSkill.Level, DashSkill.MaxLevel,
                () => DashSkill.NextPrice, BuyDash);
            if (!showBoosters) return;

            foreach (BoosterType t in new[] { BoosterType.Shield, BoosterType.Freeze, BoosterType.Speed, BoosterType.Shockwave })
            {
                BoosterType type = t;
                AddRow(type.ToString(), config != null ? config.ColorOf(type) : Color.white, null,
                    () => $"Lv {BoosterUpgrades.Level(type)}", () => BoosterUpgrades.Level(type), BoosterUpgrades.MaxLevel,
                    () => BoosterUpgrades.NextPrice(type), () => BoosterUpgrades.TryUpgrade(type));
            }
            AddRow("Bump Pet", petColor, null,
                () => BoosterUpgrades.PetLevel == 0 ? "Not owned" : $"Lv {BoosterUpgrades.PetLevel}",
                () => BoosterUpgrades.PetLevel, BoosterUpgrades.PetMaxLevel,
                () => BoosterUpgrades.PetNextPrice, BoosterUpgrades.TryUpgradePet);
            AddRow("Brush Buddy", buddyColor, null,
                () => BoosterUpgrades.BuddyLevel == 0 ? "Not owned" : $"Lv {BoosterUpgrades.BuddyLevel}",
                () => BoosterUpgrades.BuddyLevel, BoosterUpgrades.BuddyMaxLevel,
                () => BoosterUpgrades.BuddyNextPrice, BoosterUpgrades.TryUpgradeBuddy);
        }

        private void AddStat(string title, Color accent, Sprite icon, PlayerStat stat) =>
            AddRow(title, accent, icon, () => StatUpgrades.Describe(stat), () => StatUpgrades.Level(stat),
                StatUpgrades.MaxLevel(stat), () => StatUpgrades.NextPrice(stat), () => StatUpgrades.TryUpgrade(stat));

        // The first Dash purchase also teaches it.
        private bool BuyDash()
        {
            bool first = !DashSkill.Owned;
            if (!DashSkill.TryUpgrade()) return false;
            if (first) DashUnlockedPopup.Show(transform, font, dashIcon, dashColor);
            return true;
        }

        private void OnEnable()
        {
            StatUpgrades.Changed += Refresh;
            DashSkill.Changed += Refresh;
            BoosterUpgrades.Changed += Refresh;
            PlayerProgress.CoinsChanged += OnCoins;
            Refresh();
        }

        private void OnDisable()
        {
            StatUpgrades.Changed -= Refresh;
            DashSkill.Changed -= Refresh;
            BoosterUpgrades.Changed -= Refresh;
            PlayerProgress.CoinsChanged -= OnCoins;
        }

        private void OnCoins(int _) => Refresh();

        private void AddRow(string title, Color accent, Sprite icon, System.Func<string> value,
            System.Func<int> level, int maxLevel, System.Func<int> price, System.Func<bool> buy)
        {
            int i = _rows.Count;
            var card = new GameObject(title, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_area, false);
            var rt = (RectTransform)card.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
            float h = rowHeight - 12f;
            rt.sizeDelta = new Vector2(0f, h);
            rt.anchoredPosition = new Vector2(0f, -i * rowHeight - h * 0.5f);
            RoundedSprite.Apply(card.GetComponent<Image>(), rowColor);

            // A thin accent stripe along the card's left edge.
            var stripe = Child(card.transform, "Stripe", typeof(Image));
            stripe.anchorMin = new Vector2(0f, 0.18f); stripe.anchorMax = new Vector2(0f, 0.82f);
            stripe.sizeDelta = new Vector2(8f, 0f); stripe.anchoredPosition = new Vector2(6f, 0f);
            RoundedSprite.Apply(stripe.GetComponent<Image>(), accent, 0.2f);

            var r = new Row { Card = rt, Accent = accent, ValueText = value, Level = level, Price01 = price, Buy = buy };

            // Icon: the doodle badge, or a plain accent disc when a row has none.
            float iconSize = h - 14f;
            r.Icon = Child(card.transform, "Icon", typeof(Image));
            r.Icon.anchorMin = r.Icon.anchorMax = new Vector2(0f, 0.5f);
            r.Icon.sizeDelta = new Vector2(iconSize, iconSize);
            r.Icon.anchoredPosition = new Vector2(22f + iconSize * 0.5f, 0f);
            var iconImg = r.Icon.GetComponent<Image>();
            iconImg.preserveAspect = true; iconImg.raycastTarget = false;
            if (icon != null) iconImg.sprite = icon;
            else RoundedSprite.Apply(iconImg, accent, 3f);

            float textLeft = 22f + iconSize + 18f;
            var name = Label(card.transform, title, h * 0.44f, TextAlignmentOptions.BottomLeft);
            Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.62f, 1f), textLeft, -4f);
            name.fontStyle = FontStyles.Bold;

            r.Value = Label(card.transform, "", h * 0.32f, TextAlignmentOptions.TopLeft);
            Place(r.Value.rectTransform, new Vector2(0f, 0f), new Vector2(0.42f, 0.5f), textLeft, 4f);
            r.Value.color = Color.Lerp(accent, Color.white, 0.25f);

            // One pip per level, after the value text.
            var pips = Child(card.transform, "Pips");
            pips.anchorMin = new Vector2(0.42f, 0.1f); pips.anchorMax = new Vector2(0.66f, 0.46f);
            pips.offsetMin = pips.offsetMax = Vector2.zero;
            var layout = pips.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f; layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            float pipW = Mathf.Min(34f, (areaSize.x * 0.24f - 6f * (maxLevel - 1)) / Mathf.Max(1, maxLevel));
            for (int p = 0; p < maxLevel; p++)
            {
                var pip = Child(pips, "Pip", typeof(Image));
                pip.sizeDelta = new Vector2(pipW, 16f);
                var img = pip.GetComponent<Image>();
                RoundedSprite.Apply(img, pipOffColor, 0.3f);
                img.raycastTarget = false;
                r.Pips.Add(img);
            }

            // Buy button: coin + price.
            var btn = Child(card.transform, "Buy", typeof(Image), typeof(Button));
            btn.anchorMin = new Vector2(0.68f, 0.16f); btn.anchorMax = new Vector2(0.975f, 0.84f);
            btn.offsetMin = btn.offsetMax = Vector2.zero;
            r.Button = btn.GetComponent<Image>();
            RoundedSprite.Apply(r.Button, buyColor, 0.8f);

            float coin = h * 0.42f;
            var coinRt = Child(btn, "Coin", typeof(Image));
            coinRt.anchorMin = coinRt.anchorMax = new Vector2(0.5f, 0.5f);
            coinRt.sizeDelta = new Vector2(coin, coin);
            r.CoinImage = coinRt.GetComponent<Image>();
            r.CoinImage.sprite = coinIcon; r.CoinImage.preserveAspect = true; r.CoinImage.raycastTarget = false;
            r.CoinImage.enabled = coinIcon != null;

            r.Price = Label(btn, "", h * 0.44f, TextAlignmentOptions.Center);
            Place(r.Price.rectTransform, Vector2.zero, Vector2.one, 0f, 0f);
            r.Price.fontStyle = FontStyles.Bold;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!r.Buy()) return;
                r.PopUntil = Time.unscaledTime + PopSeconds;
                Haptics.Light();
                Refresh();
            });
            _rows.Add(r);
        }

        private void Refresh()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                Row r = _rows[i];
                int price = r.Price01();
                int level = r.Level();
                r.Value.text = r.ValueText();
                for (int p = 0; p < r.Pips.Count; p++) r.Pips[p].color = p < level ? r.Accent : pipOffColor;

                bool maxed = price < 0;
                bool afford = !maxed && PlayerProgress.Coins >= price;
                r.Price.text = maxed ? "MAX" : price.ToString();
                r.Price.color = maxed ? new Color(0.35f, 0.22f, 0.02f) : Color.white;
                r.Button.color = maxed ? maxColor : afford ? buyColor : cantColor;
                r.CoinImage.enabled = !maxed && coinIcon != null;
                r.CoinImage.color = afford ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                LayoutPrice(r, maxed);
            }
        }

        // Coin on the left, number beside it, the pair centred in the button.
        private static void LayoutPrice(Row r, bool maxed)
        {
            var coin = (RectTransform)r.CoinImage.transform;
            var text = r.Price.rectTransform;
            if (maxed)
            {
                text.offsetMin = text.offsetMax = Vector2.zero;
                return;
            }
            r.Price.ForceMeshUpdate();
            float tw = r.Price.preferredWidth;
            float cw = coin.sizeDelta.x;
            const float gap = 8f;
            float total = cw + gap + tw;
            coin.anchoredPosition = new Vector2(-total * 0.5f + cw * 0.5f, 0f);
            float shift = (cw + gap) * 0.5f;
            text.offsetMin = new Vector2(shift, 0f);
            text.offsetMax = new Vector2(shift, 0f);
        }

        private void Update()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                Row r = _rows[i];
                float left = r.PopUntil - Time.unscaledTime;
                float k = left > 0f ? Mathf.Sin((1f - left / PopSeconds) * Mathf.PI) : 0f;
                r.Card.localScale = Vector3.one * (1f + 0.04f * k);
                r.Icon.localScale = Vector3.one * (1f + 0.22f * k);
            }
        }

        private static RectTransform Child(Transform parent, string name, params System.Type[] components)
        {
            var types = new System.Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            components.CopyTo(types, 1);
            var go = new GameObject(name, types);
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, float left, float y)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = new Vector2(left, y); rt.offsetMax = new Vector2(0f, y);
        }

        private TextMeshProUGUI Label(Transform parent, string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.alignment = align; t.fontSize = size; t.color = Color.white; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }
    }
}
