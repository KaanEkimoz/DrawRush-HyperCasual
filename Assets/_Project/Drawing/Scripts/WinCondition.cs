using UnityEngine;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Flips <see cref="GameState.IsGameWon"/> when the level's <see cref="EdgeNetwork"/>
    /// reports every paintable edge filled. Lives on the same GameObject as the EdgeNetwork
    /// for each level group in the mega-scene; the network is rebuilt on enable, so this only
    /// needs to (re)subscribe.
    /// </summary>
    public sealed class WinCondition : MonoBehaviour
    {
        [SerializeField] private EdgeNetwork edgeNetwork;

        [Tooltip("Coins awarded to the player when this level is completed.")]
        [SerializeField] private int coinReward = 10;

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
            PlayerProgress.AddCoins(coinReward);
            if (GameServices.State != null)
            {
                GameServices.State.IsGameWon = true;
            }
        }
    }
}
