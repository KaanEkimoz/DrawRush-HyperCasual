using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// A corner anchor for the edge-painting puzzle — the sphere the player touches to start
    /// or continue painting. Edges are authored separately (see <see cref="DrawEdgeAuthor"/> /
    /// the Kenar prefab); DrawPart only exposes its transform and toggles an optional highlight
    /// while the player is touching it.
    /// </summary>
    public sealed class DrawPart : MonoBehaviour
    {
        [Header("Visuals (optional)")]
        [Tooltip("Optional child GameObject toggled on while the player is touching this anchor.")]
        [SerializeField] private GameObject armedHighlight;

        public Transform Transform => transform;

        private void Awake() => SetHighlight(false);

        /// <summary>Highlight on while the player is touching this anchor.</summary>
        public void OnPlayerEntered() => SetHighlight(true);

        /// <summary>Highlight off when the player leaves this anchor.</summary>
        public void OnPlayerExited() => SetHighlight(false);

        private void SetHighlight(bool on)
        {
            if (armedHighlight != null && armedHighlight.activeSelf != on)
            {
                armedHighlight.SetActive(on);
            }
        }
    }
}
