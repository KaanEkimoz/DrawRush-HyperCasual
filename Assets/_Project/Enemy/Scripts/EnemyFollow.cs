using UnityEngine;
using UnityEngine.AI;
using DrawRush.Core;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Chases the player via NavMeshAgent. Subscribes to GameState.GameWonChanged
    /// to halt cleanly instead of polling IsGameWon every frame.
    ///
    /// Two authored behaviours ride on the same component so one enemy type gives several feels:
    ///   • <see cref="speedMultiplier"/> — a fast enemy that presses harder than the shape can be
    ///     drawn, or a slow lumbering one.
    ///   • <see cref="wakeRadius"/> — a dormant guardian that sits at its spawn until the player
    ///     comes within range, then wakes for good. Slipping past a sleeping one becomes a choice.
    /// Both default to the plain relentless chaser, so existing enemies are unchanged.
    ///
    /// Restart hygiene: the authored Transform position/rotation is captured at Awake
    /// and re-applied on every OnEnable, so a Restart returns the enemy to its placed
    /// spawn instead of leaving it wherever it cornered the player.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyFollow : MonoBehaviour
    {
        [Header("Behaviour")]
        [Tooltip("Multiplies the agent's authored speed. >1 = a fast, pressing enemy; <1 = a slow one.")]
        [SerializeField] private float speedMultiplier = 1f;
        [Tooltip("0 = always chasing (the default). >0 = stays asleep at its spawn until the player " +
                 "comes within this many world units, then wakes and chases for the rest of the level.")]
        [SerializeField] private float wakeRadius = 0f;

        [Header("Visual cue (so the behaviour reads)")]
        [Tooltip("Tint for a fast enemy (speedMultiplier > 1). A different colour is what makes the " +
                 "variety felt rather than invisible.")]
        [SerializeField] private Color fastColor = new Color(1f, 0.5f, 0.05f, 1f);
        [Tooltip("Tint for a sleeping guardian; it switches to its normal colour when it wakes.")]
        [SerializeField] private Color sleepColor = new Color(0.45f, 0.35f, 0.85f, 1f);

        // SetDestination every frame is wasted work — the agent re-plans far faster than it needs to.
        // Re-target a few times a second; the chase looks identical and the pathfinding cost drops.
        private const float RetargetInterval = 0.1f;

        private NavMeshAgent _agent;
        private GameState _state;
        private bool _halted;
        private bool _awake;          // for wakeRadius: has this guardian been roused yet
        private float _baseSpeed;
        private float _retargetTimer;
        private Vector3 _spawnPos;
        private Quaternion _spawnRot;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private Color _authoredColor = Color.white;
        private bool _hasAuthoredColor;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _baseSpeed = _agent.speed;
            // Authored position is the spawn point — capture before NavMeshAgent
            // can drift the transform.
            _spawnPos = transform.position;
            _spawnRot = transform.rotation;

            _renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in _renderers)
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId))
                { _authoredColor = r.sharedMaterial.GetColor(BaseColorId); _hasAuthoredColor = true; break; }
        }

        private void OnEnable()
        {
            // Resume chasing on every (re)enable so a restarted/revisited level doesn't keep
            // an enemy frozen from a previous win (_halted persists across SetActive).
            _halted = false;
            _awake = wakeRadius <= 0f;   // no wake radius => already awake (plain chaser)
            _retargetTimer = 0f;

            // Restart: send the enemy back to its authored spawn. Warp keeps the agent on
            // the NavMesh; transform.SetPositionAndRotation handles the off-NavMesh edge
            // case (Warp would no-op there).
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                _agent.speed = _baseSpeed * Mathf.Max(0.05f, speedMultiplier);
                _agent.Warp(_spawnPos);
                if (_agent.isOnNavMesh)
                {
                    _agent.ResetPath();
                    _agent.velocity = Vector3.zero;
                    _agent.isStopped = false;
                }
            }
            else
            {
                transform.SetPositionAndRotation(_spawnPos, _spawnRot);
            }
            transform.rotation = _spawnRot;

            ApplyTint();   // wear the colour of whatever behaviour was authored

            _state = GameServices.State;
            if (_state != null) _state.GameWonChanged += OnGameWonChanged;
        }

        // Fast = orange, sleeping guardian = purple, everything else keeps its authored red. Via a
        // property block so the shared EnemyMaterial is never instanced.
        private void ApplyTint()
        {
            if (_renderers == null) return;
            Color c;
            if (!_awake) c = sleepColor;                       // dormant guardian
            else if (speedMultiplier > 1.01f) c = fastColor;   // fast chaser
            else if (_hasAuthoredColor) c = _authoredColor;    // plain chaser — leave it be
            else return;
            SetColor(c);
        }

        private void SetColor(Color c)
        {
            _mpb ??= new MaterialPropertyBlock();
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, c);
                _mpb.SetColor(ColorId, c);
                r.SetPropertyBlock(_mpb);
            }
        }

        private void OnDisable()
        {
            if (_state != null) _state.GameWonChanged -= OnGameWonChanged;
        }

        private void Update()
        {
            if (_halted) return;
            var player = GameServices.Player;
            if (player == null) return;
            if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

            // A dormant guardian holds position until the player strays too close, then wakes —
            // switching from its sleeping colour to its active one so the change is visible.
            if (!_awake)
            {
                float sqr = (player.position - transform.position).sqrMagnitude;
                if (sqr > wakeRadius * wakeRadius) return;
                _awake = true;
                ApplyTint();
            }

            // Throttled re-target — SetDestination throws off-mesh (handled above) and is wasteful
            // every frame, so re-plan a few times a second toward the player's current position.
            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer > 0f) return;
            _retargetTimer = RetargetInterval;
            _agent.SetDestination(player.position);
        }

        private void OnGameWonChanged(bool won)
        {
            if (!won) return;
            _halted = true;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
        }
    }
}
