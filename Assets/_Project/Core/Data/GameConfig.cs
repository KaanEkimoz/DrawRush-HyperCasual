using UnityEngine;

namespace Studios208.DrawRush.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "DrawRush/Core/Game Config", order = 0)]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Player Movement")]
        [Tooltip("CharacterController forward speed (units / second).")]
        public float playerSpeed = 1.5f;

        [Tooltip("SmoothDampAngle time for player turn (seconds).")]
        public float turnSmoothTime = 0.1f;

        [Tooltip("Gravity applied to player (units / s^2). Negative.")]
        public float gravity = -9.81f;

        [Header("Combat")]
        [Tooltip("Starting health for the player.")]
        public int playerStartingHealth = 3;

        [Tooltip("Damage dealt by an enemy on touch (signed). Negative reduces HP.")]
        public int enemyTouchDamage = -1;

        [Header("Drawing")]
        [Tooltip("Seconds to wait before destroying the connecting LineRenderer.")]
        public float lineDestroyDelay = 2.0f;

        [Tooltip("Width of the connecting LineRenderer between two parts.")]
        public float lineWidth = 0.4f;

        [Tooltip("Lerp factor used when trail catches up to the player.")]
        public float trailCatchUpLerp = 100f;

        [Header("Flow")]
        [Tooltip("Seconds to wait after Game Won before showing the win panel.")]
        public float gameWonDelay = 3.0f;

        [Tooltip("Seconds to wait on splash before loading next scene.")]
        public float splashWaitSeconds = 2.1f;
    }
}
