using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Forces that drag the hero across the page (the sharpener sucking it in). Each source adds
    /// its pull for this frame with <see cref="Add"/>; movement adds <see cref="Velocity"/> to the
    /// hero's own motion. A short hold means a stopped source lets go at once.
    /// </summary>
    public static class PullZones
    {
        private const float Hold = 0.06f;
        private static Vector3 _velocity;
        private static float _until;
        private static int _frame = -1;

        public static void Add(Vector3 velocity)
        {
            if (Time.frameCount != _frame) { _frame = Time.frameCount; _velocity = Vector3.zero; }
            velocity.y = 0f;
            _velocity += velocity;
            _until = Time.time + Hold;
        }

        public static Vector3 Velocity => Time.time < _until ? _velocity : Vector3.zero;

        public static void Clear() { _until = 0f; _velocity = Vector3.zero; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();
    }
}
