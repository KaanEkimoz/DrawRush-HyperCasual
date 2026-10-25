using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DrawRush.Core;

namespace DrawRush.Collection
{
    /// <summary>
    /// On the win panel: shows the 2D illustration of the shape just drawn, popping in with a little
    /// spin, its name underneath, and a "NEW!" badge the first time it joins the collection. Reads the
    /// active level from LevelManager and the first-clear flag from GameState, so it needs no wiring
    /// to the win flow. Unscaled time — the game is paused behind the panel.
    /// </summary>
    public sealed class WinShapeReveal : MonoBehaviour
    {
        [SerializeField] private ShapeCollection collection;
        [SerializeField] private Image illustration;
        [SerializeField] private TMP_Text nameLabel;
        [Tooltip("Shown only when this clear added the shape to the collection.")]
        [SerializeField] private GameObject newBadge;

        [SerializeField] private float popSeconds = 0.45f;
        [SerializeField] private float startDelay = 0.1f;

        private void OnEnable()
        {
            var levels = FindFirstObjectByType<LevelManager>();
            ShapeCollection.Entry entry = collection != null && levels != null
                ? collection.ForLevel(levels.CurrentIndex)
                : null;

            bool show = entry != null && entry.Illustration != null;
            if (illustration != null)
            {
                illustration.gameObject.SetActive(show);
                if (show) illustration.sprite = entry.Illustration;
            }
            if (nameLabel != null) nameLabel.text = entry != null ? entry.DisplayName : string.Empty;

            GameState state = GameServices.State;
            if (newBadge != null) newBadge.SetActive(show && state != null && state.LastShapeWasNew);

            if (show) _ = PopAsync(illustration.rectTransform);
        }

        private async Awaitable PopAsync(RectTransform target)
        {
            target.localScale = Vector3.zero;
            float t = -startDelay;
            try
            {
                while (t < popSeconds)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / popSeconds);
                    // Back-out ease: overshoots a touch, then settles.
                    const float s = 1.9f;
                    float x = k - 1f;
                    float scale = 1f + (s + 1f) * x * x * x + s * x * x;
                    target.localScale = Vector3.one * scale;
                    target.localRotation = Quaternion.Euler(0f, 0f, (1f - k) * -25f);
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
            }
            catch (System.OperationCanceledException) { return; }
            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;
        }
    }
}
