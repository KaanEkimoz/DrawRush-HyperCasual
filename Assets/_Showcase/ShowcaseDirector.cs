// Showcase tool (editor play-mode only). Drives an automated "levels drawing themselves"
// montage and records it with Unity Recorder to an MP4. Uses reflection for both the gameplay
// types (EdgeNetwork/LevelManager/DrawEdgeAuthor) and the Recorder API so it compiles in plain
// Assembly-CSharp with no asmdef/editor-assembly references. Attach to a GameObject, press Play.
// NOT shipped — delete the _Showcase folder when done.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShowcaseDirector : MonoBehaviour
{
    [Tooltip("Curated level indices, in play order.")]
    public int[] levels = { 18, 12, 16, 23, 15, 22, 17, 24, 32, 33, 25, 34 };
    public float perEdgeDelay = 0.20f;   // seconds between each wall appearing
    public float preRevealHold = 0.45f;  // pause after activating a level (corner posts settle)
    public float postRevealHold = 1.30f; // pause to admire the finished shape
    public int outWidth = 1080, outHeight = 1920;
    public float frameRate = 30f;
    public string outputName = "showcase";

    private object _controller;     // RecorderController
    private MethodInfo _stop;
    [System.NonSerialized] public bool gameplayMode; // true = passively record real gameplay
    [System.NonSerialized] public bool playlistMode; // true = drive a custom level order for live play
    // Custom play order for the gameplay clip: easy(square) -> heart -> smiley(emoji) -> star.
    public int[] playlist = { 1, 16, 25, 12 };

    private static readonly BindingFlags FL =
        BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

#if UNITY_EDITOR
    // Auto-spawns the director on Play ONLY when a gate pref is set (one-shot), so it survives
    // domain reloads / unsaved-scene reverts without needing a serialized scene object.
    // DR_RunShowcase = auto wall-draw montage; DR_RunGameplayRec = passive gameplay capture.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        bool show = UnityEditor.EditorPrefs.GetBool("DR_RunShowcase", false);
        bool game = UnityEditor.EditorPrefs.GetBool("DR_RunGameplayRec", false);
        bool plist = UnityEditor.EditorPrefs.GetBool("DR_RunGameplayPlaylist", false);
        if (!show && !game && !plist) return;
        UnityEditor.EditorPrefs.SetBool("DR_RunShowcase", false);
        UnityEditor.EditorPrefs.SetBool("DR_RunGameplayRec", false);
        UnityEditor.EditorPrefs.SetBool("DR_RunGameplayPlaylist", false);
        var go = new GameObject("ShowcaseDirector_GO");
        var d = go.AddComponent<ShowcaseDirector>();
        if (plist && !show && !game) d.playlistMode = true;          // drive level order, recorder window captures
        else if (game && !show) { d.gameplayMode = true; d.outputName = "gameplay"; }
    }

    // Exiting Play fires OnApplicationQuit — finalize the passive gameplay recording here.
    private void OnApplicationQuit()
    {
        if (gameplayMode) StopRecorder();
    }
#endif

    private IEnumerator Start()
    {
#if UNITY_EDITOR
        yield return null; // let the scene settle one frame

        if (playlistMode)
        {
            // Live gameplay in a custom order. Recorder WINDOW captures; we only drive levels.
            yield return RunPlaylist();
            yield break;
        }

        if (gameplayMode)
        {
            // Passive mode: just record the real game. No camera override, no hiding, no montage.
            if (!StartRecorder())
                Debug.LogError("[Gameplay] Recorder failed to start.");
            else
                Debug.Log("[Gameplay] Recording your play session — press Stop (exit Play) to finish.");
            yield break;
        }

        SetupCamera();
        HidePlayer();
        if (!StartRecorder())
        {
            Debug.LogError("[Showcase] Recorder failed to start. Aborting.");
            yield break;
        }
        yield return new WaitForSeconds(0.4f);

        object lm = FindByTypeName("LevelManager");
        MethodInfo activate = lm.GetType().GetMethod("ActivateLevel");

        foreach (int idx in levels)
        {
            activate.Invoke(lm, new object[] { idx });
            DisableActiveEnemies();
            yield return new WaitForSeconds(preRevealHold);
            yield return RevealActiveLevel();
            yield return new WaitForSeconds(postRevealHold);
        }

        yield return new WaitForSeconds(0.4f);
        StopRecorder();
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[Showcase] DONE — recording saved.");
        EditorApplication.ExitPlaymode();
#else
        yield break;
#endif
    }

    // ---- camera ---------------------------------------------------------------
    private void SetupCamera()
    {
        // Disable every existing camera so only the showcase cam renders (Game view + Recorder).
        foreach (Camera c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            c.enabled = false;

        var go = new GameObject("ShowcaseCam");
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.36f, 0.78f, 0.30f); // grassy backdrop
        cam.fieldOfView = 44f;
        cam.depth = 1000f;
        cam.transform.position = new Vector3(1.79f, 13.2f, -0.47f - 7.4f);
        cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
    }

    // ---- reveal ---------------------------------------------------------------
    private IEnumerator RevealActiveLevel()
    {
        object en = FindActiveEdgeNetwork();
        if (en == null) yield break;
        Type t = en.GetType();

        var edges = t.GetProperty("Edges").GetValue(en) as IEnumerable;
        var authors = t.GetField("_authors", FL).GetValue(en) as IDictionary;
        var interiors = t.GetField("_edgeInterior", FL).GetValue(en) as IDictionary;
        MethodInfo cornerEndFor = t.GetMethod("CornerEndFor", FL);

        foreach (object edge in edges)
        {
            Type et = edge.GetType();
            object a = et.GetProperty("A").GetValue(edge);
            object b = et.GetProperty("B").GetValue(edge);
            var endA = (Vector3)cornerEndFor.Invoke(en, new[] { edge, a });
            var endB = (Vector3)cornerEndFor.Invoke(en, new[] { edge, b });
            Vector3 interior = interiors.Contains(edge) ? (Vector3)interiors[edge] : Vector3.zero;
            object author = authors[edge];
            author.GetType().GetMethod("Reveal")
                .Invoke(author, new object[] { edge, interior, endA, endB });
            yield return new WaitForSeconds(perEdgeDelay);
        }

        // Rise every corner post for this level.
        var corners = t.GetField("_corners", FL).GetValue(en) as IEnumerable;
        Type cornerType = t.GetNestedType("Corner", BindingFlags.NonPublic);
        FieldInfo postF = cornerType.GetField("Post", FL);
        FieldInfo revF = cornerType.GetField("Revealed", FL);
        foreach (object c in corners)
        {
            object post = postF.GetValue(c);
            if (post == null) continue;
            revF.SetValue(c, true);
            ((Component)post).GetType().GetMethod("Reveal").Invoke(post, null);
        }
    }

    private object FindActiveEdgeNetwork()
    {
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb.GetType().Name == "EdgeNetwork") return mb;
        return null;
    }

    // Walk the custom playlist: activate each level, wait until the PLAYER finishes drawing it
    // (EdgeNetwork.IsComplete), hold a moment for the win juice, then advance. The recorder
    // window is doing the actual capture — this only sequences levels for a tidy clip.
    private IEnumerator RunPlaylist()
    {
        object lm = FindByTypeName("LevelManager");
        if (lm == null) { Debug.LogError("[Playlist] No LevelManager."); yield break; }
        MethodInfo activate = lm.GetType().GetMethod("ActivateLevel");

        for (int i = 0; i < playlist.Length; i++)
        {
            activate.Invoke(lm, new object[] { playlist[i] });
            Debug.Log("[Playlist] Level " + playlist[i] + " (" + (i + 1) + "/" + playlist.Length + ") — draw it!");
            yield return new WaitForSeconds(1.2f);   // countdown grace; avoids reading stale state
            yield return WaitLevelComplete();
            yield return new WaitForSeconds(2.0f);    // admire the win
        }
        Debug.Log("[Playlist] DONE — all levels played. Press Stop in Recorder to finish.");
    }

    private IEnumerator WaitLevelComplete()
    {
        while (true)
        {
            object en = FindActiveEdgeNetwork();
            if (en != null)
            {
                var p = en.GetType().GetProperty("IsComplete");
                if (p != null && (bool)p.GetValue(en)) yield break;
            }
            yield return null;
        }
    }

    private object FindByTypeName(string name)
    {
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (mb.GetType().Name == name) return mb;
        return null;
    }

    // Hide the player visual + rail so the shot focuses on the shapes (clean showcase).
    private void HidePlayer()
    {
        // Deactivate ONLY the Player GameObject (child of ===SHARED===) — NOT transform.root,
        // which is ===SHARED=== itself and would also kill LevelManager/Camera.
        var rpc = FindByTypeName("RailPaintController") as Component;
        if (rpc != null) rpc.gameObject.SetActive(false);
    }

    // Deactivate the active level's enemies so they don't chase/kill during the montage.
    private void DisableActiveEnemies()
    {
        object en = FindActiveEdgeNetwork();
        if (en == null) return;
        // EdgeNetwork lives under the level; walk up to the level root (child of ===LEVELS===).
        Transform t = ((Component)en).transform;
        while (t.parent != null && !t.parent.name.Contains("LEVELS")) t = t.parent;
        Transform enemies = t.Find("Enemies");
        if (enemies != null) enemies.gameObject.SetActive(false);
    }

    // ---- recorder (via reflection) -------------------------------------------
    private static Type FT(string fullName)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }
    private static void SP(object o, string prop, object val)
    {
        var p = o.GetType().GetProperty(prop);
        p.SetValue(o, val);
    }

    private bool StartRecorder()
    {
#if UNITY_EDITOR
        try
        {
            Type tRCS = FT("UnityEditor.Recorder.RecorderControllerSettings");
            Type tMovie = FT("UnityEditor.Recorder.MovieRecorderSettings");
            Type tEnc = FT("UnityEditor.Recorder.Encoder.CoreEncoderSettings");
            Type tGV = FT("UnityEditor.Recorder.Input.GameViewInputSettings");
            Type tRC = FT("UnityEditor.Recorder.RecorderController");
            if (tRCS == null || tMovie == null || tEnc == null || tGV == null || tRC == null)
            { Debug.LogError("[Showcase] Recorder types not found."); return false; }

            var rcs = ScriptableObject.CreateInstance(tRCS);
            tRCS.GetMethod("SetRecordModeToManual").Invoke(rcs, null);
            SP(rcs, "FrameRate", frameRate);
            SP(rcs, "CapFrameRate", true);
            SP(rcs, "ExitPlayMode", false);

            var movie = ScriptableObject.CreateInstance(tMovie);
            SP(movie, "name", "ShowcaseMovie");
            SP(movie, "Enabled", true);

            var enc = Activator.CreateInstance(tEnc);
            var codecP = tEnc.GetProperty("Codec");
            codecP.SetValue(enc, Enum.Parse(codecP.PropertyType, "MP4"));
            var qP = tEnc.GetProperty("EncodingQuality");
            qP.SetValue(enc, Enum.Parse(qP.PropertyType, "High"));
            SP(movie, "EncoderSettings", enc);

            var gv = Activator.CreateInstance(tGV);
            SP(gv, "OutputWidth", outWidth);
            SP(gv, "OutputHeight", outHeight);
            SP(movie, "ImageInputSettings", gv);

            string dir = System.IO.Path.Combine(Application.dataPath, "..", "Recordings");
            System.IO.Directory.CreateDirectory(dir);
            string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(dir, outputName));
            SP(movie, "OutputFile", outPath);

            tRCS.GetMethod("AddRecorderSettings").Invoke(rcs, new[] { movie });

            _controller = Activator.CreateInstance(tRC, new[] { rcs });
            tRC.GetMethod("PrepareRecording").Invoke(_controller, null);
            tRC.GetMethod("StartRecording").Invoke(_controller, null);
            _stop = tRC.GetMethod("StopRecording");
            Debug.Log("[Showcase] Recording -> " + outPath + ".mp4");
            return true;
        }
        catch (Exception e) { Debug.LogError("[Showcase] StartRecorder error: " + e); return false; }
#else
        return false;
#endif
    }

    private void StopRecorder()
    {
        try { if (_controller != null && _stop != null) _stop.Invoke(_controller, null); }
        catch (Exception e) { Debug.LogError("[Showcase] StopRecorder error: " + e); }
    }
}
