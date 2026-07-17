using UnityEngine;
using DrawRush.Core;
using DrawRush.Player;

namespace DrawRush.Drawing
{
    /// <summary>
    /// The tutorial's "you can only slide along this line" sign: a left-right arrow that appears
    /// over the edge the moment the rail takes hold of the player, and goes away once they have
    /// drawn it. Touching an anchor takes control of movement away from the stick, which is the
    /// one moment in the game that needs explaining and never explained itself.
    ///
    /// This lives in the tutorial group, so it exists only while the tutorial is the active level
    /// — no "am I the tutorial?" check anywhere, and nothing to strip later.
    ///
    /// It is a billboard, not a ground decal like the overhead arrow: this one is a MESSAGE rather
    /// than a direction, so it should stay square-on and legible instead of foreshortening into the
    /// floor. That it happens to read left-right across a rail that is horizontal on screen is what
    /// makes the message land.
    /// </summary>
    [DefaultExecutionOrder(60)]   // after RailPaintController has refreshed its guidance
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
        private RailPaintController _rail;
        private float _shown;

        private void OnEnable()
        {
            _shown = 0f;
            if (sign != null)
            {
                _renderer = sign.GetComponentInChildren<SpriteRenderer>(true);
                sign.SetActive(false);
            }
            // The player is a shared object that outlives every level, so it cannot be wired in the
            // inspector from inside a level group — resolve it when the group wakes.
            _rail = null;
        }

        private void LateUpdate()
        {
            if (sign == null) return;

            if (_rail == null)
            {
                Transform player = GameServices.Player;
                if (player == null) return;
                _rail = player.GetComponent<RailPaintController>();
                if (_rail == null) return;
            }

            bool onRail = _rail.TryGetGuidance(out _, out _);
            bool stillNeeded = !IsEdgeDrawn();
            bool show = onRail && stillNeeded;

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

        // Unscaled time above and this check together mean the sign behaves during the 3-2-1
        // countdown too, when timeScale is 0 and the player is already standing on the rail.
        private bool IsEdgeDrawn()
        {
            if (edge == null || network == null || !edge.IsValid) return false;
            foreach (DrawEdge e in network.Edges)
                if (e.A == edge.AnchorA && e.B == edge.AnchorB) return e.IsComplete;
            return false;
        }
    }
}
