using System.Collections.Generic;
using UnityEngine;

namespace DrawRush.UI
{
    /// <summary>
    /// Keeps a full-screen panel's edge-anchored children out of the phone's notch / rounded
    /// corners / gesture bar. Children anchored to the top are pushed down by the top inset, those
    /// anchored to the bottom are pushed up by the bottom inset; centred and stretched children
    /// (backgrounds, dims) are left alone so they still cover the whole screen.
    /// Re-applies when the safe area or resolution changes (rotation, split screen).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafeAreaPadding : MonoBehaviour
    {
        private readonly List<RectTransform> _children = new();
        private readonly List<Vector2> _basePositions = new();
        private Canvas _canvas;
        private Rect _applied;
        private Vector2Int _appliedScreen;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            foreach (Transform child in transform)
            {
                if (child is not RectTransform rt) continue;
                bool top = rt.anchorMin.y >= 0.999f && rt.anchorMax.y >= 0.999f;
                bool bottom = rt.anchorMin.y <= 0.001f && rt.anchorMax.y <= 0.001f;
                if (!top && !bottom) continue;
                _children.Add(rt);
                _basePositions.Add(rt.anchoredPosition);
            }
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != _applied || Screen.width != _appliedScreen.x || Screen.height != _appliedScreen.y)
                Apply();
        }

        private void Apply()
        {
            _applied = Screen.safeArea;
            _appliedScreen = new Vector2Int(Screen.width, Screen.height);
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            float topInset = (Screen.height - _applied.yMax) / scale;
            float bottomInset = _applied.yMin / scale;

            for (int i = 0; i < _children.Count; i++)
            {
                RectTransform rt = _children[i];
                if (rt == null) continue;
                bool top = rt.anchorMin.y >= 0.999f;
                rt.anchoredPosition = _basePositions[i] + new Vector2(0f, top ? -topInset : bottomInset);
            }
        }
    }
}
