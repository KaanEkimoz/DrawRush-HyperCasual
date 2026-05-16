using UnityEngine;
using UnityEngine.Serialization;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Thin facade kept for backwards-compatibility with UI Button OnClick references
    /// in existing prefabs/scenes. Delegates each concern to a dedicated component:
    ///   - <see cref="LevelFlow"/>          for scene navigation
    ///   - <see cref="HudPanels"/>          for panel visibility + level label
    ///   - <see cref="WinSequenceDirector"/> for game-won presentation
    ///
    /// All three components are auto-resolved on Awake from the same GameObject
    /// (and added with sensible defaults if missing — zero-config migration path).
    /// Legacy serialized fields are forwarded into the sub-components at runtime
    /// via their public Bind() methods so existing scene/prefab refs keep working.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Sub-components (auto-resolved)")]
        [SerializeField] private LevelFlow levelFlow;
        [SerializeField] private HudPanels hudPanels;
        [SerializeField] private WinSequenceDirector winSequence;

        [Header("State Refs")]
        [SerializeField] private GameState state;
        [SerializeField] private PlayerHealth playerHealth;

        // Legacy serialized fields preserved so existing prefab/scene bindings keep
        // resolving — values are forwarded into the new sub-components on Awake.
        [FormerlySerializedAs("winPanel")] [SerializeField] private GameObject legacyWinPanel;
        [FormerlySerializedAs("losePanel")] [SerializeField] private GameObject legacyLosePanel;
        [FormerlySerializedAs("gameUI")] [SerializeField] private GameObject legacyGameUi;
        [FormerlySerializedAs("_levelText")] [SerializeField] private TMPro.TextMeshProUGUI legacyLevelText;
        [FormerlySerializedAs("_particles")] [SerializeField] private GameObject legacyParticles;

        private void Awake()
        {
            Time.timeScale = 0f;
            ResolveOrAttachSubComponents();
            ForwardLegacyFields();
        }

        private void OnEnable()
        {
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        }

        private void Start()
        {
            if (hudPanels != null && levelFlow != null) hudPanels.SetLevelLabel(levelFlow.CurrentLevel);
        }

        private void OnPlayerDied()
        {
            if (hudPanels != null) hudPanels.ShowLosePanel();
            Time.timeScale = 0f;
        }

        #region Legacy facade (UI button bindings)

        public void StartTheGame() => levelFlow?.StartTheGame();
        public void RestartLevel() => levelFlow?.RestartLevel();
        public void NextLevel() => levelFlow?.NextLevel();
        public void LoadRandomLevel() => levelFlow?.LoadRandomLevel();
        public void ResetTheGame() => hudPanels?.HideAllPanels();

        #endregion

        private void ResolveOrAttachSubComponents()
        {
            if (levelFlow == null && !TryGetComponent(out levelFlow))   levelFlow = gameObject.AddComponent<LevelFlow>();
            if (hudPanels == null && !TryGetComponent(out hudPanels))   hudPanels = gameObject.AddComponent<HudPanels>();
            if (winSequence == null && !TryGetComponent(out winSequence)) winSequence = gameObject.AddComponent<WinSequenceDirector>();
        }

        private void ForwardLegacyFields()
        {
            hudPanels?.Bind(
                winPanel: legacyWinPanel,
                losePanel: legacyLosePanel,
                gameUI: legacyGameUi,
                levelText: legacyLevelText);

            winSequence?.Bind(
                state: state,
                hudPanels: hudPanels,
                winParticles: legacyParticles);
        }
    }
}
