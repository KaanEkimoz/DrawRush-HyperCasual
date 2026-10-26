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
/// Dev-only regression check for free drawing: in Play mode, drives the player through every
/// level (enemies off) by writing stick input the way a player would — walk to the nearest
/// unpainted stretch of the ghost shape, then follow it — and logs PASS/FAIL per level.
/// Catches "the shape can't be finished" after drawing changes.
/// The "with enemies" variant keeps every enemy on, dressed as campaign level 26 (all four
/// variants), and keeps the bot invulnerable: it checks that Eaters, Dashers and the rest never
/// break a level or throw, not whether a human would survive.
/// Menu: DrawRush > Dev > Autoplay Levels [with Enemies] (enter Play mode first).
/// </summary>
public static class LevelAutoplay
{
    private const float LevelTimeout = 40f;
    private const float OnLine = 0.3f;        // closer than this counts as walking the line
    private const float Lookahead = 0.6f;     // metres ahead along the edge to steer at
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly FieldInfo MoveField = typeof(ThirdPersonMovement).GetField("_move", Private);
    private static readonly FieldInfo InvulnerableField = typeof(PlayerCombat).GetField("_invulnerableUntil", Private);
    private const int EnemyCampaignLevel = 26;

    private static bool _withEnemies;
    private static int _savedPlayed;
    private static PlayerCombat _combat;

    private static Queue<int> _queue;
    private static LevelManager _levels;
    private static Transform _player;
    private static ThirdPersonMovement _movement;
    private static EdgeNetwork _network;
    private static Transform _level;
    private static DrawEdge _target;
    private static float _goalT, _entryT, _botT;
    private static double _levelStart;
    private static readonly StringBuilder Report = new();

    [MenuItem("DrawRush/Dev/Autoplay Levels")]
    private static void Run() => Run(null);

    [MenuItem("DrawRush/Dev/Autoplay Levels with Enemies")]
    private static void RunWithEnemies() => Run(null, true);

    /// <summary>Autoplay the given level indices (null = every level except the tutorial).</summary>
    public static void Run(IEnumerable<int> indices) => Run(indices, false);

    /// <summary>Autoplay; <paramref name="withEnemies"/> keeps every enemy on (bot invulnerable).</summary>
    public static void Run(IEnumerable<int> indices, bool withEnemies)
    {
        _withEnemies = withEnemies;
        if (!EditorApplication.isPlaying) { Debug.LogWarning("[Autoplay] Enter Play mode first."); return; }
        _levels = Object.FindFirstObjectByType<LevelManager>();
        _player = GameServices.Player;
        if (_levels == null || _player == null) { Debug.LogWarning("[Autoplay] No LevelManager/Player."); return; }
        _movement = _player.GetComponent<ThirdPersonMovement>();
        _combat = _player.GetComponent<PlayerCombat>();
        if (_withEnemies) { _savedPlayed = PlayerProgress.LevelsPlayed; PlayerProgress.LevelsPlayed = EnemyCampaignLevel; }

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
        if (_withEnemies) LevelDifficulty.ApplyEnemyBudget(_level, LevelDifficulty.AvailableEnemies(_level));
        else
            foreach (EnemyFollow enemy in _level.GetComponentsInChildren<EnemyFollow>(true))
                enemy.gameObject.SetActive(false);
        _target = null;
        _levelStart = EditorApplication.timeSinceStartup;
    }

    private static void Stop()
    {
        EditorApplication.update -= Tick;
        if (_movement != null) MoveField.SetValue(_movement, Vector2.zero);
        if (_withEnemies) { PlayerProgress.LevelsPlayed = _savedPlayed; _withEnemies = false; }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || _network == null) { Stop(); return; }
        Time.timeScale = 1f;   // skip the 3-2-1 countdown
        if (_withEnemies && _combat != null) InvulnerableField.SetValue(_combat, Time.time + 1f);

        IReadOnlyList<DrawEdge> edges = _network.Edges;
        int done = 0;
        foreach (DrawEdge e in edges) if (e.IsComplete) done++;

        double elapsed = EditorApplication.timeSinceStartup - _levelStart;
        var state = GameServices.State;
        bool won = state != null && state.IsGameWon;
        if (won || elapsed > LevelTimeout)
        {
            string line = $"{_level.name} {done}/{edges.Count} cov={_network.Coverage:P0} t={elapsed:F1} {(won ? "PASS" : "FAIL")}";
            Report.AppendLine(line);
            Debug.Log("[Autoplay] " + line);
            MoveField.SetValue(_movement, Vector2.zero);
            if (_queue.Count > 0) StartNext();
            else { Stop(); Debug.Log("[Autoplay] DONE\n" + Report); }
            return;
        }

        Vector3 pos = _player.position;
        if (_target == null || _target.IsComplete || !GapStillOpen()) PickTarget(edges, pos);
        if (_target == null) { MoveField.SetValue(_movement, Vector2.zero); return; }

        float len = Mathf.Max(0.01f, _target.Length);
        // Track progress locally around the last known t: on loop-like edges (flower petals) both
        // ends sit side by side, and a global nearest-point query flips to the wrong end.
        float tNow = LocalT(_target, pos, _botT, 1.5f / len, out float dist);
        _botT = tNow;
        Vector3 aim;
        if (dist > OnLine) aim = _target.PointAt(_entryT);               // walk onto the gap
        else
        {
            float sign = Mathf.Sign(_goalT - _entryT);
            float ahead = Mathf.Clamp01(tNow + sign * Lookahead / len);    // pure pursuit along the edge
            if ((ahead - _goalT) * sign > 0f) ahead = _goalT;
            aim = _target.PointAt(ahead);
        }
        Vector3 dir = aim - pos; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0004f) dir = _target.TangentAt(tNow) * Mathf.Sign(_goalT - _entryT);

        Transform cam = GameServices.MainCamera;
        float camY = cam != null ? cam.eulerAngles.y : 0f;
        float angle = (Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - camY) * Mathf.Deg2Rad;
        MoveField.SetValue(_movement, new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)));
    }

    // Nearest unpainted stretch on any incomplete edge; enter it at its nearer end.
    private static void PickTarget(IReadOnlyList<DrawEdge> edges, Vector3 pos)
    {
        _target = null;
        float best = float.PositiveInfinity;
        foreach (DrawEdge e in edges)
        {
            if (e.IsComplete) continue;
            float cursor = 0f;
            var spans = e.Fill.Spans;
            for (int i = 0; i <= spans.Count; i++)
            {
                float g0 = cursor, g1 = i < spans.Count ? spans[i].x : 1f;
                if (i < spans.Count) cursor = Mathf.Max(cursor, spans[i].y);
                if (g1 - g0 < 0.001f) continue;
                float d0 = (e.PointAt(g0) - pos).sqrMagnitude, d1 = (e.PointAt(g1) - pos).sqrMagnitude;
                float d = Mathf.Min(d0, d1);
                if (d >= best) continue;
                best = d;
                _target = e;
                _entryT = d0 <= d1 ? g0 : g1;
                _goalT = d0 <= d1 ? g1 : g0;
                _botT = _entryT;
            }
        }
    }

    // Nearest point on the edge within ±window (t units) of around, by sampling.
    private static float LocalT(DrawEdge e, Vector3 pos, float around, float window, out float dist)
    {
        float best = around, bestSqr = float.PositiveInfinity;
        for (int i = 0; i <= 30; i++)
        {
            float t = Mathf.Clamp01(around + Mathf.Lerp(-window, window, i / 30f));
            Vector3 d = e.PointAt(t) - pos; d.y = 0f;
            if (d.sqrMagnitude < bestSqr) { bestSqr = d.sqrMagnitude; best = t; }
        }
        dist = Mathf.Sqrt(bestSqr);
        return best;
    }

    // The stretch being walked is still (partly) unpainted around the goal.
    private static bool GapStillOpen()
    {
        float probe = Mathf.Lerp(_entryT, _goalT, 0.98f);
        foreach (Vector2 s in _target.Fill.Spans) if (probe >= s.x && probe <= s.y) return false;
        return true;
    }
}
