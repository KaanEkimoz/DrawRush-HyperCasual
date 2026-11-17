using System;
using DrawRush.Boosters;
using DrawRush.Core;
using UnityEngine;

namespace DrawRush.Player
{
    /// <summary>
    /// The dash skill (bought in the shop): double-tap with the steering thumb and the hero darts
    /// a few metres the way it faces, unhurt for a moment. Made for one-handed portrait play: the
    /// same thumb that steers triggers it, no button to reach for. It does not paint: it is for
    /// getting away (from an enemy, a trap, a saw), not for drawing faster.
    /// </summary>
    [RequireComponent(typeof(PlayerKnockback))]
    public sealed class PlayerDash : MonoBehaviour
    {
        // How far it goes grows with the shop level (DashSkill.Distance).
        [SerializeField] private float seconds = 0.22f;
        [Tooltip("Seconds the hero cannot be hurt, from the start of the dash.")]
        [SerializeField] private float dodgeSeconds = 0.35f;
        [Tooltip("Level starts that still show the double-tap hint after the dash was bought.")]
        [SerializeField] private int hintTimes = 3;

        /// <summary>A dash started (for the sound and effects).</summary>
        public static event Action Dashed;
        /// <summary>True while the hero is mid-dash: the painter lays no paint.</summary>
        public static bool IsDashing { get; private set; }
        /// <summary>0 just after a dash .. 1 ready again (0 when the skill is not owned).</summary>
        public static float Readiness { get; private set; }

        private const string HintKey = "DashHintShown";
        private PlayerKnockback _knockback;
        private PlayerCombat _combat;
        private TrailRenderer _trail;
        private float _readyAt, _dashEnds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Dashed = null; IsDashing = false; Readiness = 0f; }

        private void Awake()
        {
            _knockback = GetComponent<PlayerKnockback>();
            _combat = GetComponent<PlayerCombat>();
            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.18f;
            _trail.minVertexDistance = 0.05f;
            _trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.55f), new Keyframe(1f, 0f));
            _trail.material = new Material(Shader.Find("Sprites/Default"));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },   // white streak (Kaan)
                      new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            _trail.colorGradient = g;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.emitting = false;
        }

        private void OnEnable()
        {
            DrawRush.UI.FloatingJoystick.DoubleTapped += TryDash;
            var painter = FindFirstObjectByType<DrawRush.Drawing.FreeDrawPainter>();
            if (painter != null) painter.LevelBound += OnLevelBound;
        }

        private void OnDisable()
        {
            DrawRush.UI.FloatingJoystick.DoubleTapped -= TryDash;
            var painter = FindFirstObjectByType<DrawRush.Drawing.FreeDrawPainter>();
            if (painter != null) painter.LevelBound -= OnLevelBound;
            IsDashing = false;
        }

        // Every level starts with the dash ready; the first few after buying it say how to use it.
        private void OnLevelBound(DrawRush.Drawing.EdgeNetwork _)
        {
            _readyAt = 0f;
            if (!DashSkill.Owned) return;
            if (DashTutorial.PracticePending && GetComponent<DashPractice>() == null)
            {
                DashPractice.Begin(gameObject);   // the guided first dash replaces the first hint
                PlayerPrefs.SetInt(HintKey, Mathf.Max(1, PlayerPrefs.GetInt(HintKey, 0)));
                return;
            }
            if (PlayerPrefs.GetInt(HintKey, 0) >= hintTimes) return;
            PlayerPrefs.SetInt(HintKey, PlayerPrefs.GetInt(HintKey, 0) + 1);
            var hud = FindFirstObjectByType<BoosterHud>();
            if (hud != null) hud.Toast("DASH READY", Application.isMobilePlatform ? "Double-tap to dash" : "Double-tap or Space to dash", new Color(0.35f, 0.55f, 1f));
        }

        private void Update()
        {
            // PC: Space dashes too (phones have no keyboard, so this is a no-op there).
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) TryDash();
            Readiness = !DashSkill.Owned ? 0f
                : Mathf.Clamp01(1f - (_readyAt - Time.time) / Mathf.Max(0.01f, DashSkill.Cooldown));
            if (IsDashing && Time.time >= _dashEnds)
            {
                IsDashing = false;
                _trail.emitting = false;
            }
        }

        private void TryDash()
        {
            if (!DashSkill.Owned || Time.timeScale <= 0f || Time.time < _readyAt || IsDashing) return;
            var state = GameServices.State;
            if (state != null && (state.IsGameWon || state.IsFinishing)) return;
            if (_knockback.IsActive) return;   // already being pushed

            Vector3 dir = transform.forward; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            _knockback.Slide(dir, DashSkill.Distance, seconds);
            if (_combat != null) _combat.Dodge(dodgeSeconds);
            IsDashing = true;
            _dashEnds = Time.time + seconds + 0.05f;   // the slide eases out a touch past its time
            _readyAt = Time.time + DashSkill.Cooldown;
            _trail.Clear();
            _trail.emitting = true;
            Haptics.Light();
            Dashed?.Invoke();
        }
    }
}
