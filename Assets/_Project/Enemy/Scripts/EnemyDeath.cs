using System;
using UnityEngine;
using UnityEngine.AI;
using DrawRush.Core;
using DrawRush.Player;

namespace DrawRush.Enemy
{
    /// <summary>
    /// When the level ends — the shape is completed (win) OR the player runs out of health (lose) —
    /// the enemy dies with feedback instead of just freezing: a colour poof where it stood, a pop
    /// sound (raised as the <see cref="EnemyDied"/> event so audio stays decoupled), the death
    /// animation, and a quick shrink-out so it leaves the field promptly rather than lingering.
    ///
    /// Self-resets on every OnEnable (scale/renderers/colliders restored, dead-flag cleared) so the
    /// same authored enemy is reusable across Restart / Next Level without respawning.
    /// </summary>
    public sealed class EnemyDeath : MonoBehaviour
    {
        /// <summary>Raised once whenever any enemy dies. SfxPlayer listens and plays the pop, so no
        /// enemy code depends on the audio system.</summary>
        public static event Action EnemyDied;

        [Tooltip("Seconds to shrink out after death before the enemy is hidden.")]
        [SerializeField] private float shrinkDuration = 0.28f;
        [Tooltip("Particles in the death poof.")]
        [SerializeField] private int poofCount = 18;

        private Animator _anim;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private NavMeshAgent _agent;
        private EnemyFollow _follow;
        private GameState _state;
        private PlayerHealth _health;
        private ParticleSystem _poof;
        private ParticleSystemRenderer _poofRenderer;

        private Vector3 _baseScale;
        private bool _dead;
        private float _shrinkT;
        private Color _color = new Color(0.94f, 0.14f, 0f, 1f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            _anim = GetComponentInChildren<Animator>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
            _agent = GetComponent<NavMeshAgent>();
            _follow = GetComponent<EnemyFollow>();
            _baseScale = transform.localScale;

            foreach (var r in _renderers)
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId))
                { _color = r.sharedMaterial.GetColor(BaseColorId); break; }

            BuildPoof();
        }

        private void OnEnable()
        {
            // Restart hygiene: come back alive, full size, visible and solid. Re-enable the chaser —
            // Die() switched it off, and a SetActive cycle does NOT restore a disabled component.
            _dead = false;
            _shrinkT = 0f;
            transform.localScale = _baseScale;
            SetVisible(true);
            if (_follow != null) _follow.enabled = true;

            _state = GameServices.State;
            if (_state != null) _state.GameWonChanged += OnWonChanged;
            _health = GameServices.Health;
            if (_health != null) _health.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (_state != null) _state.GameWonChanged -= OnWonChanged;
            if (_health != null) _health.Died -= OnPlayerDied;
        }

        private void OnWonChanged(bool won) { if (won) Die(); }
        private void OnPlayerDied() => Die();

        private void Die()
        {
            if (_dead) return;
            _dead = true;

            // Stop chasing at once so the enemy doesn't slide during its death.
            if (_follow != null) _follow.enabled = false;
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }

            if (_anim != null) _anim.SetTrigger(AnimatorIds.EnemyDie);

            if (_poof != null)
            {
                var main = _poof.main;
                Color c = _color; c.a = 1f;
                main.startColor = c;
                _poof.transform.position = transform.position + Vector3.up * 0.6f;
                _poof.Emit(poofCount);
            }

            EnemyDied?.Invoke();   // -> pop sound (SfxPlayer)
            _shrinkT = shrinkDuration;
        }

        // Unscaled time so the shrink + poof still play if the end-of-level flow drops timeScale.
        private void Update()
        {
            if (!_dead || _shrinkT <= 0f) return;
            _shrinkT -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_shrinkT / shrinkDuration);
            transform.localScale = _baseScale * Mathf.SmoothStep(0f, 1f, t);
            if (_shrinkT <= 0f)
            {
                SetVisible(false);
                transform.localScale = _baseScale;   // hidden now; ready for next reuse
            }
        }

        private void SetVisible(bool on)
        {
            if (_renderers != null) foreach (var r in _renderers) if (r != null) r.enabled = on;
            if (_colliders != null) foreach (var c in _colliders) if (c != null) c.enabled = on;
        }

        // A small world-space burst in the enemy's colour, same lightweight pattern as DrawJuice.
        private void BuildPoof()
        {
            var go = new GameObject("EnemyPoof");
            go.transform.SetParent(transform, false);
            _poof = go.AddComponent<ParticleSystem>();
            _poof.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _poof.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 4.5f;
            main.startSize = 0.28f;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = _poof.emission; emission.enabled = true; emission.rateOverTime = 0f;
            var shape = _poof.shape; shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var sol = _poof.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            _poofRenderer = go.GetComponent<ParticleSystemRenderer>();
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s != null) _poofRenderer.material = new Material(s);
            _poofRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        private void OnDestroy()
        {
            if (_poofRenderer != null && _poofRenderer.material != null) Destroy(_poofRenderer.material);
        }
    }
}
