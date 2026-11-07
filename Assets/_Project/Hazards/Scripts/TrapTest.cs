#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace DrawRush.Hazards
{
    /// <summary>
    /// Trap test level (editor only): menu DrawRush → Tuzak Test Leveli opens the game scene and
    /// plays it with every trap kind on the shape, no enemies and no desk accidents. Lasts until
    /// Play mode ends. Always off in a build.
    /// </summary>
    public static class TrapTest
    {
        private const string Key = "DrawRush.TrapTest";
        private const string ResumeKey = "DrawRush.TrapTest.LastLevel";   // the save it may move

#if UNITY_EDITOR
        public static bool Active => SessionState.GetBool(Key, false);

        [MenuItem("DrawRush/Tuzak Test Leveli")]
        private static void Begin()
        {
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            const string scene = "Assets/Scenes/01_DrawRushGame.unity";
            if (EditorSceneManager.GetActiveScene().path != scene)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(scene);
            }
            SessionState.SetInt(ResumeKey, DrawRush.Core.PlayerProgress.LastLevelIndex);
            SessionState.SetBool(Key, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode || !Active) return;
            // Looking for a shape with a straight edge moves the resume point: put it back.
            DrawRush.Core.PlayerProgress.LastLevelIndex = SessionState.GetInt(ResumeKey, DrawRush.Core.PlayerProgress.LastLevelIndex);
            SessionState.SetBool(Key, false);
        }
#else
        public static bool Active => false;
#endif
    }
}
