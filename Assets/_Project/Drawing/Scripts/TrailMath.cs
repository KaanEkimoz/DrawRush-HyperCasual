using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>Pure helpers extracted from DrawPart for unit testability.</summary>
    public static class TrailMath
    {
        /// <summary>Per-component Lerp scaled by deltaTime. Equivalent to the legacy
        /// frame-rate-dependent catch-up the trail uses while returning to the player.</summary>
        public static Vector3 Lerp(Vector3 from, Vector3 to, float lerpRate, float deltaTime)
        {
            float t = lerpRate * deltaTime;
            return new Vector3(
                Mathf.Lerp(from.x, to.x, t),
                Mathf.Lerp(from.y, to.y, t),
                Mathf.Lerp(from.z, to.z, t));
        }
    }
}
