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
        private bool _rewardClosed;
        private string _placement;
        private int _initTries, _interstitialTries, _rewardedTries, _bannerTries;
        private bool _bannerHidden;

        private AdPacing _pacing;

        private void Start()
        {
            if (config == null)
            {
                Debug.LogWarning("[Ads] No AdConfig assigned — ads disabled.");
                return;
            }
            if (string.IsNullOrEmpty(config.AppKey))
            {
                Debug.LogWarning("[Ads] AdConfig has no App Key — ads disabled.");
                return;
            }

            _pacing = new AdPacing(config.InterstitialEveryNWins, config.MinSecondsBetweenAds, Time.realtimeSinceStartup);

            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;

            // GDPR region, first launch: ask before the SDK starts, so no ad request goes out
            // without an answer. With no settings menu to ask through, start non-personalised.
            if (AdConsent.NeedsAsk)
            {
                var prompt = FindFirstObjectByType<DrawRush.UI.SettingsMenu>(FindObjectsInactive.Include);
                if (prompt != null) { prompt.AskConsent(granted => { AdConsent.Set(granted); InitSdk(); }); return; }
                AdConsent.Set(false);
            }
            InitSdk();
        }

        private void InitSdk()
        {
            AdConsent.Apply();
            LevelPlay.Init(config.AppKey);
        }

        // The banner sits where the thumb lands for the joystick, so it only shows while the game
        // is frozen (countdown, menus, win and lose panels) and hides the moment play runs.
        private void Update()
        {
            if (_banner == null) return;
            bool hide = Time.timeScale > 0f;
            if (hide == _bannerHidden) return;
            _bannerHidden = hide;
            if (hide) _banner.HideAd();
            else _banner.ShowAd();
        }

        // No network at boot or no fill used to kill that ad type for the whole session. Retry with
        // a growing wait (5, 10, 20 ... 120 s) on unscaled time, so a paused game still retries.
        private async void RetryLater(int tries, Action retry)
        {
            float wait = Mathf.Min(120f, 5f * Mathf.Pow(2f, Mathf.Min(tries, 5)));
            try
            {
                for (float t = 0f; t < wait; t += Time.unscaledDeltaTime)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException) { return; }
            retry?.Invoke();
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
            _placement = placement;
            _rewardEarned = false;
            _rewardClosed = false;
            _rewarded.ShowAd(placement);
        }

        private void OnInitSuccess(LevelPlayConfiguration _)
        {
            _initialised = true;
            _initTries = 0;
            Audio.IosAudioSession.Apply();   // SDK init can reset the audio session category
            if (config.VerboseLogging) Debug.Log("[Ads] LevelPlay init success.");

            SetUpRewarded();
            SetUpBanner();

            if (string.IsNullOrEmpty(config.InterstitialAdUnitId))
            {
                Debug.LogWarning("[Ads] No interstitial ad unit id — interstitials disabled.");
                return;
            }

            _interstitial = new LevelPlayInterstitialAd(config.InterstitialAdUnitId);
            _interstitial.OnAdClosed += _ =>
            {
                Audio.IosAudioSession.Apply();   // the ad may leave its own audio session behind
                _interstitial.LoadAd();          // preload the next one
            };
            _interstitial.OnAdDisplayFailed += (_, __) => _interstitial.LoadAd();
            _interstitial.OnAdLoaded += _ =>
            {
                _interstitialTries = 0;
                if (config.VerboseLogging) Debug.Log("[Ads] Interstitial loaded.");
            };
            _interstitial.OnAdLoadFailed += e =>
            {
                if (config.VerboseLogging) Debug.Log($"[Ads] Interstitial load failed: {e}");
                RetryLater(_interstitialTries++, () => _interstitial?.LoadAd());
            };
            _interstitial.LoadAd();
        }

        // Rewarded: hand the reward out once the ad has BOTH rewarded and closed, so the game
        // resumes under a fresh frame rather than behind the ad activity. LevelPlay may send the
        // reward before or after the close; granting only on "reward, then close" silently lost
        // revives and x2 coins whenever the reward came late.
        private void SetUpRewarded()
        {
            if (string.IsNullOrEmpty(config.RewardedAdUnitId)) return;
            _rewarded = new LevelPlayRewardedAd(config.RewardedAdUnitId);
            _rewarded.OnAdRewarded += (_, __) => { _rewardEarned = true; TryGrantReward(); };
            _rewarded.OnAdClosed += _ =>
            {
                MarkAdShown();
                Audio.IosAudioSession.Apply();
                _rewardClosed = true;
                _rewarded.LoadAd();
                TryGrantReward();
                // No reward by the close: allow a late one for a few seconds, then drop the offer.
                if (_onReward != null) DropRewardLater();
            };
            _rewarded.OnAdDisplayFailed += (_, __) => { _onReward = null; _rewarded.LoadAd(); };
            _rewarded.OnAdLoaded += _ => _rewardedTries = 0;
            _rewarded.OnAdLoadFailed += e =>
            {
                if (config.VerboseLogging) Debug.Log($"[Ads] Rewarded load failed: {e}");
                RetryLater(_rewardedTries++, () => _rewarded?.LoadAd());
            };
            _rewarded.LoadAd();
        }

        private void TryGrantReward()
        {
            if (!_rewardEarned || !_rewardClosed || _onReward == null) return;
            Action grant = _onReward;
            _onReward = null;
            RecordAd("rewarded", _placement);
            _rewardEarned = false;
            _rewardClosed = false;
            grant.Invoke();
        }

        private async void DropRewardLater()
        {
            Action pending = _onReward;
            try
            {
                for (float t = 0f; t < 5f; t += Time.unscaledDeltaTime)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            catch (OperationCanceledException) { return; }
            if (_onReward == pending) _onReward = null;   // the reward never came: the player skipped
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
            _banner.OnAdLoaded += _ => { _bannerTries = 0; if (_bannerHidden) _banner.HideAd(); };
            _banner.OnAdLoadFailed += e =>
            {
                if (config.VerboseLogging) Debug.Log($"[Ads] Banner load failed: {e}");
                RetryLater(_bannerTries++, () => _banner?.LoadAd());
            };
            _bannerHidden = false;
            _banner.LoadAd();
        }

        // A rewarded video the player chose to watch counts as an ad shown: it resets the win
        // count and the time gate, so no interstitial lands right after it (e.g. x2 Coins → Next).
        private static void RecordAd(string type, string placement) =>
            DrawRush.Analytics.Telemetry.Record(DrawRush.Analytics.Telemetry.Events.AdWatched, ("adType", type), ("placement", placement ?? ""),
                ("levelNumber", Mathf.Max(1, DrawRush.Core.PlayerProgress.LevelsPlayed)));

        private void MarkAdShown()
        {
            _pacing?.MarkShown(Time.realtimeSinceStartup);
        }

        private void OnInitFailed(LevelPlayInitError error)
        {
            Debug.LogWarning($"[Ads] LevelPlay init failed: {error}");
            RetryLater(_initTries++, () => { if (!_initialised) LevelPlay.Init(config.AppKey); });
        }

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
            RecordAd("interstitial", "between_levels");
        }
    }
}
