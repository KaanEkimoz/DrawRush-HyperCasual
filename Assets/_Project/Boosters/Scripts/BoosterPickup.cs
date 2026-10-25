using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// One booster lying on the field: bobs and spins so it reads as collectable, blinks in its
    /// last seconds, and hands itself to the spawner when the player touches it.
    /// </summary>
    public sealed class BoosterPickup : MonoBehaviour
    {
        private BoosterSpawner _owner;
        private Renderer _renderer;
        private Transform _label;
        private float _born, _lifetime, _blink;
        private Vector3 _basePos;

        public BoosterType Type { get; private set; }

        public void Init(BoosterSpawner owner, BoosterType type, float lifetime, float blinkSeconds)
        {
            _owner = owner;
            Type = type;
            _lifetime = lifetime;
            _blink = blinkSeconds;
            _born = Time.time;
            _basePos = transform.position;
            _renderer = transform.GetChild(0).GetComponent<Renderer>();
            _label = transform.Find("Label");
        }

        private void Update()
        {
            float age = Time.time - _born;
            if (age >= _lifetime) { _owner.Expired(this); return; }
            transform.position = _basePos + Vector3.up * (0.15f * Mathf.Sin(age * 3f));
            if (_renderer != null)
            {
                _renderer.transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);   // spin the body, not the label
                float left = _lifetime - age;
                _renderer.enabled = left > _blink || Mathf.Repeat(age * 8f, 1f) < 0.6f;
            }
            // The label faces the camera (the gameplay camera never rotates, so this is cheap).
            var cam = UnityEngine.Camera.main;
            if (_label != null && cam != null) _label.rotation = cam.transform.rotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) _owner.Collected(this);
        }
    }
}
