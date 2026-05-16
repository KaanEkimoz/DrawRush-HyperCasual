using System.Collections;
using UnityEngine;
using Studios208.DrawRush.Common;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Detects when the player enters the drawable area / a DrawPart and triggers
    /// the connecting LineRenderer between two parts. LineRenderer width / destroy
    /// delay come from <see cref="GameConfig"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerInteract : MonoBehaviour
    {
        [Header("Refs"), Space]
        [SerializeField] private Material lineMaterial;

        [Header("Tags"), Space]
        [SerializeField] private string drawAreaTag = "DrawArea";

        [HideInInspector] public bool isDrawing;
        [HideInInspector] public GameObject trail;

        private GameObject _previousPart;
        private bool _canDraw;

        private void Awake()
        {
            isDrawing = false;
            _previousPart = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(drawAreaTag))
            {
                _canDraw = true;
                return;
            }

            var interactable = other.GetComponent<IInteractable>();
            if (interactable == null || !_canDraw) return;

            if (_previousPart == null)
            {
                interactable.Interact();
                _previousPart = other.gameObject;
                return;
            }

            if (!isDrawing || _previousPart == other.gameObject) return;

            interactable.Interact();
            var previousDrawPart = _previousPart.GetComponent<DrawPart>();
            var currentDrawPart = other.gameObject.GetComponent<DrawPart>();
            if (previousDrawPart != null) previousDrawPart.MarkCompleted();
            if (currentDrawPart != null) currentDrawPart.MarkCompleted();

            var lineRenderer = _previousPart.AddComponent<LineRenderer>();
            AdjustLineRenderer(lineRenderer, other.transform.position, _previousPart.transform.position);

            if (trail != null) Destroy(trail);
            _previousPart = null;
            isDrawing = false;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(drawAreaTag)) return;

            if (trail != null) Destroy(trail);
            _canDraw = false;
            isDrawing = false;

            if (_previousPart != null)
            {
                var drawPart = _previousPart.GetComponent<DrawPart>();
                if (drawPart != null) drawPart.isPlayerEntered = false;
                _previousPart = null;
            }
        }

        private void AdjustLineRenderer(LineRenderer lineRenderer, Vector3 startPosition, Vector3 endPosition)
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
