using UnityEngine;

namespace DrawRush.Audio
{
    /// <summary>The two sound switches the player has, remembered across launches. Anyone who had
    /// the old single mute on starts with both off.</summary>
    public static class SoundChannels
    {
        private const string OldMuteKey = "Muted", MusicKey = "MusicMuted", SfxKey = "SfxMuted";

        public static bool MusicMuted
        {
            get => Read(MusicKey);
            set => Write(MusicKey, value);
        }

        public static bool SfxMuted
        {
            get => Read(SfxKey);
            set => Write(SfxKey, value);
        }

        private static bool Read(string key) => PlayerPrefs.GetInt(key, PlayerPrefs.GetInt(OldMuteKey, 0)) == 1;

        private static void Write(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
