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
        /// Show an interstitial when the cadence lands on an ad boundary and one is ready. Pass the
        /// number of levels completed so far (i.e. before it is incremented for the level about to
        /// start), so ads land between plays rather than mid-level.
        /// </summary>
        public void MaybeShowInterstitial(int levelsCompleted)
        {
            if (!_initialised || _interstitial == null) return;

            int every = Mathf.Max(1, config.InterstitialEveryNLevels);
            if (levelsCompleted <= 0 || levelsCompleted % every != 0) return;

            if (!_interstitial.IsAdReady())
            {
                if (config.VerboseLogging) Debug.Log("[Ads] Interstitial not ready — skipping this boundary.");
                return;
            }
            _interstitial.ShowAd();
        }
    }
}
