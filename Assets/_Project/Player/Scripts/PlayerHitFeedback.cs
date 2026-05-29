using UnityEngine;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Juice for taking damage: flashes the player's meshes toward a color and gives a
    /// quick scale "squash" (shrink then back) on the visuals. Driven by
    /// <see cref="Play"/> from PlayerCombat.TakeDamage — purely cosmetic, no gameplay state.
    ///
    /// Colors are applied through a MaterialPropertyBlock so no material instances are
    /// created and the original colors are restored when the pulse ends (or the object is
    /// disabled mid-pulse).
    /// </summary>
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [Tooltip("Transform that gets the scale punch. Defaults to a child named 'Visuals', " +
                 "else this GameObject. Should NOT be the CharacterController root.")]
        [SerializeField] private Transform punchTarget;
        [Header("Flash")]
        [SerializeField] private Color flashColor = Color.red;
        [Range(0f, 1f)]
        [Tooltip("How far the mesh color lerps toward flashColor at each blink peak.")]
        [SerializeField] private float flashStrength = 0.85f;
        [Tooltip("How long the red flash lasts, in seconds.")]
        [SerializeField] private float flashDuration = 1f;
        [Range(1, 8)]
        [Tooltip("How many red pulses (blinks) happen within flashDuration.")]
        [SerializeField] private int flashBlinks = 3;

        [Header("Punch")]
        [Range(0f, 0.9f)]
        [Tooltip("Fraction the visuals shrink by at the peak of the punch (0.2 = down to 80%, " +
                 "then back to normal).")]
        [SerializeField] private float punchScale = 0.2f;
        [Tooltip("How long the scale punch lasts, in seconds.")]
        [SerializeField] private float punchDuration = 0.8f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private Color[] _baseColors;
        private int[] _colorProp;          // BaseColorId, ColorId, or 0 (none)
        private Vector3 _baseScale;
        private bool _playing;

        private void Awake()
        {
            if (punchTarget == null)
            {
                Transform v = transform.Find("Visuals");
                punchTarget = v != null ? v : transform;
            }
            _baseScale = punchTarget.localScale;

            _renderers = GetComponentsInChildren<Renderer>(true);
            _mpb = new MaterialPropertyBlock();
            _baseColors = new Color[_renderers.Length];
            _colorProp = new int[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material m = _renderers[i].sharedMaterial;
                if (m != null && m.HasProperty(BaseColorId)) { _colorProp[i] = BaseColorId; _baseColors[i] = m.GetColor(BaseColorId); }
                else if (m != null && m.HasProperty(ColorId)) { _colorProp[i] = ColorId; _baseColors[i] = m.GetColor(ColorId); }
                else { _colorProp[i] = 0; _baseColors[i] = Color.white; }
            }
        }

        /// <summary>Play one flash + squash. Ignored if one is already running.</summary>
        public void Play()
        {
            if (!isActiveAndEnabled || _playing) return;
            _ = RunAsync();
        }

        private async Awaitable RunAsync()
        {
            _playing = true;
            float t = 0f;
            float total = Mathf.Max(flashDuration, punchDuration);
            try
            {
                while (t < total)
                {
                    t += Time.deltaTime;

                    // Flash: |sin| with flashBlinks half-cycles → that many red pulses.
                    if (t <= flashDuration && flashDuration > 0f)
                    {
                        float fp = t / flashDuration;                 // 0..1
                        float k = Mathf.Abs(Mathf.Sin(fp * Mathf.PI * flashBlinks));
                        ApplyColor(k * flashStrength);
                    }
                    else ApplyColor(0f);

                    // Punch: a single 0 → shrink → 0 pulse on its own clock (shrink, then back).
                    if (t <= punchDuration && punchDuration > 0f)
                    {
                        float pp = t / punchDuration;                 // 0..1
                        float s = Mathf.Sin(pp * Mathf.PI);
                        punchTarget.localScale = _baseScale * (1f - punchScale * s);
                    }
                    else punchTarget.localScale = _baseScale;

                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                ApplyColor(0f);
                if (punchTarget != null) punchTarget.localScale = _baseScale;
                _playing = false;
            }
        }

        private void ApplyColor(float amount)
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_colorProp[i] == 0 || _renderers[i] == null) continue;
                _renderers[i].GetPropertyBlock(_mpb);
                _mpb.SetColor(_colorProp[i], Color.Lerp(_baseColors[i], flashColor, amount));
                _renderers[i].SetPropertyBlock(_mpb);
            }
        }

        // If the player is disabled mid-pulse (level switch), don't leave it red/shrunk.
        private void OnDisable()
        {
            ApplyColor(0f);
            if (punchTarget != null) punchTarget.localScale = _baseScale;
            _playing = false;
        }
    }
}
