using DrawRush.Drawing;
using UnityEngine;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Raises the chase as the shape nears completion: each coverage threshold the player passes
    /// speeds every live enemy up a little (Qix's Sparx count, Paper.io's crowding). The pressure
    /// is per level: enemies reset it themselves when the level restarts.
    /// </summary>
    public sealed class ChasePressure : MonoBehaviour
    {
        [SerializeField] private FreeDrawPainter painter;
        [Tooltip("Coverage marks (0-1) that each add one step of pressure.")]
        [SerializeField] private float[] thresholds = { 0.5f, 0.8f };
        [Tooltip("Speed added per step, as a fraction (0.1 = +10%).")]
        [SerializeField] private float stepBoost = 0.1f;

        private int _steps;

        private void OnEnable()
        {
            if (painter == null) painter = FindFirstObjectByType<FreeDrawPainter>();
            if (painter != null) painter.CoverageChanged += OnCoverage;
        }

        private void OnDisable()
        {
            if (painter != null) painter.CoverageChanged -= OnCoverage;
        }

        private void OnCoverage(float coverage)
        {
            int steps = 0;
            foreach (float t in thresholds) if (coverage >= t) steps++;
            if (coverage < 0.01f) steps = 0;   // a fresh level
            if (steps == _steps) return;
            _steps = steps;
            float pressure = 1f + stepBoost * steps;
            foreach (EnemyFollow e in FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                e.Pressure = pressure;
        }
    }
}
