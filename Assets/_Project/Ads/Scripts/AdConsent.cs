using System;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace DrawRush.Ads
{
    /// <summary>
    /// GDPR consent for ads. Players in the EEA, the UK and Switzerland are asked once, before the
    /// ad SDK starts, whether ad partners may personalise ads; everyone else gets the default.
    /// The answer is stored and handed to LevelPlay, which passes it to every mediated network.
    /// The game only mediates Unity Ads today; adding AdMob would need Google's certified UMP form.
    /// </summary>
    public static class AdConsent
    {
        public const string PrivacyPolicyUrl = "https://kaanusta.dev/drawrush-privacy";
        public const string TermsOfServiceUrl = "https://kaanusta.dev/drawrush-terms";

        private const string Key = "AdConsent";   // -1 never asked, 0 declined, 1 granted

        /// <summary>Raised after the player changes their answer (settings label listens).</summary>
        public static event Action Changed;

        public static bool HasAnswer => PlayerPrefs.GetInt(Key, -1) >= 0;
        public static bool Granted => PlayerPrefs.GetInt(Key, -1) == 1;

        /// <summary>What the ads actually do now: the stored answer, else the regional default
        /// (personalised outside the GDPR regions). Drives the settings toggle.</summary>
        public static bool Personalised => HasAnswer ? Granted : !InConsentRegion();

        /// <summary>True when consent must be asked before ads start: a GDPR-region player with no answer yet.</summary>
        public static bool NeedsAsk => !HasAnswer && InConsentRegion();

        /// <summary>Store the player's answer and tell the ad SDK at once.</summary>
        public static void Set(bool granted)
        {
            PlayerPrefs.SetInt(Key, granted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }

        /// <summary>Hand the stored answer to LevelPlay. Call before LevelPlay.Init. Outside the
        /// GDPR regions with no answer nothing is set, so the networks use their defaults.</summary>
        public static void Apply()
        {
            if (!HasAnswer) return;
            LevelPlayPrivacySettings.SetGDPRConsent(Granted);
        }

        // EU/EEA members plus the UK and Switzerland (ISO 3166 alpha-2, lower case).
        private static readonly string[] Region =
        {
            "at", "be", "bg", "hr", "cy", "cz", "dk", "ee", "fi", "fr", "de", "gr", "hu", "ie", "it",
            "lv", "lt", "lu", "mt", "nl", "pl", "pt", "ro", "sk", "si", "es", "se",
            "is", "li", "no", "gb", "uk", "ch",
        };

        /// <summary>Best guess at whether the player is in a consent region: SIM country, then the
        /// network's, then the device locale. When nothing is known, assume yes and ask.</summary>
        public static bool InConsentRegion()
        {
            string country = DeviceCountry();
            if (string.IsNullOrEmpty(country)) return true;
            return Array.IndexOf(Region, country) >= 0;
        }

        private static string DeviceCountry()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var phone = activity.Call<AndroidJavaObject>("getSystemService", "phone");
                if (phone != null)
                {
                    string sim = phone.Call<string>("getSimCountryIso");
                    if (!string.IsNullOrEmpty(sim)) return sim.ToLowerInvariant();
                    string net = phone.Call<string>("getNetworkCountryIso");
                    if (!string.IsNullOrEmpty(net)) return net.ToLowerInvariant();
                }
                using var locale = new AndroidJavaClass("java.util.Locale");
                using var current = locale.CallStatic<AndroidJavaObject>("getDefault");
                string loc = current.Call<string>("getCountry");
                return string.IsNullOrEmpty(loc) ? null : loc.ToLowerInvariant();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Ads] Could not read the device country: {e.Message}");
                return null;
            }
#else
            return null;   // editor and other platforms: unknown, so the prompt shows (testable)
#endif
        }
    }
}
