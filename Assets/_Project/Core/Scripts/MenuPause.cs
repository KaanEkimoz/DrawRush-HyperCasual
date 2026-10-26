using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// The one owner of "a menu is covering the game". Menus (settings, shop, collection) call
    /// <see cref="Open"/>/<see cref="Close"/>; game flow (countdown, level start, revive) sets its
    /// time scale through <see cref="SetGameTimeScale"/>. While a menu is open the game's wish is
    /// stored instead of applied, and the last menu to close restores it. Without this, opening
    /// settings during the 3-2-1 countdown froze the game for good: the countdown un-paused
    /// underneath the menu, then closing the menu restored the stale 0 it had saved.
    /// </summary>
    public static class MenuPause
    {
        private static int _openCount;
        private static float _resumeScale = 1f;

        public static bool IsOpen => _openCount > 0;

        public static void Open()
        {
            if (_openCount++ == 0) _resumeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        public static void Close()
        {
            if (_openCount == 0) return;
            if (--_openCount == 0) Time.timeScale = _resumeScale;
        }

        /// <summary>Game-flow time scale: applied now, or on the last menu close if one is open.</summary>
        public static void SetGameTimeScale(float scale)
        {
            if (IsOpen) _resumeScale = scale;
            else Time.timeScale = scale;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _openCount = 0;
            _resumeScale = 1f;
        }
    }
}
