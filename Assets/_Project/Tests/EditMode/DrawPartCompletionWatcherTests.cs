using System;
using NUnit.Framework;
using UnityEngine;
using Studios208.DrawRush.Common;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Tests.EditMode
{
    [TestFixture]
    public sealed class DrawPartCompletionWatcherTests
    {
        private sealed class FakeDrawPart : IDrawPart
        {
            public event Action<IDrawPart> Completed;
            public bool IsCompleted { get; private set; }
            public Transform Transform => null;
            public void Interact() { }
            public void OnPlayerEntered() { }
            public void OnPlayerExited() { }
            public void Complete()
            {
                if (IsCompleted) return;
                IsCompleted = true;
                Completed?.Invoke(this);
            }
        }

        [Test]
        public void DoesNotFire_BeforeAllPartsComplete()
        {
            var a = new FakeDrawPart();
            var b = new FakeDrawPart();
            var watcher = new DrawPartCompletionWatcher(new IDrawPart[] { a, b });
            int fired = 0;
            watcher.AllCompleted += () => fired++;
            watcher.Enable();

            a.Complete();

            Assert.AreEqual(0, fired);
            Assert.AreEqual(1, watcher.CompletedCount);
        }

        [Test]
        public void Fires_OnceWhenAllComplete()
        {
            var a = new FakeDrawPart();
            var b = new FakeDrawPart();
            var watcher = new DrawPartCompletionWatcher(new IDrawPart[] { a, b });
            int fired = 0;
            watcher.AllCompleted += () => fired++;
            watcher.Enable();

            a.Complete();
            b.Complete();

            Assert.AreEqual(1, fired);
            Assert.IsTrue(watcher.IsFinalized);
        }

        [Test]
        public void RepeatedComplete_DoesNotDoubleFire()
        {
            var a = new FakeDrawPart();
            var watcher = new DrawPartCompletionWatcher(new IDrawPart[] { a });
            int fired = 0;
            watcher.AllCompleted += () => fired++;
            watcher.Enable();

            a.Complete();
            a.Complete(); // idempotent on the part, but still — watcher must not double-fire
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Disable_StopsListening()
        {
            var a = new FakeDrawPart();
            var watcher = new DrawPartCompletionWatcher(new IDrawPart[] { a });
            int fired = 0;
            watcher.AllCompleted += () => fired++;
            watcher.Enable();
            watcher.Disable();

            a.Complete();

            Assert.AreEqual(0, fired);
        }

        [Test]
        public void Constructor_NullParts_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DrawPartCompletionWatcher(null));
        }
    }
}
