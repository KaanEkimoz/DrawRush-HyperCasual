using UnityEngine;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Enemy
{
    /// <summary>
    /// Damages the player on touch. Player ref is taken from <see cref="GameServices.Player"/>
    /// at first contact instead of FindObjectOfType at Awake — works in additive scene loads.
    /// </summary>
    public sealed class EnemyCombat : MonoBehaviour
    {
        [SerializeField] private Animator enemyAnim;
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Damage applied on touch (signed). Negative reduces HP. If 0, uses GameConfig.enemyTouchDamage.")]
        [SerializeField] private int damageOverride;

        private PlayerCombat _playerCombat;

        public Animator EnemyAnim => enemyAnim;

        private void Awake()
        {
            if (enemyAnim == null)
            {
                enemyAnim = GetComponentInChildren<Animator>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            var state = GameServices.State;
            if (state != null && state.IsGameWon) return;

            if (_playerCombat == null)
            {
                _playerCombat = other.GetComponent<PlayerCombat>();
            }
            if (_playerCombat == null) return;

            int dmg = damageOverride != 0
                ? damageOverride
                : (GameServices.Config != null ? GameServices.Config.enemyTouchDamage : -1);
            _playerCombat.TakeDamage(dmg);
        }
    }
}
