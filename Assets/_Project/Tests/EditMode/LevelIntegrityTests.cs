using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Studios208.DrawRush.Tests.EditMode
{
    /// <summary>
    /// Authoring guards for the mega-scene. These assert properties of the LEVEL DATA that no
    /// unit test of the code can catch — every one of them shipped broken at some point.
    /// </summary>
    public sealed class LevelIntegrityTests
    {
        private const string ScenePath = "Assets/Scenes/01_DrawRushGame.unity";

        // Endpoints further apart than this are intentionally open ends (the smiley's mouth,
        // the apple's stem) rather than a corner waiting to be bridged.
        private const float OpenEndBound = 1.2f;

        private static Scene Load(out bool openedHere)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            openedHere = false;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }
            return scene;
        }

        private static Transform LevelsRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name.Contains("LEVELS")) return root.transform;
            return null;
        }

        /// <summary>
        /// EdgeNetwork groups edge endpoints into corners with a plain radius, so that radius
        /// must be bigger than the gap between two edges' drops at a shared vertex yet smaller
        /// than the shortest edge. Ship it too large and an edge's OWN two endpoints collapse
        /// into one corner: posts drift, walls extend to the wrong ends, and the shape loses its
        /// symmetry. Ten levels were authored that way (the 5-petal flower resolved to 3 corners
        /// instead of 5) because every level kept the 1.5 default while insets left edges as
        /// short as 0.68.
        /// </summary>
        [Test]
        public void EveryLevel_CornerMergeDistance_IsBetweenDropGapAndShortestEdge()
        {
            Scene scene = Load(out bool openedHere);
            try
            {
                Transform levels = LevelsRoot(scene);
                Assert.IsNotNull(levels, "===LEVELS=== root not found in " + ScenePath);

                var failures = new List<string>();
                for (int i = 0; i < levels.childCount; i++)
                {
                    Transform level = levels.GetChild(i);
                    Transform edgesRoot = level.Find("Edges");
                    if (edgesRoot == null || edgesRoot.childCount < 2) continue;

                    var a = new List<Vector3>();
                    var b = new List<Vector3>();
                    foreach (Transform edge in edgesRoot)
                    {
                        Transform pa = edge.Find("AnchorA"), pb = edge.Find("AnchorB");
                        if (pa == null || pb == null) continue;
                        a.Add(pa.position);
                        b.Add(pb.position);
                    }
                    if (a.Count < 2) continue;

                    float shortestEdge = float.MaxValue;
                    for (int e = 0; e < a.Count; e++)
                        shortestEdge = Mathf.Min(shortestEdge, Vector3.Distance(a[e], b[e]));

                    // flatten endpoints, remembering which edge each came from
                    var pts = new List<Vector3>();
                    var owner = new List<int>();
                    for (int e = 0; e < a.Count; e++)
                    {
                        pts.Add(a[e]); owner.Add(e);
                        pts.Add(b[e]); owner.Add(e);
                    }

                    float bound = Mathf.Min(OpenEndBound, shortestEdge * 0.95f);
                    float widestDropGap = 0f;
                    for (int p = 0; p < pts.Count; p++)
                    {
                        float nearest = float.MaxValue;
                        for (int q = 0; q < pts.Count; q++)
                        {
                            if (owner[q] == owner[p]) continue;
                            nearest = Mathf.Min(nearest, Vector3.Distance(pts[p], pts[q]));
                        }
                        if (nearest <= bound) widestDropGap = Mathf.Max(widestDropGap, nearest);
                    }

                    Transform win = level.Find("WinCondition");
                    if (win == null) continue;
                    Component network = win.GetComponent("EdgeNetwork");
                    if (network == null) continue;
                    float merge = new SerializedObject(network)
                        .FindProperty("cornerMergeDistance").floatValue;

                    if (merge <= widestDropGap || merge >= shortestEdge)
                    {
                        failures.Add($"{level.name}: cornerMergeDistance={merge:F2} must sit " +
                                     $"strictly between dropGap={widestDropGap:F2} and " +
                                     $"shortestEdge={shortestEdge:F2}");
                    }
                }

                Assert.IsEmpty(failures, "Corner-merge invariant violated:\n" + string.Join("\n", failures));
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// LevelManager.ActivateLevel addresses levels by CHILD INDEX, while everything human
        /// (memory notes, playlists, the design doc) refers to them by name. If a level is ever
        /// reordered or re-appended, index N stops being Level_N and the wrong level loads —
        /// which is exactly what happened when a level was rebuilt and landed at the end.
        /// </summary>
        [Test]
        public void LevelChildOrder_MatchesLevelNames()
        {
            Scene scene = Load(out bool openedHere);
            try
            {
                Transform levels = LevelsRoot(scene);
                Assert.IsNotNull(levels, "===LEVELS=== root not found in " + ScenePath);

                var failures = new List<string>();
                for (int i = 1; i < levels.childCount; i++)   // child 0 is the tutorial
                {
                    string expected = "Level_" + i.ToString("00");
                    string actual = levels.GetChild(i).name;
                    if (actual != expected) failures.Add($"index {i}: expected {expected}, found {actual}");
                }
                Assert.IsEmpty(failures, "ActivateLevel(index) would load the wrong level:\n"
                                         + string.Join("\n", failures));
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
