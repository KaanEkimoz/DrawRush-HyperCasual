#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>Editor menu DrawRush → Dev → Kampanya Simülasyonu: runs the campaign on paper with
    /// the scene's real shapes and writes the table to Logs/campaign_sim.txt.</summary>
    public static class CampaignSimMenu
    {
        [MenuItem("DrawRush/Dev/Kampanya Simülasyonu")]
        public static void Run()
        {
            var lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
            if (lm == null) { Debug.LogWarning("Kampanya simülasyonu: oyun sahnesini (01_DrawRushGame) aç."); return; }
            var shapes = new List<CampaignSimulator.Shape>();
            var index = new List<int>();
            for (int i = 0; i < lm.LevelCount; i++)
            {
                if (i == lm.TutorialIndex) continue;
                Transform l = lm.GetLevel(i);
                shapes.Add(new CampaignSimulator.Shape { Name = l.name, Score = LevelDifficulty.ShapeScore(l), Enemies = LevelDifficulty.AvailableEnemies(l) });
                index.Add(i);
            }
            List<CampaignSimulator.Row> rows = CampaignSimulator.Run(shapes, 2 * shapes.Count + 10);
            string path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Logs", "campaign_sim.txt");
            System.IO.File.WriteAllText(path, CampaignSimulator.Table(rows));
            Debug.Log($"Kampanya simülasyonu: {shapes.Count} şekil, {rows.Count} level → {path}");
        }
    }
}
#endif
