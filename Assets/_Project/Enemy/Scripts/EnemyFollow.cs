using UnityEngine;
using UnityEngine.AI;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Enemy
{
    /// <summary>
    /// Chases the player using NavMeshAgent. Player position is read from
    /// <see cref="GameServices.Player"/> every frame — survives scene-additive load
    /// where the legacy FindWithTag at Start() would have missed the player.
    /// Stops chasing once <see cref="GameState.IsGameWon"/> flips true.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyFollow : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private bool _haltedOnWin;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            var state = GameServices.State;
            if (state != null && state.IsGameWon)
            {
                if (!_haltedOnWin)
                {
                    _agent.isStopped = true;
                    _haltedOnWin = true;
                }
                return;
            }

            var player = GameServices.Player;
            if (player == null) return;
            _agent.SetDestination(player.position);
        }
    }
}
