using UnityEngine;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Pre-hashed Animator parameter / trigger ids. Replaces string literals
    /// scattered across ThirdPersonMovement, EnemyCombat, GameManager.
    /// </summary>
    public static class AnimatorIds
    {
        public static readonly int IsDancing = Animator.StringToHash("b_isDancing");
        public static readonly int EnemyDie = Animator.StringToHash("t_die");
        public static readonly int Speed = Animator.StringToHash("f_speed");
        public static readonly int Hit = Animator.StringToHash("t_hit");
    }
}
