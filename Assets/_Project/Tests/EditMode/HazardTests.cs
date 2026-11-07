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
        public void TrapKinds_MapFromTheCampaign()
        {
            Assert.AreEqual(GroundTraps.Kind.Spikes, GroundTraps.KindOf(Novelty.Spikes));
            Assert.AreEqual(GroundTraps.Kind.Saw, GroundTraps.KindOf(Novelty.Saw));
            Assert.AreEqual(GroundTraps.Kind.Roller, GroundTraps.KindOf(Novelty.Roller));
            Assert.AreEqual(GroundTraps.Kind.Saw, GroundTraps.KindOf(Campaign.TrapKind(Campaign.DebutLevel(Novelty.Saw), 0)));
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
            float W(ClumsyGiants.Hazard h, int level, bool enemies = true) => ClumsyGiants.Weight(h, level, enemies);
            int cup = Campaign.DebutLevel(Novelty.Cup), ink = Campaign.DebutLevel(Novelty.Ink);
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Cup, cup - 1));
            Assert.Greater(W(ClumsyGiants.Hazard.Cup, cup), 0f);
            Assert.AreEqual(ClumsyGiants.Hazard.Ink, ClumsyGiants.DebutHazard(ink), "the debut comes first");
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Bump, 14, enemies: false));
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Ink, ink - 1));
            Assert.Greater(W(ClumsyGiants.Hazard.Ink, ink), 0f);
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Cup, 21), "breathers have no accidents");
            Assert.AreEqual(0f, W(ClumsyGiants.Hazard.Cup, 20), "nor boss duels");
            Assert.Greater(W(ClumsyGiants.Hazard.Ruler, Campaign.DebutLevel(Novelty.Ruler)), 0f);
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
