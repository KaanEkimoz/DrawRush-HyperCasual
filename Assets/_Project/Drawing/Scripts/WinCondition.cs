using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Drawing
{
    /// <summary>
    /// Flips <see cref="GameState.IsGameWon"/> when the level's <see cref="EdgeNetwork"/>
    /// reports every paintable edge filled. Lives on the same GameObject as the EdgeNetwork
    /// for each level group in the mega-scene; the network is rebuilt on enable, so this only
    /// needs to (re)subscribe.
    ///
    /// On a win it also scores the clear: how much health survived becomes a 1–3 star rating
    /// (<see cref="LevelScore"/>), which sets the coin reward and the player's best-for-this-level
    /// record. The rating is stashed on <see cref="GameState"/> before the win flag flips, so the
    /// win panel can read it.
    /// </summary>
    public sealed class WinCondition : MonoBehaviour
    {
        [SerializeField] private EdgeNetwork edgeNetwork;

        private void OnEnable()
        {
            if (edgeNetwork == null) edgeNetwork = GetComponent<EdgeNetwork>();
            if (edgeNetwork != null) edgeNetwork.AllCompleted += OnAllCompleted;
        }

        private void OnDisable()
        {
            if (edgeNetwork != null) edgeNetwork.AllCompleted -= OnAllCompleted;
        }

        private void OnAllCompleted()
        {
            // Health left = how cleanly it was cleared. No health service (e.g. an isolated test
            // scene) → treat it as a plain one-star clear rather than crashing.
            var health = GameServices.Health;
            int stars = health != null ? LevelScore.Evaluate(health.Current, health.StartingValue) : 1;

            int levelIndex = ResolveLevelIndex();
            bool record = levelIndex >= 0 && PlayerProgress.RecordStars(levelIndex, stars);

            var state = GameServices.State;
            if (state != null)
            {
                // Set BEFORE the win flag: flipping IsGameWon fires GameWonChanged synchronously and
                // starts the win sequence, and the panel reads these when it shows.
                state.LastStars = stars;
                state.LastStarsWereRecord = record;
            }

            PlayerProgress.AddCoins(LevelScore.CoinsForStars(stars));

            if (state != null) state.IsGameWon = true;
        }

        // The active WinCondition belongs to the current level group, so the level index is just
        // the one the LevelManager has enabled. Resolved lazily; the tutorial (index 0) is a valid
        // key too, though the sequencer never re-serves it.
        private int ResolveLevelIndex()
        {
            var lm = FindFirstObjectByType<LevelManager>();
            return lm != null ? lm.CurrentIndex : -1;
        }
    }
}
