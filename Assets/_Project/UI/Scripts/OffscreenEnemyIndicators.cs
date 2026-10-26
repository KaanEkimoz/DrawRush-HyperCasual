using DrawRush.Core;
using DrawRush.Enemy;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// Pins a small arrow to the screen edge for each nearby enemy that is off screen, pointing
    /// at it and fading with distance. Arrows are pooled; an enemy back on screen hides its arrow.
    /// Put on a full-screen RectTransform under a Screen Space Overlay canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class OffscreenEnemyIndicators : MonoBehaviour
    {
        [SerializeField] private EnemyProximity enemies;
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Color arrowColor = new(1f, 0.3f, 0.3f, 1f);
        [Tooltip("Arrow size in canvas units.")]
        [SerializeField] private float arrowSize = 56f;
        [Tooltip("Only enemies within this ground distance (m) of the player get an arrow.")]
        [SerializeField] private float showRadius = 14f;
        [Tooltip("Gap from the safe-area edge, in canvas units.")]
        [SerializeField] private float edgeInset = 48f;
        [SerializeField, Range(1, 8)] private int maxArrows = 4;

        private RectTransform _rect;
        private Image[] _arrows;
        private Transform[] _near;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _near = new Transform[maxArrows];
            _arrows = new Image[maxArrows];
            for (int i = 0; i < maxArrows; i++)
            {
                var go = new GameObject("EnemyArrow" + i, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var img = go.GetComponent<Image>();
                img.sprite = arrowSprite;
                img.raycastTarget = false;
                img.rectTransform.sizeDelta = Vector2.one * arrowSize;
                go.SetActive(false);
                _arrows[i] = img;
            }
        }

        private void LateUpdate()
        {
            UnityEngine.Camera cam = UnityEngine.Camera.main;
            Transform player = GameServices.Player;
            int count = enemies != null && player != null && cam != null
                ? enemies.Nearest(player.position, showRadius, _near, maxArrows) : 0;

            Rect safe = SafeRectInCanvas();
            for (int i = 0; i < maxArrows; i++)
            {
                Image arrow = _arrows[i];
                if (i >= count || !Place(arrow, _near[i], cam, player, safe)) arrow.gameObject.SetActive(false);
            }
        }

        private bool Place(Image arrow, Transform enemy, UnityEngine.Camera cam, Transform player, Rect safe)
        {
            Vector3 vp = cam.WorldToViewportPoint(enemy.position);
            bool onScreen = vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
            if (onScreen) return false;

            // Behind the camera the projection mirrors; flip so the arrow still points the right way.
            Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (vp.z < 0f) dir = -dir;
            if (dir.sqrMagnitude < 1e-6f) return false;

            // Ray from the safe-area centre to its edge along dir.
            Vector2 c = safe.center, half = safe.size * 0.5f - Vector2.one * edgeInset;
            float t = Mathf.Min(Mathf.Abs(half.x / Mathf.Max(1e-4f, Mathf.Abs(dir.x))), Mathf.Abs(half.y / Mathf.Max(1e-4f, Mathf.Abs(dir.y))));
            Vector2 pos = c + dir * t;

            Vector3 d = enemy.position - player.position; d.y = 0f;
            float closeness = 1f - Mathf.Clamp01(d.magnitude / showRadius);
            var col = arrowColor; col.a *= Mathf.Lerp(0.35f, 1f, closeness);

            arrow.gameObject.SetActive(true);
            arrow.color = col;
            arrow.rectTransform.anchoredPosition = pos;
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);   // sprite points up
            arrow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.15f, closeness);
            return true;
        }

        // Screen.safeArea mapped into this rect's local space (anchoredPosition frame, centre pivot).
        private Rect SafeRectInCanvas()
        {
            Rect s = Screen.safeArea, r = _rect.rect;
            float sx = r.width / Mathf.Max(1, Screen.width), sy = r.height / Mathf.Max(1, Screen.height);
            return new Rect(r.xMin + s.xMin * sx, r.yMin + s.yMin * sy, s.width * sx, s.height * sy);
        }
    }
}
