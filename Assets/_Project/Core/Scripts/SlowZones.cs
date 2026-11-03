using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Ground that slows the hero while it stands in it (glue puddles). Each zone that touches the
    /// hero this frame calls <see cref="Touch"/>; movement reads <see cref="Factor"/>. A short hold
    /// time means leaving the zone frees the hero almost at once.
    /// </summary>
    public static class SlowZones
    {
        private const float Hold = 0.1f;
        private static float _until;
        private static float _factor = 1f;

        public static void Touch(float factor)
        {
            _factor = Time.time < _until ? Mathf.Min(_factor, factor) : factor;
            _until = Time.time + Hold;
        }

        public static float Factor => Time.time < _until ? _factor : 1f;

        public static void Clear() => _until = 0f;
    }
}
