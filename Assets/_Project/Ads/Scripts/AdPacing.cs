namespace DrawRush.Ads
{
    /// <summary>
    /// The interstitial cadence: an ad falls due after N wins, but never sooner than a minimum
    /// time since the last one. A win count that arrives early is kept, so the ad lands on the
    /// first win after the time gate opens. Plain C# so the rules are unit-testable.
    /// </summary>
    public sealed class AdPacing
    {
        private readonly int _winsPerAd;
        private readonly float _minSeconds;
        private int _wins;
        private float _lastAdTime;

        public AdPacing(int winsPerAd, float minSeconds, float sessionStart)
        {
            _winsPerAd = winsPerAd < 1 ? 1 : winsPerAd;
            _minSeconds = minSeconds;
            _lastAdTime = sessionStart;
        }

        /// <summary>Count one finished level.</summary>
        public void RecordWin() => _wins++;

        /// <summary>True when both the win count and the time gate have passed.</summary>
        public bool IsDue(float now) => _wins >= _winsPerAd && now - _lastAdTime >= _minSeconds;

        /// <summary>An ad was shown: both gates start over.</summary>
        public void MarkShown(float now)
        {
            _wins = 0;
            _lastAdTime = now;
        }
    }
}
