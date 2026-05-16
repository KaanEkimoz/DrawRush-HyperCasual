using System;
using UnityEngine;
using UnityEngine.Serialization;
using Studios208.DrawRush.Common;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// A single connectable part. Implements <see cref="IInteractable"/> so PlayerInteract
    /// can drive it generically. Raises <see cref="Completed"/> when this part finishes —
    /// PartManager / WallManager listen instead of polling Update.
    /// </summary>
    public sealed class DrawPart : MonoBehaviour, IInteractable
    {
        public event Action<DrawPart> Completed;

        [HideInInspector] public bool isPlayerEntered;
        [FormerlySerializedAs("_trailPrefab")]
        [SerializeField] private GameObject trailPrefab;
        [SerializeField] private Vector3 trailEulerAngles = new(91f, 40f, 38f);
        [SerializeField] private Vector3 trailOffset = new(0f, 0.25f, 0f);

        private bool _isDrawCompleted;
        private GameObject _currTrail;
        private Transform _trailPoint;
        private PlayerInteract _playerInteract;
        private bool _isGoingToPlayer;
        private bool _isReachedToPlayer;

        public bool IsDrawCompleted => _isDrawCompleted;

        private void Awake()
        {
            ResetState();
            EnsureTrail();
        }

        private void Start()
        {
            ResolvePlayerRefs();
        }

        private void Update()
        {
            if (_isDrawCompleted) return;

            EnsureTrail();
            if (_trailPoint == null)
            {
                ResolvePlayerRefs();
                if (_trailPoint == null) return;
            }

            if (Mathf.Abs(_currTrail.transform.position.z - _trailPoint.position.z) > 0.001f && _isGoingToPlayer)
            {
                LerpTrailTowardsPlayer();
                return;
            }

            if (_isGoingToPlayer)
            {
                _isReachedToPlayer = true;
                _isGoingToPlayer = false;
                Interact();
            }
        }

        public void Interact()
        {
            EnsureTrail();
            if (_isDrawCompleted || isPlayerEntered) return;
            if (!_isReachedToPlayer)
            {
                _isGoingToPlayer = true;
                return;
            }

            if (_playerInteract == null) ResolvePlayerRefs();
            if (_playerInteract == null) return;

            if (!_playerInteract.isDrawing)
            {
                isPlayerEntered = true;
                _currTrail.transform.SetParent(_trailPoint, worldPositionStays: false);
                _currTrail.transform.localPosition = Vector3.zero;
                _currTrail.SetActive(true);
                _playerInteract.trail = _currTrail;
                _playerInteract.isDrawing = true;
                return;
            }

            CompleteDraw();
        }

        /// <summary>
        /// Marks this part as drawn — invoked externally by PlayerInteract when the
        /// connecting line is finalized between two parts. Idempotent.
        /// </summary>
        public void MarkCompleted()
        {
            if (_isDrawCompleted) return;
            _isDrawCompleted = true;
            Completed?.Invoke(this);
        }

        private void CompleteDraw()
        {
            if (_playerInteract == null) return;
            _playerInteract.isDrawing = false;
            if (_playerInteract.trail != null)
            {
                _playerInteract.trail.transform.SetParent(transform, worldPositionStays: false);
            }
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            _playerInteract.trail = null;
            isPlayerEntered = false;
            MarkCompleted();
        }

        private void EnsureTrail()
        {
            if (_currTrail != null || trailPrefab == null) return;
            _currTrail = Instantiate(
                trailPrefab,
                transform.position - new Vector3(0f, -trailOffset.y, 0f),
                Quaternion.Euler(trailEulerAngles),
                transform);
            _currTrail.SetActive(true);
        }

        private void LerpTrailTowardsPlayer()
        {
            if (_currTrail == null || _trailPoint == null) return;
            float lerp = GameServices.Config != null ? GameServices.Config.trailCatchUpLerp : 100f;
            var current = _currTrail.transform.position;
            var target = _trailPoint.position;
            float t = lerp * Time.deltaTime;
            _currTrail.transform.position = new Vector3(
                Mathf.Lerp(current.x, target.x, t),
                Mathf.Lerp(current.y, target.y, t),
                Mathf.Lerp(current.z, target.z, t));
        }

        private void ResolvePlayerRefs()
        {
            if (GameServices.Player == null) return;
            _trailPoint = GameServices.TrailPoint != null
                ? GameServices.TrailPoint
                : GameServices.Player.childCount > 0 ? GameServices.Player.GetChild(0) : null;
            if (_playerInteract == null)
            {
                _playerInteract = GameServices.Player.GetComponent<PlayerInteract>();
            }
        }

        private void ResetState()
        {
            isPlayerEntered = false;
            _isDrawCompleted = false;
            _isGoingToPlayer = false;
            _isReachedToPlayer = false;
        }
    }
}
