using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Short vibration pulses for game feedback (a pickup, a hit). Android gets a real one-shot
    /// pulse through the system Vibrator (Unity's Handheld.Vibrate is a long ~400 ms buzz); other
    /// platforms and the editor do nothing. The player can turn it off in Settings.
    /// </summary>
    public static class Haptics
    {
        private const string EnabledKey = "HapticsEnabled";

        /// <summary>Player setting, on by default.</summary>
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(EnabledKey, 1) == 1;
            set { PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>A tick: pickups.</summary>
        public static void Light() => Pulse(18, 90);

        /// <summary>A thump: hits, a breaking shield.</summary>
        public static void Medium() => Pulse(40, 200);

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static bool _resolved;

        private static void Pulse(long ms, int amplitude)
        {
            if (!Enabled) return;
            try
            {
                if (!_resolved)
                {
                    _resolved = true;
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (_vibrator == null) return;
                using var effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                _vibrator.Call("vibrate", effect);
            }
            catch (System.Exception)
            {
                Handheld.Vibrate();   // very old devices; also makes Unity add the VIBRATE permission
            }
        }
#else
        private static void Pulse(long ms, int amplitude) { }
#endif
    }
}
