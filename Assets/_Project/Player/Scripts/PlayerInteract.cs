using System.Collections;
using UnityEngine;
using Studios208.DrawRush.Common;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Owns the drawing session state (current trail + previous-part pointer) and
    /// translates trigger events into IDrawPart calls. Width / destroy delay come
    /// from <see cref="GameConfig"/>. Connecting LineRenderer is added to the
    /// previous part on completion (visual artefact of the connection).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerInteract : MonoBehaviour
    {
        [Header("Refs"), Space]
        [SerializeField] private Material lineMaterial;

        [Header("Tags"), Space]
        [SerializeField] private string drawAreaTag = "DrawArea";

        private bool _isDrawing;
        private GameObject _activeTrail;
        private GameObject _previousPart;
        private bool _canDraw;

        /// <summary>True while a trail is currently anchored to the player.</summary>
        public bool IsDrawing => _isDrawing;

        /// <summary>Called by DrawPart.Interact when it attaches its trail to the player.</summary>
        public void BeginDrawing(GameObject trail)
        {
            _isDrawing = true;
            _activeTrail = trail;
        }

        /// <summary>Called by DrawPart.CompleteDraw when the connection finalizes.
        /// Reparents the active trail under <paramref name="reparentTrailTo"/>.</summary>
        public void EndDrawing(Transform reparentTrailTo)
        {
            _isDrawing = false;
            if (_activeTrail != null && reparentTrailTo != null)
            {
                _activeTrail.transform.SetParent(reparentTrailTo, worldPositionStays: false);
            }
            _activeTrail = null;
        }

        private void Awake()
        {
            _isDrawing = false;
            _previousPart = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(drawAreaTag))
            {
                _canDraw = true;
                return;
            }

            if (!_canDraw) return;

            var interactable = other.GetComponent<IInteractable>();
            if (interactable == null) return;
            var drawPart = other.GetComponent<IDrawPart>();
            if (drawPart == null) return;

            // First anchor — arm the part and remember it as the previous.
            if (_previousPart == null)
            {
                drawPart.OnPlayerEntered();
                drawPart.Interact();
                _previousPart = other.gameObject;
                return;
            }

            // Already armed but not yet drawing, or hit the same part again — bail.
            if (!_isDrawing || _previousPart == other.gameObject) return;

            // Second anchor while drawing — finalize the connection.
            drawPart.Interact();

            var previousDrawPart = _previousPart.GetComponent<IDrawPart>();
            previousDrawPart?.Complete();
            drawPart.Complete();

            SpawnConnectionLine(other.transform.position, _previousPart.transform.position);

            if (_activeTrail != null) Destroy(_activeTrail);
            _activeTrail = null;
            _previousPart = null;
            _isDrawing = false;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(drawAreaTag)) return;

            if (_activeTrail != null) Destroy(_activeTrail);
            _activeTrail = null;
            _canDraw = false;
            _isDrawing = false;

            if (_previousPart != null)
            {
                var drawPart = _previousPart.GetComponent<IDrawPart>();
                drawPart?.OnPlayerExited();
                _previousPart = null;
            }
        }

        private void SpawnConnectionLine(Vector3 toWorld, Vector3 fromWorld)
        {
            if (_previousPart == null) return;
            var lineRenderer = _previousPart.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, fromWorld, toWorld);
        }

        private void ConfigureLineRenderer(LineRenderer lineRenderer, Vector3 startPosition, Vector3 endPosition)
        {
            startPosition.y = 0f;
            endPosition.y = 0f;

            float width = GameServices.Config != null ? GameServices.Config.lineWidth : 0.4f;
            float destroyDelay = GameServices.Config != null ? GameServices.Config.lineDestroyDelay : 2.0f;

            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, endPosition);

            StartCoroutine(DestroyLineAfter(lineRenderer, destroyDelay));
        }

        private static IEnumerator DestroyLineAfter(LineRenderer lineRenderer, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (lineRenderer != null) Destroy(lineRenderer);
        }
    }
}
