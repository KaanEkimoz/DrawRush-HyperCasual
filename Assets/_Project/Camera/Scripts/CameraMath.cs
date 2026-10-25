using UnityEngine;

namespace DrawRush.Cameras
{
    /// <summary>Pure framing math for the fixed-pitch gameplay camera (kept separate so it is testable).</summary>
    public static class CameraMath
    {
        /// <summary>Camera-to-player distance that shows <paramref name="width"/> m across the
        /// screen at the player's depth, for a vertical FOV and aspect (width / height).</summary>
        public static float DistanceForWidth(float width, float verticalFov, float aspect)
        {
            float halfW = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) * Mathf.Max(0.01f, aspect);
            return width / (2f * halfW);
        }

        /// <summary>World offset from the player to a camera tilted down <paramref name="pitch"/>
        /// degrees (yaw 0) at <paramref name="distance"/>, placing the player at viewport height
        /// <paramref name="screenY"/> (0 bottom, 1 top).</summary>
        public static Vector3 FollowOffset(float pitch, float verticalFov, float distance, float screenY)
        {
            // Angle of the camera→player ray above the view axis for that viewport height.
            float up = Mathf.Atan((2f * screenY - 1f) * Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg;
            float rayPitch = (pitch - up) * Mathf.Deg2Rad;
            return new Vector3(0f, Mathf.Sin(rayPitch), -Mathf.Cos(rayPitch)) * distance;
        }

        /// <summary>Zoom multiple needed so a threat <paramref name="distance"/> m from the player,
        /// plus <paramref name="margin"/>, fits inside the half-width of the base view.</summary>
        public static float ZoomForThreat(float distance, float margin, float baseWidth, float maxZoom)
            => Mathf.Clamp((distance + margin) * 2f / Mathf.Max(0.01f, baseWidth), 1f, maxZoom);
    }
}
