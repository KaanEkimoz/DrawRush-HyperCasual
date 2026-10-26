using DrawRush.Boosters;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Enemy;
using DrawRush.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.Ads
{
    /// <summary>
    /// The rewarded-video offers from the redesign research, all opt-in and never mid-level:
    ///   • level start (during the 3-2-1, after a fail on this same level): "Start with a shield";
    ///   • lose panel: "Second chance" — once per attempt: full health, a shield, enemies sent back
    ///     to their spawns, the painted progress kept;
    ///   • lose panel (after the second chance is spent): "Retry with a shield";
    ///   • win panel: "x2 coins" — the level's coin reward once more.
    /// Buttons are built at runtime inside the existing panels and only show while an ad is ready,
    /// so with no rewarded ad unit configured the game looks exactly as before.
    /// </summary>
    public sealed class RewardedOffers : MonoBehaviour
    {
        [SerializeField] private AdManager ads;
        [SerializeField] private HudPanels hud;
        [SerializeField] private GameManager game;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerBoosts boosts;
        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private TMP_FontAsset font;

        [Header("Layout (canvas units from each panel's centre, like the panel's own buttons)")]
        [SerializeField] private Vector2 buttonSize = new(620f, 130f);
        [Tooltip("Under the lose panel's Restart button (at -210).")]
        [SerializeField] private float loseButtonY = -400f;
        [Tooltip("Under the win panel's Shop/Collection row (at -380).")]
        [SerializeField] private float winButtonY = -560f;
        [Tooltip("Pre-level offer, from the screen centre, under the 3-2-1.")]
        [SerializeField] private float startButtonY = -330f;
        [Tooltip("Fails on the same level before the pre-level shield is offered.")]
        [SerializeField] private int failsBeforeStartOffer = 1;
        [SerializeField] private Color buttonColor = new(1f, 0.75f, 0.1f, 1f);

        [SerializeField] private LevelStartCountdown countdown;
        [SerializeField] private LevelManager levels;

        private Button _revive, _shieldRetry, _doubleCoins, _startShield;
        private bool _reviveUsed, _doubleUsed, _startShieldTaken;
        private int _failLevel = -1, _failsHere;

        private void Start()
        {
            if (hud == null) hud = FindFirstObjectByType<HudPanels>();
            if (game == null) game = FindFirstObjectByType<GameManager>();
            if (ads == null) ads = FindFirstObjectByType<AdManager>();
            if (hud == null) return;
            if (hud.LosePanel != null)
            {
                _revive = MakeButton(hud.LosePanel.transform, "Second Chance (Ad)", loseButtonY, OnRevive);
                _shieldRetry = MakeButton(hud.LosePanel.transform, "Retry with Shield (Ad)", loseButtonY, OnShieldRetry);
            }
            if (hud.WinPanel != null)
                _doubleCoins = MakeButton(hud.WinPanel.transform, "x2 Coins (Ad)", winButtonY, OnDoubleCoins);
            if (countdown == null) countdown = FindFirstObjectByType<LevelStartCountdown>();
            if (levels == null) levels = FindFirstObjectByType<LevelManager>();
            // On the HUD canvas itself (the panels' parent), since no panel is open at level start.
            Transform canvas = hud.LosePanel != null ? hud.LosePanel.transform.parent : null;
            if (canvas != null) _startShield = MakeButton(canvas, "Start with Shield (Ad)", startButtonY, OnStartShield);
        }

        private void OnEnable()
        {
            if (painter != null) painter.LevelBound += OnLevelBound;
            if (health != null) health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (painter != null) painter.LevelBound -= OnLevelBound;
            if (health != null) health.Died -= OnDied;
        }

        private void OnLevelBound(EdgeNetwork _)
        {
            _reviveUsed = false;
            _doubleUsed = false;
            _startShieldTaken = false;
        }

        private int CurrentLevel => levels != null ? levels.CurrentIndex : -1;

        private void OnDied()
        {
            int level = CurrentLevel;
            if (level == _failLevel) _failsHere++;
            else { _failLevel = level; _failsHere = 1; }
        }

        private void Update()
        {
            bool ready = ads != null && ads.IsRewardedReady;
            bool losing = hud != null && hud.LosePanel != null && hud.LosePanel.activeInHierarchy;
            bool winning = hud != null && hud.WinPanel != null && hud.WinPanel.activeInHierarchy;
            Show(_revive, ready && losing && !_reviveUsed);
            Show(_shieldRetry, ready && losing && _reviveUsed);
            Show(_doubleCoins, ready && winning && !_doubleUsed && CoinsThisWin() > 0);
            bool starting = countdown != null && countdown.IsRunning && !MenuPause.IsOpen;
            bool failedHere = CurrentLevel == _failLevel && _failsHere >= failsBeforeStartOffer;
            Show(_startShield, ready && starting && failedHere && !_startShieldTaken);
        }

        private void OnRevive()
        {
            _reviveUsed = true;
            ads.ShowRewarded("revive", () =>
            {
                hud.HideAllPanels();
                if (health != null) health.ResetToStarting();
                if (combat != null) combat.ResetForLevel();
                RespawnEnemies();
                if (boosts != null) boosts.Grant(BoosterType.Shield);
                MenuPause.SetGameTimeScale(1f);
            });
        }

        private void OnShieldRetry()
        {
            ads.ShowRewarded("shield_retry", () =>
            {
                if (boosts != null) boosts.GrantOnNextLevel(BoosterType.Shield);
                if (game != null) game.RestartLevel();
            });
        }

        // The 3-2-1 runs before the painter binds the level (no fixed steps at timeScale 0), and
        // binding clears boosters: so queue it for the bind, and show the bubble right away too.
        private void OnStartShield()
        {
            _startShieldTaken = true;
            ads.ShowRewarded("start_shield", () =>
            {
                if (boosts == null) return;
                boosts.GrantOnNextLevel(BoosterType.Shield);
                boosts.Grant(BoosterType.Shield);
            });
        }

        private void OnDoubleCoins()
        {
            _doubleUsed = true;
            ads.ShowRewarded("double_coins", () => PlayerProgress.AddCoins(CoinsThisWin()));
        }

        private static int CoinsThisWin()
        {
            var state = GameServices.State;
            return state != null && state.LastStars > 0 ? LevelScore.CoinsForStars(state.LastStars) : 0;
        }

        // The player's death took every enemy down with it; bring the ones that were fielded back
        // at their own spawns (re-enabling resets position, speed and the death shrink).
        private static void RespawnEnemies()
        {
            foreach (EnemyFollow e in FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                e.gameObject.SetActive(false);
                e.gameObject.SetActive(true);
            }
        }

        private static void Show(Button b, bool on)
        {
            if (b != null && b.gameObject.activeSelf != on) b.gameObject.SetActive(on);
        }

        private Button MakeButton(Transform parent, string text, float y, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Rewarded_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = buttonSize;
            rt.anchoredPosition = new Vector2(0f, y);
            go.GetComponent<Image>().color = buttonColor;
            var button = go.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.text = text;
            label.fontSize = buttonSize.y * 0.42f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            go.SetActive(false);
            return button;
        }
    }
}
