using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DrawRush.Core;

namespace DrawRush.Shop
{
    /// <summary>
    /// The coin shop: it turns the coin balance into skins the player can buy and wear, which is the
    /// piece the loop was missing — you win, you earn, you spend, you look different.
    ///
    /// Cells are built from the <see cref="CosmeticLibrary"/> at runtime into a grid, so adding a
    /// skin is a data edit, not a UI edit. A tap buys an unaffordable-checked skin, or equips one you
    /// already own. Opening pauses the game and restores the exact timescale on close, so the shop is
    /// safe to open mid-level or during the countdown.
    /// </summary>
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private CosmeticLibrary library;

        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform content;   // grid parent
        [SerializeField] private TMP_Text coinText;

        [Header("Buttons")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Cell look")]
        [SerializeField] private float cellSize = 220f;
        [SerializeField] private Color affordText = Color.white;
        [SerializeField] private Color lockedText = new Color(1f, 0.85f, 0.2f, 1f);
        [Tooltip("Character silhouette shown in each cell, tinted to the skin's colour — so the " +
                 "player sees how the character will look, not just an abstract colour block.")]
        [SerializeField] private Sprite characterSprite;
        [Tooltip("Neutral cell background behind the tinted character.")]
        [SerializeField] private Color cardColor = new Color(0.16f, 0.17f, 0.24f, 1f);

        private readonly List<Cell> _cells = new();
        private float _prevTimeScale = 1f;

        private sealed class Cell
        {
            public CosmeticItem item;
            public Image swatch;
            public Image border;
            public TMP_Text label;
        }

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
            BuildCells();
        }

        private void OnEnable() => PlayerProgress.CoinsChanged += OnCoinsChanged;
        private void OnDisable() => PlayerProgress.CoinsChanged -= OnCoinsChanged;

        private void OnCoinsChanged(int total)
        {
            if (coinText != null) coinText.text = total.ToString();
            Refresh();   // affordability may have changed
        }

        public void Open()
        {
            if (panel == null) return;
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            panel.SetActive(true);
            if (coinText != null) coinText.text = PlayerProgress.Coins.ToString();
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            Time.timeScale = _prevTimeScale;   // exactly what it was, so a countdown isn't skipped
        }

        private void BuildCells()
        {
            if (library == null || content == null) return;

            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellSize, cellSize);
            grid.spacing = new Vector2(24f, 24f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(20, 20, 20, 20);

            for (int i = 0; i < library.Count; i++)
            {
                var item = library.Get(i);
                if (item == null) continue;
                _cells.Add(BuildCell(item));
            }
            Refresh();
        }

        private Cell BuildCell(CosmeticItem item)
        {
            // Root: a neutral card is the button; the character silhouette on top wears the skin
            // colour, so the cell shows how the CHARACTER will look, not just a colour block.
            var root = new GameObject("Cell_" + item.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            root.transform.SetParent(content, false);
            var swatch = root.GetComponent<Image>();
            swatch.color = cardColor;

            // Character preview, tinted to the skin colour.
            var charGo = new GameObject("Char", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            charGo.transform.SetParent(root.transform, false);
            var chrt = charGo.GetComponent<RectTransform>();
            chrt.anchorMin = new Vector2(0.5f, 1f); chrt.anchorMax = new Vector2(0.5f, 1f); chrt.pivot = new Vector2(0.5f, 1f);
            chrt.anchoredPosition = new Vector2(0f, -14f);
            chrt.sizeDelta = new Vector2(cellSize * 0.62f, cellSize * 0.62f);
            var charImg = charGo.GetComponent<Image>();
            charImg.preserveAspect = true;
            charImg.raycastTarget = false;
            if (item.preview != null)
            {
                // A real render of the character in this colour — already coloured, so no tint,
                // and give it more of the cell since it's a full figure with the pencil.
                charImg.sprite = item.preview;
                charImg.color = Color.white;
                chrt.anchoredPosition = new Vector2(0f, -4f);
                chrt.sizeDelta = new Vector2(cellSize * 0.92f, cellSize * 0.92f);
            }
            else
            {
                // Fallback: a flat silhouette tinted to the colour.
                charImg.sprite = characterSprite;
                charImg.color = item.color;
            }

            // Border (equipped highlight) — a white frame slightly larger, behind the swatch.
            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            borderGo.transform.SetParent(root.transform, false);
            var brt = borderGo.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-8f, -8f); brt.offsetMax = new Vector2(8f, 8f);
            borderGo.transform.SetAsFirstSibling();
            var border = borderGo.GetComponent<Image>();
            border.color = Color.white;
            border.raycastTarget = false;

            // Label strip at the bottom: a dark plate so text reads on any swatch colour, with the
            // text as a child (Image and TMP are both Graphics and can't share one GameObject).
            var plateGo = new GameObject("LabelPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            plateGo.transform.SetParent(root.transform, false);
            var lrt = plateGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 0.30f);
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var plate = plateGo.GetComponent<Image>();
            plate.color = new Color(0f, 0f, 0f, 0.45f);
            plate.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            textGo.transform.SetParent(plateGo.transform, false);
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true; label.fontSizeMin = 18f; label.fontSizeMax = 40f;
            label.raycastTarget = false;

            var btn = root.GetComponent<Button>();
            root.AddComponent<DrawRush.UI.ButtonJuice>();   // press-pop + click sound like every other button
            var captured = item;
            btn.onClick.AddListener(() => OnCellTapped(captured));

            return new Cell { item = item, swatch = swatch, border = border, label = label };
        }

        private void OnCellTapped(CosmeticItem item)
        {
            string def = library.DefaultId;
            bool owned = PlayerProgress.IsCosmeticOwned(item.id, def);
            if (!owned)
            {
                if (!PlayerProgress.TrySpendCoins(item.price)) { Refresh(); return; }  // can't afford
                PlayerProgress.OwnCosmetic(item.id);
            }
            PlayerProgress.EquipCosmetic(item.id);   // buying auto-equips; owning-but-not-equipped equips
            Refresh();
        }

        private void Refresh()
        {
            if (library == null) return;
            string def = library.DefaultId;
            string equipped = PlayerProgress.EquippedCosmetic;
            if (string.IsNullOrEmpty(equipped)) equipped = def;

            foreach (var cell in _cells)
            {
                bool owned = PlayerProgress.IsCosmeticOwned(cell.item.id, def);
                bool isEquipped = cell.item.id == equipped;
                if (cell.border != null) cell.border.enabled = isEquipped;
                if (cell.label != null)
                {
                    if (isEquipped) { cell.label.text = "EQUIPPED"; cell.label.color = affordText; }
                    else if (owned) { cell.label.text = "USE"; cell.label.color = affordText; }
                    else { cell.label.text = cell.item.price.ToString(); cell.label.color = lockedText; }
                }
            }
        }
    }
}
