using UnityEngine;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Brief enemy-hit knockback. EnemyCombat calls <see cref="ApplyKnockback"/> with a
    /// world-space direction and a force; this controller then slides the
    /// CharacterController along that vector for <see cref="knockbackDuration"/> seconds
    /// with linear decay (force at start, 0 at end). While <see cref="IsActive"/> is true,
    /// ThirdPersonMovement and RailPaintController yield so this script owns the movement.
    ///
    /// Runs before the other movement controllers so its Move() lands before they'd try
    /// to apply theirs in the same physics step.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerKnockback : MonoBehaviour
    {
        [Tooltip("How long the knockback push lasts, in seconds.")]
        [SerializeField] private float knockbackDuration = 0.35f;

        private CharacterController _cc;
        private Vector3 _velocity;
        private float _remaining;
        private float _initialDuration;

        /// <summary>True while a knockback is currently being applied.</summary>
        public bool IsActive => _remaining > 0f;

        private void Awake() => _cc = GetComponent<CharacterController>();

        /// <summary>Push the player along <paramref name="worldDirection"/> for
        /// <see cref="knockbackDuration"/> seconds. Y is flattened so the push stays
        /// horizontal. <paramref name="force"/> is the starting speed in units/sec; it
        /// decays linearly to zero by the end of the duration.</summary>
        public void ApplyKnockback(Vector3 worldDirection, float force)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude < 0.0001f || force <= 0f) return;
            _velocity = worldDirection.normalized * force;
            _remaining = Mathf.Max(0f, knockbackDuration);
            _initialDuration = _remaining;
        }

        private void FixedUpdate()
        {
            if (_remaining <= 0f) return;
            float dt = Time.fixedDeltaTime;
            float decay = _initialDuration > 0f ? (_remaining / _initialDuration) : 0f;
            _cc.Move(_velocity * (decay * dt));
            _remaining -= dt;
            if (_remaining <= 0f) _velocity = Vector3.zero;
        }
    }
}
