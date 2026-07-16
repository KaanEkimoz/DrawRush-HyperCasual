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

        [Tooltip("3-2-1 countdown shown before play starts on every level activation. " +
                 "Auto-resolved if left empty.")]
        [SerializeField] private LevelStartCountdown countdown;

        [Header("Tutorial")]
        [Tooltip("Index of the tutorial level group.")]
        [SerializeField] private int tutorialLevelIndex = 0;

        [Tooltip("Level to start on once the tutorial has been completed (and for returning players).")]
        [SerializeField] private int firstLevelIndex = 1;

        [Tooltip("Dev override for the level enabled at scene start. -1 = auto (tutorial for a " +
                 "new player, otherwise the first level). >= 0 forces that index for testing.")]
        [SerializeField] private int startLevelIndex = -1;

        [Tooltip("EDITOR ONLY: when off, pressing Play keeps whichever level group is already " +
                 "active in the scene instead of running the tutorial/first-level flow — handy " +
                 "for testing a single level. Ignored in builds (always uses the normal flow).")]
        [SerializeField] private bool autoActivateOnStart = true;

        private int _currentIndex = -1;
        private GameState _boundState;
        private HudPanels _hud;

        public int LevelCount => levelsRoot != null ? levelsRoot.childCount : 0;
        public int CurrentIndex => _currentIndex;

        private void OnEnable()
        {
            _boundState = gameState != null ? gameState : GameServices.State;
            if (_boundState != null) _boundState.GameWonChanged += OnGameWonChanged;
        }

        private void OnDisable()
        {
            if (_boundState != null) _boundState.GameWonChanged -= OnGameWonChanged;
        }

        // Boot the mega-scene into exactly one active level group with the player at its
        // spawn. Runs after Awake so each group's DrawParts can auto-wire neighbors.
        private void Start()
        {
#if UNITY_EDITOR
            // Editor convenience: keep the level the developer left enabled in the scene so a
            // single level can be tested in isolation. Never compiled into builds.
            if (!autoActivateOnStart)
            {
                int active = FindActiveLevelIndex();
                ActivateLevel(active >= 0 ? active : firstLevelIndex);
                return;
            }
#endif
            ActivateLevel(startLevelIndex >= 0 ? startLevelIndex : ResolveResumeIndex());
        }

        // Where a launching player belongs: the level they last reached, else the tutorial for
        // a newcomer, else the first level. The saved index is bounds-checked so a value left
        // by an older build (or pointing at a level that no longer exists) can never boot the
        // game into nothing.
        private int ResolveResumeIndex()
        {
            int saved = PlayerProgress.LastLevelIndex;
            if (saved >= 0 && saved < LevelCount) return saved;
            return PlayerProgress.TutorialCompleted ? firstLevelIndex : tutorialLevelIndex;
        }

#if UNITY_EDITOR
        // First level group currently enabled under levelsRoot, or -1 if none.
        private int FindActiveLevelIndex()
        {
            if (levelsRoot == null) return -1;
            for (int i = 0; i < levelsRoot.childCount; i++)
                if (levelsRoot.GetChild(i).gameObject.activeSelf) return i;
            return -1;
        }
#endif

        // Completing the tutorial level marks it done so it is skipped from now on.
        private void OnGameWonChanged(bool won)
        {
            if (won && _currentIndex == tutorialLevelIndex) PlayerProgress.TutorialCompleted = true;
        }

        /// <summary>
        /// Enables level group <paramref name="index"/>, disables all others, and
        /// resets per-level state. Out-of-range indices are ignored.
        /// </summary>
        public void ActivateLevel(int index)
        {
            if (levelsRoot == null || index < 0 || index >= levelsRoot.childCount) return;

            // Two passes: disable every group, then enable the target. This forces the
            // target through a fresh OnDisable/OnEnable cycle even when it is already the
            // active level (Restart), so per-level systems re-initialize — EdgeNetwork
            // rebuilds unpainted edges, DrawEdgeAuthor re-hides its wall, WinCondition
            // re-subscribes — instead of keeping their finished state.
            for (int i = 0; i < levelsRoot.childCount; i++)
            {
                levelsRoot.GetChild(i).gameObject.SetActive(false);
            }
            levelsRoot.GetChild(index).gameObject.SetActive(true);
            _currentIndex = index;

            // Remember where the player is so the next launch resumes here. The tutorial is
            // tracked by its own completed-flag, so don't pin a returning player to it.
            if (index != tutorialLevelIndex) PlayerProgress.LastLevelIndex = index;

            Transform activeLevel = levelsRoot.GetChild(index);

            // Reset shared state that a scene reload used to clear implicitly. Resolve through
            // the locator the same way OnEnable does: if the inspector ref is unset — or points
            // at a different asset than the one WinCondition writes to — the win flag would
            // survive into the next level and freeze the player there permanently.
            GameState state = gameState != null ? gameState : GameServices.State;
            if (state != null) state.Reset();
            if (playerHealth != null) playerHealth.ResetToStarting();

            UpdateLevelLabel(index);
            MovePlayerToSpawn(activeLevel);

            // Freeze + 3-2-1 before the player can move (every activation: start/next/restart).
            if (countdown == null) countdown = FindFirstObjectByType<LevelStartCountdown>();
            if (countdown != null) countdown.Begin();
            // GameManager.Awake parks timeScale at 0 and the countdown is the ONLY thing that
            // restores it — and FindFirstObjectByType skips inactive objects. Without this the
            // game boots permanently frozen, silently, whenever the countdown is missing.
            else Time.timeScale = 1f;
        }

        // Keep the HUD level label in sync with the actually-active level (the old label read
        // a stale PlayerPrefs value and never updated on level change).
        private void UpdateLevelLabel(int index)
        {
            if (_hud == null) _hud = FindFirstObjectByType<HudPanels>();
            if (_hud != null) _hud.SetLevelText(index == tutorialLevelIndex ? "Tutorial" : "Level " + index);
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

            // The paint controller lives on the persistent Player, so its disable/enable
            // never fires on a level switch. Detach explicitly so stale _currentPart /
            // _edge refs from the previous level don't re-engage the rail at the new spawn.
            playerTf.GetComponent<Studios208.DrawRush.Player.RailPaintController>()?.Detach();
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
