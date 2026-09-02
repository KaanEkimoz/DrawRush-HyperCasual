using System;
using UnityEngine;
using DrawRush.Player;

namespace DrawRush.Core
{
    /// <summary>
    /// Lightweight runtime service locator. Avoids classic singleton MonoBehaviour pattern.
    /// Services are registered by <see cref="GameBootstrap"/> on scene load and cleared on
    /// scene unload — no global state survives between sessions.
    ///
    /// Player-related transforms (player root, trail attach point, main camera) are exposed
    /// directly so combat, drawing and camera-relative movement do not need GameObject.Find /
    /// CompareTag at runtime.
    /// </summary>
    public static class GameServices
    {
        public static Transform Player { get; private set; }
        public static Transform TrailPoint { get; private set; }
        public static Transform MainCamera { get; private set; }
        public static GameConfig Config { get; private set; }
        public static GameState State { get; private set; }
        public static PlayerHealth Health { get; private set; }

        public static event Action ServicesReady;
        public static event Action ServicesCleared;

        public static bool IsReady => Player != null && Config != null && State != null;

        public static void Register(
            Transform player,
            Transform trailPoint,
            Transform mainCamera,
            GameConfig config,
            GameState state,
            PlayerHealth health = null)
        {
            Player = player;
            TrailPoint = trailPoint;
            MainCamera = mainCamera;
            Config = config;
            State = state;
            Health = health;
            ServicesReady?.Invoke();
        }

        public static void Clear()
        {
            Player = null;
            TrailPoint = null;
            MainCamera = null;
            Config = null;
            State = null;
            Health = null;
            ServicesCleared?.Invoke();
        }
    }
}
