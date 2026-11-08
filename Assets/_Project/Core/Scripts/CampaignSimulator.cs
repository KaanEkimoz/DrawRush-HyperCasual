using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Plays the campaign on paper: the same curve, bag and budget LevelFlow uses, level after
    /// level, without a scene. Tests check its rows against the campaign rules; the editor menu
    /// DrawRush → Dev → Kampanya Simülasyonu prints them for the real shapes.
    /// </summary>
    public static class CampaignSimulator
    {
        public struct Shape
        {
            public string Name;
            public float Score;
            public int Enemies;   // authored, i.e. the most it can field
        }

        public struct Row
        {
            public int Level;
            public float Target;
            public int Index;
            public string Name;
            public float Shape;
            public int Traps;
            public bool Events;
            public int Enemies;
            public float Total;
            public Novelty Debut;
            public bool Breather, Boss;
            public Novelty[] Kinds;   // enemy kind per slot
        }

        /// <summary>LevelFlow's default tuning.</summary>
        public const float MinTarget = 3f, MaxTarget = 21f;
        public const int RampLevels = 30;

        public static List<Row> Run(IReadOnlyList<Shape> shapes, int levels, int seed = 1,
                                    float min = MinTarget, float max = MaxTarget, int ramp = RampLevels)
        {
            var rows = new List<Row>();
            var bag = new LevelBag(new System.Random(seed)) { CandidatePool = 1 };
            var all = new List<int>();
            for (int i = 0; i < shapes.Count; i++) all.Add(i);
            for (int played = 0; played < levels; played++)
            {
                int number = played + 1;
                float target = LevelDifficulty.SawtoothTarget(played, Campaign.Period, min, max, ramp);   // = Campaign.Target(number) at the defaults
                int next = bag.Draw(target, i => LevelFlow.RankScore(number, target, shapes[i].Score, shapes[i].Enemies), all);
                Shape s = shapes[next];
                int enemies = LevelFlow.Enemies(number, target, s.Score, s.Enemies);
                Novelty debut = Campaign.DebutAt(number);
                // The director's two overrides: a debut or boss level the budget left empty still
                // gets its one enemy, and a boss duel is the boss alone.
                if ((Campaign.IsEnemy(debut) || Campaign.IsBoss(number)) && s.Enemies > 0) enemies = Mathf.Max(1, Mathf.Min(enemies, 1));
                var kinds = new Novelty[enemies];
                for (int k = 0; k < enemies; k++) kinds[k] = Campaign.EnemyKind(number, k);
                rows.Add(new Row
                {
                    Level = number, Target = target, Index = next, Name = s.Name, Shape = s.Score,
                    Traps = Campaign.Traps(number, s.Score), Events = Campaign.EventsOn(number), Enemies = enemies,
                    Total = s.Score + Campaign.FixedCost(number, s.Score) + enemies * LevelDifficulty.EnemyWeight,
                    Debut = debut, Breather = Campaign.IsBreather(number), Boss = Campaign.IsBoss(number), Kinds = kinds,
                });
            }
            return rows;
        }

        public static string Table(List<Row> rows)
        {
            var sb = new StringBuilder("lvl  target  total  shape                 trap ev  enemies               new\n");
            foreach (Row r in rows)
            {
                string who = r.Boss ? (Campaign.IsStaplerBoss(r.Level) ? "BOSS stapler" : "BOSS eraser")
                    : r.Kinds.Length == 0 ? "-" : string.Join("+", System.Array.ConvertAll(r.Kinds, k => k.ToString()));
                sb.Append($"{r.Level,3}  {r.Target,6:F1} {r.Total,6:F1}  {r.Name,-20}  {r.Traps,3} {(r.Events ? "on" : "--"),2}  {who,-20}  " +
                          $"{(r.Debut != Novelty.None ? r.Debut.ToString() : "")}{(r.Breather ? " (mola)" : "")}\n");
            }
            return sb.ToString();
        }
    }
}
