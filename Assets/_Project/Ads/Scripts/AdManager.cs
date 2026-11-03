using UnityEngine;
using System;
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
        private LevelPlayRewardedAd _rewarded;
        private LevelPlayBannerAd _banner;
        private bool _initialised;
        private Action _onReward;
        private bool _rewardEarned;

        private AdPacing _pacing;

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

            _pacing = new AdPacing(config.InterstitialEveryNWins, config.MinSecondsBetweenAds, Time.realtimeSinceStartup);

            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(config.AndroidAppKey);
        }

        private void OnDestroy()
        {
            LevelPlay.OnInitSuccess -= OnInitSuccess;
            LevelPlay.OnInitFailed -= OnInitFailed;
            _interstitial?.Dispose();
            _rewarded?.Dispose();
            _banner?.Dispose();
        }

        /// <summary>True when a rewarded video can be shown right now.</summary>
        public bool IsRewardedReady
        {
            get
            {
#if UNITY_EDITOR
                if (config != null && config.SimulateRewardedInEditor) return true;
#endif
                return _initialised && _rewarded != null && _rewarded.IsAdReady();
            }
        }

        /// <summary>Show a rewarded video; <paramref name="onReward"/> runs only if the player
        /// watched it through. <paramref name="placement"/> names the offer for LevelPlay reports.</summary>
        public void ShowRewarded(string placement, Action onReward)
        {
#if UNITY_EDITOR
            if (config != null && config.SimulateRewardedInEditor) { MarkAdShown(); onReward?.Invoke(); return; }
#endif
            if (!IsRewardedReady) return;
            _onReward = onReward;
            _rewardEarned = false;
            _rewarded.ShowAd(placement);
        }

        private void OnInitSuccess(LevelPlayConfiguration _)
        {
            _initialised = true;
            if (config.VerboseLogging) Debug.Log("[Ads] LevelPlay init success.");

            SetUpRewarded();
            SetUpBanner();

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

        // Rewarded: grant on the reward callback, but hand it out after the ad closes so the game
        // resumes under a fresh frame rather than behind the ad activity.
        private void SetUpRewarded()
        {
            if (string.IsNullOrEmpty(config.RewardedAdUnitId)) return;
            _rewarded = new LevelPlayRewardedAd(config.RewardedAdUnitId);
            _rewarded.OnAdRewarded += (_, __) => _rewardEarned = true;
            _rewarded.OnAdClosed += _ =>
            {
                MarkAdShown();
                Action grant = _rewardEarned ? _onReward : null;
                _onReward = null;
                _rewardEarned = false;
                _rewarded.LoadAd();
                grant?.Invoke();
            };
            _rewarded.OnAdDisplayFailed += (_, __) => { _onReward = null; _rewarded.LoadAd(); };
            if (config.VerboseLogging) _rewarded.OnAdLoadFailed += e => Debug.Log($"[Ads] Rewarded load failed: {e}");
            _rewarded.LoadAd();
        }

        // The banner shows itself as soon as it loads and refreshes on its own from then on.
        private void SetUpBanner()
        {
            if (string.IsNullOrEmpty(config.BannerAdUnitId)) return;
            var bannerConfig = new LevelPlayBannerAd.Config.Builder()
                .SetPosition(LevelPlayBannerPosition.BottomCenter)
                .SetRespectSafeArea(true)
                .Build();
            _banner = new LevelPlayBannerAd(config.BannerAdUnitId, bannerConfig);
            if (config.VerboseLogging) _banner.OnAdLoadFailed += e => Debug.Log($"[Ads] Banner load failed: {e}");
            _banner.LoadAd();
        }

        // A rewarded video the player chose to watch counts as an ad shown: it resets the win
        // count and the time gate, so no interstitial lands right after it (e.g. x2 Coins → Next).
        private void MarkAdShown()
        {
            _pacing?.MarkShown(Time.realtimeSinceStartup);
        }

        private void OnInitFailed(LevelPlayInitError error)
            => Debug.LogWarning($"[Ads] LevelPlay init failed: {error}");

        /// <summary>
        /// Call once per win/level-advance (from <c>LevelFlow.NextLevel</c>). Shows an interstitial
        /// once <see cref="AdPacing"/> says it is due. When it is not due, or no ad is loaded, the
        /// win count is kept so the ad shows on a later win.
        /// </summary>
        public void MaybeShowInterstitial()
        {
            if (!_initialised || _interstitial == null) return;

            _pacing.RecordWin();
            float now = Time.realtimeSinceStartup;
            if (!_pacing.IsDue(now)) return;

            if (!_interstitial.IsAdReady())
            {
                if (config.VerboseLogging) Debug.Log("[Ads] Interstitial not loaded yet — skipping, will retry next win.");
                return;
            }

            _interstitial.ShowAd();
            _pacing.MarkShown(now);
        }
    }
}
