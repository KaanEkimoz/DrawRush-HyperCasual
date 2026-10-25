using System.Collections.Generic;
using UnityEngine;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Keeps a short list of live enemies (enabled <see cref="EnemyFollow"/>s) for the camera
    /// pull-back and the off-screen arrows. Rescans on a slow timer instead of every frame;
    /// dead enemies drop out because EnemyDeath disables their EnemyFollow.
    /// </summary>
    public sealed class EnemyProximity : MonoBehaviour
    {
        [Tooltip("Seconds between rescans of the scene for active enemies.")]
        [SerializeField, Min(0.1f)] private float rescanInterval = 0.5f;

        private readonly List<EnemyFollow> _all = new();
        private float _nextScan;

        /// <summary>Live enemies as of the last scan (enabled check is done per query).</summary>
        public IReadOnlyList<EnemyFollow> All { get { Rescan(); return _all; } }

        private void Rescan()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + rescanInterval;
            _all.Clear();
            _all.AddRange(FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        }

        /// <summary>Fills <paramref name="result"/> with up to <paramref name="max"/> live enemies
        /// within <paramref name="radius"/> m of <paramref name="from"/> (ground distance), nearest
        /// first. Returns how many were written.</summary>
        public int Nearest(Vector3 from, float radius, Transform[] result, int max)
        {
            Rescan();
            int count = 0;
            float r2 = radius * radius;
            foreach (EnemyFollow e in _all)
            {
                if (e == null || !e.isActiveAndEnabled) continue;
                Vector3 d = e.transform.position - from; d.y = 0f;
                float s = d.sqrMagnitude;
                if (s > r2) continue;
                // Insertion into a tiny sorted array; max is a handful.
                int i = Mathf.Min(count, max - 1);
                if (count == max && s >= Sqr(result[max - 1], from)) continue;
                while (i > 0 && Sqr(result[i - 1], from) > s) { result[i] = result[i - 1]; i--; }
                result[i] = e.transform;
                if (count < max) count++;
            }
            return count;
        }

        private static float Sqr(Transform t, Vector3 from) { Vector3 d = t.position - from; d.y = 0f; return d.sqrMagnitude; }
    }
}
