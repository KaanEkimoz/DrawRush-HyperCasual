using UnityEngine;
using UnityEngine.AI;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Enemy
{
    /// <summary>
    /// Chases the player via NavMeshAgent. Subscribes to GameState.GameWonChanged
    /// to halt cleanly instead of polling IsGameWon every frame.
    ///
    /// Restart hygiene: the authored Transform position/rotation is captured at Awake
    /// and re-applied on every OnEnable, so a Restart returns the enemy to its placed
    /// spawn instead of leaving it wherever it cornered the player.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyFollow : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private GameState _state;
        private bool _halted;
        private Vector3 _spawnPos;
        private Quaternion _spawnRot;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            // Authored position is the spawn point — capture before NavMeshAgent
            // can drift the transform.
            _spawnPos = transform.position;
            _spawnRot = transform.rotation;
        }

        private void OnEnable()
        {
            // Resume chasing on every (re)enable so a restarted/revisited level doesn't keep
            // an enemy frozen from a previous win (_halted persists across SetActive).
            _halted = false;

            // Restart: send the enemy back to its authored spawn. Warp keeps the agent on
            // the NavMesh; transform.SetPositionAndRotation handles the off-NavMesh edge
            // case (Warp would no-op there).
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                _agent.Warp(_spawnPos);
                if (_agent.isOnNavMesh)
                {
                    _agent.ResetPath();
                    _agent.velocity = Vector3.zero;
                    _agent.isStopped = false;
                }
            }
            else
            {
                transform.SetPositionAndRotation(_spawnPos, _spawnRot);
            }
            transform.rotation = _spawnRot;

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
            _halted = true;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
        }
    }
}
