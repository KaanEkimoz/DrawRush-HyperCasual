using NUnit.Framework;
using UnityEngine;
using DrawRush.Cameras;
using DrawRush.Core;
using DrawRush.UI;

namespace DrawRush.Tests.EditMode
{
    /// <summary>Control and camera feel math: the joystick deadzone rescale, the analog speed
    /// curve, and the fixed-pitch camera framing.</summary>
    public sealed class ControlFeelTests
    {
        [Test]
        public void Deflect_IsZeroInsideDeadzone_AndStartsFromZeroPastIt()
        {
            Assert.AreEqual(Vector2.zero, FloatingJoystick.Deflect(new Vector2(0.05f, 0f), 0.1f));
            Assert.AreEqual(0f, FloatingJoystick.Deflect(new Vector2(0.1f, 0f), 0.1f).magnitude, 1e-5f);
            Assert.AreEqual(0.5f, FloatingJoystick.Deflect(new Vector2(0f, 0.55f), 0.1f).magnitude, 1e-4f);
            Assert.AreEqual(1f, FloatingJoystick.Deflect(new Vector2(0.6f, 0.8f), 0.1f).magnitude, 1e-4f);
        }

        [Test]
        public void Deflect_KeepsDirection()
        {
            Vector2 d = FloatingJoystick.Deflect(new Vector2(-0.3f, 0.4f), 0.1f);
            Assert.AreEqual(new Vector2(-0.6f, 0.8f).normalized.x, d.normalized.x, 1e-4f);
            Assert.AreEqual(new Vector2(-0.6f, 0.8f).normalized.y, d.normalized.y, 1e-4f);
        }

        [Test]
        public void SpeedFactor_StartsSlower_AndBuildsUpToFullWhileMoving()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                Assert.AreEqual(0.55f, config.SpeedFactor(0f), 1e-4f);
                Assert.Less(config.SpeedFactor(0.2f), config.SpeedFactor(0.4f));
                Assert.AreEqual(1f, config.SpeedFactor(0.6f), 1e-4f);
                Assert.AreEqual(1f, config.SpeedFactor(5f), 1e-4f);
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void DistanceForWidth_ShowsTheRequestedWidth()
        {
            float aspect = 1080f / 2400f, fov = 40f, d = CameraMath.DistanceForWidth(10.5f, fov, aspect);
            float halfW = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * aspect;
            Assert.AreEqual(10.5f, 2f * d * halfW, 1e-3f);
        }

        [Test]
        public void FollowOffset_PutsThePlayerAtTheRequestedScreenHeight()
        {
            const float pitch = 58f, fov = 40f, distance = 20f, screenY = 0.58f;
            Vector3 offset = CameraMath.FollowOffset(pitch, fov, distance, screenY);
            Assert.AreEqual(distance, offset.magnitude, 1e-3f);
            Assert.AreEqual(0f, offset.x, 1e-6f);

            // Project the player (at the origin) through a camera at offset, tilted down by pitch.
            Quaternion rot = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 local = Quaternion.Inverse(rot) * (Vector3.zero - offset);
            float viewportY = 0.5f + 0.5f * (local.y / local.z) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            Assert.AreEqual(screenY, viewportY, 1e-3f);
        }

        [Test]
        public void ZoomForThreat_IsClampedBetweenOneAndMax()
        {
            Assert.AreEqual(1f, CameraMath.ZoomForThreat(1f, 1.5f, 10.5f, 1.5f), 1e-4f);
            Assert.AreEqual(1.5f, CameraMath.ZoomForThreat(20f, 1.5f, 10.5f, 1.5f), 1e-4f);
            Assert.AreEqual((5f + 1.5f) * 2f / 10.5f, CameraMath.ZoomForThreat(5f, 1.5f, 10.5f, 1.5f), 1e-4f);
        }
    }
}
