using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// A corner filler that rises from below ground when both edges meeting at the corner are
    /// painted. Spawned and driven by <see cref="EdgeNetwork"/>; angle-agnostic (a vertical
    /// post fits any corner — square, triangle, hexagon, …). Pure visual: it animates its own
    /// transform, nothing else depends on it.
    /// </summary>
    public sealed class CornerPost : MonoBehaviour
    {
        private float _riseSeconds = 0.4f;
        private Vector3 _shown;
        private Vector3 _hidden;
        private bool _revealed;

        /// <summary>Capture the shown position and drop the post below ground (hidden state).</summary>
        public void Init(float riseSeconds, float sinkDepth)
        {
            _riseSeconds = Mathf.Max(0.01f, riseSeconds);
            _shown = transform.localPosition;
            _hidden = _shown + Vector3.down * Mathf.Max(0.1f, sinkDepth);
            transform.localPosition = _hidden;
            _revealed = false;
        }

        /// <summary>Rise into view. Idempotent.</summary>
        public void Reveal()
        {
            if (_revealed) return;
            _revealed = true;
            _ = RiseAsync();
        }

        private async Awaitable RiseAsync()
        {
            float t = 0f;
            try
            {
                while (t < _riseSeconds)
                {
                    t += Time.deltaTime;
                    transform.localPosition = Vector3.LerpUnclamped(
                        _hidden, _shown, Mathf.SmoothStep(0f, 1f, t / _riseSeconds));
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
            }
            catch (System.OperationCanceledException) { return; }
            transform.localPosition = _shown;
        }
    }
}
