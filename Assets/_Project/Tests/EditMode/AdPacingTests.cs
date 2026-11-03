using NUnit.Framework;
using DrawRush.Ads;

namespace DrawRush.Tests.EditMode
{
    public class AdPacingTests
    {
        [Test]
        public void ThirdSlowWin_IsDue()
        {
            var p = new AdPacing(3, 90f, 0f);
            p.RecordWin();
            p.RecordWin();
            Assert.IsFalse(p.IsDue(200f));
            p.RecordWin();
            Assert.IsTrue(p.IsDue(200f));
        }

        [Test]
        public void ThreeFastWins_WaitForTheFirstWinAfterTheTimeGate()
        {
            var p = new AdPacing(3, 90f, 0f);
            p.RecordWin();
            p.RecordWin();
            p.RecordWin();
            Assert.IsFalse(p.IsDue(60f));
            p.RecordWin();
            Assert.IsFalse(p.IsDue(89f));
            p.RecordWin();
            Assert.IsTrue(p.IsDue(95f));
        }

        [Test]
        public void TimeGate_CountsFromTheSessionStart()
        {
            var p = new AdPacing(1, 90f, 100f);
            p.RecordWin();
            Assert.IsFalse(p.IsDue(150f));
            Assert.IsTrue(p.IsDue(190f));
        }

        [Test]
        public void MarkShown_ResetsBothGates()
        {
            var p = new AdPacing(3, 90f, 0f);
            for (int i = 0; i < 3; i++) p.RecordWin();
            p.MarkShown(100f);
            p.RecordWin();
            Assert.IsFalse(p.IsDue(300f));
            p.RecordWin();
            p.RecordWin();
            Assert.IsFalse(p.IsDue(150f));
            Assert.IsTrue(p.IsDue(190f));
        }
    }
}
