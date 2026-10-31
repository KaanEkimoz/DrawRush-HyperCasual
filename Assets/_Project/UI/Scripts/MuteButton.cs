using DrawRush.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// One-tap switch on the HUD for one sound channel, music or effects. The choice is remembered
    /// by <see cref="SoundChannels"/> and the button swaps between its "on" and "off" sprites.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class MuteButton : MonoBehaviour
    {
        private enum Channel { Sfx, Music }

        [SerializeField] private Channel channel;
        [Tooltip("The Image to swap (usually this button's own Image).")]
        [SerializeField] private Image icon;
        [Tooltip("Shown when the channel is ON.")]
        [SerializeField] private Sprite onSprite;
        [Tooltip("Shown when the channel is muted.")]
        [SerializeField] private Sprite offSprite;

        private bool Muted
        {
            get => channel == Channel.Music ? SoundChannels.MusicMuted : SoundChannels.SfxMuted;
            set
            {
                if (channel == Channel.Music) SoundChannels.MusicMuted = value;
                else SoundChannels.SfxMuted = value;
            }
        }

        private void Awake()
        {
            Apply();
            GetComponent<Button>().onClick.AddListener(Toggle);
        }

        private void Toggle()
        {
            Muted = !Muted;
            Apply();
        }

        private void Apply()
        {
            if (icon != null && onSprite != null && offSprite != null)
                icon.sprite = Muted ? offSprite : onSprite;
        }
    }
}
