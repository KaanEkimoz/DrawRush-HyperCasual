using TMPro;
using UnityEngine;

namespace DrawRush.Localization
{
    /// <summary>
    /// Put on a scene text written in English: it shows the current language's version and follows
    /// language changes. The English it started with is kept as the key.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [Tooltip("English source text. Filled from the label on first run when empty.")]
        [SerializeField] private string english;
        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(english)) english = _text.text;
        }

        private void OnEnable()
        {
            Loc.Changed += Apply;
            Apply();
        }

        private void OnDisable() => Loc.Changed -= Apply;

        private void Apply()
        {
            if (_text != null && !string.IsNullOrEmpty(english)) _text.text = Loc.T(english);
        }
    }
}
