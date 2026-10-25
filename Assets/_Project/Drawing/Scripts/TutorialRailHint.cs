using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// The tutorial's "walk along this line" sign: a left-right arrow over the one edge left to
    /// draw, shown from the start and gone once it is drawn. With free drawing there is no rail to
    /// take hold of the player any more, so the sign no longer waits for one; it simply points at
    /// the stretch to walk until it is done.
    ///
    /// This lives in the tutorial group, so it exists only while the tutorial is the active level
    /// — no "am I the tutorial?" check anywhere, and nothing to strip later.
    ///
    /// It is a billboard, not a ground decal like the overhead arrow: this one is a MESSAGE rather
    /// than a direction, so it should stay square-on and legible instead of foreshortening into the
    /// floor. That it happens to read left-right across a rail that is horizontal on screen is what
    /// makes the message land.
    /// </summary>
    public sealed class TutorialRailHint : MonoBehaviour
    {
        [Tooltip("The sign itself. Kept as a child so this component can survive it being off.")]
        [SerializeField] private GameObject sign;

        [Tooltip("Edge this hint is about. The sign hides for good once it has been drawn.")]
        [SerializeField] private DrawEdgeAuthor edge;

        [Tooltip("Network that owns the edge — used to tell when it has been drawn.")]
        [SerializeField] private EdgeNetwork network;

        [Tooltip("Seconds to fade in, so it arrives rather than pops.")]
        [SerializeField] private float fadeSeconds = 0.25f;

        private SpriteRenderer _renderer;
        private float _shown;

        private void OnEnable()
        {
            _shown = 0f;
            if (sign != null)
            {
                _renderer = sign.GetComponentInChildren<SpriteRenderer>(true);
                sign.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (sign == null) return;

            bool show = !IsEdgeDrawn();

            if (show && !sign.activeSelf) sign.SetActive(true);
            if (!show && sign.activeSelf && _shown <= 0f) sign.SetActive(false);

            _shown = Mathf.MoveTowards(_shown, show ? 1f : 0f,
                                       Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
            if (_renderer != null)
            {
                Color c = _renderer.color;
                c.a = _shown;
                _renderer.color = c;
            }
            if (_shown <= 0f && sign.activeSelf && !show) sign.SetActive(false);
        }

        // Unscaled time above means the sign is already up during the 3-2-1 countdown, when
        // timeScale is 0, so the player knows where to go the moment they can move.
        private bool IsEdgeDrawn()
        {
            if (edge == null || network == null || !edge.IsValid) return false;
            foreach (DrawEdge e in network.Edges)
                if (e.A == edge.AnchorA && e.B == edge.AnchorB) return e.IsComplete;
            return false;
        }
    }
}
