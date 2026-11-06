using System;
using System.Collections.Generic;
using DrawRush.Ads;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

namespace DrawRush.Analytics
{
    /// <summary>
    /// The game's analytics, on Unity Gaming Services (the project's linked cloud project). One
    /// static entry point, <see cref="Record"/>, so gameplay code never touches the SDK. Events
    /// recorded before the service is up (or without consent) are simply dropped.
    ///
    /// Consent: in the GDPR regions data collection starts only after the player accepted the
    /// consent card (<see cref="AdConsent"/>); elsewhere it starts right away. Turning personalised
    /// ads off in Settings also stops collection. Crash and exception reports come from the Cloud
    /// Diagnostics package once Unity Services is initialised.
    ///
    /// Every event and parameter must exist in the dashboard's Event Manager, or the service drops
    /// it as invalid: see <see cref="Events"/> for the full list.
    /// </summary>
    public static class Telemetry
    {
        /// <summary>Event names and their parameters, as they must be defined in Event Manager.</summary>
        public static class Events
        {
            public const string LevelStart = "levelStart";        // levelNumber, shapeIndex, enemies, attempt
            public const string LevelComplete = "levelComplete";  // levelNumber, shapeIndex, enemies, attempt, seconds, stars, health
            public const string LevelFail = "levelFail";          // levelNumber, shapeIndex, enemies, attempt, seconds, coverage
            public const string TutorialComplete = "tutorialComplete"; // seconds
            public const string AdWatched = "adWatched";          // adType (interstitial / rewarded), placement, levelNumber
            public const string ShopPurchase = "shopPurchase";    // itemId, price, coinsLeft
        }

        private static bool _ready, _collecting, _starting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            AdConsent.Changed -= OnConsentChanged;
            AdConsent.Changed += OnConsentChanged;
            _ = StartAsync();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _ready = _collecting = _starting = false;
        }

        // The editor never sends: play-testing would flood the live dashboard with fake players.
#if UNITY_EDITOR
        private static bool MayCollect => false;
#else
        private static bool MayCollect => AdConsent.HasAnswer ? AdConsent.Granted : !AdConsent.InConsentRegion();
#endif

        private static async Awaitable StartAsync()
        {
            if (_ready || _starting) return;
            _starting = true;
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    await UnityServices.InitializeAsync();
                _ready = true;
                SyncCollection();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Telemetry] Unity Services init failed, analytics off this session: {e.Message}");
            }
            finally { _starting = false; }
        }

        private static void OnConsentChanged()
        {
            if (_ready) SyncCollection();
            else _ = StartAsync();
        }

        private static void SyncCollection()
        {
            bool want = MayCollect;
            if (want == _collecting) return;
            _collecting = want;
            if (want) AnalyticsService.Instance.StartDataCollection();
            else AnalyticsService.Instance.StopDataCollection();
        }

        /// <summary>Record one event with optional parameters (int, float, bool or string values).</summary>
        public static void Record(string name, params (string key, object value)[] parameters)
        {
            if (!_ready || !_collecting) return;
            try
            {
                var e = new CustomEvent(name);
                foreach (var (key, value) in parameters) e.Add(key, value);
                AnalyticsService.Instance.RecordEvent(e);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Telemetry] {name} not recorded: {ex.Message}");
            }
        }

        // ---- Level bookkeeping shared by start / complete / fail ----

        private static float _levelStartTime;
        private static int _levelIndex = -1, _attempt;

        /// <summary>A level just went live (after its activation, before the 3-2-1).</summary>
        public static void LevelStarted(int shapeIndex, bool tutorial)
        {
            _attempt = shapeIndex == _levelIndex ? _attempt + 1 : 1;
            _levelIndex = shapeIndex;
            _levelStartTime = Time.realtimeSinceStartup;
            if (tutorial) return;
            Record(Events.LevelStart, LevelParams());
        }

        public static float LevelSeconds => Time.realtimeSinceStartup - _levelStartTime;

        public static (string, object)[] LevelParams(params (string, object)[] extra)
        {
            var list = new List<(string, object)>
            {
                ("levelNumber", PlayerProgressLevel()),
                ("shapeIndex", _levelIndex),
                ("enemies", Core.PlayerProgress.LastLevelEnemies),
                ("attempt", _attempt),
            };
            list.AddRange(extra);
            return list.ToArray();
        }

        private static int PlayerProgressLevel() => Mathf.Max(1, Core.PlayerProgress.LevelsPlayed);
    }
}
