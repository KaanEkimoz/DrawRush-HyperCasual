using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrawRush.Localization
{
    public enum Language { English = 0, Turkish = 1, Spanish = 2 }

    /// <summary>
    /// The game's text in English, Turkish and Spanish. Code and scenes keep writing English; the
    /// English text is the key, and <see cref="T"/> returns the current language's version (or the
    /// English when a line has no translation yet). The choice is saved; on first launch it follows
    /// the device language.
    /// </summary>
    public static class Loc
    {
        private const string Key = "Language";

        /// <summary>Raised after the language changes.</summary>
        public static event Action Changed;

        public static Language Current
        {
            get
            {
                if (_current < 0) _current = PlayerPrefs.HasKey(Key) ? Mathf.Clamp(PlayerPrefs.GetInt(Key), 0, 2) : (int)FromDevice();
                return (Language)_current;
            }
            set
            {
                if ((int)value == _current && PlayerPrefs.HasKey(Key)) return;
                _current = (int)value;
                PlayerPrefs.SetInt(Key, _current);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }
        private static int _current = -1;

        public static Language FromDevice() => Application.systemLanguage switch
        {
            SystemLanguage.Turkish => Language.Turkish,
            SystemLanguage.Spanish => Language.Spanish,
            _ => Language.English,
        };

        /// <summary>Native name of a language, for the picker.</summary>
        public static string NameOf(Language l) => l switch
        {
            Language.Turkish => "Türkçe",
            Language.Spanish => "Español",
            _ => "English",
        };

        /// <summary>The current language's version of <paramref name="english"/>.</summary>
        public static string T(string english)
        {
            if (string.IsNullOrEmpty(english) || Current == Language.English) return english;
            if (Strings.Table.TryGetValue(english, out string[] t))
            {
                string s = t[(int)Current - 1];
                if (!string.IsNullOrEmpty(s)) return s;
            }
            return english;
        }

        /// <summary><see cref="T(string)"/> then string.Format with <paramref name="args"/>.</summary>
        public static string F(string englishFormat, params object[] args) => string.Format(T(englishFormat), args);

        /// <summary>Upper-case in the current language (Turkish i → İ).</summary>
        public static string Upper(string s) => Current == Language.Turkish
            ? s.ToUpper(new System.Globalization.CultureInfo("tr-TR"))
            : s.ToUpperInvariant();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Changed = null; _current = -1; }
    }
}
