using System;
using UnityEngine;
using UnityEngine.Serialization;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// A single connectable part. Implements <see cref="IDrawPart"/> so PlayerInteract
    /// can drive it without seeing the internal state machine.
    /// Raises <see cref="Completed"/> once when the part is finalised; PartManager /
    /// WallManager listen instead of polling.
    /// </summary>
    public sealed class DrawPart : MonoBehaviour, IDrawPart
    {
        public event Action<IDrawPart> Completed;

        [FormerlySerializedAs("_trailPrefab")]
        [SerializeField] private GameObject trailPrefab;
        [SerializeField] private Vector3 trailEulerAngles = new(91f, 40f, 38f);
        [SerializeField] private Vector3 trailOffset = new(0f, 0.25f, 0f);

        private bool _isCompleted;
        private bool _isArmed;
        private bool _isGoingToPlayer;
        private bool _isReachedToPlayer;
        private GameObject _currTrail;
        private Transform _trailPoint;
        private PlayerInteract _playerInteract;

        public bool IsCompleted => _isCompleted;
        public Transform Transform => transform;

        // Kept temporarily for backwards-compat with any prefab inspector script that
        // pokes the field. Reads route to the internal _isArmed value; writes are
        // ignored — callers must use OnPlayerEntered/OnPlayerExited.
        [Obsolete("Use OnPlayerEntered / OnPlayerExited instead.")]
        public bool isPlayerEntered
        {
            get => _isArmed;
            set { /* intentional no-op; encapsulation enforcement */ }
        }

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
            if (_isCompleted) return;

            EnsureTrail();
            if (_trailPoint == null)
            {
                ResolvePlayerRefs();
                if (_trailPoint == null) return;
            }

            if (_currTrail == null) return;

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
            if (_isCompleted || _isArmed) return;
            if (!_isReachedToPlayer)
            {
                _isGoingToPlayer = true;
                return;
            }

            if (_playerInteract == null) ResolvePlayerRefs();
            if (_playerInteract == null) return;

            if (!_playerInteract.IsDrawing)
            {
                _isArmed = true;
                _currTrail.transform.SetParent(_trailPoint, worldPositionStays: false);
                _currTrail.transform.localPosition = Vector3.zero;
                _currTrail.SetActive(true);
                _playerInteract.BeginDrawing(_currTrail);
                return;
            }

            CompleteDraw();
        }

        public void OnPlayerEntered()
        {
            _isArmed = true;
        }

        public void OnPlayerExited()
        {
            _isArmed = false;
        }

        public void Complete()
        {
            if (_isCompleted) return;
            _isCompleted = true;
            Completed?.Invoke(this);
        }

        private void CompleteDraw()
        {
            if (_playerInteract == null) return;
            _playerInteract.EndDrawing(reparentTrailTo: transform);
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            _isArmed = false;
            Complete();
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
            _currTrail.transform.position = TrailMath.Lerp(_currTrail.transform.position, _trailPoint.position, lerp, Time.deltaTime);
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
            _isArmed = false;
            _isCompleted = false;
            _isGoingToPlayer = false;
            _isReachedToPlayer = false;
        }
    }
}
