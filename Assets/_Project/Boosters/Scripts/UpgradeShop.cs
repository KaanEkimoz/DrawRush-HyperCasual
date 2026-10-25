using System.Collections.Generic;
using DrawRush.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.Boosters
{
    /// <summary>
    /// The upgrade half of the coin shop, under the skin grid: one row per booster (longer effect
    /// per level) plus the bump pet (buy, then faster and farther). Rows are built at runtime and
    /// refresh on every purchase or coin change, greying out what the player cannot afford yet.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UpgradeShop : MonoBehaviour
    {
        [SerializeField] private BoosterConfig config;
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("List area inside the shop panel, from its centre (canvas units).")]
        [SerializeField] private Vector2 areaPosition = new(0f, -510f);
        [SerializeField] private Vector2 areaSize = new(900f, 470f);
        [SerializeField] private float rowHeight = 86f;
        [SerializeField] private Color rowColor = new(0.16f, 0.17f, 0.24f, 1f);
        [SerializeField] private Color buyColor = new(0.25f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color cantColor = new(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] private Color petColor = new(1f, 0.55f, 0.75f, 1f);

        private sealed class Row { public TextMeshProUGUI Name, Level, Price; public Image Button; public System.Func<int> Price01; public System.Func<bool> Buy; }
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

            foreach (BoosterType t in new[] { BoosterType.Shield, BoosterType.Freeze, BoosterType.Speed, BoosterType.Magnet })
            {
                BoosterType type = t;
                AddRow(type.ToString(), config != null ? config.ColorOf(type) : Color.white,
                    () => $"Lv {BoosterUpgrades.Level(type)}/{BoosterUpgrades.MaxLevel}",
                    () => BoosterUpgrades.NextPrice(type), () => BoosterUpgrades.TryUpgrade(type));
            }
            AddRow("Bump Pet", petColor,
                () => BoosterUpgrades.PetLevel == 0 ? "Not owned" : $"Lv {BoosterUpgrades.PetLevel}/{BoosterUpgrades.PetMaxLevel}",
                () => BoosterUpgrades.PetNextPrice, BoosterUpgrades.TryUpgradePet);
        }

        private void OnEnable()
        {
            BoosterUpgrades.Changed += Refresh;
            PlayerProgress.CoinsChanged += OnCoins;
            Refresh();
        }

        private void OnDisable()
        {
            BoosterUpgrades.Changed -= Refresh;
            PlayerProgress.CoinsChanged -= OnCoins;
        }

        private void OnCoins(int _) => Refresh();

        private void AddRow(string title, Color accent, System.Func<string> level, System.Func<int> price, System.Func<bool> buy)
        {
            int i = _rows.Count;
            var row = new GameObject(title, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_area, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, rowHeight - 8f);
            rt.anchoredPosition = new Vector2(0f, -i * rowHeight);
            row.GetComponent<Image>().color = rowColor;

            var dot = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(row.transform, false);
            var drt = (RectTransform)dot.transform; drt.anchorMin = new Vector2(0f, 0f); drt.anchorMax = new Vector2(0f, 1f); drt.sizeDelta = new Vector2(16f, 0f); drt.anchoredPosition = new Vector2(8f, 0f);
            dot.GetComponent<Image>().color = accent;

            var r = new Row
            {
                Name = Label(row.transform, title, new Vector2(0.04f, 0f), new Vector2(0.45f, 1f), TextAlignmentOptions.Left),
                Level = Label(row.transform, "", new Vector2(0.45f, 0f), new Vector2(0.68f, 1f), TextAlignmentOptions.Center),
                Price01 = price,
                Buy = buy,
            };
            r.Level.fontSize *= 0.8f;

            var btn = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            btn.transform.SetParent(row.transform, false);
            var brt = (RectTransform)btn.transform; brt.anchorMin = new Vector2(0.7f, 0.12f); brt.anchorMax = new Vector2(0.98f, 0.88f); brt.offsetMin = brt.offsetMax = Vector2.zero;
            r.Button = btn.GetComponent<Image>();
            r.Price = Label(btn.transform, "", Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
            btn.GetComponent<Button>().onClick.AddListener(() => { if (r.Buy()) Refresh(); });
            r.Name.text = title;
            _rows.Add(r);
            _levelText.Add(level);
        }

        private readonly List<System.Func<string>> _levelText = new();

        private void Refresh()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                Row r = _rows[i];
                int price = r.Price01();
                r.Level.text = _levelText[i]();
                r.Price.text = price < 0 ? "MAX" : price.ToString();
                r.Button.color = price >= 0 && PlayerProgress.Coins >= price ? buyColor : cantColor;
            }
        }

        private TextMeshProUGUI Label(Transform parent, string text, Vector2 aMin, Vector2 aMax, TextAlignmentOptions align)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.alignment = align; t.fontSize = rowHeight * 0.42f; t.color = Color.white; t.raycastTarget = false;
            return t;
        }
    }
}
