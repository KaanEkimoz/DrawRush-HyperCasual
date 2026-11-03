using UnityEngine;
using UnityEngine.AI;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Animates a rigid character by moving its whole visual: it hops while moving, squashes on
    /// landing, tips toward its heading and breathes at rest. For the stationery enemies, whose
    /// box bodies tear under a humanoid skeleton, and for the pencil hero. Reads real ground speed
    /// (the agent's, or the parent's movement without one), so a blocked or frozen character
    /// stops hopping by itself.
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
        public enum AttackStyle { Bash, Chomp, Snip, Slam, Grind, Stab, Throw, Squirt }

        private const float AttackLength = 0.45f;
        private AttackStyle _attackStyle;
        private float _attackTime = -1f;

        /// <summary>Play one attack move on top of whatever the body is doing.</summary>
        public void PlayAttack(AttackStyle style)
        {
            _attackStyle = style;
            _attackTime = 0f;
        }

        public bool IsAttacking => _attackTime >= 0f;

        // Lean (deg), stretch, yaw (deg), forward shift and arm swing (deg) of the attack at time k (0..1).
        private void AttackPose(float k, out float lean, out float stretch, out float yaw, out float forward, out float arms)
        {
            float e = Mathf.Sin(k * Mathf.PI);   // in and out
            lean = 0f; stretch = 1f; yaw = 0f; forward = 0f; arms = 0f;
            switch (_attackStyle)
            {
                case AttackStyle.Bash:   // shoulder charge
                    lean = 30f * e; forward = 0.25f * e; stretch = 1f + 0.1f * e; arms = 50f * Mathf.Sin(k * Mathf.PI * 2f) * e;
                    break;
                case AttackStyle.Chomp:   // two quick bites
                    lean = 22f * e; stretch = 1f - 0.28f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 2f)); forward = 0.15f * e; arms = 40f * e;
                    break;
                case AttackStyle.Snip:   // blades snap side to side
                    yaw = 32f * Mathf.Sin(k * Mathf.PI * 4f) * e; lean = 12f * e; forward = 0.12f * e; arms = 60f * Mathf.Sin(k * Mathf.PI * 4f) * e;
                    break;
                case AttackStyle.Slam:   // rise up, then crash down
                    if (k < 0.45f) { float u = k / 0.45f; lean = -18f * u; stretch = 1f + 0.3f * u; arms = 80f * u; }
                    else { float d = (k - 0.45f) / 0.55f; float hit = Mathf.Sin(Mathf.Min(1f, d * 1.6f) * Mathf.PI * 0.5f); lean = Mathf.Lerp(-18f, 14f, hit) * (1f - d * 0.7f); stretch = Mathf.Lerp(1.3f, 0.72f, hit) + 0.28f * d; arms = 80f * (1f - hit); }
                    break;
                case AttackStyle.Grind:   // spins like the blade
                    yaw = 360f * Mathf.SmoothStep(0f, 1f, k); stretch = 1f - 0.12f * e; arms = 45f * e;
                    break;
                case AttackStyle.Stab:   // pin first
                    lean = 42f * e; forward = 0.3f * e; stretch = 1f + 0.22f * e;
                    break;
                case AttackStyle.Throw:   // wind back, then hurl
                    lean = k < 0.5f ? -26f * (k / 0.5f) : Mathf.Lerp(-26f, 22f, (k - 0.5f) / 0.5f) * (1f - (k - 0.5f));
                    stretch = 1f + 0.15f * e; arms = k < 0.5f ? -70f * (k / 0.5f) : Mathf.Lerp(-70f, 60f, (k - 0.5f) / 0.5f);
                    break;
                case AttackStyle.Squirt:   // squeeze down, then shoot up
                    stretch = k < 0.5f ? 1f - 0.3f * (k / 0.5f) : 0.7f + 0.55f * Mathf.Sin((k - 0.5f) / 0.5f * Mathf.PI); lean = 16f * e; arms = 40f * e;
                    break;
            }
        }

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

        private NavMeshAgent _agent;
        private EnemyFollow _enemy;
        private Vector3 _basePos, _baseScale, _lastParentPos, _step;
        private Quaternion _baseRot;
        private float _phase, _move, _flip = 1f;

#if UNITY_EDITOR
        public void SetLimbs(Limb[] value) => swingLimbs = value;
#endif

        private void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
            _enemy = GetComponentInParent<EnemyFollow>();
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;
            _baseRot = transform.localRotation;
            if (transform.parent != null) _lastParentPos = transform.parent.position;
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
            _move = Mathf.MoveTowards(_move, target, dt * 6f);
            _phase += dt * hopRate * Mathf.Lerp(0.25f, 1f, _move);

            if (limbs != null)
            {
                limbs.SetBool(MovingId, _move > 0.08f);
                limbs.speed = Mathf.Lerp(0.5f, 1f, _move) * limbSpeed;
            }

            // A sliding enemy flails: full limb swing, though it isn't walking.
            float slide = _enemy != null ? _enemy.SlideAmount : 0f;
            float swing = Mathf.Sin(_phase * Mathf.PI) * Mathf.Lerp(6f, swingAngle, Mathf.Max(_move, slide));

            float aLean = 0f, aStretch = 1f, aYaw = 0f, aForward = 0f, aArms = 0f;
            if (_attackTime >= 0f)
            {
                _attackTime += dt;
                float k = _attackTime / AttackLength;
                if (k >= 1f) _attackTime = -1f;
                else AttackPose(k, out aLean, out aStretch, out aYaw, out aForward, out aArms);
            }
            foreach (Limb limb in swingLimbs)
            {
                if (limb.bone == null) continue;
                float extra = limb.bone.name.Contains("Arm") ? aArms : 0f;
                limb.bone.localRotation = Quaternion.AngleAxis((swing + extra) * limb.direction, limb.axis);
            }

            float cycle = Mathf.Repeat(_phase, 1f);
            float air = Mathf.Sin(cycle * Mathf.PI);          // 0 on the ground, 1 at the top
            float y = air * hopHeight * _move;
            // Stretched in the air, squashed at contact; a soft breath when standing still.
            float s = Mathf.Lerp(0.03f * Mathf.Sin(_phase * Mathf.PI * 2f), (air - 0.5f) * 2f * squash, _move);
            float side = Mathf.Sin(_phase * Mathf.PI) * waddle * _move;

            transform.localPosition = _basePos + Vector3.up * y + Vector3.forward * aForward;
            float stretch = ExtraStretch * aStretch;
            float widen = 1f / Mathf.Sqrt(Mathf.Max(0.2f, stretch));   // keep the volume when it stretches
            var scale = new Vector3(_baseScale.x * (1f - s * 0.5f) * widen, _baseScale.y * (1f + s) * HeightFactor * stretch, _baseScale.z * (1f - s * 0.5f) * widen) * SizeFactor;
            if (faceCamera && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                float sideways = Vector3.Dot(_step, cam.right);
                if (Mathf.Abs(sideways) > 0.001f) _flip = Mathf.Sign(sideways);
                scale.x *= _flip;
                transform.rotation = Quaternion.LookRotation(-cam.forward, Vector3.up) * Quaternion.Euler(0f, 0f, side);
            }
            else transform.localRotation = _baseRot * Quaternion.Euler(lean * _move + ExtraLean + aLean, aYaw, side);
            if (slide > 0f)
            {
                float wobble = 0.75f + 0.25f * Mathf.Sin(Time.time * 30f);
                transform.rotation = Quaternion.AngleAxis(slideLean * slide * wobble, Vector3.Cross(_enemy.SlideDirection, Vector3.up)) * transform.rotation;
            }
            transform.localScale = scale;
        }
    }
}
