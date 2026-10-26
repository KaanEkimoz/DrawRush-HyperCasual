#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Development-build-only FPS readout (top-left), so frame rate can be checked on a
    /// device without the profiler. Spawns itself after the first scene load; compiled out
    /// of release builds.
    /// </summary>
    public sealed class DevFpsOverlay : MonoBehaviour
    {
        private const float SampleWindow = 0.5f;

        private int _frames;
        private float _elapsed;
        private float _worstFrame;
        private string _text = "";
        private GUIStyle _style;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Spawn()
        {
            var go = new GameObject(nameof(DevFpsOverlay));
            DontDestroyOnLoad(go);
            go.AddComponent<DevFpsOverlay>();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _frames++;
            _elapsed += dt;
            if (dt > _worstFrame) _worstFrame = dt;
            if (_elapsed < SampleWindow) return;
            _text = $"{_frames / _elapsed:F0} fps  worst {_worstFrame * 1000f:F0} ms  target {Application.targetFrameRate}";
            _frames = 0;
            _elapsed = 0f;
            _worstFrame = 0f;
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.018f) };
            GUI.Label(new Rect(16f, 16f + Screen.height - Screen.safeArea.yMax, Screen.width, _style.fontSize * 2f), _text, _style);
        }
    }
}
#endif
