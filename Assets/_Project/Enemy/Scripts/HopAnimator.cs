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
            foreach (Limb limb in swingLimbs)
                if (limb.bone != null) limb.bone.localRotation = Quaternion.AngleAxis(swing * limb.direction, limb.axis);

            float cycle = Mathf.Repeat(_phase, 1f);
            float air = Mathf.Sin(cycle * Mathf.PI);          // 0 on the ground, 1 at the top
            float y = air * hopHeight * _move;
            // Stretched in the air, squashed at contact; a soft breath when standing still.
            float s = Mathf.Lerp(0.03f * Mathf.Sin(_phase * Mathf.PI * 2f), (air - 0.5f) * 2f * squash, _move);
            float side = Mathf.Sin(_phase * Mathf.PI) * waddle * _move;

            transform.localPosition = _basePos + Vector3.up * y;
            float stretch = ExtraStretch;
            float widen = 1f / Mathf.Sqrt(Mathf.Max(0.2f, stretch));   // keep the volume when it stretches
            var scale = new Vector3(_baseScale.x * (1f - s * 0.5f) * widen, _baseScale.y * (1f + s) * HeightFactor * stretch, _baseScale.z * (1f - s * 0.5f) * widen);
            if (faceCamera && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                float sideways = Vector3.Dot(_step, cam.right);
                if (Mathf.Abs(sideways) > 0.001f) _flip = Mathf.Sign(sideways);
                scale.x *= _flip;
                transform.rotation = Quaternion.LookRotation(-cam.forward, Vector3.up) * Quaternion.Euler(0f, 0f, side);
            }
            else transform.localRotation = _baseRot * Quaternion.Euler(lean * _move + ExtraLean, 0f, side);
            if (slide > 0f)
            {
                float wobble = 0.75f + 0.25f * Mathf.Sin(Time.time * 30f);
                transform.rotation = Quaternion.AngleAxis(slideLean * slide * wobble, Vector3.Cross(_enemy.SlideDirection, Vector3.up)) * transform.rotation;
            }
            transform.localScale = scale;
        }
    }
}
