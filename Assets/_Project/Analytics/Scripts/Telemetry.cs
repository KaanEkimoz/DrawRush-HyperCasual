using System.Collections.Generic;
using UnityEngine;

namespace DrawRush.Analytics
{
    /// <summary>
    /// One static entry point for game events. Today it sends nothing: Kaan chose to keep the
    /// game SDK-free at this size and read installs, retention, crashes and ANRs from Play Console.
    /// Gameplay already calls <see cref="Record"/> at every key moment, so adding a backend later
    /// (Firebase, GameAnalytics) is a change to this file only. Respect <c>AdConsent</c> when you do.
    /// </summary>
    public static class Telemetry
    {
        /// <summary>Event names and their parameters.</summary>
        public static class Events
        {
            public const string LevelStart = "levelStart";        // levelNumber, shapeIndex, enemies, attempt
            public const string LevelComplete = "levelComplete";  // levelNumber, shapeIndex, enemies, attempt, seconds, stars, health
            public const string LevelFail = "levelFail";          // levelNumber, shapeIndex, enemies, attempt, seconds, coverage
            public const string TutorialComplete = "tutorialComplete"; // seconds
            public const string AdWatched = "adWatched";          // adType (interstitial / rewarded), placement, levelNumber
            public const string ShopPurchase = "shopPurchase";    // itemId, price, coinsLeft
        }

        /// <summary>Log events to the console in development builds and the editor.</summary>
        public static bool LogToConsole = Debug.isDebugBuild;

        /// <summary>Record one event with optional parameters. No backend yet: logs only.</summary>
        public static void Record(string name, params (string key, object value)[] parameters)
        {
            if (!LogToConsole) return;
            var parts = new List<string>(parameters.Length);
            foreach (var (key, value) in parameters) parts.Add($"{key}={value}");
            Debug.Log($"[Telemetry] {name} {string.Join(" ", parts)}");
        }

        // ---- Level bookkeeping shared by start / complete / fail ----

        private static float _levelStartTime;
        private static int _levelIndex = -1, _attempt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _levelIndex = -1;
            _attempt = 0;
            LogToConsole = Debug.isDebugBuild;
        }

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
                ("levelNumber", Mathf.Max(1, Core.PlayerProgress.LevelsPlayed)),
                ("shapeIndex", _levelIndex),
                ("enemies", Core.PlayerProgress.LastLevelEnemies),
                ("attempt", _attempt),
            };
            list.AddRange(extra);
            return list.ToArray();
        }
    }
}
