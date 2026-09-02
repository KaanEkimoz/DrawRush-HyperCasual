using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.Core
{
    /// <summary>
    /// Lights up the win panel's star row to match the rating just earned. Reads
    /// <see cref="GameState.LastStars"/> (set by WinCondition before the win), so it needs no
    /// wiring to the scoring — it just reflects what the last clear was worth.
    ///
    /// Earned stars pop in one after another for a beat of reward; unearned ones stay dimmed so
    /// the player sees exactly what they left on the table and has a reason to replay.
    /// </summary>
    public sealed class WinPanelStars : MonoBehaviour
    {
        [Tooltip("The three star Images, left to right. Fewer/more is allowed; the loop is bounded.")]
        [SerializeField] private Image[] stars;

        [Tooltip("Colour of an earned star.")]
        [SerializeField] private Color earned = new Color(1f, 0.82f, 0.15f, 1f);
        [Tooltip("Colour of a star the player didn't earn.")]
        [SerializeField] private Color unearned = new Color(0.25f, 0.25f, 0.30f, 0.55f);

        [Tooltip("Pop scale at the peak of a star appearing.")]
        [SerializeField] private float popScale = 1.35f;
        [Tooltip("Seconds each star takes to settle from the pop.")]
        [SerializeField] private float popSettle = 0.18f;
        [Tooltip("Stagger between stars appearing.")]
        [SerializeField] private float stagger = 0.14f;

        private GameState _state;

        private void OnEnable()
        {
            _state = GameServices.State;
            int earnedCount = _state != null ? Mathf.Clamp(_state.LastStars, 0, StarCount) : StarCount;
            Paint(earnedCount);
        }

        private int StarCount => stars != null ? stars.Length : 0;

        private void Paint(int earnedCount)
        {
            for (int i = 0; i < StarCount; i++)
            {
                if (stars[i] == null) continue;
                bool on = i < earnedCount;
                stars[i].color = on ? earned : unearned;
                // Earned stars start tiny and pop in on their own coroutine-free timer; unearned
                // ones just sit at rest so they read as "not yet".
                stars[i].transform.localScale = on ? Vector3.zero : Vector3.one;
            }
            if (earnedCount > 0 && isActiveAndEnabled) StartPops(earnedCount);
        }

        private void StartPops(int earnedCount)
        {
            for (int i = 0; i < earnedCount && i < StarCount; i++)
                if (stars[i] != null) StartCoroutine(PopStar(stars[i].transform, i * stagger));
        }

        private System.Collections.IEnumerator PopStar(Transform t, float delay)
        {
            // Unscaled: the win panel shows while gameplay time may be paused.
            float end = Time.unscaledTime + delay;
            while (Time.unscaledTime < end) yield return null;
            if (t == null) yield break;

            float dur = Mathf.Max(0.01f, popSettle);
            float start = Time.unscaledTime;
            for (float k = 0f; k < 1f; k = (Time.unscaledTime - start) / dur)
            {
                if (t == null) yield break;
                // 0 -> popScale -> 1: a quick overshoot that eases back to rest.
                float s = k < 0.5f
                    ? Mathf.Lerp(0f, popScale, k * 2f)
                    : Mathf.Lerp(popScale, 1f, (k - 0.5f) * 2f);
                t.localScale = Vector3.one * s;
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }
    }
}
