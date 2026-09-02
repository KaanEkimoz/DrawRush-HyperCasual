using UnityEngine;
using UnityEngine.SceneManagement;
using DrawRush.Player;

namespace DrawRush.Core
{
    /// <summary>
    /// Wires up runtime services for each loaded scene.
    ///
    /// Attach one GameBootstrap to a bootstrap GameObject inside every gameplay scene
    /// (or to the persistent root). The bootstrap finds the player and main camera ONCE
    /// at Awake, stores them in GameServices, and resets per-scene state on the assets.
    /// Other systems then read GameServices.Player / GameServices.MainCamera instead of
    /// performing their own Find / Tag lookups every Awake.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig config;
        [SerializeField] private GameState state;
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Scene References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform trailPoint;
        [SerializeField] private Transform mainCamera;

        [Header("Tags (fallback if scene refs are empty)")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private string mainCameraTag = "MainCamera";

        private void Awake()
        {
            ResolveSceneReferences();

            if (state != null) state.Reset();
            if (playerHealth != null) playerHealth.ResetToStarting();

            GameServices.Register(player, trailPoint, mainCamera, config, state, playerHealth);
        }

        private void OnDestroy()
        {
            GameServices.Clear();
        }

        private void ResolveSceneReferences()
        {
            if (player == null && !string.IsNullOrEmpty(playerTag))
            {
                var found = GameObject.FindGameObjectWithTag(playerTag);
                if (found != null) player = found.transform;
            }

            if (trailPoint == null && player != null && player.childCount > 0)
            {
                trailPoint = player.GetChild(0);
            }

            if (mainCamera == null && !string.IsNullOrEmpty(mainCameraTag))
            {
                var found = GameObject.FindGameObjectWithTag(mainCameraTag);
                if (found != null) mainCamera = found.transform;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void DomainReload()
        {
            // Domain reload disabled in Player builds — services start clean on each session.
            GameServices.Clear();
        }
    }
}
