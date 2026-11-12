using UnityEngine;

namespace DrawRush.Ads
{
    /// <summary>
    /// Data-only settings for the ad layer, kept out of code so the LevelPlay keys can change without a
    /// recompile and so ads stay a self-contained feature (its own folder, its own config) rather than
    /// bleeding into <c>GameConfig</c>. The App Key is not a secret — it ships inside the APK — so it is
    /// fine to keep here; only real network credentials would need to stay out of the repo.
    /// </summary>
    [CreateAssetMenu(fileName = "AdConfig", menuName = "DrawRush/Ad Config")]
    public sealed class AdConfig : ScriptableObject
    {
        [Header("LevelPlay (ironSource) — Android")]
        [Tooltip("LevelPlay App Key for the Android app. From the LevelPlay console → Apps.")]
        [SerializeField] private string androidAppKey = "27f1c2215";

        [Tooltip("Ad Unit ID of the Interstitial to show between levels. From the LevelPlay console.")]
        [SerializeField] private string interstitialAdUnitId = "gn5krubs30l4gsgf";

        [Tooltip("Ad Unit ID of the Rewarded video (revive, x2 coins, start with shield). Empty = the " +
                 "rewarded offers stay hidden. Create it in the LevelPlay console → Ad Units.")]
        [SerializeField] private string rewardedAdUnitId = "";

        [Tooltip("Ad Unit ID of the bottom banner. Empty = no banner. Create it in the LevelPlay " +
                 "console → Ad Units.")]
        [SerializeField] private string bannerAdUnitId = "";

        [Header("LevelPlay (ironSource) — iOS")]
        [Tooltip("LevelPlay App Key for the iOS app (a separate app in the LevelPlay console). " +
                 "Empty = no ads on iOS.")]
        [SerializeField] private string iosAppKey = "";

        [Tooltip("iOS Interstitial Ad Unit ID. Ad units belong to one app, so iOS has its own.")]
        [SerializeField] private string iosInterstitialAdUnitId = "";

        [Tooltip("iOS Rewarded Ad Unit ID. Empty = the rewarded offers stay hidden on iOS.")]
        [SerializeField] private string iosRewardedAdUnitId = "";

        [Tooltip("iOS Banner Ad Unit ID. Empty = no banner on iOS.")]
        [SerializeField] private string iosBannerAdUnitId = "";

        [Tooltip("Editor only: pretend a rewarded ad was watched, so the offer flows can be tested " +
                 "without a device (ads never fill in the Editor).")]
        [SerializeField] private bool simulateRewardedInEditor = true;

        [Header("Cadence (both gates must pass)")]
        [Tooltip("Show an interstitial at most once every N wins/completed levels (1 = every win). " +
                 "The ad only shows when this AND the time gate below are both satisfied.")]
        [Min(1)]
        [SerializeField] private int interstitialEveryNWins = 3;

        [Tooltip("Minimum real seconds between interstitials. Even if the win count is due, an ad is " +
                 "held back until this long has passed since the last one — a hard frequency cap.")]
        [Min(0f)]
        [SerializeField] private float minSecondsBetweenAds = 90f;

        [Tooltip("Log init/load/show steps to the console. Handy on device; leave off for release.")]
        [SerializeField] private bool verboseLogging = false;

        public string AndroidAppKey => androidAppKey;

#if UNITY_IOS
        /// <summary>App Key of the platform this build runs on.</summary>
        public string AppKey => iosAppKey;
        public string InterstitialAdUnitId => iosInterstitialAdUnitId;
        public string RewardedAdUnitId => iosRewardedAdUnitId;
        public string BannerAdUnitId => iosBannerAdUnitId;
#else
        /// <summary>App Key of the platform this build runs on.</summary>
        public string AppKey => androidAppKey;
        public string InterstitialAdUnitId => interstitialAdUnitId;
        public string RewardedAdUnitId => rewardedAdUnitId;
        public string BannerAdUnitId => bannerAdUnitId;
#endif
        public bool SimulateRewardedInEditor => simulateRewardedInEditor;
        public int InterstitialEveryNWins => interstitialEveryNWins;
        public float MinSecondsBetweenAds => minSecondsBetweenAds;
        public bool VerboseLogging => verboseLogging;
    }
}
