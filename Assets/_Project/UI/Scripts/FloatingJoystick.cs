using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace Studios208.DrawRush.UI
{
    /// <summary>
    /// A floating on-screen stick: instead of a fixed control, the joystick spawns where the
    /// player first touches inside this RectTransform and follows the drag, then hides on
    /// release. It feeds the same virtual control as Unity's OnScreenStick
    /// (<c>&lt;Gamepad&gt;/leftStick</c> by default), so the existing input pipeline
    /// (PlayerControls.Move → ThirdPersonMovement) is unchanged.
    ///
    /// Setup: put this on a full-area RectTransform with a transparent, raycast-target Image
    /// (the touch zone). Assign <see cref="background"/> (the ring) and <see cref="handle"/>
    /// (the knob); both live in this rect's local space. The background is shown/hidden as a
    /// whole, so parent the handle under the background in the hierarchy.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FloatingJoystick : OnScreenControl,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("The joystick ring. Moved to the touch point and toggled on/off.")]
        [SerializeField] private RectTransform background;
        [Tooltip("The knob. Should be a child of background; clamped within movementRange.")]
        [SerializeField] private RectTransform handle;
        [Tooltip("Max knob travel from center, in canvas units. Full deflection = input 1.")]
        [SerializeField] private float movementRange = 80f;

        [InputControl(layout = "Vector2")]
        [SerializeField] private string controlPath = "<Gamepad>/leftStick";
        protected override string controlPathInternal
        {
            get => controlPath;
            set => controlPath = value;
        }

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            HideStick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (background == null) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                background.anchoredPosition = local;
                background.gameObject.SetActive(true);
                if (handle != null) handle.anchoredPosition = Vector2.zero;
            }
            SendValueToControl(Vector2.zero);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            Vector2 delta = local - background.anchoredPosition;
            Vector2 clamped = Vector2.ClampMagnitude(delta, movementRange);
            if (handle != null) handle.anchoredPosition = clamped;   // handle is child of background
            SendValueToControl(movementRange > 0f ? clamped / movementRange : Vector2.zero);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SendValueToControl(Vector2.zero);
            HideStick();
        }

        private void HideStick()
        {
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (background != null) background.gameObject.SetActive(false);
        }
    }
}
