using UnityEngine;
using Unity.Services.LevelPlay;

namespace DrawRush.Ads
{
    /// <summary>
    /// Thin wrapper over the LevelPlay SDK: initialises mediation once at boot, keeps a single
    /// interstitial preloaded, and shows it between levels on a cadence (see <see cref="AdConfig"/>).
    ///
    /// Deliberately not a singleton — it lives on the game's boot object and is reached the same way the
    /// rest of the project reaches its scene pieces (a serialized reference, with a
    /// <c>FindFirstObjectByType</c> fallback). Every entry point no-ops cleanly when there is no config,
    /// the SDK has not initialised, or no ad is loaded, so gameplay never blocks on the ad layer.
    ///
    /// Ads only render in a real device build with network adapters resolved (LevelPlay Integration
    /// Manager + EDM). In the Editor init succeeds but no ad fills — use the LevelPlay Test Suite on a
    /// device to verify. The account must also be approved before live fill/revenue.
    /// </summary>
    public sealed class AdManager : MonoBehaviour
    {
        [Tooltip("Ad keys and cadence. Without this, the ad layer stays disabled.")]
        [SerializeField] private AdConfig config;

        private LevelPlayInterstitialAd _interstitial;
        private bool _initialised;

        private int _winsSinceLastAd;
        private float _lastShownRealtime = float.NegativeInfinity;  // so the first eligible ad isn't time-gated

        private void Start()
        {
            if (config == null)
            {
                Debug.LogWarning("[Ads] No AdConfig assigned — ads disabled.");
                return;
            }
            if (string.IsNullOrEmpty(config.AndroidAppKey))
            {
                Debug.LogWarning("[Ads] AdConfig has no App Key — ads disabled.");
                return;
            }

            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(config.AndroidAppKey);
        }

        private void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            _interstitial?.Dispose();
        }

        private void OnInitSuccess(LevelPlayConfiguration _)
        {
            _initialised = true;
            if (config.VerboseLogging) Debug.Log("[Ads] LevelPlay init success.");

            if (string.IsNullOrEmpty(config.InterstitialAdUnitId))
            {
                Debug.LogWarning("[Ads] No interstitial ad unit id — interstitials disabled.");
                return;
            }

            _interstitial = new LevelPlayInterstitialAd(config.InterstitialAdUnitId);
            _interstitial.OnAdClosed += _ => _interstitial.LoadAd();          // preload the next one
            _interstitial.OnAdDisplayFailed += (_, __) => _interstitial.LoadAd();
            if (config.VerboseLogging)
            {
                _interstitial.OnAdLoaded += _ => Debug.Log("[Ads] Interstitial loaded.");
                _interstitial.OnAdLoadFailed += e => Debug.Log($"[Ads] Interstitial load failed: {e}");
            }
            _interstitial.LoadAd();
        }

        private void OnInitFailed(LevelPlayInitError error)
            => Debug.LogWarning($"[Ads] LevelPlay init failed: {error}");

        /// <summary>
        /// Call once per win/level-advance (from <c>LevelFlow.NextLevel</c>). Shows an interstitial only
        /// when BOTH frequency gates pass: at least <see cref="AdConfig.InterstitialEveryNWins"/> wins
        /// since the last ad, AND at least <see cref="AdConfig.MinSecondsBetweenAds"/> real seconds since
        /// the last ad. When only the count is due but the time gate blocks (or no ad is loaded), the
        /// win tally is kept so the ad shows on a later win once the cooldown clears.
        /// </summary>
        public void MaybeShowInterstitial()
        {
            if (!_initialised || _interstitial == null) return;

            _winsSinceLastAd++;
            if (_winsSinceLastAd < Mathf.Max(1, config.InterstitialEveryNWins)) return;

            float sinceLast = Time.realtimeSinceStartup - _lastShownRealtime;
            if (sinceLast < config.MinSecondsBetweenAds)
            {
                if (config.VerboseLogging)
                    Debug.Log($"[Ads] Win count due but only {sinceLast:F0}s since last ad (need {config.MinSecondsBetweenAds:F0}s) — holding.");
                return;   // keep the win tally; retry on a later win once the cooldown clears
            }

            if (!_interstitial.IsAdReady())
            {
                if (config.VerboseLogging) Debug.Log("[Ads] Interstitial not loaded yet — skipping, will retry next win.");
                return;   // keep the tally so we try again as soon as one is loaded
            }

            _interstitial.ShowAd();
            _winsSinceLastAd = 0;
            _lastShownRealtime = Time.realtimeSinceStartup;
        }
    }
}
