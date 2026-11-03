using UnityEngine;
using UnityEngine.AI;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Animates a rigid character by moving its whole visual, for the stationery enemies (whose box
    /// bodies tear under a skeleton) and the pencil hero. Built on the classic principles so it reads
    /// as alive rather than mechanical:
    ///   • a real hop arc, stretched on the way up and squashed on landing, volume kept;
    ///   • lean and side bank on springs, so starts, stops and turns overshoot and settle;
    ///   • legs step in turn with the hops, arms swing against them and trail behind on springs;
    ///   • an idle that breathes and sways, offset per character so a crowd never moves in sync;
    ///   • attack moves made of keyed poses: gather, a fast strike with a smear stretch, a short
    ///     hold on the hit, and a springy recovery.
    /// Reads real ground speed (the agent's, or the parent's movement without one), so a blocked or
    /// frozen character stops hopping by itself.
    /// </summary>
    public sealed class HopAnimator : MonoBehaviour
    {
        [Tooltip("Hops per second at the reference speed.")]
        [SerializeField] private float hopRate = 3.5f;
        [Tooltip("Speed (m/s) that gives the full hop rate.")]
        [SerializeField] private float referenceSpeed = 2.5f;
        [Tooltip("Hop height, in the visual's parent units.")]
        [SerializeField] private float hopHeight = 0.35f;
        [Tooltip("Squash and stretch amount (0 = rigid).")]
        [SerializeField, Range(0f, 0.5f)] private float squash = 0.2f;
        [Tooltip("Forward tilt (degrees) at full speed.")]
        [SerializeField] private float lean = 12f;
        [Tooltip("Side-to-side waddle (degrees).")]
        [SerializeField] private float waddle = 8f;
        [Tooltip("For flat cut-out models: always face the camera like a paper doll and mirror to " +
                 "the side it is moving, instead of turning with the character.")]
        [SerializeField] private bool faceCamera;
        [Tooltip("Stand side-on to the camera (front of the model toward the player's eye), turning to " +
                 "the side it faces, and roll to point at the target when attacking. For the scissors, " +
                 "whose blades only read from the front.")]
        [SerializeField] private bool sideOn;
        [Tooltip("Optional limb rig: arms and legs swing on this Animator while the body stays rigid.")]
        [SerializeField] private Animator limbs;
        [Tooltip("Limb playback speed at the reference speed.")]
        [SerializeField] private float limbSpeed = 1.2f;
        [Tooltip("Baked limbs (LimbRigBaker): each swings on its own joint around the stiff body.")]
        [SerializeField] private Limb[] swingLimbs = System.Array.Empty<Limb>();
        [Tooltip("Limb swing either way at full speed (degrees).")]
        [SerializeField] private float swingAngle = 34f;
        [Tooltip("How far (degrees) the body tips back, against the skid, while its enemy slides.")]
        [SerializeField] private float slideLean = 26f;

        /// <summary>Extra height multiple (the pencil hero shortens as it takes hits).</summary>
        public float HeightFactor { get; set; } = 1f;

        /// <summary>Extra forward tilt in degrees (negative leans back), set by the owner for
        /// moves the hop cannot express, like the pushpin's wind-up and lunge.</summary>
        public float ExtraLean { get; set; }

        /// <summary>Extra height multiple on top of the hop's squash (below 1 crouches).</summary>
        public float ExtraStretch { get; set; } = 1f;

        /// <summary>Whole-body size multiple (the putty shrinks as it throws bits of itself).</summary>
        public float SizeFactor { get; set; } = 1f;

        /// <summary>How an enemy's attack looks; each stationery character hits in its own way.</summary>
        public enum AttackStyle { Bash, Chomp, Snip, Slam, Grind, Stab, Throw, Squirt, Recoil }

        /// <summary>Raised at the moment an attack lands (for the hit effect).</summary>
        public event System.Action<AttackStyle> Struck;

        [System.Serializable]
        public struct Limb
        {
            public Transform bone;
            public Vector3 axis;
            public float direction;   // +1 or -1: opposite limbs swing in opposite directions

            public Limb(Transform bone, Vector3 axis, float direction)
            {
                this.bone = bone;
                this.axis = axis;
                this.direction = direction;
            }
        }

        public static readonly int MovingId = Animator.StringToHash("Moving");

        // A critically-damped-ish spring: chases a target with a little overshoot.
        private struct Spring
        {
            public float Value, Velocity;

            public void Step(float target, float frequency, float damping, float dt)
            {
                float w = frequency * Mathf.PI * 2f;
                Velocity += (target - Value) * w * w * dt;
                Velocity *= Mathf.Max(0f, 1f - 2f * damping * w * dt);
                Value += Velocity * dt;
            }
        }

        // One keyed pose of an attack: time (share of the move) and the offsets at that time.
        private readonly struct Key
        {
            public readonly float T, Lean, Yaw, Forward, Lift, Height, Reach, Arms;

            public Key(float t, float lean, float yaw, float forward, float lift, float height, float reach, float arms)
            {
                T = t; Lean = lean; Yaw = yaw; Forward = forward; Lift = lift; Height = height; Reach = reach; Arms = arms;
            }
        }

        private readonly struct Move
        {
            public readonly float Seconds, StrikeAt;
            public readonly Key[] Keys;

            public Move(float seconds, float strikeAt, params Key[] keys)
            {
                Seconds = seconds; StrikeAt = strikeAt; Keys = keys;
            }
        }

        private static Key Rest(float t) => new(t, 0f, 0f, 0f, 0f, 1f, 1f, 0f);

        // lean (deg, + forward), yaw (deg), forward (parent units), lift, height and forward scale, arms (deg).
        // Every move: gather (pull back, crouch, arms up), a fast strike with a smear stretch,
        // a short hold on the hit, then back to rest.
        private static readonly Move[] Moves =
        {
            // Bash: shoulder charge, twisting into it.
            new(0.5f, 0.42f, Rest(0f),
                new Key(0.30f, -9.6f, -24.0f, -0.22f, 0.00f, 0.82f, 0.92f, 60.0f),
                new Key(0.42f, 14.4f, 22.0f, 0.76f, 0.04f, 1.09f, 1.48f, -45.0f),
                new Key(0.56f, 12.0f, 16.0f, 0.71f, 0.00f, 0.92f, 1.24f, -35.0f),
                Rest(1f)),
            // Chomp: rears back, lunges and snaps its jaw twice.
            new(0.55f, 0.40f, Rest(0f),
                new Key(0.28f, -14.4f, 0.0f, -0.20f, 0.00f, 0.76f, 0.92f, 70.0f),
                new Key(0.40f, 16.8f, 0.0f, 0.61f, 0.08f, 1.30f, 1.40f, -50.0f),
                new Key(0.48f, 19.2f, 0.0f, 0.63f, 0.00f, 0.67f, 1.19f, -50.0f),
                new Key(0.56f, 16.8f, 0.0f, 0.61f, 0.04f, 1.18f, 1.16f, -40.0f),
                new Key(0.64f, 14.4f, 0.0f, 0.59f, 0.00f, 0.73f, 1.08f, -30.0f),
                Rest(1f)),
            // Snip: blades flash shut three times as it darts in.
            new(0.5f, 0.38f, Rest(0f),
                new Key(0.28f, -9.6f, -5.4f, -0.16f, 0.00f, 0.85f, 1.00f, 60.0f),
                new Key(0.38f, 12.0f, 7.2f, 0.61f, 0.00f, 1.15f, 1.56f, -60.0f),
                new Key(0.46f, 12.0f, -7.2f, 0.63f, 0.00f, 1.08f, 1.40f, -40.0f),
                new Key(0.54f, 10.8f, 6.0f, 0.61f, 0.00f, 1.08f, 1.32f, -30.0f),
                new Key(0.62f, 9.6f, -4.2f, 0.57f, 0.00f, 1.00f, 1.16f, -20.0f),
                Rest(1f)),
            // Slam: jumps up high, crashes down flat.
            new(0.7f, 0.46f, Rest(0f),
                new Key(0.18f, 0.0f, 0.0f, 0.00f, 0.00f, 0.73f, 1.08f, 30.0f),
                new Key(0.38f, -9.6f, 0.0f, 0.05f, 0.85f, 1.42f, 0.81f, 100.0f),
                new Key(0.46f, 7.2f, 0.0f, 0.25f, 0.00f, 0.40f, 1.56f, -25.0f),
                new Key(0.60f, 4.8f, 0.0f, 0.25f, 0.00f, 0.55f, 1.40f, -10.0f),
                new Key(0.78f, 0.0f, 0.0f, 0.12f, 0.00f, 1.09f, 0.95f, 0.0f),
                Rest(1f)),
            // Grind: lunges, then rattles like the blade is turning.
            new(0.65f, 0.36f, Rest(0f),
                new Key(0.24f, -7.2f, 0.0f, -0.16f, 0.00f, 0.79f, 1.00f, 50.0f),
                new Key(0.36f, 9.6f, 14.0f, 0.51f, 0.03f, 1.15f, 1.32f, -30.0f),
                new Key(0.43f, 9.6f, -16.0f, 0.51f, 0.00f, 0.92f, 1.24f, -25.0f),
                new Key(0.50f, 9.6f, 15.0f, 0.51f, 0.03f, 1.08f, 1.24f, -25.0f),
                new Key(0.57f, 8.4f, -12.0f, 0.49f, 0.00f, 0.92f, 1.16f, -20.0f),
                new Key(0.64f, 7.2f, 9.0f, 0.46f, 0.00f, 1.04f, 1.08f, -10.0f),
                new Key(0.72f, 4.8f, -5.0f, 0.35f, 0.00f, 1.00f, 1.00f, 0.0f),
                Rest(1f)),
            // Stab: coils, then drives in pin first and holds there.
            new(0.5f, 0.40f, Rest(0f),
                new Key(0.30f, -16.8f, 0.0f, -0.31f, 0.00f, 0.76f, 0.81f, 40.0f),
                new Key(0.40f, 19.2f, 0.0f, 0.92f, 0.05f, 1.22f, 1.80f, -30.0f),
                new Key(0.56f, 16.8f, 0.0f, 0.86f, 0.00f, 1.12f, 1.48f, -25.0f),
                Rest(1f)),
            // Throw: rears back with the arm behind, then hurls.
            new(0.55f, 0.46f, Rest(0f),
                new Key(0.36f, -24.0f, -10.0f, -0.31f, 0.00f, 1.12f, 0.92f, -95.0f),
                new Key(0.46f, 12.0f, 12.0f, 0.25f, 0.04f, 0.85f, 1.29f, 85.0f),
                new Key(0.60f, 7.2f, 6.0f, 0.20f, 0.00f, 1.03f, 1.08f, 50.0f),
                Rest(1f)),
            // Squirt: squeezes flat, then shoots up and forward.
            new(0.55f, 0.42f, Rest(0f),
                new Key(0.34f, -4.8f, 0.0f, -0.05f, 0.00f, 0.49f, 1.29f, 40.0f),
                new Key(0.42f, 12.0f, 0.0f, 0.31f, 0.12f, 1.57f, 0.90f, -30.0f),
                new Key(0.54f, 9.6f, 0.0f, 0.31f, 0.00f, 1.18f, 1.00f, -20.0f),
                Rest(1f)),
            // Recoil (not an attack): knocked back, squashed, wobbles upright. Never "strikes".
            new(0.5f, 2f, Rest(0f),
                new Key(0.12f, -26f, 0f, -0.15f, 0f, 0.74f, 0.9f, 80f),
                new Key(0.32f, 10f, 0f, 0.02f, 0f, 1.12f, 1f, -30f),
                new Key(0.55f, -5f, 0f, 0f, 0f, 0.95f, 1f, 15f),
                new Key(0.75f, 2f, 0f, 0f, 0f, 1.02f, 1f, -5f),
                Rest(1f)),
        };

        private NavMeshAgent _agent;
        private EnemyFollow _enemy;
        private Vector3 _basePos, _baseScale, _lastParentPos, _step;
        private Quaternion _baseRot;
        private float _phase, _move, _flip = 1f, _seed, _lastYaw, _lastSpeed;
        private Spring _leanSpring, _bankSpring, _armSpring, _landSpring;
        private float _lastAir;

        private AttackStyle _attackStyle;
        private float _attackTime = -1f;
        private bool _struck;

#if UNITY_EDITOR
        public void SetLimbs(Limb[] value) => swingLimbs = value;
#endif

        /// <summary>Play one attack move on top of whatever the body is doing.</summary>
        public void PlayAttack(AttackStyle style)
        {
            _attackStyle = style;
            _attackTime = 0f;
            _struck = false;
        }

        public bool IsAttacking => _attackTime >= 0f;

        /// <summary>0..1 while an attack gathers itself before the strike, else 0 (the warning).</summary>
        public float AttackGather
        {
            get
            {
                if (_attackTime < 0f) return 0f;
                Move m = Moves[(int)_attackStyle];
                float k = _attackTime / m.Seconds;
                return k < m.StrikeAt ? k / m.StrikeAt : 0f;
            }
        }

        private void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
            _enemy = GetComponentInParent<EnemyFollow>();
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;
            _baseRot = transform.localRotation;
            if (transform.parent != null)
            {
                _lastParentPos = transform.parent.position;
                _lastYaw = transform.parent.eulerAngles.y;
            }
            _seed = Random.value * 10f;
            _phase = _seed;
            _landSpring.Value = 1f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (transform.parent != null)
            {
                _step = transform.parent.position - _lastParentPos;
                _step.y = 0f;
                _lastParentPos = transform.parent.position;
            }
            float speed = _agent != null && _agent.isActiveAndEnabled ? _agent.velocity.magnitude
                        : dt > 0f ? _step.magnitude / dt : 0f;
            float target = Mathf.Clamp01(speed / Mathf.Max(0.01f, referenceSpeed));
            _move = Mathf.MoveTowards(_move, target, dt * 5f);
            _phase += dt * hopRate * Mathf.Lerp(0.3f, 1f, _move);

            if (limbs != null)
            {
                limbs.SetBool(MovingId, _move > 0.08f);
                limbs.speed = Mathf.Lerp(0.5f, 1f, _move) * limbSpeed;
            }

            // Hop: in the air for the first 80% of a cycle on a real arc, on the ground for the rest.
            float cycle = Mathf.Repeat(_phase, 1f);
            const float airShare = 0.8f;
            float u = Mathf.Clamp01(cycle / airShare);
            float air = cycle < airShare ? 4f * u * (1f - u) : 0f;
            float rise = cycle < airShare ? (1f - 2f * u) : 0f;        // + going up, - coming down
            float y = air * hopHeight * _move;

            // Landing squash on a spring: kicked on each touchdown, it overshoots back to 1.
            if (_lastAir > 0.02f && air <= 0.02f && _move > 0.15f) _landSpring.Velocity -= 6f * squash * _move;
            _lastAir = air;
            _landSpring.Step(1f, 4.5f, 0.35f, dt);
            float breath = 0.025f * Mathf.Sin((Time.time + _seed) * 2.4f) * (1f - _move);
            float height = _landSpring.Value + squash * 0.6f * rise * _move + breath;

            // Lean into speed and acceleration on a spring: tips forward when it sets off, rocks
            // back past upright when it stops.
            float accel = dt > 0f ? (speed - _lastSpeed) / dt : 0f;
            _lastSpeed = speed;
            _leanSpring.Step(lean * _move + Mathf.Clamp(accel * 1.5f, -14f, 14f), 3.2f, 0.3f, dt);

            // Bank into turns, also on a spring.
            float yaw = transform.parent != null ? transform.parent.eulerAngles.y : 0f;
            float turnRate = dt > 0f ? Mathf.DeltaAngle(_lastYaw, yaw) / dt : 0f;
            _lastYaw = yaw;
            _bankSpring.Step(Mathf.Clamp(-turnRate * 0.05f, -14f, 14f) * Mathf.Max(0.3f, _move), 3f, 0.35f, dt);
            float side = Mathf.Sin(_phase * Mathf.PI) * waddle * _move + _bankSpring.Value
                       + 1.5f * Mathf.Sin((Time.time + _seed) * 1.3f) * (1f - _move);   // idle sway

            // Attack pose on top.
            float aLean = 0f, aYaw = 0f, aForward = 0f, aLift = 0f, aHeight = 1f, aReach = 1f, aArms = 0f;
            if (_attackTime >= 0f)
            {
                Move m = Moves[(int)_attackStyle];
                _attackTime += dt;
                float k = _attackTime / m.Seconds;
                if (!_struck && k >= m.StrikeAt) { _struck = true; Struck?.Invoke(_attackStyle); }
                if (k >= 1f) _attackTime = -1f;
                else Sample(m.Keys, k, out aLean, out aYaw, out aForward, out aLift, out aHeight, out aReach, out aArms);
            }

            // Limbs: legs step in turn with the hops, arms swing against them; the arms trail on a
            // spring, so they flop a beat behind the body. A sliding enemy flails.
            float slide = _enemy != null ? _enemy.SlideAmount : 0f;
            float stride = Mathf.Sin(_phase * Mathf.PI) * Mathf.Lerp(5f, swingAngle, Mathf.Max(_move, slide));
            _armSpring.Step(stride * 0.8f + aArms, 6f, 0.25f, dt);
            float idleArms = 4f * Mathf.Sin((Time.time + _seed) * 2.4f + 1f) * (1f - _move);
            // Scissor blades: snap open and shut as it runs, and fast and wide while it snips.
            float blades = 14f * Mathf.Abs(Mathf.Sin(_phase * Mathf.PI)) * _move;
            if (_attackTime >= 0f && _attackStyle == AttackStyle.Snip)
            {
                Move m = Moves[(int)_attackStyle];
                float k = Mathf.Clamp01(_attackTime / m.Seconds);
                float open = k < m.StrikeAt ? Mathf.SmoothStep(0f, 1f, k / m.StrikeAt)                 // opens wide
                           : Mathf.Abs(Mathf.Cos((k - m.StrikeAt) / (1f - m.StrikeAt) * Mathf.PI * 3f))   // snaps shut three times
                             * (1f - (k - m.StrikeAt) / (1f - m.StrikeAt) * 0.7f);
                blades = 42f * open;
            }
            foreach (Limb limb in swingLimbs)
            {
                if (limb.bone == null) continue;
                if (limb.bone.name.Contains("Blade"))
                {
                    limb.bone.localRotation = Quaternion.AngleAxis(blades * limb.direction, limb.axis);
                    continue;
                }
                bool arm = limb.bone.name.Contains("Arm");
                float angle = arm ? _armSpring.Value + idleArms : stride * (limb.bone.name.StartsWith("L") ? 1f : -1f);
                limb.bone.localRotation = Quaternion.AngleAxis(angle * limb.direction, limb.axis);
            }

            transform.localPosition = _basePos + Vector3.up * (y + aLift) + Vector3.forward * aForward;

            float stretch = ExtraStretch * aHeight * height;
            float widen = 1f / Mathf.Sqrt(Mathf.Max(0.2f, stretch));   // keep the volume
            var scale = new Vector3(_baseScale.x * widen, _baseScale.y * HeightFactor * stretch, _baseScale.z * widen * aReach) * SizeFactor;
            if (sideOn && Camera.main != null && transform.parent != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 facing = transform.parent.forward;
                float sideways = Vector3.Dot(facing, cam.right);
                if (Mathf.Abs(sideways) > 0.05f) _flip = Mathf.Sign(sideways);
                // During an attack it tips over sideways so the blades point at the target.
                float tip = 0f;
                if (_attackTime >= 0f)
                {
                    float k = Mathf.Clamp01(_attackTime / Moves[(int)_attackStyle].Seconds);
                    tip = k < 0.3f ? Mathf.SmoothStep(0f, 1f, k / 0.3f) : k > 0.75f ? Mathf.SmoothStep(0f, 1f, (1f - k) / 0.25f) : 1f;
                }
                float roll = side - _flip * 80f * tip;   // handles swing away, blades swing at the target
                Vector3 toCam = -cam.forward;
                toCam.y = Mathf.Max(0f, toCam.y) * 0.35f;   // stays mostly upright, just turned to the camera
                transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, roll);
                // Tip over around the middle of the body, not the feet, so it stays where it stands.
                float half = 0.5f * transform.lossyScale.y;
                transform.position += Vector3.up * half - transform.rotation * Vector3.up * half;
            }
            else if (faceCamera && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                float sideways = Vector3.Dot(_step, cam.right);
                if (Mathf.Abs(sideways) > 0.001f) _flip = Mathf.Sign(sideways);
                scale.x *= _flip;
                transform.rotation = Quaternion.LookRotation(-cam.forward, Vector3.up) * Quaternion.Euler(0f, 0f, side);
            }
            else transform.localRotation = _baseRot * Quaternion.Euler(_leanSpring.Value + ExtraLean + aLean, aYaw, side);
            if (slide > 0f)
            {
                float wobble = 0.75f + 0.25f * Mathf.Sin(Time.time * 30f);
                transform.rotation = Quaternion.AngleAxis(slideLean * slide * wobble, Vector3.Cross(_enemy.SlideDirection, Vector3.up)) * transform.rotation;
            }
            transform.localScale = scale;
        }

        // Pose between the two keys around k, eased so each segment starts and lands softly.
        private static void Sample(Key[] keys, float k, out float lean, out float yaw, out float forward, out float lift, out float height, out float reach, out float arms)
        {
            int i = 0;
            while (i < keys.Length - 2 && k > keys[i + 1].T) i++;
            Key a = keys[i], b = keys[i + 1];
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.T, b.T, k));
            lean = Mathf.Lerp(a.Lean, b.Lean, t);
            yaw = Mathf.Lerp(a.Yaw, b.Yaw, t);
            forward = Mathf.Lerp(a.Forward, b.Forward, t);
            lift = Mathf.Lerp(a.Lift, b.Lift, t);
            height = Mathf.Lerp(a.Height, b.Height, t);
            reach = Mathf.Lerp(a.Reach, b.Reach, t);
            arms = Mathf.Lerp(a.Arms, b.Arms, t);
        }
    }
}
