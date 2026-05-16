using UnityEngine;
//TO DO: Add a Joystick system that creating JoyStick at touch position
public class CreateJoystick : MonoBehaviour
{
    private RectTransform _joyStickTransform;
    private GameObject _joyStickPrefab;
    private GameObject _joyStick;

    private void Update()
    {
        if (Input.touches.Length > 0)
        {
            CreateJoystickAtPosition(Input.GetTouch(0).position);
        }
        else
        {
            Destroy(_joyStick);
        }
    }

    public void CreateJoystickAtPosition(Vector2 position)
    {
        _joyStick = Instantiate(_joyStickPrefab) as GameObject;
        _joyStickTransform = _joyStick.GetComponent<RectTransform>();
        _joyStickTransform.anchoredPosition = position;
    }
}
