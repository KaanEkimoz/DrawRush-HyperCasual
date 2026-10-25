using DrawRush.Core;
using DrawRush.Enemy;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace DrawRush.Cameras
{
    /// <summary>
    /// Drives the gameplay CinemachineCamera from <see cref="CameraConfig"/>: fixed yaw and
    /// pitch, a distance sized to a ground width for the current aspect, the player placed a
    /// little above centre, light follow damping, and a pull-back while enemies are close.
    /// Sits on the CinemachineCamera with a CinemachineFollow body and no aim.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineFollow))]
    public sealed class GameplayCameraRig : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        [Tooltip("Live enemy list used for the threat pull-back.")]
        [SerializeField] private EnemyProximity enemies;

        private CinemachineCamera _camera;
        private CinemachineFollow _follow;
        private float _zoom = 1f, _zoomVelocity, _holdUntil;
        private readonly Transform[] _threats = new Transform[8];

        /// <summary>Current pull-back multiple (1 = base framing).</summary>
        public float Zoom => _zoom;

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            _follow = GetComponent<CinemachineFollow>();
        }

        private void Update()
        {
            if (config == null) return;
            if (_camera.Follow == null && GameServices.Player != null) _camera.Follow = GameServices.Player;

            float target = ThreatZoomTarget();
            _zoom = Mathf.SmoothDamp(_zoom, target, ref _zoomVelocity, config.zoomSmoothTime);
            Apply(_zoom);
        }

        private void Apply(float zoom)
        {
            float fov = config.fieldOfView;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 9f / 19.5f;
            float distance = CameraMath.DistanceForWidth(config.visibleWidth, fov, aspect) * zoom;

            var lens = _camera.Lens;
            lens.FieldOfView = fov;
            _camera.Lens = lens;
            transform.rotation = Quaternion.Euler(config.pitch, 0f, 0f);   // never rotates with the player
            _follow.FollowOffset = CameraMath.FollowOffset(config.pitch, fov, distance, config.playerScreenY);
            var tracker = _follow.TrackerSettings;
            tracker.BindingMode = BindingMode.WorldSpace;
            tracker.PositionDamping = Vector3.one * config.followDamping;
            _follow.TrackerSettings = tracker;
        }

        // Pull back far enough to show the farthest of the nearest few threats; hold briefly
        // after they leave, and use a wider exit radius than enter so the zoom never pumps.
        private float ThreatZoomTarget()
        {
            Transform player = GameServices.Player;
            if (enemies == null || player == null) return 1f;

            int count = enemies.Nearest(player.position, config.threatExitRadius, _threats, Mathf.Min(config.maxThreats, _threats.Length));
            float farthest = -1f;
            bool anyInside = false;
            for (int i = 0; i < count; i++)
            {
                Vector3 d = _threats[i].position - player.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist <= config.threatEnterRadius) anyInside = true;
                farthest = Mathf.Max(farthest, dist);
            }

            if (anyInside) _holdUntil = Time.time + config.threatHoldTime;
            bool engaged = anyInside || (Time.time < _holdUntil && count > 0);
            if (!engaged) return Time.time < _holdUntil ? _zoom : 1f;
            return CameraMath.ZoomForThreat(farthest, config.threatMargin, config.visibleWidth, config.maxZoomOut);
        }
    }
}
