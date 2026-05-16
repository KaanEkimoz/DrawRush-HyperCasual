using NUnit.Framework;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Tests.EditMode
{
    [TestFixture]
    public sealed class DrawPartStateMachineTests
    {
        [Test]
        public void StartsInIdle()
        {
            var fsm = new DrawPartStateMachine();
            Assert.AreEqual(DrawingPhase.Idle, fsm.Phase);
            Assert.IsFalse(fsm.IsCompleted);
        }

        [Test]
        public void HappyPath_Idle_Returning_Armed_Drawing_Done()
        {
            var fsm = new DrawPartStateMachine();
            Assert.IsTrue(fsm.TryTransition(DrawingPhase.Returning));
            Assert.IsTrue(fsm.TryTransition(DrawingPhase.Armed));
            Assert.IsTrue(fsm.TryTransition(DrawingPhase.Drawing));
            Assert.IsTrue(fsm.TryTransition(DrawingPhase.Done));
            Assert.IsTrue(fsm.IsCompleted);
        }

        [Test]
        public void DoneIsTerminal_NoFurtherTransitions()
        {
            var fsm = new DrawPartStateMachine();
            fsm.TryTransition(DrawingPhase.Armed);
            fsm.TryTransition(DrawingPhase.Done);

            Assert.IsFalse(fsm.TryTransition(DrawingPhase.Idle));
            Assert.IsFalse(fsm.TryTransition(DrawingPhase.Armed));
            Assert.AreEqual(DrawingPhase.Done, fsm.Phase);
        }

        [Test]
        public void SameStateTransition_IsRejected()
        {
            var fsm = new DrawPartStateMachine();
            Assert.IsFalse(fsm.TryTransition(DrawingPhase.Idle));
        }

        [Test]
        public void Idle_CannotJumpToDrawingOrDone()
        {
            Assert.IsFalse(DrawPartStateMachine.CanTransition(DrawingPhase.Idle, DrawingPhase.Drawing));
            Assert.IsFalse(DrawPartStateMachine.CanTransition(DrawingPhase.Idle, DrawingPhase.Done));
        }

        [Test]
        public void Armed_CanCancelToIdle()
        {
            var fsm = new DrawPartStateMachine();
            fsm.TryTransition(DrawingPhase.Armed);
            Assert.IsTrue(fsm.TryTransition(DrawingPhase.Idle));
            Assert.AreEqual(DrawingPhase.Idle, fsm.Phase);
        }

        [Test]
        public void Transitioned_Event_FiresWithFromAndTo()
        {
            var fsm = new DrawPartStateMachine();
            DrawingPhase? capturedFrom = null;
            DrawingPhase? capturedTo = null;
            fsm.Transitioned += (from, to) => { capturedFrom = from; capturedTo = to; };

            fsm.TryTransition(DrawingPhase.Armed);

            Assert.AreEqual(DrawingPhase.Idle, capturedFrom);
            Assert.AreEqual(DrawingPhase.Armed, capturedTo);
        }

        [Test]
        public void ResetToIdle_ClearsPhase_WithoutFiringEvent()
        {
            var fsm = new DrawPartStateMachine();
            fsm.TryTransition(DrawingPhase.Armed);
            int events = 0;
            fsm.Transitioned += (_, _) => events++;

            fsm.ResetToIdle();

            Assert.AreEqual(DrawingPhase.Idle, fsm.Phase);
            Assert.AreEqual(0, events);
        }
    }
}
