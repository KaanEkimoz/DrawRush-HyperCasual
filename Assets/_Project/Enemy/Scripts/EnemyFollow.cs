using UnityEngine;
using UnityEngine.AI;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Enemy
{
    /// <summary>
    /// Chases the player via NavMeshAgent. Subscribes to GameState.GameWonChanged
    /// to halt cleanly instead of polling IsGameWon every frame.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyFollow : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private GameState _state;
        private bool _halted;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
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

        private void Update()
        {
            if (_halted) return;
            var player = GameServices.Player;
            if (player == null) return;
            _agent.SetDestination(player.position);
        }

        private void OnGameWonChanged(bool won)
        {
            if (!won) return;
            _agent.isStopped = true;
            _halted = true;
        }
    }
}
