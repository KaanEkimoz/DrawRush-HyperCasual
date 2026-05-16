using NUnit.Framework;
using UnityEngine;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Tests.EditMode
{
    [TestFixture]
    public sealed class TrailMathTests
    {
        [Test]
        public void Lerp_AtFullRate_ReachesTargetInOneFrame()
        {
            var from = new Vector3(0, 0, 0);
            var to = new Vector3(10, 0, 0);
            var result = TrailMath.Lerp(from, to, lerpRate: 1f, deltaTime: 1f);
            Assert.That(result.x, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void Lerp_AtZeroDeltaTime_DoesNotMove()
        {
            var from = new Vector3(5, 5, 5);
            var to = new Vector3(10, 10, 10);
            var result = TrailMath.Lerp(from, to, lerpRate: 100f, deltaTime: 0f);
            Assert.AreEqual(from, result);
        }

        [Test]
        public void Lerp_HalfRate_MovesHalfway()
        {
            var from = Vector3.zero;
            var to = new Vector3(0, 0, 10);
            var result = TrailMath.Lerp(from, to, lerpRate: 0.5f, deltaTime: 1f);
            Assert.That(result.z, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void Lerp_PerComponentIndependent()
        {
            var from = new Vector3(0, 100, 0);
            var to = new Vector3(10, 100, 10);
            var result = TrailMath.Lerp(from, to, lerpRate: 1f, deltaTime: 1f);
            Assert.AreEqual(10f, result.x);
            Assert.AreEqual(100f, result.y);
            Assert.AreEqual(10f, result.z);
        }
    }
}
