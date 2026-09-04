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

        [Header("Cadence")]
        [Tooltip("Show an interstitial once every N completed levels (1 = every level). 3 keeps it " +
                 "unobtrusive for a hyper-casual loop.")]
        [Min(1)]
        [SerializeField] private int interstitialEveryNLevels = 3;

        [Tooltip("Log init/load/show steps to the console. Handy on device; leave off for release.")]
        [SerializeField] private bool verboseLogging = false;

        public string AndroidAppKey => androidAppKey;
        public string InterstitialAdUnitId => interstitialAdUnitId;
        public int InterstitialEveryNLevels => interstitialEveryNLevels;
        public bool VerboseLogging => verboseLogging;
    }
}
