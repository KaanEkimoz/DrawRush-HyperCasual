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

        [Tooltip("SmoothDampAngle time for player turn (seconds).")]
        [FormerlySerializedAs("turnSmoothTime")]
        [SerializeField] private float _turnSmoothTime = 0.1f;

        [Tooltip("Gravity applied to player (units / s^2). Negative.")]
        [FormerlySerializedAs("gravity")]
        [SerializeField] private float _gravity = -9.81f;

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
        public int playerStartingHealth => _playerStartingHealth;
        public int enemyTouchDamage => _enemyTouchDamage;
        public float lineWidth => _lineWidth;
        public float gameWonDelay => _gameWonDelay;
    }
}
