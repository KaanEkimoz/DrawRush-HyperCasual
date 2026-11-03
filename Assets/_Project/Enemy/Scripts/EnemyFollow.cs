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
        Dasher = 4,     // moves only by lunges: crouch and flash, leap, land, again
        Boss = 5,       // the Giant Eraser: huge, unpushable, wipes fresh paint in a wide circle
        Flanker = 6,    // runs to where the player is heading, not where they are
        Orbiter = 7,    // circles the player and throws chunks of itself
        Glue = 8,       // slow, leaves glue puddles that slow the player
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
        [Tooltip("Pace of every enemy in the game, whatever its role: the one dial for the chase.")]
        [SerializeField] private float globalSpeed = 0.9f;
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
        [Tooltip("How strongly behaviour colours tint the enemy (0 = never, 1 = full). The eraser " +
                 "model is textured pink, and a full-strength tint multiplied it into mud.")]
        [SerializeField, Range(0f, 1f)] private float tintStrength = 0.6f;

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
        [Tooltip("Seconds a shove (Shockwave, Bump Pet) takes to play out.")]
        [SerializeField] private float pushSeconds = 0.4f;
        private float _slideLeft, _slideSeconds, _slideSpeed;
        private Vector3 _slideDirection;
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

        [Tooltip("Visual per variant, indexed by EnemyVariant (Normal, Sprinter, Tank, Eater, Dasher): " +
                 "children of this enemy, only the worn one active. Each role is its own stationery " +
                 "character, so it reads by shape and needs no tier tint. Empty = keep tinting.")]
        [SerializeField] private GameObject[] variantVisuals;
        [Tooltip("Turn rate multiple for the Giant Eraser: slower to swing round, easier to get behind.")]
        [SerializeField] private float bossTurnScale = 0.8f;
        private float _authoredTurn;
        private bool _skinned;
        private float _variantScale = 1f;
        private float _voiceIn;
        private Vector3 _playerLast, _playerVelocity;
        private float _orbitTimer, _orbitSide = 1f, _burst = 1f;
        private int _orbitPhase;   // 0 circling, 1 winding up a throw
        private Vector3 _lastPuddle;
        [Tooltip("Enemies closer than this (m) to the player make their noise.")]
        [SerializeField] private float voiceRange = 8f;
        [SerializeField] private Vector2 voiceInterval = new(1.5f, 3.5f);

        /// <summary>True if this enemy carries the Giant Eraser visual.</summary>
        public Vector3 VariantScale => _authoredScale == Vector3.zero ? transform.localScale : _authoredScale * _variantScale;

        /// <summary>Raised when a nearby enemy makes its noise (stapler chomp, scissors snip...).</summary>
        public static event System.Action<EnemyVariant> Voiced;

        public bool CanBeBoss => variantVisuals != null && variantVisuals.Length > (int)EnemyVariant.Boss && variantVisuals[(int)EnemyVariant.Boss] != null;

        [Header("Dasher (pushpin)")]
        [Tooltip("Seconds it crouches, aims and flashes before each lunge (the telegraph).")]
        [SerializeField] private float lungeWindUp = 0.616f;
        [Tooltip("Seconds a lunge lasts.")]
        [SerializeField] private float lungeTime = 0.44f;
        [Tooltip("Seconds it stands after landing.")]
        [SerializeField] private float lungeRest = 0.462f;
        [Tooltip("How far one lunge goes (m), at speed 1.")]
        [SerializeField] private float lungeDistance = 4.5f;
        [Tooltip("How far past the player a lunge may carry it (m), so a late dodge still works.")]
        [SerializeField] private float lungeOvershoot = 1.2f;
        [Tooltip("The wind-up sound plays only for a player closer than this (m).")]
        [SerializeField] private float lungeSoundRange = 9f;
        [SerializeField] private Color windUpColor = Color.white;

        [Header("Flanker / Orbiter")]
        [Tooltip("Flanker: furthest ahead (seconds of player movement) it aims.")]
        [SerializeField] private float flankLead = 1.4f;
        [Tooltip("Flanker: closer than this it simply chases.")]
        [SerializeField] private float flankDirectRange = 2.5f;
        [SerializeField] private float orbitRadius = 4.5f;
        [Tooltip("Orbiter: seconds of circling between throws.")]
        [SerializeField] private float throwEvery = 3f;
        [Tooltip("Orbiter: seconds it winds up (flashing) before a throw.")]
        [SerializeField] private float throwWarn = 0.45f;
        [Tooltip("Orbiter: seconds a thrown chunk flies.")]
        [SerializeField] private float throwFlight = 0.9f;
        [SerializeField] private float throwArc = 2.2f;
        [Tooltip("Orbiter: radius (m) of the landing ring; the hero is hit only inside it.")]
        [SerializeField] private float throwRadius = 0.9f;
        [Tooltip("Orbiter: size it keeps after a throw, before growing back.")]
        [SerializeField, Range(0.5f, 1f)] private float throwShrink = 0.85f;

        [Header("Attack")]
        [Tooltip("Hit marks: 0 star burst, 1 slash, 2 shockwave ring, 3 glue splat.")]
        [SerializeField] private Sprite[] attackFx = new Sprite[0];

        [Header("Glue")]
        [SerializeField] private Sprite gluePuddleSprite;
        [Tooltip("Metres walked between two puddles.")]
        [SerializeField] private float glueSpacing = 0.9f;
        [SerializeField] private float glueRadius = 0.6f;
        [Tooltip("Hero speed multiple inside a puddle.")]
        [SerializeField, Range(0.2f, 1f)] private float glueSlow = 0.6f;
        [SerializeField] private float glueSeconds = 6f;

        /// <summary>Raised when any Dasher starts its wind-up (audio and haptics listen).</summary>
        public static event System.Action DashWindUpStarted;

        private enum DashPhase { Idle, WindUp, Lunge }
        private DashPhase _dash;
        private float _dashTimer;
        private Vector3 _lungeFrom, _lungeTo;
        private HopAnimator _hop;   // the shown look's hop animator, for the lunge pose
        private float _lastWhine = float.NegativeInfinity;

        /// <summary>True while this enemy can land a hit: chasing, not frozen, asleep or done.</summary>
        public bool CanAttack => isActiveAndEnabled && _awake && !_frozen && !_halted && _slideLeft <= 0f;

        /// <summary>How much of a <see cref="Slide"/> is left, 1 at its start and 0 at rest.</summary>
        public float SlideAmount => _slideLeft > 0f ? _slideLeft / _slideSeconds : 0f;

        /// <summary>Ground direction of the current <see cref="Slide"/>.</summary>
        public Vector3 SlideDirection => _slideDirection;

        /// <summary>True while a Freeze booster holds this enemy.</summary>
        public bool IsFrozen => _frozen;

        /// <summary>Turn this enemy into a variant for the level: speed, size, pushability and an
        /// optional tint. Reverted to the authored enemy by <see cref="ClearVariant"/>.</summary>
        public void ApplyVariant(EnemyVariant variant, float speed, float scale, bool unpushable, Color? tint)
        {
            CaptureAuthored();
            Variant = variant;
            _dash = DashPhase.Idle;
            _dashTimer = lungeRest;
            speedMultiplier = speed;
            _variantScale = scale;
            transform.localScale = _authoredScale * scale;
            Unpushable = unpushable;
            _tierColor = tint;
            _agent.angularSpeed = _authoredTurn * (variant == EnemyVariant.Boss ? bossTurnScale : 1f);
            ApplySkin(variant);
            ApplySpeed();
            ApplyTint();
            if (variant == EnemyVariant.Dasher && _agent.isOnNavMesh) _agent.ResetPath();   // it only ever lunges
            FitStoppingDistance();
        }

        // Stop at the hero's edge, not on top of the pencil: a bigger enemy stops further out.
        private void FitStoppingDistance()
        {
            if (_agent != null) _agent.stoppingDistance = 1.2f * transform.lossyScale.x + 0.3f;
        }

        /// <summary>Back to the enemy as authored in the scene.</summary>
        public void ClearVariant()
        {
            CaptureAuthored();
            Variant = EnemyVariant.Normal;
            _dash = DashPhase.Idle;
            speedMultiplier = _authoredSpeedMultiplier;
            _variantScale = 1f;
            transform.localScale = _authoredScale;
            Unpushable = false;
            _tierColor = null;
            _agent.angularSpeed = _authoredTurn;
            ApplySkin(EnemyVariant.Normal);
            ApplySpeed();
            ApplyTint();
            FitStoppingDistance();
        }

        // Show this role's stationery character; an unset slot falls back to the Normal one.
        private void ApplySkin(EnemyVariant variant)
        {
            _skinned = variantVisuals != null && variantVisuals.Length > 0;
            if (!_skinned) return;
            int shown = (int)variant < variantVisuals.Length && variantVisuals[(int)variant] != null ? (int)variant : 0;
            for (int k = 0; k < variantVisuals.Length; k++)
                if (variantVisuals[k] != null) variantVisuals[k].SetActive(variantVisuals[k] == variantVisuals[shown]);
            GameObject visual = variantVisuals[shown];
            if (visual == null) return;
            // Each look drives its own animator (the rigged eraser has one, the hopping stationery none).
            _anim = visual.GetComponentInChildren<Animator>(true);
            if (_hop != null) { _hop.ExtraLean = 0f; _hop.ExtraStretch = 1f; _hop.SizeFactor = 1f; _hop.Struck -= OnStruck; }
            _hop = visual.GetComponent<HopAnimator>();
            if (_hop != null) _hop.Struck += OnStruck;
            if (visual.TryGetComponent(out EnemyPalette palette)) palette.ApplyRandom();
        }

        /// <summary>Stop in place for <paramref name="seconds"/>, tinted icy, then resume.</summary>
        public void Freeze(float seconds)
        {
            if (_dash != DashPhase.Idle) { _dash = DashPhase.Idle; _dashTimer = lungeRest; }
            Pose(0f, 1f);
            if (seconds <= 0f || _halted) return;
            _frozenUntil = Mathf.Max(_frozenUntil, Time.time + seconds);
            if (_frozen) return;
            _frozen = true;
            if (_agent != null && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.velocity = Vector3.zero; }
            if (_anim != null) _anim.speed = 0f;   // frozen mid-pose, not jogging in place
            SetColor(frozenColor);
        }

        /// <summary>End the freeze now; an enemy mid-slide finishes the skid first.</summary>
        public void Thaw() => _frozenUntil = 0f;

        /// <summary>Shove this enemy <paramref name="distance"/> m along <paramref name="direction"/>
        /// (ground plane), staying on the NavMesh. Heavy enemies ignore it unless
        /// <paramref name="evenHeavy"/> (a shockwave moves everything).</summary>
        public void Push(Vector3 direction, float distance, bool evenHeavy = false)
        {
            if ((Unpushable && !evenHeavy) || _agent == null || !_agent.isOnNavMesh) return;
            Slide(direction, distance, pushSeconds);
        }

        /// <summary>Skid <paramref name="distance"/> m along <paramref name="direction"/> over
        /// <paramref name="seconds"/>, slowing to a stop, helpless until it ends. Unlike
        /// <see cref="Push"/> it moves every enemy.</summary>
        public void Slide(Vector3 direction, float distance, float seconds)
        {
            direction.y = 0f;
            if (_halted || seconds <= 0f || direction.sqrMagnitude < 1e-6f) return;
            if (_agent == null || !_agent.isOnNavMesh) return;
            _slideDirection = direction.normalized;
            _slideSeconds = seconds;
            _slideLeft = seconds;
            if (_dash != DashPhase.Idle) { _dash = DashPhase.Idle; _dashTimer = lungeRest; }
            Pose(0f, 1f);
            if (_hop != null) _hop.PlayAttack(HopAnimator.AttackStyle.Recoil);
            _slideSpeed = 2f * distance / seconds;   // falls to 0, so the area under it is the distance
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
            if (_anim != null && !_frozen) _anim.SetTrigger(AnimatorIds.Hit);
        }

        private void UpdateSlide()
        {
            _agent.Move(_slideDirection * (_slideSpeed * SlideAmount * Time.deltaTime));   // stays on the NavMesh
            _slideLeft -= Time.deltaTime;
            if (_slideLeft > 0f) return;
            _slideLeft = 0f;
            _retargetTimer = 0.25f;   // a beat of stagger before it re-plans
            if (!_frozen) _agent.isStopped = false;
        }

        // Variants can be applied to an enemy that has never been active (no Awake yet).
        private void CaptureAuthored()
        {
            if (_authoredScale != Vector3.zero) return;
            _authoredScale = transform.localScale;
            _authoredSpeedMultiplier = speedMultiplier;
            if (_agent == null) { _agent = GetComponent<NavMeshAgent>(); _baseSpeed = _agent.speed; }
            _authoredTurn = _agent.angularSpeed;
        }

        private void ApplySpeed()
        {
            if (_agent != null) _agent.speed = _baseSpeed * globalSpeed * Mathf.Max(0.05f, speedMultiplier) * _pressure * _burst;
        }

        // Dasher: it never walks. Each step is a lunge: crouch and flash while aiming (the
        // telegraph), leap along a straight line to where the player stood (a little past it),
        // land, and go again. Moved by hand on the NavMesh so the leap is instant, not eased in by
        // the agent's acceleration.
        // Returns true while the lunge cycle owns movement this frame (always, for a Dasher).
        private bool UpdateDash(Transform player)
        {
            _dashTimer -= Time.deltaTime;
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            switch (_dash)
            {
                case DashPhase.Idle:   // resting after a landing
                    Pose(-8f * Mathf.Clamp01(_dashTimer / Mathf.Max(0.01f, lungeRest)), 1f);
                    if (_dashTimer > 0f) return true;
                    _dash = DashPhase.WindUp;
                    _dashTimer = lungeWindUp;
                    // A whine every lunge would drone; one every few seconds still warns.
                    if (toPlayer.sqrMagnitude < lungeSoundRange * lungeSoundRange && Time.time - _lastWhine > 2.5f)
                    {
                        _lastWhine = Time.time;
                        DashWindUpStarted?.Invoke();
                    }
                    if (_anim != null) _anim.SetTrigger(AnimatorIds.EnemyWindUp);
                    return true;
                case DashPhase.WindUp:
                {
                    float k = 1f - Mathf.Clamp01(_dashTimer / Mathf.Max(0.01f, lungeWindUp));
                    Pose(-22f * k, 1f - 0.25f * k);   // rears back and crouches
                    Face(toPlayer, 14f);
                    SetColor(Mathf.Repeat(_dashTimer * 12f, 1f) < 0.5f ? windUpColor : (_tierColor ?? fastColor));
                    if (_dashTimer > 0f) return true;

                    float reach = lungeDistance * Mathf.Clamp(speedMultiplier * globalSpeed * _pressure, 0.6f, 1.6f);
                    float dist = Mathf.Min(reach, toPlayer.magnitude + lungeOvershoot);
                    Vector3 dir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
                    _lungeFrom = transform.position;
                    _lungeTo = _lungeFrom + dir * dist;
                    _dash = DashPhase.Lunge;
                    _dashTimer = lungeTime;
                    if (_anim != null) _anim.SetTrigger(AnimatorIds.EnemyLunge);
                    ApplyTint();
                    return true;
                }
                default:   // Lunge
                {
                    float k = 1f - Mathf.Clamp01(_dashTimer / Mathf.Max(0.01f, lungeTime));
                    float eased = 1f - (1f - k) * (1f - k) * (1f - k);   // fast off the mark, settles on landing
                    Vector3 want = Vector3.Lerp(_lungeFrom, _lungeTo, eased);
                    Vector3 step = want - transform.position;
                    step.y = 0f;
                    _agent.Move(step);   // stays on the NavMesh, stops at walls
                    Face(_lungeTo - _lungeFrom, 30f);
                    Pose(38f * Mathf.Sin(k * Mathf.PI), 1f + 0.3f * Mathf.Sin(k * Mathf.PI));   // stretched, pin first
                    if (_dashTimer > 0f) return true;
                    _dash = DashPhase.Idle;
                    _dashTimer = lungeRest;
                    Pose(0f, 0.85f);
                    return true;
                }
            }
        }

        private void Pose(float lean, float stretch)
        {
            if (_hop == null) return;
            _hop.ExtraLean = lean;
            _hop.ExtraStretch = stretch;
        }

        private void Face(Vector3 dir, float sharpness)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        }

        // Eater: wipes the paint of any unfinished edge in its range.
        private void EatPaint(Transform player)
        {
            var painter = player.GetComponent<DrawRush.Drawing.FreeDrawPainter>();
            if (painter != null) painter.EraseNear(transform.position, Variant == EnemyVariant.Boss ? eatRadius * bossEatScale : eatRadius);
        }

        [Tooltip("Boss paint-wipe radius as a multiple of the Eater's.")]
        [SerializeField] private float bossEatScale = 3f;

        [Header("Eater")]
        [Tooltip("Paint on unfinished edges within this distance (m) of an Eater is wiped.")]
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
            _authoredTurn = _agent.angularSpeed;
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
            _slideLeft = 0f;
            if (_anim != null) _anim.speed = 1f;
            _dash = DashPhase.Idle;
            _dashTimer = lungeRest;
            _orbitPhase = 0;
            _orbitTimer = throwEvery;
            _lastPuddle = transform.position;
            _orbitSide = Random.value < 0.5f ? -1f : 1f;
            _burst = 1f;
            _playerVelocity = Vector3.zero;
            _playerLast = GameServices.Player != null ? GameServices.Player.position : transform.position;

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
            else if (_tierColor.HasValue && !_skinned) c = _tierColor.Value;
            else if (_skinned) c = Color.white;
            else if (speedMultiplier > 1.01f) c = fastColor;   // fast chaser
            else if (_hasAuthoredColor) c = _authoredColor;    // plain chaser — leave it be
            else return;
            SetColor(c);
        }

        private void SetColor(Color c)
        {
            c = Color.Lerp(Color.white, c, tintStrength);
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

            if (_slideLeft > 0f) { UpdateSlide(); SetAnimSpeed(0f); return; }

            if (_frozen)
            {
                SetAnimSpeed(0f);
                if (Time.time < _frozenUntil) return;
                _frozen = false;
                _agent.isStopped = false;
                if (_anim != null) _anim.speed = 1f;
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

            _voiceIn -= Time.deltaTime;
            if (_voiceIn <= 0f)
            {
                _voiceIn = Random.Range(voiceInterval.x, voiceInterval.y);
                if ((player.position - transform.position).sqrMagnitude < voiceRange * voiceRange) Voiced?.Invoke(Variant);
            }
            if (Variant == EnemyVariant.Eater || Variant == EnemyVariant.Boss) EatPaint(player);
            if (Variant == EnemyVariant.Dasher && UpdateDash(player)) { SetAnimSpeed(_agent.velocity.magnitude); return; }

            TrackPlayer(player);
            if (Variant == EnemyVariant.Glue) DropGlue();
            Vector3 target = Variant switch
            {
                EnemyVariant.Flanker => FlankTarget(player),
                EnemyVariant.Orbiter => OrbitTarget(player),
                _ => player.position,
            };

            // Throttled re-target — SetDestination throws off-mesh (handled above) and is wasteful
            // every frame, so re-plan a few times a second toward the target.
            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                _retargetTimer = RetargetInterval;
                _agent.SetDestination(target);
            }

            // At the hero it stands its ground facing them, instead of jostling and spinning on the
            // spot (the agent stops steering once it is inside its stopping distance).
            Vector3 toHero = player.position - transform.position;
            toHero.y = 0f;
            if (toHero.sqrMagnitude < (_agent.stoppingDistance + 0.6f) * (_agent.stoppingDistance + 0.6f)) Face(toHero, 10f);

            // Drive the run/idle blend by how fast the agent is actually moving, so a blocked or
            // barely-moving enemy doesn't sprint on the spot.
            SetAnimSpeed(_agent.velocity.magnitude);
        }

        // A smoothed player velocity, for the flanker's lead.
        private void TrackPlayer(Transform player)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 v = (player.position - _playerLast) / dt;
            v.y = 0f;
            _playerLast = player.position;
            if (v.sqrMagnitude > 400f) v = Vector3.zero;   // a teleport (respawn), not a run
            _playerVelocity = Vector3.Lerp(_playerVelocity, v, 1f - Mathf.Exp(-6f * dt));
        }

        // Flanker: aim where the player will be by the time it gets there, up to flankLead
        // seconds ahead, so running in a straight line runs into it.
        private Vector3 FlankTarget(Transform player)
        {
            Vector3 to = player.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < flankDirectRange) return player.position;
            float lead = Mathf.Min(flankLead, dist / Mathf.Max(0.5f, _agent.speed));
            Vector3 aim = player.position + _playerVelocity * lead;
            return NavMesh.SamplePosition(aim, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : player.position;
        }

        // Orbiter: circle the player at orbitRadius; every throwEvery seconds stop, flash and wind
        // up for throwWarn, then tear a chunk off itself and lob it at where the player stands.
        // It shrinks a little with each throw and grows back.
        private Vector3 OrbitTarget(Transform player)
        {
            _orbitTimer -= Time.deltaTime;
            if (_hop != null) _hop.SizeFactor = Mathf.MoveTowards(_hop.SizeFactor, 1f, Time.deltaTime * 0.08f);
            if (_orbitPhase == 1)
            {
                SetColor(Mathf.Repeat(_orbitTimer * 10f, 1f) < 0.5f ? windUpColor : fastColor);
                Vector3 look = player.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 1f - Mathf.Exp(-14f * Time.deltaTime));
                if (_orbitTimer > 0f) return transform.position;   // stands still while winding up
                ThrowChunk(player);
                _orbitPhase = 0;
                _orbitTimer = throwEvery;
                ApplyTint();
                _retargetTimer = 0f;
            }
            if (_orbitTimer <= 0f)
            {
                _orbitPhase = 1;
                _orbitTimer = throwWarn;
                if (_hop != null) _hop.PlayAttack(HopAnimator.AttackStyle.Throw);
                return transform.position;
            }
            Vector3 from = transform.position - player.position;
            from.y = 0f;
            if (from.sqrMagnitude < 0.01f) from = Vector3.forward;
            float angle = Mathf.Atan2(from.z, from.x) + _orbitSide * 0.8f;
            Vector3 spot = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * orbitRadius;
            return NavMesh.SamplePosition(spot, out NavMeshHit hit, 2f, NavMesh.AllAreas) ? hit.position : player.position;
        }

        private void ThrowChunk(Transform player)
        {
            var combat = GetComponent<EnemyCombat>();
            if (combat == null) return;
            Renderer look = _hop != null ? _hop.GetComponentInChildren<Renderer>() : null;
            Vector3 hand = transform.position + Vector3.up * (1.2f * transform.lossyScale.y);
            Vector3 at = player.position;
            at.y = 0f;
            PuttyGlob.Launch(combat, look, hand, at, throwFlight, throwArc, throwRadius);
            if (_hop != null) _hop.SizeFactor = Mathf.Max(throwShrink, _hop.SizeFactor * throwShrink);
        }

        // Glue: a puddle every glueSpacing metres it walks.
        private void DropGlue()
        {
            Vector3 d = transform.position - _lastPuddle;
            d.y = 0f;
            if (d.sqrMagnitude < glueSpacing * glueSpacing) return;
            _lastPuddle = transform.position;
            GluePuddle.Spawn(gluePuddleSprite, transform.position, glueRadius, glueSlow, glueSeconds);
        }

        /// <summary>Raised the moment this enemy's attack move lands (combat deals the damage then).</summary>
        public event System.Action Struck;

        // The hit mark at the moment the move lands.
        private void OnStruck(HopAnimator.AttackStyle style)
        {
            Struck?.Invoke();
            int fx = style switch
            {
                HopAnimator.AttackStyle.Snip => 1,
                HopAnimator.AttackStyle.Slam => 2,
                HopAnimator.AttackStyle.Squirt => 3,
                HopAnimator.AttackStyle.Throw => -1,
                _ => 0,
            };
            if (fx < 0 || fx >= attackFx.Length) return;
            float s = transform.lossyScale.x;
            if (fx >= 2)   // on the page
            {
                Vector3 ground = transform.position + transform.forward * (fx == 2 ? 0.4f : 1.1f) * s;
                AttackFx.Spawn(attackFx[fx], new Vector3(ground.x, 0.05f, ground.z), (fx == 2 ? 3.2f : 1.6f) * s, true);
            }
            else AttackFx.Spawn(attackFx[fx], transform.position + transform.forward * (1.2f * s) + Vector3.up * (1.1f * s), 1.3f * s, false);
        }

        /// <summary>0..1 while the attack move winds up (the cone brightens as a warning).</summary>
        public float AttackGather => _hop != null ? _hop.AttackGather : 0f;

        /// <summary>Play this enemy's attack move (its look decides how it hits).</summary>
        public void PlayAttack()
        {
            if (_hop == null) return;
            _hop.PlayAttack(Variant switch
            {
                EnemyVariant.Normal => HopAnimator.AttackStyle.Chomp,
                EnemyVariant.Sprinter => HopAnimator.AttackStyle.Snip,
                EnemyVariant.Tank => HopAnimator.AttackStyle.Slam,
                EnemyVariant.Eater => HopAnimator.AttackStyle.Grind,
                EnemyVariant.Dasher => HopAnimator.AttackStyle.Stab,
                EnemyVariant.Orbiter => HopAnimator.AttackStyle.Throw,
                EnemyVariant.Boss => HopAnimator.AttackStyle.Slam,
                EnemyVariant.Glue => HopAnimator.AttackStyle.Squirt,
                _ => HopAnimator.AttackStyle.Bash,
            });
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
            _slideLeft = 0f;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
        }
    }
}
