using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// One-tap sound toggle on the HUD: flips <see cref="AudioListener.volume"/> between full and
    /// silent, remembers the choice in PlayerPrefs so it survives relaunches, and swaps between an
    /// "on" and "off" speaker sprite. Mobile players expect to be able to mute a game; this is that.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class MuteButton : MonoBehaviour
    {
        [Tooltip("The speaker Image to swap (usually this button's own Image).")]
        [SerializeField] private Image icon;
        [Tooltip("Shown when sound is ON.")]
        [SerializeField] private Sprite onSprite;
        [Tooltip("Shown when sound is muted.")]
        [SerializeField] private Sprite offSprite;

        private const string Key = "Muted";
        private bool _muted;

        private void Awake()
        {
            _muted = PlayerPrefs.GetInt(Key, 0) == 1;
            Apply();
            GetComponent<Button>().onClick.AddListener(Toggle);
        }

        // Re-assert on every enable so a scene/level reactivation can't leave the volume wrong.
        private void OnEnable() => AudioListener.volume = _muted ? 0f : 1f;

        private void Toggle()
        {
            _muted = !_muted;
            PlayerPrefs.SetInt(Key, _muted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        private void Apply()
        {
            AudioListener.volume = _muted ? 0f : 1f;
            if (icon != null && onSprite != null && offSprite != null)
                icon.sprite = _muted ? offSprite : onSprite;
        }
    }
}
