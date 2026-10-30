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

        /// <summary>Extra height multiple (the pencil hero shortens as it takes hits).</summary>
        public float HeightFactor { get; set; } = 1f;

        private NavMeshAgent _agent;
        private Vector3 _basePos, _baseScale, _lastParentPos, _step;
        private Quaternion _baseRot;
        private float _phase, _move, _flip = 1f;

        private void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
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

            float cycle = Mathf.Repeat(_phase, 1f);
            float air = Mathf.Sin(cycle * Mathf.PI);          // 0 on the ground, 1 at the top
            float y = air * hopHeight * _move;
            // Stretched in the air, squashed at contact; a soft breath when standing still.
            float s = Mathf.Lerp(0.03f * Mathf.Sin(_phase * Mathf.PI * 2f), (air - 0.5f) * 2f * squash, _move);
            float side = Mathf.Sin(_phase * Mathf.PI) * waddle * _move;

            transform.localPosition = _basePos + Vector3.up * y;
            var scale = new Vector3(_baseScale.x * (1f - s * 0.5f), _baseScale.y * (1f + s) * HeightFactor, _baseScale.z * (1f - s * 0.5f));
            if (faceCamera && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                float sideways = Vector3.Dot(_step, cam.right);
                if (Mathf.Abs(sideways) > 0.001f) _flip = Mathf.Sign(sideways);
                scale.x *= _flip;
                transform.rotation = Quaternion.LookRotation(-cam.forward, Vector3.up) * Quaternion.Euler(0f, 0f, side);
            }
            else transform.localRotation = _baseRot * Quaternion.Euler(lean * _move, 0f, side);
            transform.localScale = scale;
        }
    }
}
