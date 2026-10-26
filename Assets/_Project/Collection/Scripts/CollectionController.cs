using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DrawRush.Core;

namespace DrawRush.Collection
{
    /// <summary>
    /// The collection screen: a scrolling grid of every shape in the game. Shapes the player has
    /// drawn show in full colour with their name and best stars; the rest are dark silhouettes with
    /// "?" — the silhouette is the pull to keep going. Cells are built from the
    /// <see cref="ShapeCollection"/> at runtime, so adding a level is a data edit.
    /// Opening pauses the game and restores the exact timescale on close (same contract as the shop).
    /// </summary>
    public sealed class CollectionController : MonoBehaviour
    {
        [SerializeField] private ShapeCollection collection;

        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform content;   // grid parent inside a ScrollRect
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_FontAsset font;

        [Header("Buttons")]
        [SerializeField] private Button[] openButtons;
        [SerializeField] private Button closeButton;

        [Header("Cell look")]
        [SerializeField] private float cellSize = 300f;
        [SerializeField] private Color cardColor = new Color(0.16f, 0.17f, 0.24f, 1f);
        [SerializeField] private Color lockedTint = new Color(0.05f, 0.06f, 0.10f, 0.55f);
        [SerializeField] private Color starColor = new Color(1f, 0.82f, 0.15f, 1f);
        [SerializeField] private Color starOffColor = new Color(0.30f, 0.30f, 0.36f, 1f);
        [SerializeField] private Sprite starSprite;

        private readonly List<Cell> _cells = new();

        private sealed class Cell
        {
            public ShapeCollection.Entry entry;
            public Image art;
            public TMP_Text label;
            public Image[] stars;
        }

        private void Awake()
        {
            if (openButtons != null)
                foreach (Button b in openButtons) if (b != null) b.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
            BuildCells();
        }

        public void Open()
        {
            if (panel == null) return;
            MenuPause.Open();
            panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            MenuPause.Close();
        }

        private void BuildCells()
        {
            if (collection == null || content == null) return;

            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellSize, cellSize);
            grid.spacing = new Vector2(24f, 24f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(20, 20, 20, 40);

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < collection.Count; i++)
            {
                ShapeCollection.Entry entry = collection.Get(i);
                if (entry != null) _cells.Add(BuildCell(entry));
            }
        }

        private Cell BuildCell(ShapeCollection.Entry entry)
        {
            var root = new GameObject("Shape_" + entry.LevelIndex, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(content, false);
            var card = root.GetComponent<Image>();
            card.color = cardColor;
            card.raycastTarget = false;

            var artGo = new GameObject("Art", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            artGo.transform.SetParent(root.transform, false);
            var artRt = artGo.GetComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.1f, 0.24f);
            artRt.anchorMax = new Vector2(0.9f, 0.96f);
            artRt.offsetMin = artRt.offsetMax = Vector2.zero;
            var art = artGo.GetComponent<Image>();
            art.sprite = entry.Illustration;
            art.preserveAspect = true;
            art.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(root.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.04f, 0.10f);
            labelRt.anchorMax = new Vector2(0.96f, 0.28f);
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 40f;
            label.raycastTarget = false;

            var stars = new Image[3];
            for (int s = 0; s < stars.Length; s++)
            {
                var starGo = new GameObject("Star" + s, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                starGo.transform.SetParent(root.transform, false);
                var starRt = starGo.GetComponent<RectTransform>();
                starRt.anchorMin = starRt.anchorMax = new Vector2(0.5f, 0f);
                starRt.sizeDelta = new Vector2(cellSize * 0.1f, cellSize * 0.1f);
                starRt.anchoredPosition = new Vector2((s - 1) * cellSize * 0.11f, cellSize * 0.065f);
                stars[s] = starGo.GetComponent<Image>();
                stars[s].sprite = starSprite;
                stars[s].preserveAspect = true;
                stars[s].raycastTarget = false;
            }

            return new Cell { entry = entry, art = art, label = label, stars = stars };
        }

        private void Refresh()
        {
            int owned = 0;
            foreach (Cell cell in _cells)
            {
                int stars = PlayerProgress.BestStars(cell.entry.LevelIndex);
                bool collected = stars > 0;
                if (collected) owned++;
                cell.art.color = collected ? Color.white : lockedTint;
                cell.label.text = collected ? cell.entry.DisplayName : "?";
                for (int s = 0; s < cell.stars.Length; s++)
                {
                    cell.stars[s].gameObject.SetActive(collected);
                    cell.stars[s].color = s < stars ? starColor : starOffColor;
                }
            }
            if (progressText != null) progressText.text = $"{owned}/{_cells.Count}";
        }
    }
}
