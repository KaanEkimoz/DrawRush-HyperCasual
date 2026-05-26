using UnityEngine;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Drives in-scene level switching for the mega-scene architecture. All levels
    /// live as sibling GameObject groups under <see cref="levelsRoot"/>; only the
    /// active group is enabled. Because there is no scene reload anymore, this is
    /// also where per-level state is reset (health, win flag, chain progress, player
    /// position). Shared objects (player, camera, UI, services) are never duplicated.
    /// </summary>
    public sealed class LevelManager : MonoBehaviour
    {
        [Tooltip("Parent transform whose children are the level groups (Level_00, Level_01 …).")]
        [SerializeField] private Transform levelsRoot;

        [Tooltip("Player health SO, reset to starting value on each level activation.")]
        [SerializeField] private PlayerHealth playerHealth;

        [Tooltip("Game state SO, win flag cleared on each level activation.")]
        [SerializeField] private GameState gameState;

        [Tooltip("Optional explicit player transform. Falls back to GameServices.Player.")]
        [SerializeField] private Transform player;

        [Tooltip("Child name searched inside each level group for the player spawn point.")]
        [SerializeField] private string spawnChildName = "Spawn";

        [Tooltip("Level group enabled at scene start (index into levelsRoot). Set in the " +
                 "Inspector to test a specific level in Play.")]
        [SerializeField] private int startLevelIndex = 0;

        private int _currentIndex = -1;

        public int LevelCount => levelsRoot != null ? levelsRoot.childCount : 0;
        public int CurrentIndex => _currentIndex;

        // Boot the mega-scene into exactly one active level group with the player at its
        // spawn. Runs after Awake so each group's DrawParts can auto-wire neighbors.
        private void Start()
        {
            ActivateLevel(startLevelIndex);
        }

        /// <summary>
        /// Enables level group <paramref name="index"/>, disables all others, and
        /// resets per-level state. Out-of-range indices are ignored.
        /// </summary>
        public void ActivateLevel(int index)
        {
            if (levelsRoot == null || index < 0 || index >= levelsRoot.childCount) return;

            for (int i = 0; i < levelsRoot.childCount; i++)
            {
                levelsRoot.GetChild(i).gameObject.SetActive(i == index);
            }
            _currentIndex = index;

            Transform activeLevel = levelsRoot.GetChild(index);

            // Reset shared state that a scene reload used to clear implicitly.
            if (gameState != null) gameState.Reset();
            if (playerHealth != null) playerHealth.ResetToStarting();

            MovePlayerToSpawn(activeLevel);
        }

        private void MovePlayerToSpawn(Transform activeLevel)
        {
            Transform playerTf = player != null ? player : GameServices.Player;
            if (playerTf == null) return;

            Transform spawn = ResolveSpawn(activeLevel);
            if (spawn != null)
            {
                // Disable the controller so the teleport isn't fought by collision resolution.
                var controller = playerTf.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                playerTf.SetPositionAndRotation(spawn.position, spawn.rotation);
                if (controller != null) controller.enabled = true;
            }

            // Wipe any in-progress chain/trail so the new level starts clean.
            playerTf.GetComponent<PlayerInteract>()?.ResetChain();
        }

        private Transform ResolveSpawn(Transform activeLevel)
        {
            Transform named = activeLevel.Find(spawnChildName);
            if (named != null) return named;

            // Fallback: a SPAWNPOINTS container's first child, else the level root.
            Transform spawnPoints = activeLevel.Find("SPAWNPOINTS");
            if (spawnPoints != null && spawnPoints.childCount > 0) return spawnPoints.GetChild(0);
            return spawnPoints != null ? spawnPoints : activeLevel;
        }
    }
}
