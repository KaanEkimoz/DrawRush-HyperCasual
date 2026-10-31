using UnityEngine;

namespace DrawRush.Audio
{
    /// <summary>The background loop. Lives on a shared object, so it keeps playing across level
    /// switches; the music switch mutes it without stopping it.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip loop;
        [SerializeField, Range(0f, 1f)] private float volume = 0.3f;

        private AudioSource _source;

        private void Awake()
        {
            AudioListener.volume = 1f;   // the old mute switch silenced everything through this
            _source = GetComponent<AudioSource>();
            _source.clip = loop;
            _source.loop = true;
            _source.volume = volume;
            _source.spatialBlend = 0f;
            _source.playOnAwake = false;
            _source.mute = SoundChannels.MusicMuted;
            if (loop != null) _source.Play();
        }

        private void Update() => _source.mute = SoundChannels.MusicMuted;
    }
}
