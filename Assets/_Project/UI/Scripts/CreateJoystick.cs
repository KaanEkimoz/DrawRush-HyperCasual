using UnityEngine;

namespace Studios208.DrawRush.UI
{
    /// <summary>
    /// Spawns a UI joystick at first touch and destroys it when the touch lifts.
    /// Legacy version had a null prefab field (private + no Inspector exposure) — it
    /// now requires a <see cref="joystickPrefab"/> reference and uses the new Input
    /// System fallback (Touchscreen + classic Input.touches).
    /// </summary>
    public sealed class CreateJoystick : MonoBehaviour
    {
        [SerializeField] private GameObject joystickPrefab;
        [SerializeField] private Transform canvasParent;

        private GameObject _joystick;
        private RectTransform _joystickTransform;

        private void Update()
        {
            if (joystickPrefab == null) return;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (_joystick == null)
                {
                    CreateAt(touch.position);
                }
                else if (_joystickTransform != null)
                {
                    _joystickTransform.anchoredPosition = touch.position;
                }
                return;
            }

            if (_joystick != null)
            {
                Destroy(_joystick);
                _joystick = null;
                _joystickTransform = null;
            }
        }

        public void CreateAt(Vector2 position)
        {
            if (joystickPrefab == null) return;
            _joystick = canvasParent != null
                ? Instantiate(joystickPrefab, canvasParent)
                : Instantiate(joystickPrefab);
            _joystickTransform = _joystick.GetComponent<RectTransform>();
            if (_joystickTransform != null)
            {
                _joystickTransform.anchoredPosition = position;
            }
        }
    }
}
