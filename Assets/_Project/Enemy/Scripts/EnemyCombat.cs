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

        [Header("Knockback")]
        [Tooltip("Push speed (units/sec) applied to the player on contact, decaying to 0 over " +
                 "PlayerKnockback.knockbackDuration. 0 disables knockback.")]
        [SerializeField] private float knockbackForce = 9f;

        private PlayerCombat _playerCombat;
        private RailPaintController _railPaint;
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

            // Contact frees the player from the paint rail so they can flee.
            if (_railPaint == null) _railPaint = other.GetComponent<RailPaintController>();
            if (_railPaint != null) _railPaint.Detach();

            // Knock the player away from the enemy along the horizontal contact direction.
            if (knockbackForce > 0f)
            {
                var kb = other.GetComponent<PlayerKnockback>();
                if (kb != null)
                {
                    Vector3 dir = other.transform.position - transform.position;
                    kb.ApplyKnockback(dir, knockbackForce);
                }
            }

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
