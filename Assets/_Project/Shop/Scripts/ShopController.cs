using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.UI;
using TMPro;
using DrawRush.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrawRush.Shop
{
    /// <summary>
    /// The coin shop's skin showroom, at the top of the shop panel. Like unlocking cars in a racing
    /// game: one skin stands in the middle, its neighbours peek in from the sides, and a swipe (or
    /// the arrows) rolls the line. Skins unlock in order: the next one shows its price, the ones
    /// after it stay dark until the one before them is bought. The button under the card buys,
    /// equips or says what is needed.
    ///
    /// Opening pauses the game (<see cref="MenuPause"/>) and restores it on close, so the shop is
    /// safe mid-level or during the countdown. Cards are built from the <see cref="CosmeticLibrary"/>
    /// at runtime, so adding a skin is a data edit.
    /// </summary>
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private CosmeticLibrary library;

        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [Tooltip("The showroom area (top of the shop panel).")]
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text coinText;
        [Tooltip("The in-game coin pill. The shop shows an exact copy of it in the same spot, so " +
                 "opening the shop doesn't shift the coin counter.")]
        [SerializeField] private RectTransform hudCoinPill;
        private RectTransform _coinPill;

        [Header("Buttons")]
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [Header("Showroom look")]
        [SerializeField] private Vector2 cardSize = new(540f, 560f);
        [Tooltip("Distance between card centres, in canvas units.")]
        [SerializeField] private float spacing = 520f;
        [SerializeField] private float sideScale = 0.7f;
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Color cardColor = new(0.16f, 0.17f, 0.27f, 1f);
        [SerializeField] private Color equippedColor = new(1f, 0.82f, 0.15f, 1f);
        [SerializeField] private Color buyColor = new(0.25f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color useColor = new(0.3f, 0.55f, 1f, 1f);
        [SerializeField] private Color cantColor = new(0.33f, 0.34f, 0.42f, 1f);
        [SerializeField] private Color lockedText = new(1f, 0.85f, 0.2f, 1f);

        private sealed class Card
        {
            public CosmeticItem Item;
            public RectTransform Root;
            public CanvasGroup Group;
            public Image Frame, Preview, Glow;
            public Outline Ring;
            public TMP_Text Name, Tag;
        }

        private readonly List<Card> _cards = new();
        private RectTransform _row, _dotsRow;
        private readonly List<Image> _dots = new();
        private Image _action, _actionCoin;
        private TMP_Text _actionText, _hint;
        private TMP_FontAsset _font;
        private float _scroll;          // current position, in cards
        private int _target;            // card the row is settling on
        private bool _dragging;
        private float _dragStartScroll, _dragStartX, _popUntil;

        private void Awake()
        {
            _font = coinText != null ? coinText.font : null;
            MirrorHudCoins();
            if (openButton != null) { openButton.onClick.AddListener(Open); BuildBadge(); }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (panel != null) panel.SetActive(false);
            Build();
        }

        private void OnEnable()
        {
            PlayerProgress.CoinsChanged += OnCoinsChanged;
            StatUpgrades.Changed += RefreshBadge;
            DashSkill.Changed += RefreshBadge;
            RefreshBadge();
        }

        private void OnDisable()
        {
            PlayerProgress.CoinsChanged -= OnCoinsChanged;
            StatUpgrades.Changed -= RefreshBadge;
            DashSkill.Changed -= RefreshBadge;
        }

        private void OnCoinsChanged(int total)
        {
            if (coinText != null) coinText.text = CoinHud.Abbreviate(total);
            Refresh();
            RefreshBadge();
        }

        // ---------- coin counter ----------

        // The shop's own counter is replaced by a copy of the HUD pill (its CoinHud keeps the number
        // current), laid exactly over the original.
        private void MirrorHudCoins()
        {
            if (hudCoinPill == null || panel == null) return;
            _coinPill = Instantiate(hudCoinPill, panel.transform);
            _coinPill.name = "ShopCoins";
            if (coinText != null)
            {
                coinText.gameObject.SetActive(false);
                coinText = _coinPill.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void AlignCoinPill()
        {
            if (_coinPill == null || hudCoinPill == null) return;
            _coinPill.position = hudCoinPill.position;
            _coinPill.rotation = hudCoinPill.rotation;
            Vector3 hs = hudCoinPill.lossyScale, ps = _coinPill.parent.lossyScale;
            _coinPill.localScale = new Vector3(hs.x / ps.x, hs.y / ps.y, 1f);
            _coinPill.sizeDelta = hudCoinPill.rect.size;
            _coinPill.SetAsLastSibling();
        }

        // ---------- "upgrades you can buy" badge on the shop button ----------

        private RectTransform _badge;
        private TMP_Text _badgeText;
        private int _badgeCount = -1;
        private float _badgeBumpAt = -10f;

        /// <summary>Upgrades (stats and the dash) the player can afford right now.</summary>
        public static int AffordableUpgrades()
        {
            int coins = PlayerProgress.Coins, n = 0;
            foreach (PlayerStat stat in new[] { PlayerStat.Speed, PlayerStat.Health, PlayerStat.DrawArea })
            {
                int p = StatUpgrades.NextPrice(stat);
                if (p >= 0 && coins >= p) n++;
            }
            int d = DashSkill.NextPrice;
            if (d >= 0 && coins >= d) n++;
            return n;
        }

        // A red count bubble on the shop button's top-right corner.
        private void BuildBadge()
        {
            _badge = Child(openButton.transform, "UpgradeBadge", typeof(Image));
            _badge.anchorMin = _badge.anchorMax = new Vector2(1f, 1f);
            _badge.anchoredPosition = new Vector2(-6f, -6f);
            _badge.sizeDelta = new Vector2(60f, 60f);
            var img = _badge.GetComponent<Image>();
            RoundedSprite.Apply(img, new Color(0.93f, 0.22f, 0.25f, 1f), 3f);
            img.raycastTarget = false;
            var outline = _badge.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white; outline.effectDistance = new Vector2(3f, 3f);
            _badgeText = Text(_badge, "", 40f, Color.white);
            Stretch(_badgeText.rectTransform);
            _badgeText.fontStyle = FontStyles.Bold;
            _badge.gameObject.SetActive(false);
        }

        private void RefreshBadge()
        {
            if (_badge == null) return;
            int n = AffordableUpgrades();
            if (n > _badgeCount && _badgeCount >= 0) _badgeBumpAt = Time.unscaledTime;   // a new one just became buyable
            _badgeCount = n;
            _badge.gameObject.SetActive(n > 0);
            _badgeText.text = n.ToString();
        }

        private void LateUpdate()
        {
            if (_badge == null || !_badge.gameObject.activeSelf) return;
            float t = Time.unscaledTime - _badgeBumpAt;
            float bump = t < 0.4f ? Mathf.Sin(t / 0.4f * Mathf.PI) * 0.35f : 0f;
            float breathe = 0.05f * Mathf.Sin(Time.unscaledTime * 3f);   // a gentle pulse so it draws the eye
            _badge.localScale = Vector3.one * (1f + bump + breathe);
        }

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Open()
        {
            if (panel == null || IsOpen) return;   // MenuPause counts opens: never open twice
            MenuPause.Open();
            panel.SetActive(true);
            if (coinText != null) coinText.text = CoinHud.Abbreviate(PlayerProgress.Coins);
            AlignCoinPill();
            // Start on the skin being worn.
            _target = Mathf.Max(0, IndexOf(Equipped));
            _scroll = _target;
            Refresh();
            Layout();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            MenuPause.Close();
        }

        // ---------- rules ----------

        private string DefaultId => library != null ? library.DefaultId : "default";
        private string Equipped
        {
            get { string e = PlayerProgress.EquippedCosmetic; return string.IsNullOrEmpty(e) ? DefaultId : e; }
        }

        private bool Owned(int i) => PlayerProgress.IsCosmeticOwned(_cards[i].Item.id, DefaultId);

        /// <summary>Skins unlock in order: every skin before this one must be owned.</summary>
        private bool InReach(int i)
        {
            for (int k = 0; k < i; k++) if (!Owned(k)) return false;
            return true;
        }

        private int IndexOf(string id)
        {
            for (int i = 0; i < _cards.Count; i++) if (_cards[i].Item.id == id) return i;
            return -1;
        }

        private void OnAction()
        {
            if (_cards.Count == 0) return;
            int i = Mathf.Clamp(_target, 0, _cards.Count - 1);
            CosmeticItem item = _cards[i].Item;
            if (!Owned(i))
            {
                if (!InReach(i)) { GoTo(FirstLocked()); return; }     // show what to buy first
                if (!PlayerProgress.TrySpendCoins(item.price)) { Refresh(); return; }
                PlayerProgress.OwnCosmetic(item.id);
                DrawRush.Analytics.Telemetry.Record(DrawRush.Analytics.Telemetry.Events.ShopPurchase, ("itemId", "skin_" + item.id), ("price", item.price), ("coinsLeft", PlayerProgress.Coins));
                _popUntil = Time.unscaledTime + 0.35f;
                Haptics.Medium();
            }
            PlayerProgress.EquipCosmetic(item.id);   // buying equips; an owned skin equips on tap
            Refresh();
        }

        private int FirstLocked()
        {
            for (int i = 0; i < _cards.Count; i++) if (!Owned(i)) return i;
            return _cards.Count - 1;
        }

        private void GoTo(int i) { _target = Mathf.Clamp(i, 0, _cards.Count - 1); Refresh(); }

        // ---------- build ----------

        private void Build()
        {
            if (library == null || content == null) return;
            var grid = content.GetComponent<GridLayoutGroup>();
            if (grid != null) Destroy(grid);

            // Swipe surface over the whole showroom.
            var surface = content.gameObject.GetComponent<Image>() ?? content.gameObject.AddComponent<Image>();
            surface.color = new Color(0f, 0f, 0f, 0f);
            var swipe = content.gameObject.AddComponent<SwipeArea>();
            swipe.Began = x => { _dragging = true; _dragStartX = x; _dragStartScroll = _scroll; };
            swipe.Moved = x => _scroll = Mathf.Clamp(_dragStartScroll - (x - _dragStartX) / Scale() / spacing, -0.3f, _cards.Count - 0.7f);
            swipe.Ended = (x, v) =>
            {
                _dragging = false;
                float moved = _dragStartScroll - _scroll;
                int to = Mathf.RoundToInt(_scroll);
                float vel = -v / Scale() / spacing;               // cards per second, + = forward
                if (Mathf.Abs(vel) > 1.2f) to = Mathf.RoundToInt(_dragStartScroll) + (vel > 0 ? 1 : -1);
                else if (Mathf.Abs(moved) > 0.18f && to == Mathf.RoundToInt(_dragStartScroll)) to += moved < 0 ? 1 : -1;
                GoTo(to);
            };

            _row = Child(content, "Row");
            _row.anchorMin = _row.anchorMax = new Vector2(0.5f, 1f);
            _row.anchoredPosition = new Vector2(0f, -cardSize.y * 0.5f - 10f);

            for (int i = 0; i < library.Count; i++)
            {
                CosmeticItem item = library.Get(i);
                if (item != null) _cards.Add(BuildCard(item));
            }

            // Arrows.
            Arrow("Prev", -1);
            Arrow("Next", +1);

            // Dots.
            _dotsRow = Child(content, "Dots");
            _dotsRow.anchorMin = _dotsRow.anchorMax = new Vector2(0.5f, 1f);
            _dotsRow.anchoredPosition = new Vector2(0f, -cardSize.y - 40f);
            _dotsRow.sizeDelta = new Vector2(700f, 24f);
            var h = _dotsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = h.childForceExpandHeight = false;
            for (int i = 0; i < _cards.Count; i++)
            {
                var d = Child(_dotsRow, "Dot", typeof(Image));
                d.sizeDelta = new Vector2(18f, 18f);
                var img = d.GetComponent<Image>(); RoundedSprite.Apply(img, Color.white, 3f); img.raycastTarget = false;
                _dots.Add(img);
            }

            // The action button.
            var btn = Child(content, "Action", typeof(Image), typeof(Button));
            btn.anchorMin = btn.anchorMax = new Vector2(0.5f, 1f);
            btn.sizeDelta = new Vector2(480f, 120f);
            btn.anchoredPosition = new Vector2(0f, -cardSize.y - 130f);
            _action = btn.GetComponent<Image>();
            RoundedSprite.Apply(_action, buyColor, 1.4f);
            btn.gameObject.AddComponent<ButtonJuice>();
            btn.GetComponent<Button>().onClick.AddListener(OnAction);
            var coin = Child(btn, "Coin", typeof(Image));
            coin.anchorMin = coin.anchorMax = new Vector2(0.5f, 0.5f);
            coin.sizeDelta = new Vector2(64f, 64f);
            _actionCoin = coin.GetComponent<Image>();
            _actionCoin.sprite = coinIcon; _actionCoin.preserveAspect = true; _actionCoin.raycastTarget = false;
            _actionText = Text(btn, "", 58f, Color.white);
            Stretch(_actionText.rectTransform);
            _actionText.fontStyle = FontStyles.Bold;

            _hint = Text(content, "", 38f, new Color(1f, 1f, 1f, 0.75f));
            _hint.rectTransform.anchorMin = _hint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _hint.rectTransform.sizeDelta = new Vector2(900f, 60f);
            _hint.rectTransform.anchoredPosition = new Vector2(0f, -cardSize.y - 230f);

            Refresh();
            Layout();
        }

        private Card BuildCard(CosmeticItem item)
        {
            var root = Child(_row, "Card_" + item.id, typeof(Image), typeof(CanvasGroup));
            root.sizeDelta = cardSize;
            var frame = root.GetComponent<Image>();
            RoundedSprite.Apply(frame, cardColor, 1.8f);
            var ring = root.gameObject.AddComponent<Outline>();   // gold ring on the worn skin
            ring.effectColor = equippedColor; ring.effectDistance = new Vector2(7f, 7f); ring.enabled = false;

            // A soft floor of the skin's colour behind the figure.
            var glow = Child(root, "Glow", typeof(Image));
            glow.anchorMin = new Vector2(0.1f, 0.2f); glow.anchorMax = new Vector2(0.9f, 0.32f);
            glow.offsetMin = glow.offsetMax = Vector2.zero;
            var glowImg = glow.GetComponent<Image>();
            RoundedSprite.Apply(glowImg, new Color(item.color.r, item.color.g, item.color.b, 0.28f), 3f);
            glowImg.raycastTarget = false;

            var prev = Child(root, "Preview", typeof(Image));
            prev.anchorMin = new Vector2(0.06f, 0.2f); prev.anchorMax = new Vector2(0.94f, 0.88f);
            prev.offsetMin = prev.offsetMax = Vector2.zero;
            var img = prev.GetComponent<Image>();
            img.preserveAspect = true; img.raycastTarget = false;
            if (item.preview != null) { img.sprite = item.preview; img.color = Color.white; }
            else { img.sprite = characterSprite; img.color = item.color; }

            var name = Text(root, Loc.T(item.displayName), 50f, Color.white);
            name.rectTransform.anchorMin = new Vector2(0f, 0.86f); name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;
            name.fontStyle = FontStyles.Bold;

            var tag = Text(root, "", 40f, lockedText);
            tag.rectTransform.anchorMin = Vector2.zero; tag.rectTransform.anchorMax = new Vector2(1f, 0.17f);
            tag.rectTransform.offsetMin = tag.rectTransform.offsetMax = Vector2.zero;

            return new Card { Ring = ring, Item = item, Root = root, Group = root.GetComponent<CanvasGroup>(), Frame = frame, Preview = img, Glow = glowImg, Name = name, Tag = tag };
        }

        private void Arrow(string name, int dir)
        {
            var a = Child(content, name, typeof(Image), typeof(Button));
            a.anchorMin = a.anchorMax = new Vector2(0.5f, 1f);
            a.sizeDelta = new Vector2(96f, 96f);
            a.anchoredPosition = new Vector2(dir * (cardSize.x * 0.5f + 70f), -cardSize.y * 0.5f - 10f);
            RoundedSprite.Apply(a.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.92f), 4f);
            a.gameObject.AddComponent<ButtonJuice>();
            a.GetComponent<Button>().onClick.AddListener(() => GoTo(_target + dir));
            var t = Text(a, dir < 0 ? "<" : ">", 64f, new Color(0.16f, 0.17f, 0.3f));
            Stretch(t.rectTransform);
            t.fontStyle = FontStyles.Bold;
        }

        // ---------- state ----------

        private void Refresh()
        {
            if (_cards.Count == 0) return;
            _target = Mathf.Clamp(_target, 0, _cards.Count - 1);
            string equipped = Equipped;

            for (int i = 0; i < _cards.Count; i++)
            {
                Card c = _cards[i];
                bool owned = Owned(i), reach = InReach(i), worn = c.Item.id == equipped;
                c.Frame.color = cardColor;
                c.Ring.enabled = worn;
                // Out of reach: a dark silhouette, like a covered car.
                c.Preview.color = owned || reach
                    ? (c.Item.preview != null ? Color.white : c.Item.color)
                    : new Color(0.08f, 0.08f, 0.14f, 0.92f);
                c.Glow.enabled = owned || reach;
                c.Tag.text = worn ? Loc.T("EQUIPPED") : owned ? Loc.T("OWNED") : reach ? Loc.F("{0} coins", c.Item.price) : Loc.F("LOCKED  ·  {0}", c.Item.price);
                c.Tag.color = worn ? equippedColor : owned ? new Color(1f, 1f, 1f, 0.7f) : lockedText;
                if (i < _dots.Count)
                    _dots[i].color = i == _target ? Color.white : owned ? new Color(1f, 1f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.18f);
            }

            Card t = _cards[_target];
            bool tOwned = Owned(_target), tReach = InReach(_target), tWorn = t.Item.id == equipped;
            _actionCoin.enabled = false;
            _hint.text = "";
            if (tWorn) { SetAction(Loc.T("EQUIPPED"), equippedColor, new Color(0.3f, 0.2f, 0f)); }
            else if (tOwned) { SetAction(Loc.T("USE"), useColor, Color.white); }
            else if (!tReach)
            {
                SetAction(Loc.T("LOCKED"), cantColor, Color.white);
                _hint.text = Loc.F("Unlock {0} first", Loc.T(_cards[FirstLocked()].Item.displayName));
            }
            else
            {
                bool afford = PlayerProgress.Coins >= t.Item.price;
                SetAction(t.Item.price.ToString(), afford ? buyColor : cantColor, Color.white);
                _actionCoin.enabled = coinIcon != null;
                if (!afford) _hint.text = Loc.F("{0} more coins to unlock", t.Item.price - PlayerProgress.Coins);
                LayoutCoin();
            }
        }

        private void SetAction(string text, Color bg, Color fg)
        {
            _action.color = bg; _actionText.text = text; _actionText.color = fg;
            _actionText.rectTransform.offsetMin = _actionText.rectTransform.offsetMax = Vector2.zero;
        }

        private void LayoutCoin()
        {
            _actionText.ForceMeshUpdate();
            float tw = _actionText.preferredWidth, cw = 64f, gap = 12f, total = cw + gap + tw;
            ((RectTransform)_actionCoin.transform).anchoredPosition = new Vector2(-total * 0.5f + cw * 0.5f, 0f);
            float shift = (cw + gap) * 0.5f;
            _actionText.rectTransform.offsetMin = new Vector2(shift, 0f);
            _actionText.rectTransform.offsetMax = new Vector2(shift, 0f);
        }

        private void Update()
        {
            if (!IsOpen || _cards.Count == 0) return;
            if (!_dragging) _scroll = Mathf.Lerp(_scroll, _target, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            else
            {
                int near = Mathf.Clamp(Mathf.RoundToInt(_scroll), 0, _cards.Count - 1);
                if (near != _target) { _target = near; Refresh(); }
            }
            Layout();
        }

        private void Layout()
        {
            float pop = _popUntil > Time.unscaledTime ? Mathf.Sin((1f - (_popUntil - Time.unscaledTime) / 0.35f) * Mathf.PI) : 0f;
            for (int i = 0; i < _cards.Count; i++)
            {
                Card c = _cards[i];
                float d = i - _scroll;
                float ad = Mathf.Abs(d);
                c.Root.anchoredPosition = new Vector2(d * spacing, 0f);
                float s = Mathf.Lerp(1f, sideScale, Mathf.Clamp01(ad));
                if (i == _target) s *= 1f + 0.08f * pop;
                c.Root.localScale = Vector3.one * s;
                c.Group.alpha = Mathf.Clamp01(1.6f - ad * 0.75f);
                c.Root.gameObject.SetActive(ad < 2.2f);
            }
            // Nearest card on top.
            int front = Mathf.Clamp(Mathf.RoundToInt(_scroll), 0, _cards.Count - 1);
            _cards[front].Root.SetAsLastSibling();
        }

        // ---------- helpers ----------

        private float Scale()
        {
            var canvas = content.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
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

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private TMP_Text Text(Transform parent, string text, float size, Color color)
        {
            var rt = Child(parent, "Text", typeof(TextMeshProUGUI));
            var t = rt.GetComponent<TextMeshProUGUI>();
            if (_font != null) t.font = _font;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        /// <summary>Reports horizontal drags (screen x) over the showroom.</summary>
        private sealed class SwipeArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public System.Action<float> Began, Moved;
            public System.Action<float, float> Ended;   // x, velocity (px/s)
            private float _lastX, _lastT, _vel;

            public void OnBeginDrag(PointerEventData e)
            {
                _lastX = e.position.x; _lastT = Time.unscaledTime; _vel = 0f;
                Began?.Invoke(e.position.x);
            }

            public void OnDrag(PointerEventData e)
            {
                float dt = Mathf.Max(0.001f, Time.unscaledTime - _lastT);
                _vel = Mathf.Lerp(_vel, (e.position.x - _lastX) / dt, 0.5f);
                _lastX = e.position.x; _lastT = Time.unscaledTime;
                Moved?.Invoke(e.position.x);
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (Time.unscaledTime - _lastT > 0.08f) _vel = 0f;   // held still before letting go
                Ended?.Invoke(e.position.x, _vel);
            }
        }
    }
}
