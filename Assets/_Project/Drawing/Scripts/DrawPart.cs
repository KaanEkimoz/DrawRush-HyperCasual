using System;
using UnityEngine;
using UnityEngine.Serialization;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// A single connectable part. Implements <see cref="IDrawPart"/> so PlayerInteract
    /// drives it through the interface. Internal state is held in a
    /// <see cref="DrawPartStateMachine"/> — a single <see cref="DrawingPhase"/> field
    /// instead of four collateral booleans.
    /// </summary>
    public sealed class DrawPart : MonoBehaviour, IDrawPart
    {
        public event Action<IDrawPart> Completed;

        [FormerlySerializedAs("_trailPrefab")]
        [SerializeField] private GameObject trailPrefab;
        [SerializeField] private Vector3 trailEulerAngles = new(91f, 40f, 38f);
        [SerializeField] private Vector3 trailOffset = new(0f, 0.25f, 0f);

        private readonly DrawPartStateMachine _fsm = new();
        private GameObject _currTrail;
        private Transform _trailPoint;
        private PlayerInteract _playerInteract;

        public bool IsCompleted => _fsm.IsCompleted;
        public Transform Transform => transform;
        public DrawingPhase Phase => _fsm.Phase;

        [Obsolete("Use OnPlayerEntered / OnPlayerExited instead.")]
        public bool isPlayerEntered
        {
            get => _fsm.Phase is DrawingPhase.Armed or DrawingPhase.Drawing;
            set { /* intentional no-op; encapsulation enforcement */ }
        }

        private void Awake()
        {
            _fsm.ResetToIdle();
            EnsureTrail();
        }

        private void Start()
        {
            ResolvePlayerRefs();
        }

        private void Update()
        {
            if (_fsm.IsCompleted) return;

            EnsureTrail();
            if (_trailPoint == null)
            {
                ResolvePlayerRefs();
                if (_trailPoint == null) return;
            }
            if (_currTrail == null) return;

            // While Returning: lerp toward the player; flip to Armed when close enough.
            if (_fsm.Phase == DrawingPhase.Returning)
            {
                if (Mathf.Abs(_currTrail.transform.position.z - _trailPoint.position.z) > 0.001f)
                {
                    LerpTrailTowardsPlayer();
                    return;
                }
                _fsm.TryTransition(DrawingPhase.Armed);
                Interact();
            }
        }

        public void Interact()
        {
            EnsureTrail();
            if (_fsm.IsCompleted) return;

            // Idle → Returning: schedule trail catch-up on next Update.
            if (_fsm.Phase == DrawingPhase.Idle)
            {
                _fsm.TryTransition(DrawingPhase.Returning);
                return;
            }

            if (_fsm.Phase != DrawingPhase.Armed) return;
            if (_playerInteract == null) ResolvePlayerRefs();
            if (_playerInteract == null) return;

            // Armed → Drawing: anchor trail to player.
            if (!_playerInteract.IsDrawing)
            {
                _fsm.TryTransition(DrawingPhase.Drawing);
                _currTrail.transform.SetParent(_trailPoint, worldPositionStays: false);
                _currTrail.transform.localPosition = Vector3.zero;
                _currTrail.SetActive(true);
                _playerInteract.BeginDrawing(_currTrail);
                return;
            }

            // Armed → Done: finalize the connection.
            CompleteDraw();
        }

        public void OnPlayerEntered()
        {
            if (_fsm.Phase == DrawingPhase.Idle)
            {
                _fsm.TryTransition(DrawingPhase.Armed);
            }
        }

        public void OnPlayerExited()
        {
            if (_fsm.Phase is DrawingPhase.Armed or DrawingPhase.Returning)
            {
                _fsm.TryTransition(DrawingPhase.Idle);
            }
        }

        public void Complete()
        {
            if (_fsm.IsCompleted) return;
            _fsm.TryTransition(DrawingPhase.Done);
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
            _currTrail.transform.position = TrailMath.Lerp(
                _currTrail.transform.position, _trailPoint.position, lerp, Time.deltaTime);
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
    }
}
