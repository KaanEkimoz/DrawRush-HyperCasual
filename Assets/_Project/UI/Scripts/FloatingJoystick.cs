using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace DrawRush.UI
{
    /// <summary>
    /// A floating on-screen stick: instead of a fixed control, the joystick spawns where the
    /// player first touches inside this RectTransform and follows the drag, then hides on
    /// release. It feeds the same virtual control as Unity's OnScreenStick
    /// (<c>&lt;Gamepad&gt;/leftStick</c> by default), so the existing input pipeline
    /// (PlayerControls.Move → ThirdPersonMovement) is unchanged.
    ///
    /// Feel: the reach is sized in dp so the thumb travels the same physical distance on every
    /// phone, it only spawns in the lower part of the screen (thumb zone), the ring follows a
    /// thumb that drifts past its edge, and a radial deadzone is rescaled so output starts at 0.
    ///
    /// Setup: put this on a full-area RectTransform with a transparent, raycast-target Image
    /// (the touch zone). Assign <see cref="background"/> (the ring) and <see cref="handle"/>
    /// (the knob); both are siblings in this rect's local space and are shown/hidden together.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FloatingJoystick : OnScreenControl,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("The joystick ring. Moved to the touch point and toggled on/off.")]
        [SerializeField] private RectTransform background;
        [Tooltip("The knob (sibling of background, same local space). Clamped within movementRange.")]
        [SerializeField] private RectTransform handle;
        [Tooltip("Knob travel the ring/knob art was authored for, in canvas units. The art is " +
                 "scaled by (dp-based reach / this) so it matches the real reach.")]
        [SerializeField] private float movementRange = 80f;
        [Tooltip("Max knob travel from center in dp (density-independent pixels). Full deflection = input 1.")]
        [SerializeField] private float reachDp = 46f;
        [Tooltip("Ring/knob art size relative to the reach. Below 1 keeps the art smaller than the " +
                 "thumb travel so the stick does not dominate the screen.")]
        [SerializeField, Range(0.4f, 1.5f)] private float artScale = 0.75f;
        [Tooltip("Fraction of the screen height, from the bottom, where a touch may spawn the stick.")]
        [SerializeField, Range(0.1f, 1f)] private float spawnZoneHeight = 0.45f;
        [Tooltip("Once the thumb is this many reaches from the center, the ring follows it.")]
        [SerializeField, Min(1f)] private float followThreshold = 1f;
        [Tooltip("Radial deadzone as a fraction of the reach; output is rescaled to start at 0 past it.")]
        [SerializeField, Range(0f, 0.5f)] private float deadzone = 0.1f;
        [Tooltip("Fixed mode (Settings): where the always-visible stick sits, as a fraction of the " +
                 "screen height from the bottom, horizontally centred.")]
        [SerializeField, Range(0.05f, 0.45f)] private float fixedHeight = 0.17f;

        private const string FixedKey = "JoystickFixed";

        /// <summary>Raised when the player switches between floating and fixed in Settings.</summary>
        public static event System.Action ModeChanged;

        /// <summary>Player setting: false = floating (spawns under the thumb, the default),
        /// true = fixed (always visible at the bottom centre, no follow).</summary>
        public static bool FixedMode
        {
            get => PlayerPrefs.GetInt(FixedKey, 0) == 1;
            set { PlayerPrefs.SetInt(FixedKey, value ? 1 : 0); PlayerPrefs.Save(); ModeChanged?.Invoke(); }
        }

        [InputControl(layout = "Vector2")]
        [SerializeField] private string controlPath = "<Gamepad>/leftStick";
        protected override string controlPathInternal
        {
            get => controlPath;
            set => controlPath = value;
        }

        // Typical phone density, used when Screen.dpi is unknown (reports 0 on some devices).
        private const float FallbackDpi = 420f;

        private RectTransform _rect;
        private Canvas _canvas;
        private int _pointerId = int.MinValue;
        private float _reach;
        private Vector3 _backgroundScale, _handleScale;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            if (background != null) _backgroundScale = background.localScale;
            if (handle != null) _handleScale = handle.localScale;
            ModeChanged += ApplyMode;
            ApplyMode();
        }

        private void OnDestroy() => ModeChanged -= ApplyMode;

        // Fixed mode keeps the stick parked and visible; floating hides it until a touch.
        private void ApplyMode()
        {
            if (background == null) return;
            if (!FixedMode) { HideStick(); return; }
            Sizing();
            // From the visible screen, not this rect: the touch rect is scaled in the scene and
            // is larger than the screen, so its own fractions do not land where they look.
            var screenHome = new Vector2(Screen.width * 0.5f, Screen.height * fixedHeight);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenHome, EventCamera(), out Vector2 home)) return;
            background.anchoredPosition = home;
            if (handle != null) handle.anchoredPosition = home;
            background.gameObject.SetActive(true);
            if (handle != null) handle.gameObject.SetActive(true);
        }

        private UnityEngine.Camera EventCamera()
        {
            Canvas root = _canvas != null ? _canvas.rootCanvas : null;
            return root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
        }

        private void Sizing()
        {
            _reach = ResolveReach();
            float art = (movementRange > 0f ? _reach / movementRange : 1f) * artScale;
            background.localScale = _backgroundScale * art;
            if (handle != null) handle.localScale = _handleScale * art;
        }

        // Reach in this rect's units for the device's density; recomputed per touch so a
        // rotation or resolution change never leaves a stale size.
        private float ResolveReach()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : FallbackDpi;
            float scale = _canvas != null ? _canvas.rootCanvas.scaleFactor : 1f;
            return Mathf.Max(1f, reachDp * dpi / 160f / Mathf.Max(0.0001f, scale));
        }

        /// <summary>The steering thumb tapped twice (lifted and came straight back): the dash.</summary>
        public static event System.Action DoubleTapped;

        [Header("Double tap (dash)")]
        [Tooltip("Seconds between lifting the thumb and touching again that count as a double tap.")]
        [SerializeField] private float doubleTapSeconds = 0.28f;
        [Tooltip("How far (dp) the second touch may land from where the thumb lifted.")]
        [SerializeField] private float doubleTapDp = 70f;

        private float _lastUpTime = -10f;
        private Vector2 _lastUpPosition;
        private Vector2 _lastDeflection;   // knob offset / reach while steering (-1..1)
        private Vector2 _heldDeflection;   // what it was when the thumb lifted

        /// <summary>Whether a touch at <paramref name="now"/>/<paramref name="at"/> is the second tap
        /// of a double tap after a lift at <paramref name="upTime"/>/<paramref name="upAt"/>.</summary>
        public static bool IsDoubleTap(float upTime, Vector2 upAt, float now, Vector2 at, float maxSeconds, float maxPixels) =>
            now - upTime <= maxSeconds && now >= upTime && (at - upAt).sqrMagnitude <= maxPixels * maxPixels;

        public void OnPointerDown(PointerEventData eventData)
        {
            bool dashTap = false;
            if (_pointerId == int.MinValue)
            {
                float dpi = Screen.dpi > 0f ? Screen.dpi : FallbackDpi;
                if (IsDoubleTap(_lastUpTime, _lastUpPosition, Time.unscaledTime, eventData.position, doubleTapSeconds, doubleTapDp * dpi / 160f))
                {
                    _lastUpTime = -10f;   // a third tap starts over
                    dashTap = true;
                    DoubleTapped?.Invoke();
                }
            }
            if (background == null || _pointerId != int.MinValue) return;   // one thumb owns the stick
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                if (eventData.position.y / Mathf.Max(1, Screen.height) > spawnZoneHeight) return;   // outside the thumb zone (screen fraction)
                _pointerId = eventData.pointerId;
                Sizing();
                if (FixedMode) { SendValueToControl(Vector2.zero); OnDrag(eventData); return; }   // parked stick: steer from its centre
                background.anchoredPosition = local;
                // After a dash the hero keeps running the way the pencil faces (the way it just
                // dashed): the ring opens behind the thumb, so the stick is already at full
                // deflection in that direction. The facing is used rather than the last stick
                // value because the first tap of the double tap has already reset that to zero.
                if (dashTap && TryDashDirection(out Vector2 runDir))
                    background.anchoredPosition = local - runDir * _reach;
                if (handle != null) handle.anchoredPosition = local;
                background.gameObject.SetActive(true);
                if (handle != null) handle.gameObject.SetActive(true);
            }
            SendValueToControl(Vector2.zero);
            OnDrag(eventData);
        }

        private Transform _hero;

        /// <summary>Stick direction (unit) that would steer the way the hero faces, else the stick's
        /// last held direction. False when neither is known.</summary>
        private bool TryDashDirection(out Vector2 dir)
        {
            dir = Vector2.zero;
            if (_hero == null)
            {
                var dash = FindFirstObjectByType<DrawRush.Player.PlayerDash>();
                if (dash != null) _hero = dash.transform;
            }
            if (_hero != null)
            {
                // Inverse of ThirdPersonMovement: heading yaw = atan2(stick.x, stick.y) + camera yaw.
                var cam = DrawRush.Core.GameServices.MainCamera;
                float yaw = (_hero.eulerAngles.y - (cam != null ? cam.eulerAngles.y : 0f)) * Mathf.Deg2Rad;
                dir = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
                return true;
            }
            if (_heldDeflection.sqrMagnitude > 0.04f) { dir = _heldDeflection.normalized; return true; }
            return false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || eventData.pointerId != _pointerId) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            Vector2 delta = local - background.anchoredPosition;
            // Thumb drifted past the ring: drag the ring along so the player never has to
            // come back to the spawn point to keep steering.
            float followAt = _reach * followThreshold;
            if (!FixedMode && delta.magnitude > followAt)
            {
                background.anchoredPosition += delta.normalized * (delta.magnitude - followAt);
                delta = local - background.anchoredPosition;
            }

            Vector2 clamped = Vector2.ClampMagnitude(delta, _reach);
            _lastDeflection = clamped / Mathf.Max(0.0001f, _reach);
            // handle is a sibling (same local space), so add the ring's position.
            if (handle != null) handle.anchoredPosition = background.anchoredPosition + clamped;
            SendValueToControl(Deflect(clamped / _reach, deadzone));
        }

        /// <summary>Radial deadzone with rescale: 0 inside, then 0..1 from its edge to full reach.</summary>
        public static Vector2 Deflect(Vector2 raw, float deadzone)
        {
            float m = raw.magnitude;
            if (m <= deadzone || m <= 0f) return Vector2.zero;
            return raw / m * Mathf.Clamp01((m - deadzone) / (1f - deadzone));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            _pointerId = int.MinValue;
            _lastUpTime = Time.unscaledTime;
            _lastUpPosition = eventData.position;
            _heldDeflection = _lastDeflection;
            _lastDeflection = Vector2.zero;
            SendValueToControl(Vector2.zero);
            ApplyMode();   // floating: hide; fixed: knob back to the parked centre
        }

        // The dash's cooldown, drawn as a ring around the stick: it fills while recharging and
        // lights up when ready. Only once the dash is owned.
        private UnityEngine.UI.Image _dashRing;

        private void LateUpdate()
        {
            bool show = DrawRush.Core.DashSkill.Owned && background != null && background.gameObject.activeInHierarchy;
            if (_dashRing == null)
            {
                if (!show) return;
                var bgImage = background.GetComponent<UnityEngine.UI.Image>();
                var go = new GameObject("DashRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(background, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = background.rect.size * 1.18f;
                _dashRing = go.GetComponent<UnityEngine.UI.Image>();
                if (bgImage != null) _dashRing.sprite = bgImage.sprite;
                _dashRing.type = UnityEngine.UI.Image.Type.Filled;
                _dashRing.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
                _dashRing.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
                _dashRing.fillClockwise = true;
                _dashRing.raycastTarget = false;
                rt.SetAsFirstSibling();   // behind the ring art
            }
            _dashRing.enabled = show;
            if (!show) return;
            float r = DrawRush.Player.PlayerDash.Readiness;
            _dashRing.fillAmount = r;
            _dashRing.color = r >= 1f ? new Color(0.35f, 0.55f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.45f);
        }

        // A disabled stick (pause, level end) must not keep a held direction or a stale owner.
        protected override void OnDisable()
        {
            _pointerId = int.MinValue;
            HideStick();
            base.OnDisable();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_rect != null) ApplyMode();
        }

        // Both the ring and the knob are hidden when not in use, so nothing lingers on screen.
        private void HideStick()
        {
            if (background != null) background.gameObject.SetActive(false);
            if (handle != null) handle.gameObject.SetActive(false);
        }
    }
}
