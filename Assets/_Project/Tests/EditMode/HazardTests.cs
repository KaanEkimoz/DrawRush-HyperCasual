using NUnit.Framework;
using UnityEngine;
using DrawRush.Core;
using DrawRush.Enemy;
using DrawRush.Hazards;

namespace DrawRush.Tests.EditMode
{
    /// <summary>Ground traps and desk accidents: when they join the campaign and what blocks paint.</summary>
    public sealed class HazardTests
    {
        [TearDown] public void TearDown() => PaintBlockers.Clear();

        [Test]
        public void GroundTraps_JoinGradually()
        {
            Assert.AreEqual(0, GroundTraps.TrapCount(5, 6, 14, 26));
            Assert.AreEqual(1, GroundTraps.TrapCount(6, 6, 14, 26));
            Assert.AreEqual(1, GroundTraps.TrapCount(13, 6, 14, 26));
            Assert.AreEqual(2, GroundTraps.TrapCount(14, 6, 14, 26));
            Assert.AreEqual(3, GroundTraps.TrapCount(26, 6, 14, 26));
            Assert.AreEqual(3, GroundTraps.TrapCount(200, 6, 14, 26));
        }

        [Test]
        public void TrapKinds_JoinOneByOne_AndDebutFirst()
        {
            GroundTraps.Kind K(int level, int slot, float roll) => GroundTraps.KindFor(level, slot, 10, 18, roll);
            Assert.AreEqual(GroundTraps.Kind.Spikes, K(9, 0, 0.99f));
            Assert.AreEqual(GroundTraps.Kind.Saw, K(10, 0, 0f));
            Assert.AreEqual(GroundTraps.Kind.Spikes, K(10, 1, 0.99f), "only the debut slot is a saw");
            Assert.AreEqual(GroundTraps.Kind.Saw, K(12, 1, 0.99f));
            Assert.AreNotEqual(GroundTraps.Kind.Roller, K(17, 0, 0.99f));
            Assert.AreEqual(GroundTraps.Kind.Roller, K(18, 0, 0f));
            Assert.AreEqual(GroundTraps.Kind.Spikes, K(30, 1, 0.1f));
            Assert.AreEqual(GroundTraps.Kind.Saw, K(30, 1, 0.5f));
            Assert.AreEqual(GroundTraps.Kind.Roller, K(30, 1, 0.9f));
        }

        [Test]
        public void PaintBlockers_MoveWithTheirSource()
        {
            int id = PaintBlockers.Add(Vector3.zero, 0.5f);
            PaintBlockers.Move(id, new Vector3(3f, 0f, 0f));
            Assert.IsFalse(PaintBlockers.Blocks(Vector3.zero));
            Assert.IsTrue(PaintBlockers.Blocks(new Vector3(3.2f, 0f, 0f)));
        }

        [Test]
        public void DeskAccidents_JoinTheRosterLevelByLevel()
        {
            const int ink = 12, gust = 16, ruler = 20;
            float W(ClumsyGiants.Hazard h, int level, bool enemies = true) => ClumsyGiants.Weight(h, level, enemies, ink, gust, ruler);
            Assert.Greater(W(ClumsyGiants.Hazard.Cup, 8), 0f);
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Bump, 8, enemies: false));
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Ink, 11));
            Assert.Greater(W(ClumsyGiants.Hazard.Ink, 12), 0f);
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Gust, 15));
            Assert.Greater(W(ClumsyGiants.Hazard.Gust, 16), 0f);
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Ruler, 19));
            Assert.Greater(W(ClumsyGiants.Hazard.Ruler, 20), 0f);
        }

        [Test]
        public void PaintBlockers_BlockOnlyInsideAndLetGo()
        {
            int id = PaintBlockers.Add(new Vector3(2f, 0f, 3f), 0.6f);
            Assert.IsTrue(PaintBlockers.Blocks(new Vector3(2.3f, 5f, 3.2f)), "height does not matter");
            Assert.IsFalse(PaintBlockers.Blocks(new Vector3(2.7f, 0f, 3f)));
            PaintBlockers.Remove(id);
            Assert.IsFalse(PaintBlockers.Blocks(new Vector3(2f, 0f, 3f)));
            Assert.AreEqual(0, PaintBlockers.Count);
        }
    }
}
