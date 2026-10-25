using UnityEngine;
using UnityEngine.Serialization;

namespace DrawRush.Core
{
    /// <summary>
    /// Game-wide tunables exposed as read-only properties so consumers cannot mutate
    /// them at runtime. Serialized backing fields are private — designers tweak via
    /// the Inspector on the GameConfig.asset instance.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "DrawRush/Core/Game Config", order = 0)]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Player Movement")]
        [Tooltip("CharacterController forward speed (units / second).")]
        [FormerlySerializedAs("playerSpeed")]
        [SerializeField] private float _playerSpeed = 1.5f;

        [Tooltip("Stick deflection (0 = deadzone edge, 1 = full) → fraction of playerSpeed. " +
                 "A gentle push walks for precise tracing, a full push runs.")]
        [SerializeField] private AnimationCurve _speedByDeflection = DefaultSpeedCurve();

        [Tooltip("Seconds from standstill to full speed.")]
        [SerializeField] private float _accelerationTime = 0.08f;

        [Tooltip("Seconds from full speed to standstill once the stick is released.")]
        [SerializeField] private float _decelerationTime = 0.05f;

        [Tooltip("SmoothDampAngle time for player turn (seconds).")]
        [FormerlySerializedAs("turnSmoothTime")]
        [SerializeField] private float _turnSmoothTime = 0.1f;

        [Tooltip("Gravity applied to player (units / s^2). Negative.")]
        [FormerlySerializedAs("gravity")]
        [SerializeField] private float _gravity = -9.81f;

        [Header("Performance")]
        [Tooltip("Application.targetFrameRate set at boot. Android otherwise caps at 30 fps, " +
                 "which makes 50/60 Hz movement step unevenly from frame to frame.")]
        [SerializeField] private int _targetFrameRate = 60;

        [Header("Combat")]
        [Tooltip("Starting health for the player.")]
        [FormerlySerializedAs("playerStartingHealth")]
        [SerializeField] private int _playerStartingHealth = 3;

        [Tooltip("Damage dealt by an enemy on touch (positive magnitude).")]
        [FormerlySerializedAs("enemyTouchDamage")]
        [SerializeField] private int _enemyTouchDamage = 1;

        [Header("Drawing")]
        [Tooltip("Width of the painted edge line.")]
        [FormerlySerializedAs("lineWidth")]
        [SerializeField] private float _lineWidth = 0.4f;

        [Header("Flow")]
        [Tooltip("Seconds to wait after Game Won before showing the win panel.")]
        [FormerlySerializedAs("gameWonDelay")]
        [SerializeField] private float _gameWonDelay = 3.0f;

        public float playerSpeed => _playerSpeed;
        public float turnSmoothTime => _turnSmoothTime;
        public float gravity => _gravity;
        public int targetFrameRate => _targetFrameRate;
        public float accelerationTime => _accelerationTime;
        public float decelerationTime => _decelerationTime;

        /// <summary>Fraction of <see cref="playerSpeed"/> for a stick deflection in 0..1.</summary>
        public float SpeedFactor(float deflection)
            => Mathf.Clamp01(_speedByDeflection != null && _speedByDeflection.length > 0
                ? _speedByDeflection.Evaluate(Mathf.Clamp01(deflection))
                : 1f);

        // Walk band up to about half deflection, a short ramp, then a flat run.
        private static AnimationCurve DefaultSpeedCurve() => new AnimationCurve(
            new Keyframe(0f, 0.35f, 0f, 0f), new Keyframe(0.5f, 0.45f, 0f, 0f),
            new Keyframe(0.7f, 1f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));
        public int playerStartingHealth => _playerStartingHealth;
        public int enemyTouchDamage => _enemyTouchDamage;
        public float lineWidth => _lineWidth;
        public float gameWonDelay => _gameWonDelay;
    }
}
