using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// Marks some of a level's edges as already drawn the moment the level opens, so the tutorial
    /// can show a whole square with only one side left for the player. It teaches the goal —
    /// "close the shape" — without asking a first-time player to draw four edges.
    ///
    /// The edges are REAL edges, not decorative stand-ins. Fake walls would have to imitate the
    /// real ones and would drift away from them: thickness, colour and corner posts are all tuned
    /// in one place, and a copy silently misses every future change (the walls were thinned 10%
    /// once already). Instead only the "starts finished" idea is special, and it lives here —
    /// inside the tutorial group — so nothing in the shared drawing code knows the tutorial exists.
    ///
    /// The win condition needs no help: EdgeNetwork just counts unpainted edges down to zero, so
    /// pre-painting three of four leaves exactly one, and finishing it wins as usual.
    /// </summary>
    [DefaultExecutionOrder(100)]   // after EdgeNetwork.OnEnable has rebuilt the edges
    public sealed class TutorialPrePainted : MonoBehaviour
    {
        [Tooltip("Network that owns these edges. Auto-resolved from this level group if empty.")]
        [SerializeField] private EdgeNetwork network;

        [Tooltip("Edges that begin fully drawn. Leave the one the player is meant to draw out of " +
                 "this list.")]
        [SerializeField] private DrawEdgeAuthor[] alreadyDrawn;

        // OnEnable, not Start: the mega-scene never reloads, so a level group is switched off and
        // on again for every retry. Start would run once and a restarted tutorial would come back
        // with all four sides waiting to be drawn.
        private void OnEnable()
        {
            if (network == null) network = GetComponentInParent<EdgeNetwork>();
            if (network == null) network = GetComponentInChildren<EdgeNetwork>(true);
            if (network == null || alreadyDrawn == null) return;

            foreach (DrawEdgeAuthor author in alreadyDrawn)
            {
                if (author == null || !author.IsValid) continue;
                foreach (DrawEdge edge in network.Edges)
                {
                    if (edge.A != author.AnchorA || edge.B != author.AnchorB) continue;
                    edge.PaintFrom(edge.A, 1f);   // fills the whole span; the wall rises with it
                    break;
                }
            }
        }
    }
}
