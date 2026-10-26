using UnityEngine;
using UnityEngine.AI;
using DrawRush.Core;

namespace DrawRush.Enemy
{
    /// <summary>What a fielded enemy has been turned into for the current level.</summary>
    public enum EnemyVariant
    {
        Normal = 0,
        Sprinter = 1,   // fast and small
        Tank = 2,       // slow, big, cannot be pushed
        Eater = 3,      // eats paint that is not kept for good yet
        Dasher = 4,     // winds up (flashing), then lunges
    }

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
        [Tooltip("Tint while frozen by the Freeze booster.")]
        [SerializeField] private Color frozenColor = new Color(0.55f, 0.85f, 1f, 1f);

        // SetDestination every frame is wasted work — the agent re-plans far faster than it needs to.
        // Re-target a few times a second; the chase looks identical and the pathfinding cost drops.
        private const float RetargetInterval = 0.1f;

        private NavMeshAgent _agent;
        private Animator _anim;
        private GameState _state;
        private bool _halted;
        private bool _awake;          // for wakeRadius: has this guardian been roused yet
        private float _baseSpeed;
        private float _retargetTimer;
        private Vector3 _spawnPos;
        private Quaternion _spawnRot;
        private float _pressure = 1f;
        private float _frozenUntil;
        private bool _frozen;
        private Vector3 _authoredScale;
        private float _authoredSpeedMultiplier;
        private Color? _tierColor;

        /// <summary>Extra speed multiple from rising level pressure (1 = none). Reset per level.</summary>
        public float Pressure
        {
            get => _pressure;
            set { _pressure = Mathf.Max(0.1f, value); ApplySpeed(); }
        }

        /// <summary>True for heavy enemies that pushes (pet, blast) cannot move.</summary>
        public bool Unpushable { get; private set; }

        /// <summary>The variant this enemy plays as in the current level.</summary>
        public EnemyVariant Variant { get; private set; }

        [Header("Dasher")]
        [Tooltip("Seconds between lunges.")]
        [SerializeField] private float dashInterval = 4f;
        [Tooltip("Seconds of flashing wind-up before a lunge (the telegraph).")]
        [SerializeField] private float dashWindUp = 0.6f;
        [Tooltip("Seconds the lunge lasts.")]
        [SerializeField] private float dashDuration = 0.7f;
        [SerializeField] private float dashSpeedMultiplier = 3f;
        [Tooltip("Only lunges at a player closer than this (m).")]
        [SerializeField] private float dashRange = 6f;
        [SerializeField] private Color windUpColor = Color.white;

        /// <summary>Raised when any Dasher starts its wind-up (audio and haptics listen).</summary>
        public static event System.Action DashWindUpStarted;

        private enum DashPhase { Idle, WindUp, Lunge }
        private DashPhase _dash;
        private float _dashTimer;

        /// <summary>True while a Freeze booster holds this enemy.</summary>
        public bool IsFrozen => _frozen;

        /// <summary>Turn this enemy into a variant for the level: speed, size, pushability and an
        /// optional tint. Reverted to the authored enemy by <see cref="ClearVariant"/>.</summary>
        public void ApplyVariant(EnemyVariant variant, float speed, float scale, bool unpushable, Color? tint)
        {
            CaptureAuthored();
            Variant = variant;
            _dash = DashPhase.Idle;
            _dashTimer = dashInterval;
            speedMultiplier = speed;
            transform.localScale = _authoredScale * scale;
            Unpushable = unpushable;
            _tierColor = tint;
            ApplySpeed();
            ApplyTint();
        }

        /// <summary>Back to the enemy as authored in the scene.</summary>
        public void ClearVariant()
        {
            CaptureAuthored();
            Variant = EnemyVariant.Normal;
            _dash = DashPhase.Idle;
            speedMultiplier = _authoredSpeedMultiplier;
            transform.localScale = _authoredScale;
            Unpushable = false;
            _tierColor = null;
            ApplySpeed();
            ApplyTint();
        }

        /// <summary>Stop in place for <paramref name="seconds"/>, tinted icy, then resume.</summary>
        public void Freeze(float seconds)
        {
            if (seconds <= 0f || _halted) return;
            _frozenUntil = Mathf.Max(_frozenUntil, Time.time + seconds);
            if (_frozen) return;
            _frozen = true;
            if (_agent != null && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
            SetColor(frozenColor);
        }

        /// <summary>Shove this enemy <paramref name="distance"/> m along <paramref name="direction"/>
        /// (ground plane), staying on the NavMesh. Heavy enemies ignore it.</summary>
        public void Push(Vector3 direction, float distance)
        {
            if (Unpushable || _agent == null || !_agent.isOnNavMesh) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f) return;
            Vector3 target = transform.position + direction.normalized * distance;
            if (NavMesh.Raycast(transform.position, target, out NavMeshHit hit, NavMesh.AllAreas)) target = hit.position;
            _agent.Warp(target);
            _agent.velocity = Vector3.zero;
            _retargetTimer = 0.25f;   // a beat of stagger before it re-plans
        }

        // Variants can be applied to an enemy that has never been active (no Awake yet).
        private void CaptureAuthored()
        {
            if (_authoredScale != Vector3.zero) return;
            _authoredScale = transform.localScale;
            _authoredSpeedMultiplier = speedMultiplier;
            if (_agent == null) { _agent = GetComponent<NavMeshAgent>(); _baseSpeed = _agent.speed; }
        }

        private void ApplySpeed()
        {
            float lunge = _dash == DashPhase.Lunge ? dashSpeedMultiplier : 1f;
            if (_agent != null) _agent.speed = _baseSpeed * Mathf.Max(0.05f, speedMultiplier) * _pressure * lunge;
        }

        // Dasher: every few seconds near the player it stops and flashes (the telegraph the
        // research asks for, so a hit never comes from nowhere), then lunges at where the player
        // stood when the flash began.
        // Returns true while the dash owns movement this frame.
        private bool UpdateDash(Transform player)
        {
            _dashTimer -= Time.deltaTime;
            switch (_dash)
            {
                case DashPhase.Idle:
                    if (_dashTimer > 0f) return false;
                    if ((player.position - transform.position).sqrMagnitude > dashRange * dashRange) { _dashTimer = 0.5f; return false; }
                    _dash = DashPhase.WindUp;
                    _dashTimer = dashWindUp;
                    DashWindUpStarted?.Invoke();
                    _agent.isStopped = true;
                    _agent.velocity = Vector3.zero;
                    _agent.SetDestination(player.position);   // aim now; the lunge commits to it
                    return true;
                case DashPhase.WindUp:
                    SetColor(Mathf.Repeat(_dashTimer * 10f, 1f) < 0.5f ? windUpColor : (_tierColor ?? fastColor));
                    if (_dashTimer > 0f) return true;
                    _dash = DashPhase.Lunge;
                    _dashTimer = dashDuration;
                    _agent.isStopped = false;
                    ApplySpeed();
                    ApplyTint();
                    return true;
                default:
                    if (_dashTimer > 0f) return true;
                    _dash = DashPhase.Idle;
                    _dashTimer = dashInterval;
                    ApplySpeed();
                    return false;
            }
        }

        // Eater: wipes any not-yet-kept paint it walks over.
        private void EatPaint(Transform player)
        {
            var painter = player.GetComponent<DrawRush.Drawing.FreeDrawPainter>();
            if (painter != null) painter.ErasePendingNear(transform.position, eatRadius);
        }

        [Header("Eater")]
        [Tooltip("Pending paint within this distance (m) of an Eater is wiped.")]
        [SerializeField] private float eatRadius = 0.6f;

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private Color _authoredColor = Color.white;
        private bool _hasAuthoredColor;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _anim = GetComponentInChildren<Animator>(true);
            _baseSpeed = _agent.speed;
            // Authored position is the spawn point — capture before NavMeshAgent
            // can drift the transform.
            _spawnPos = transform.position;
            _spawnRot = transform.rotation;
            _authoredScale = transform.localScale;
            _authoredSpeedMultiplier = speedMultiplier;

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
            _pressure = 1f;
            _frozen = false;
            _frozenUntil = 0f;
            _dash = DashPhase.Idle;
            _dashTimer = dashInterval;

            // Restart: send the enemy back to its authored spawn. Warp keeps the agent on
            // the NavMesh; transform.SetPositionAndRotation handles the off-NavMesh edge
            // case (Warp would no-op there).
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                ApplySpeed();
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
            if (_frozen) c = frozenColor;
            else if (!_awake) c = sleepColor;                  // dormant guardian
            else if (_tierColor.HasValue) c = _tierColor.Value;
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
            if (_halted) { SetAnimSpeed(0f); return; }
            var player = GameServices.Player;
            if (player == null) return;
            if (_agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

            if (_frozen)
            {
                SetAnimSpeed(0f);
                if (Time.time < _frozenUntil) return;
                _frozen = false;
                _agent.isStopped = false;
                ApplyTint();
            }

            // A dormant guardian holds position until the player strays too close, then wakes —
            // switching from its sleeping colour to its active one so the change is visible. While
            // asleep it stands in the Idle pose (speed 0) instead of jogging in place.
            if (!_awake)
            {
                SetAnimSpeed(0f);
                float sqr = (player.position - transform.position).sqrMagnitude;
                if (sqr > wakeRadius * wakeRadius) return;
                _awake = true;
                ApplyTint();
            }

            if (Variant == EnemyVariant.Eater) EatPaint(player);
            if (Variant == EnemyVariant.Dasher && UpdateDash(player)) { SetAnimSpeed(_agent.velocity.magnitude); return; }

            // Throttled re-target — SetDestination throws off-mesh (handled above) and is wasteful
            // every frame, so re-plan a few times a second toward the player's current position.
            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                _retargetTimer = RetargetInterval;
                _agent.SetDestination(player.position);
            }

            // Drive the run/idle blend by how fast the agent is actually moving, so a blocked or
            // barely-moving enemy doesn't sprint on the spot.
            SetAnimSpeed(_agent.velocity.magnitude);
        }

        // f_speed feeds the Idle<->Run blend in Enemy.controller.
        private void SetAnimSpeed(float v)
        {
            if (_anim != null) _anim.SetFloat(AnimatorIds.Speed, v);
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
