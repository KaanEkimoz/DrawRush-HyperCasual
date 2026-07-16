using NUnit.Framework;
using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Tests.EditMode
{
    [TestFixture]
    public sealed class EventChannelTests
    {
        [Test]
        public void VoidChannel_Raise_InvokesListeners()
        {
            var ch = ScriptableObject.CreateInstance<VoidEventChannel>();
            int count = 0;
            ch.Raised += () => count++;

            ch.Raise();
            ch.Raise();

            Assert.AreEqual(2, count);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void IntChannel_Raise_PassesPayload()
        {
            var ch = ScriptableObject.CreateInstance<IntEventChannel>();
            int? lastValue = null;
            ch.Raised += v => lastValue = v;

            ch.Raise(42);

            Assert.AreEqual(42, lastValue);
            Object.DestroyImmediate(ch);
        }

        [Test]
        public void IntChannel_Raise_WithNoListeners_DoesNotThrow()
        {
            var ch = ScriptableObject.CreateInstance<IntEventChannel>();
            Assert.DoesNotThrow(() => ch.Raise(1));
            Object.DestroyImmediate(ch);
        }
    }
}
