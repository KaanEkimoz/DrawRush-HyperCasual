using UnityEngine;

namespace DrawRush.Cameras
{
    /// <summary>
    /// Gameplay camera tunables. The camera never rotates (fixed yaw), looks down at a fixed
    /// pitch, and sizes its distance from a target ground width at the player's row, so every
    /// aspect ratio shows the same width and taller screens just see more depth.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "DrawRush/Camera/Camera Config", order = 0)]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Framing")]
        [Tooltip("Downward tilt in degrees (90 = straight down).")]
        [SerializeField, Range(30f, 89f)] private float _pitch = 58f;
        [Tooltip("Vertical field of view. Lower = flatter, less perspective stretch.")]
        [SerializeField, Range(20f, 70f)] private float _fieldOfView = 40f;
        [Tooltip("Ground width (m) visible across the screen at the player's row, before threat zoom.")]
        [SerializeField, Min(2f)] private float _visibleWidth = 10.5f;
        [Tooltip("Where the player sits on screen, 0 = bottom, 0.5 = centre. Above centre keeps the " +
                 "player clear of the thumb and shows more ground behind them.")]
        [SerializeField, Range(0.3f, 0.75f)] private float _playerScreenY = 0.58f;
        [Tooltip("Follow damping per axis in seconds. Low keeps the camera glued to the player.")]
        [SerializeField, Min(0f)] private float _followDamping = 0.25f;

        [Header("Threat zoom")]
        [Tooltip("An enemy closer than this (m) starts pulling the camera back.")]
        [SerializeField, Min(0f)] private float _threatEnterRadius = 6f;
        [Tooltip("A tracked enemy is dropped once farther than this (m). Larger than enter = no flicker.")]
        [SerializeField, Min(0f)] private float _threatExitRadius = 8f;
        [Tooltip("Seconds the zoom holds after the last threat leaves, so it does not pump.")]
        [SerializeField, Min(0f)] private float _threatHoldTime = 1f;
        [Tooltip("At most this many nearest enemies decide the zoom.")]
        [SerializeField, Min(1)] private int _maxThreats = 3;
        [Tooltip("Extra ground (m) kept around each threat so it is never on the screen edge.")]
        [SerializeField, Min(0f)] private float _threatMargin = 1.5f;
        [Tooltip("Largest pull-back as a multiple of the base distance.")]
        [SerializeField, Range(1f, 2.5f)] private float _maxZoomOut = 1.5f;
        [Tooltip("SmoothDamp time (s) of the zoom.")]
        [SerializeField, Min(0.01f)] private float _zoomSmoothTime = 0.6f;

        public float pitch => _pitch;
        public float fieldOfView => _fieldOfView;
        public float visibleWidth => _visibleWidth;
        public float playerScreenY => _playerScreenY;
        public float followDamping => _followDamping;
        public float threatEnterRadius => _threatEnterRadius;
        public float threatExitRadius => Mathf.Max(_threatExitRadius, _threatEnterRadius);
        public float threatHoldTime => _threatHoldTime;
        public int maxThreats => _maxThreats;
        public float threatMargin => _threatMargin;
        public float maxZoomOut => _maxZoomOut;
        public float zoomSmoothTime => _zoomSmoothTime;
    }
}
