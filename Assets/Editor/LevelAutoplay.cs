using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Enemy;
using DrawRush.Player;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dev-only regression check for the paint rail: in Play mode, drives the player through every
/// level (enemies off) by writing stick input the way a player would — walk to the nearest open
/// drop, push along its edge, follow the edge to the far drop — and logs PASS/FAIL per level.
/// Catches "the character gets stuck and can't finish the shape" after rail changes.
/// Menu: DrawRush > Dev > Autoplay Levels (enter Play mode first).
/// </summary>
public static class LevelAutoplay
{
    private const float LevelTimeout = 40f;
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly FieldInfo MoveField = typeof(ThirdPersonMovement).GetField("_move", Private);
    private static readonly FieldInfo EdgesField = typeof(EdgeNetwork).GetField("_edges", Private);
    private static readonly FieldInfo CurPartField = typeof(RailPaintController).GetField("_currentPart", Private);
    private static readonly FieldInfo CurEdgeField = typeof(RailPaintController).GetField("_edge", Private);

    private static Queue<int> _queue;
    private static LevelManager _levels;
    private static Transform _player;
    private static ThirdPersonMovement _movement;
    private static RailPaintController _rail;
    private static EdgeNetwork _network;
    private static Transform _level;
    private static DrawEdge _target;
    private static bool _fromA;
    private static double _levelStart;
    private static readonly StringBuilder Report = new();

    [MenuItem("DrawRush/Dev/Autoplay Levels")]
    private static void Run() => Run(null);

    /// <summary>Autoplay the given level indices (null = every level except the tutorial).</summary>
    public static void Run(IEnumerable<int> indices)
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("[Autoplay] Enter Play mode first."); return; }
        _levels = Object.FindFirstObjectByType<LevelManager>();
        _player = GameServices.Player;
        if (_levels == null || _player == null) { Debug.LogWarning("[Autoplay] No LevelManager/Player."); return; }
        _movement = _player.GetComponent<ThirdPersonMovement>();
        _rail = _player.GetComponent<RailPaintController>();

        _queue = new Queue<int>();
        if (indices != null) foreach (int i in indices) _queue.Enqueue(i);
        else for (int i = 0; i < _levels.LevelCount; i++) if (i != _levels.TutorialIndex) _queue.Enqueue(i);
        Report.Clear();
        StartNext();
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void StartNext()
    {
        int index = _queue.Dequeue();
        _levels.ActivateLevel(index);
        _level = _levels.GetLevel(index);
        _network = _level.GetComponentInChildren<EdgeNetwork>();
        foreach (EnemyFollow enemy in _level.GetComponentsInChildren<EnemyFollow>(true))
            enemy.gameObject.SetActive(false);
        _target = null;
        _levelStart = EditorApplication.timeSinceStartup;
    }

    private static void Stop()
    {
        EditorApplication.update -= Tick;
        if (_movement != null) MoveField.SetValue(_movement, Vector2.zero);
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || _network == null) { Stop(); return; }
        Time.timeScale = 1f;   // skip the 3-2-1 countdown

        var edges = (IList)EdgesField.GetValue(_network);
        var curPart = (DrawPart)CurPartField.GetValue(_rail);
        var curEdge = (DrawEdge)CurEdgeField.GetValue(_rail);
        int done = 0;
        foreach (DrawEdge e in edges) if (e.IsComplete) done++;

        double elapsed = EditorApplication.timeSinceStartup - _levelStart;
        if (done == edges.Count || elapsed > LevelTimeout)
        {
            string line = $"{_level.name} {done}/{edges.Count} t={elapsed:F1} {(done == edges.Count ? "PASS" : "FAIL")}";
            Report.AppendLine(line);
            Debug.Log("[Autoplay] " + line);
            MoveField.SetValue(_movement, Vector2.zero);
            if (_queue.Count > 0) StartNext();
            else { Stop(); Debug.Log("[Autoplay] DONE\n" + Report); }
            return;
        }

        // Which edge a player would be working on: the one on the rail, else one leaving the
        // anchored drop, else the nearest open drop.
        if (_target != null && _target.IsComplete) _target = null;
        if (curEdge != null) { _target = curEdge; _fromA = curPart == curEdge.A; }
        else if (curPart != null)
        {
            foreach (DrawEdge e in edges)
                if (!e.IsComplete && (e.A == curPart || e.B == curPart)) { _target = e; _fromA = e.A == curPart; break; }
        }
        if (_target == null)
        {
            float best = float.PositiveInfinity;
            foreach (DrawEdge e in edges)
            {
                if (e.IsComplete) continue;
                float da = (e.A.Transform.position - _player.position).sqrMagnitude;
                float db = (e.B.Transform.position - _player.position).sqrMagnitude;
                if (da < best) { best = da; _target = e; _fromA = true; }
                if (db < best) { best = db; _target = e; _fromA = false; }
            }
        }
        if (_target == null) return;

        Vector3 dir;
        if (curEdge == null && curPart == null)
            dir = (_fromA ? _target.A : _target.B).Transform.position - _player.position;   // walk to the drop
        else if (curEdge == null)
            dir = _fromA ? _target.TangentAt(0f) : -_target.TangentAt(1f);                  // push along its edge
        else
        {
            float bestT = 0f, bestSqr = float.PositiveInfinity;
            for (int k = 0; k <= 60; k++)
            {
                float t = k / 60f;
                float sqr = (_target.PointAt(t) - _player.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; bestT = t; }
            }
            dir = _fromA ? _target.TangentAt(bestT) : -_target.TangentAt(bestT);            // follow the rail
        }
        dir.y = 0f;

        Transform cam = GameServices.MainCamera;
        float camY = cam != null ? cam.eulerAngles.y : 0f;
        float angle = (Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - camY) * Mathf.Deg2Rad;
        MoveField.SetValue(_movement, new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)));
    }
}
