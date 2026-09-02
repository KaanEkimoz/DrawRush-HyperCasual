using UnityEngine;
using DrawRush.Drawing;

namespace DrawRush.Core
{
    /// <summary>
    /// Feel for the core action. Completing an edge used to just raise a wall in silence (the sound
    /// aside); now each wall that rises throws a short burst of its own colour where it appears, so
    /// finishing a stretch of the shape lands as a small reward. Reads
    /// <see cref="ProceduralWall.Revealed"/>, so it stays decoupled from the drawing code.
    ///
    /// One world-space particle system, repositioned and recoloured per burst — cheap, and bursts
    /// left behind by rapid completions still play out where they were thrown.
    /// </summary>
    [DefaultExecutionOrder(40)]
    public sealed class DrawJuice : MonoBehaviour
    {
        [SerializeField] private int burstCount = 14;
        [SerializeField] private float burstSpeed = 4.5f;
        [SerializeField] private float particleLife = 0.6f;
        [SerializeField] private float particleSize = 0.22f;

        private ParticleSystem _ps;
        private ParticleSystemRenderer _psr;

        private void Awake() => BuildSystem();

        private void OnEnable() => ProceduralWall.Revealed += OnWallRevealed;
        private void OnDisable() => ProceduralWall.Revealed -= OnWallRevealed;

        private void OnWallRevealed(ProceduralWall wall)
        {
            if (wall == null || _ps == null) return;
            Vector3 at = wall.TryGetWorldBounds(out Bounds b) ? new Vector3(b.center.x, b.max.y + 0.1f, b.center.z)
                                                              : wall.transform.position;
            _ps.transform.position = at;
            var main = _ps.main;
            Color c = wall.Color; c.a = 1f;
            main.startColor = c;
            _ps.Emit(burstCount);
        }

        private void BuildSystem()
        {
            var go = new GameObject("DrawBurst");
            go.transform.SetParent(transform, false);
            _ps = go.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = particleLife;
            main.startSpeed = burstSpeed;
            main.startSize = particleSize;
            main.gravityModifier = 1.6f;                     // little confetti chips that fall
            main.simulationSpace = ParticleSystemSimulationSpace.World;  // stay where thrown
            main.maxParticles = 200;

            var emission = _ps.emission; emission.enabled = true; emission.rateOverTime = 0f;
            var shape = _ps.shape; shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.25f;

            var sol = _ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            _psr = go.GetComponent<ParticleSystemRenderer>();
            // URP unlit particle material so the chips read as flat colour, not lit geometry.
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s != null) _psr.material = new Material(s);
            _psr.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private void OnDestroy()
        {
            if (_psr != null && _psr.material != null) Destroy(_psr.material);
        }
    }
}
