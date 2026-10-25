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
        [Tooltip("Fraction of this rect, from the bottom, where a touch may spawn the stick.")]
        [SerializeField, Range(0.1f, 1f)] private float spawnZoneHeight = 0.45f;
        [Tooltip("Once the thumb is this many reaches from the center, the ring follows it.")]
        [SerializeField, Min(1f)] private float followThreshold = 1f;
        [Tooltip("Radial deadzone as a fraction of the reach; output is rescaled to start at 0 past it.")]
        [SerializeField, Range(0f, 0.5f)] private float deadzone = 0.1f;

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
            HideStick();
        }

        // Reach in this rect's units for the device's density; recomputed per touch so a
        // rotation or resolution change never leaves a stale size.
        private float ResolveReach()
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : FallbackDpi;
            float scale = _canvas != null ? _canvas.rootCanvas.scaleFactor : 1f;
            return Mathf.Max(1f, reachDp * dpi / 160f / Mathf.Max(0.0001f, scale));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (background == null || _pointerId != int.MinValue) return;   // one thumb owns the stick
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                Rect r = _rect.rect;
                if ((local.y - r.yMin) / r.height > spawnZoneHeight) return;   // outside the thumb zone
                _pointerId = eventData.pointerId;
                _reach = ResolveReach();
                float art = (movementRange > 0f ? _reach / movementRange : 1f) * artScale;
                background.localScale = _backgroundScale * art;
                if (handle != null) handle.localScale = _handleScale * art;
                background.anchoredPosition = local;
                if (handle != null) handle.anchoredPosition = local;
                background.gameObject.SetActive(true);
                if (handle != null) handle.gameObject.SetActive(true);
            }
            SendValueToControl(Vector2.zero);
            OnDrag(eventData);
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
            if (delta.magnitude > followAt)
            {
                background.anchoredPosition += delta.normalized * (delta.magnitude - followAt);
                delta = local - background.anchoredPosition;
            }

            Vector2 clamped = Vector2.ClampMagnitude(delta, _reach);
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
            SendValueToControl(Vector2.zero);
            HideStick();
        }

        // A disabled stick (pause, level end) must not keep a held direction or a stale owner.
        protected override void OnDisable()
        {
            _pointerId = int.MinValue;
            HideStick();
            base.OnDisable();
        }

        // Both the ring and the knob are hidden when not in use, so nothing lingers on screen.
        private void HideStick()
        {
            if (background != null) background.gameObject.SetActive(false);
            if (handle != null) handle.gameObject.SetActive(false);
        }
    }
}
