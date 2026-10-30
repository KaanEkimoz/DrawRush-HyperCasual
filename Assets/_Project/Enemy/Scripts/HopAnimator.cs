using UnityEngine;
using UnityEngine.AI;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Animates a rigid character by moving its whole visual: it hops while moving, squashes on
    /// landing, tips toward its heading and breathes at rest. For the stationery enemies, whose
    /// box bodies tear under a humanoid skeleton. Reads the agent's real speed, so a blocked or
    /// frozen enemy stops hopping by itself.
    /// </summary>
    public sealed class HopAnimator : MonoBehaviour
    {
        [Tooltip("Hops per second at the reference speed.")]
        [SerializeField] private float hopRate = 3.5f;
        [Tooltip("Agent speed (m/s) that gives the full hop rate.")]
        [SerializeField] private float referenceSpeed = 2.5f;
        [Tooltip("Hop height, in the visual's parent units.")]
        [SerializeField] private float hopHeight = 0.35f;
        [Tooltip("Squash and stretch amount (0 = rigid).")]
        [SerializeField, Range(0f, 0.5f)] private float squash = 0.2f;
        [Tooltip("Forward tilt (degrees) at full speed.")]
        [SerializeField] private float lean = 12f;
        [Tooltip("Side-to-side waddle (degrees).")]
        [SerializeField] private float waddle = 8f;

        private NavMeshAgent _agent;
        private Vector3 _basePos, _baseScale;
        private Quaternion _baseRot;
        private float _phase, _move;

        private void Awake()
        {
            _agent = GetComponentInParent<NavMeshAgent>();
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;
            _baseRot = transform.localRotation;
        }

        private void Update()
        {
            float speed = _agent != null && _agent.isActiveAndEnabled ? _agent.velocity.magnitude : 0f;
            float target = Mathf.Clamp01(speed / Mathf.Max(0.01f, referenceSpeed));
            _move = Mathf.MoveTowards(_move, target, Time.deltaTime * 6f);
            _phase += Time.deltaTime * hopRate * Mathf.Lerp(0.25f, 1f, _move);

            float cycle = Mathf.Repeat(_phase, 1f);
            float air = Mathf.Sin(cycle * Mathf.PI);          // 0 on the ground, 1 at the top
            float y = air * hopHeight * _move;
            // Stretched in the air, squashed at contact; a soft breath when standing still.
            float s = Mathf.Lerp(0.03f * Mathf.Sin(_phase * Mathf.PI * 2f), (air - 0.5f) * 2f * squash, _move);

            transform.localPosition = _basePos + Vector3.up * y;
            transform.localScale = new Vector3(_baseScale.x * (1f - s * 0.5f), _baseScale.y * (1f + s), _baseScale.z * (1f - s * 0.5f));
            float side = Mathf.Sin(_phase * Mathf.PI) * waddle * _move;
            transform.localRotation = _baseRot * Quaternion.Euler(lean * _move, 0f, side);
        }
    }
}
