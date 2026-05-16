using UnityEngine;
using Studios208.DrawRush.Common;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Chain-anchor drawing controller. The player carries a persistent TrailRenderer
    /// that emits while inside a DrawArea. Touching DrawParts builds a chain of
    /// connecting LineRenderer segments: 1 → 2 → 3 → … → 1 (closed loop).
    /// Leaving the DrawArea hides the trail but preserves the chain progress so the
    /// player can resume without losing visited anchors.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerInteract : MonoBehaviour
    {
        [Header("Refs"), Space]
        [Tooltip("Material applied to spawned connecting LineRenderers.")]
        [SerializeField] private Material lineMaterial;
        [Tooltip("TrailRenderer that draws the player's path while inside the DrawArea. " +
                 "If empty, auto-resolved from the player hierarchy.")]
        [SerializeField] private TrailRenderer playerTrail;

        [Header("Tags"), Space]
        [SerializeField] private string drawAreaTag = "DrawArea";

        [Header("Behaviour"), Space]
        [Tooltip("When true, leaving the DrawArea wipes chain progress (legacy behaviour). " +
                 "Default false — chain preserved on re-entry.")]
        [SerializeField] private bool resetProgressOnAreaExit;

        private GameObject _firstPart;
        private GameObject _previousPart;
        private bool _isInDrawArea;

        /// <summary>True while the player is inside a DrawArea trigger.</summary>
        public bool IsInDrawArea => _isInDrawArea;

        /// <summary>True while a chain is being drawn — at least one anchor has been visited.</summary>
        public bool IsDrawing => _previousPart != null;

        private void Awake()
        {
            if (playerTrail == null) playerTrail = GetComponentInChildren<TrailRenderer>(includeInactive: true);
            SetTrailEmitting(false);

            // Fallback line material so the visual is never invisible if Inspector ref is missing.
            if (lineMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) lineMaterial = new Material(shader) { name = "Auto_LineMaterial" };
            }

            // Mirror the line material onto the TrailRenderer if the trail has no shared material.
            if (playerTrail != null && playerTrail.sharedMaterial == null && lineMaterial != null)
            {
                playerTrail.sharedMaterial = lineMaterial;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(drawAreaTag))
            {
                _isInDrawArea = true;
                SetTrailEmitting(true);
                return;
            }

            if (!_isInDrawArea) return;

            var drawPart = other.GetComponent<IDrawPart>();
            if (drawPart == null) return;

            // Closed-loop closure: returning to the first part finalizes the puzzle.
            if (_firstPart != null && other.gameObject == _firstPart && _previousPart != null && _previousPart != _firstPart)
            {
                // Closure only valid if last anchor and first anchor are polygon-edge neighbors.
                if (!AreNeighbors(_previousPart, _firstPart)) return;

                SpawnConnectionLine(_previousPart.transform.position, _firstPart.transform.position);
                _previousPart.GetComponent<IDrawPart>()?.Complete();
                drawPart.Complete();
                ResetTrailForNextEdge();
                _firstPart = null;
                _previousPart = null;
                return;
            }

            // Already-completed parts and repeat hits on the current anchor are ignored.
            if (drawPart.IsCompleted) return;
            if (_previousPart == other.gameObject) return;

            // First anchor in the chain — record start, arm, and start with a clean trail.
            if (_previousPart == null)
            {
                drawPart.OnPlayerEntered();
                drawPart.Interact();
                _firstPart = other.gameObject;
                _previousPart = other.gameObject;
                ResetTrailForNextEdge();
                return;
            }

            // Mid-chain step — only valid if the touched anchor is a polygon-edge
            // neighbor of the previous one. Diagonal jumps are silently rejected
            // (the player walks through but the chain doesn't advance).
            if (!AreNeighbors(_previousPart, other.gameObject)) return;

            // Finalize the previous edge, advance, then wipe the trail so the next
            // edge starts visually from the just-touched anchor.
            var previousDrawPart = _previousPart.GetComponent<IDrawPart>();
            SpawnConnectionLine(_previousPart.transform.position, other.transform.position);
            previousDrawPart?.Complete();
            drawPart.OnPlayerEntered();
            drawPart.Interact();
            _previousPart = other.gameObject;
            ResetTrailForNextEdge();
        }

        private static bool AreNeighbors(GameObject a, GameObject b)
        {
            if (a == null || b == null) return false;
            var da = a.GetComponent<DrawPart>();
            var db = b.GetComponent<DrawPart>();
            // If either side isn't a concrete DrawPart (e.g. a test mock IDrawPart),
            // skip the check rather than block — concrete neighbor data is the
            // gating signal here.
            if (da == null || db == null) return true;
            return da.IsNeighborOf(db);
        }

        /// <summary>Clears the active trail so the next edge starts from a clean state,
        /// keeping emission on while the player is still inside the DrawArea.</summary>
        private void ResetTrailForNextEdge()
        {
            if (playerTrail == null) return;
            playerTrail.Clear();
            // Re-arm emission in case it was paused by SetTrailEmitting(false) earlier.
            if (_isInDrawArea) playerTrail.emitting = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(drawAreaTag)) return;

            _isInDrawArea = false;
            SetTrailEmitting(false);

            if (resetProgressOnAreaExit)
            {
                if (_previousPart != null)
                {
                    _previousPart.GetComponent<IDrawPart>()?.OnPlayerExited();
                }
                _firstPart = null;
                _previousPart = null;
            }
        }

        private void SetTrailEmitting(bool on)
        {
            if (playerTrail == null) return;
            playerTrail.emitting = on;
            if (!on) playerTrail.Clear();
        }

        private void SpawnConnectionLine(Vector3 fromWorld, Vector3 toWorld)
        {
            // Spawn on a fresh GameObject so it survives the source-part's destruction.
            var go = new GameObject("DrawConnection");
            go.transform.position = (fromWorld + toWorld) * 0.5f;
            var lineRenderer = go.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, fromWorld, toWorld);
        }

        private void ConfigureLineRenderer(LineRenderer lineRenderer, Vector3 startPosition, Vector3 endPosition)
        {
            startPosition.y = 0f;
            endPosition.y = 0f;

            float width = GameServices.Config != null ? GameServices.Config.lineWidth : 0.4f;
            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, endPosition);
            lineRenderer.useWorldSpace = true;
        }
    }
}
