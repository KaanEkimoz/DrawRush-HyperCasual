using UnityEngine;
using UnityEngine.Serialization;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Enemy
{
    /// <summary>
    /// Damages the player on contact, plays death anim when the game is won.
    /// Self-subscribes to <see cref="GameState.GameWonChanged"/> instead of being
    /// poked by GameManager — inverts the legacy Core → Enemy dependency.
    /// </summary>
    public sealed class EnemyCombat : MonoBehaviour
    {
        [SerializeField] private Animator enemyAnim;
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Damage applied on touch (positive magnitude). If 0, uses GameConfig.enemyTouchDamage.")]
        [FormerlySerializedAs("damage")]
        [SerializeField] private int damageOverride;

        private PlayerCombat _playerCombat;
        private GameState _state;

        private void Awake()
        {
            if (enemyAnim == null)
            {
                enemyAnim = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            _state = GameServices.State;
            if (_state != null) _state.GameWonChanged += OnGameWonChanged;
        }

        private void OnDisable()
        {
            if (_state != null) _state.GameWonChanged -= OnGameWonChanged;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (_state != null && _state.IsGameWon) return;

            // Drawing-puzzle safe zone: while the player is inside a DrawArea, enemies
            // cannot damage. Combat resumes the instant the player steps outside.
            var interact = other.GetComponent<PlayerInteract>();
            if (interact != null && interact.IsInDrawArea) return;

            if (_playerCombat == null)
            {
                _playerCombat = other.GetComponent<PlayerCombat>();
            }
            if (_playerCombat == null) return;

            int dmg = damageOverride > 0
                ? damageOverride
                : (GameServices.Config != null ? GameServices.Config.enemyTouchDamage : 1);
            _playerCombat.TakeDamage(Mathf.Abs(dmg));
        }

        private void OnGameWonChanged(bool won)
        {
            if (!won || enemyAnim == null) return;
            enemyAnim.SetTrigger(AnimatorIds.EnemyDie);
        }
    }
}
