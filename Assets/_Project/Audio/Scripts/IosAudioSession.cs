using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DrawRush.Audio
{
    /// <summary>
    /// iOS: lets the game be heard with the ring/silent switch on (Unity's default Ambient session
    /// obeys the switch, so most iPhones played nothing). Applied after launch, whenever the app
    /// comes back to the front, and after every full-screen ad, because ad SDKs set their own
    /// session category while a video plays. Does nothing on other platforms.
    /// </summary>
    public sealed class IosAudioSession : MonoBehaviour
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void DrawRush_UsePlaybackAudioSession();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Spawn()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (FindFirstObjectByType<IosAudioSession>() != null) return;
            var go = new GameObject(nameof(IosAudioSession));
            DontDestroyOnLoad(go);
            go.AddComponent<IosAudioSession>();
            Apply();
#endif
        }

        /// <summary>Re-applies the playback category. Call after anything that may change it (ads).</summary>
        public static void Apply()
        {
#if UNITY_IOS && !UNITY_EDITOR
            DrawRush_UsePlaybackAudioSession();
#endif
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) Apply();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) Apply();
        }
    }
}
