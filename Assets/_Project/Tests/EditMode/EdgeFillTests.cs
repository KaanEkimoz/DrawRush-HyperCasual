using NUnit.Framework;
using DrawRush.Drawing;

namespace DrawRush.Tests.EditMode
{
    public sealed class EdgeFillTests
    {
        [Test]
        public void New_IsEmptyAndNotComplete()
        {
            var fill = new EdgeFill();
            Assert.AreEqual(0f, fill.PaintedLow);
            Assert.AreEqual(1f, fill.PaintedHigh);
            Assert.IsFalse(fill.IsComplete);
            Assert.AreEqual(0f, fill.Coverage);
        }

        [Test]
        public void PaintFromA_GrowsLowAndOnlyAdvances()
        {
            var fill = new EdgeFill();
            fill.PaintFromA(0.5f);
            Assert.AreEqual(0.5f, fill.PaintedLow, 1e-4f);
            fill.PaintFromA(0.3f);   // smaller → no regression
            Assert.AreEqual(0.5f, fill.PaintedLow, 1e-4f);
            Assert.IsFalse(fill.IsComplete);
        }

        [Test]
        public void PaintFromB_ShrinksHighAndOnlyAdvances()
        {
            var fill = new EdgeFill();
            fill.PaintFromB(0.5f);
            Assert.AreEqual(0.5f, fill.PaintedHigh, 1e-4f);
            fill.PaintFromB(0.7f);   // larger → no regression toward 1
            Assert.AreEqual(0.5f, fill.PaintedHigh, 1e-4f);
        }

        [Test]
        public void SingleEndPaintToFarEnd_Completes()
        {
            var fill = new EdgeFill();
            fill.PaintFromA(1f);   // painted all the way across from A
            Assert.IsTrue(fill.IsComplete);
            Assert.AreEqual(1f, fill.Coverage, 1e-4f);
        }

        [Test]
        public void BothEnds_CompleteWhenSpansMeet()
        {
            var fill = new EdgeFill();
            fill.PaintFromA(0.4f);   // [0, 0.4]
            fill.PaintFromB(0.6f);   // [0.6, 1] — gap in the middle
            Assert.IsFalse(fill.IsComplete);
            Assert.AreEqual(0.8f, fill.Coverage, 1e-4f);

            fill.PaintFromA(0.6f);   // [0, 0.6] now meets [0.6, 1]
            Assert.IsTrue(fill.IsComplete);
        }

        [Test]
        public void Paint_IsClampedToUnitRange()
        {
            var fill = new EdgeFill();
            fill.PaintFromA(1.5f);
            Assert.AreEqual(1f, fill.PaintedLow, 1e-4f);

            var fill2 = new EdgeFill();
            fill2.PaintFromB(-0.5f);
            Assert.AreEqual(0f, fill2.PaintedHigh, 1e-4f);
        }

        [Test]
        public void Reset_ClearsProgress()
        {
            var fill = new EdgeFill();
            fill.PaintFromA(0.5f);
            fill.PaintFromB(0.5f);
            Assert.IsTrue(fill.IsComplete);
            fill.Reset();
            Assert.AreEqual(0f, fill.PaintedLow);
            Assert.AreEqual(1f, fill.PaintedHigh);
            Assert.IsFalse(fill.IsComplete);
        }
    }
}
