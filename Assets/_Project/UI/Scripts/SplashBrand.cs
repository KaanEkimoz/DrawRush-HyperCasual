using UnityEngine;

namespace DrawRush.UI
{
    /// <summary>
    /// Studio splash: pops the logo in with a soft overshoot and fades it in, holds, then fades
    /// out just before the scene timer moves on. Replaces the old video, which still carried the
    /// previous studio's branding. Unscaled time, so it never depends on timeScale.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SplashBrand : MonoBehaviour
    {
        [Tooltip("Logo root that scales in (title + subtitle).")]
        [SerializeField] private RectTransform logo;
        [SerializeField] private float popSeconds = 0.55f;
        [Tooltip("Seconds after start when the fade-out begins. Keep it under the scene timer.")]
        [SerializeField] private float fadeOutAt = 1.7f;
        [SerializeField] private float fadeOutSeconds = 0.35f;
        [Tooltip("Peak overshoot of the pop (1 = none).")]
        [SerializeField] private float overshoot = 1.12f;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (logo == null) logo = (RectTransform)transform;
        }

        private async void Start()
        {
            float t = 0f;
            try
            {
                while (t < fadeOutAt + fadeOutSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    float pop = Mathf.Clamp01(t / popSeconds);
                    // Ease out to the overshoot, then settle back to 1.
                    float scale = pop < 0.7f
                        ? Mathf.Lerp(0.6f, overshoot, EaseOut(pop / 0.7f))
                        : Mathf.Lerp(overshoot, 1f, EaseOut((pop - 0.7f) / 0.3f));
                    logo.localScale = Vector3.one * scale;

                    float alpha = Mathf.Clamp01(t / (popSeconds * 0.6f));
                    if (t > fadeOutAt) alpha = 1f - Mathf.Clamp01((t - fadeOutAt) / fadeOutSeconds);
                    _group.alpha = alpha;

                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
            }
            catch (System.OperationCanceledException) { }
        }

        private static float EaseOut(float x) => 1f - (1f - x) * (1f - x) * (1f - x);
    }
}
