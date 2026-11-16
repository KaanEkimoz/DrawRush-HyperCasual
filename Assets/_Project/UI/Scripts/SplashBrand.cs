using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// The Ekimoz Games intro, told in the game's own language: on a flat colour (the same colour
    /// the OS launch screen shows, so there is no jump), a dotted outline appears, the pencil hero
    /// runs around it painting the line, the loop closes with a confetti pop, then EKIMOZ bounces
    /// in letter by letter and GAMES slides up. A short hold, then everything fades back to the
    /// flat colour before the game scene loads. Unscaled time throughout.
    /// </summary>
    public sealed class SplashBrand : MonoBehaviour
    {
        [Tooltip("Logo root (holds the title + subtitle). Built elements are added under it.")]
        [SerializeField] private RectTransform logo;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [Tooltip("The pencil hero that runs the outline.")]
        [SerializeField] private Sprite heroSprite;
        [SerializeField] private Vector2 loopSize = new(800f, 520f);
        [SerializeField] private float cornerRadius = 120f;
        [SerializeField] private Color guideColor = new(0.13f, 0.17f, 0.35f, 0.3f);
        [SerializeField] private Color strokeColor = new(0.92f, 0.27f, 0.27f, 1f);

        // Timeline (seconds).
        private const float GuideIn = 0.15f, DrawStart = 0.45f, DrawEnd = 1.3f;
        private const float LettersStart = 1.3f, LetterGap = 0.07f, LetterPop = 0.32f;
        private const float SubIn = 1.75f, FadeOut = 2.4f, FadeLen = 0.3f;

        /// <summary>Total length; the scene timer should wait at least this long.</summary>
        public const float Duration = FadeOut + FadeLen;

        private UiStroke _guide, _stroke;
        private RectTransform _hero;
        private Image _heroImg;
        private CanvasGroup _group;
        private readonly List<(RectTransform rt, Vector2 vel, float spin)> _confetti = new();
        private float _t;
        private bool _burst;
        private Vector2 _subtitleHome;

        private void Awake()
        {
            if (logo == null) logo = (RectTransform)transform;
            _group = logo.GetComponent<CanvasGroup>();
            if (_group == null) _group = logo.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;

            _stroke = NewStroke("Stroke", strokeColor, 26f);
            _guide = NewStroke("Guide", guideColor, 12f);   // made last = drawn first, under the stroke
            _guide.Dash = 26f; _guide.Gap = 22f;
            _stroke.Progress = 0f;
            _guide.color = new Color(guideColor.r, guideColor.g, guideColor.b, 0f);

            if (heroSprite != null)
            {
                var go = new GameObject("Hero", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(logo, false);
                _hero = (RectTransform)go.transform;
                _hero.sizeDelta = new Vector2(190f, 190f);
                _heroImg = go.GetComponent<Image>();
                _heroImg.sprite = heroSprite; _heroImg.preserveAspect = true; _heroImg.raycastTarget = false;
                _heroImg.color = new Color(1f, 1f, 1f, 0f);
            }

            if (title != null) { title.ForceMeshUpdate(); title.alpha = 1f; HideLetters(); }
            if (subtitle != null) { _subtitleHome = subtitle.rectTransform.anchoredPosition; subtitle.alpha = 0f; }
        }

        private UiStroke NewStroke(string name, Color c, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UiStroke));
            go.transform.SetParent(logo, false);
            go.transform.SetAsFirstSibling();
            var s = go.GetComponent<UiStroke>();
            s.raycastTarget = false; s.color = c; s.Width = width; s.Closed = true;
            s.Points = RoundedRect(loopSize, cornerRadius);
            return s;
        }

        // Clockwise from the bottom-left corner's end, so the hero starts at the lower left.
        private static List<Vector2> RoundedRect(Vector2 size, float r)
        {
            var pts = new List<Vector2>();
            float hx = size.x * 0.5f, hy = size.y * 0.5f;
            Vector2[] centres = { new(-hx + r, hy - r), new(hx - r, hy - r), new(hx - r, -hy + r), new(-hx + r, -hy + r) };
            float[] start = { 180f, 90f, 0f, 270f };
            const int steps = 10;
            for (int c = 0; c < 4; c++)
                for (int k = 0; k <= steps; k++)
                {
                    float a = (start[c] - 90f * k / steps) * Mathf.Deg2Rad;
                    pts.Add(centres[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            // Rotate so the path starts at the left side's lower end.
            int shift = pts.Count - (steps + 1);
            var rotated = new List<Vector2>(pts.Count);
            for (int i = 0; i < pts.Count; i++) rotated.Add(pts[(i + shift) % pts.Count]);
            return rotated;
        }

        private void Update()
        {
            _t += Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);   // no jump on a slow first frame
            float t = _t;

            // Dotted guide fades in.
            float g = Mathf.Clamp01((t - GuideIn) / 0.25f);
            _guide.color = new Color(guideColor.r, guideColor.g, guideColor.b, guideColor.a * g);

            // The hero paints the loop.
            float d = Mathf.Clamp01((t - DrawStart) / (DrawEnd - DrawStart));
            float eased = d < 1f ? Smooth(d) : 1f;
            _stroke.Progress = eased;
            _stroke.Redraw();
            if (_hero != null)
            {
                Vector2 p = _stroke.PointAt(eased, out Vector2 dir);
                float hop = Mathf.Abs(Mathf.Sin(t * 22f)) * 12f;                   // running bob
                _hero.anchoredPosition = p + new Vector2(0f, 70f + hop);
                _hero.localScale = new Vector3(dir.x < -0.1f ? -1f : 1f, 1f, 1f);
                float heroIn = Mathf.Clamp01((t - (DrawStart - 0.15f)) / 0.15f);
                float heroOut = 1f - Mathf.Clamp01((t - DrawEnd) / 0.2f);
                _heroImg.color = new Color(1f, 1f, 1f, heroIn * heroOut);
                if (t > DrawEnd) _hero.anchoredPosition += new Vector2(0f, (t - DrawEnd) * 900f);   // leaps away
            }

            // Loop closes: the line flashes and confetti pops.
            if (!_burst && t >= DrawEnd) { _burst = true; Burst(); }
            float flash = t >= DrawEnd ? 1f - Mathf.Clamp01((t - DrawEnd) / 0.25f) : 0f;
            _stroke.color = Color.Lerp(strokeColor, new Color(1f, 0.82f, 0.2f, 1f), flash);   // a golden flash
            _stroke.Width = 26f + 10f * flash;
            UpdateConfetti();

            Letters(t);

            if (subtitle != null)
            {
                float s = Mathf.Clamp01((t - SubIn) / 0.3f);
                subtitle.alpha = s;
                subtitle.rectTransform.anchoredPosition = new Vector2(_subtitleHome.x, _subtitleHome.y - 40f * (1f - Smooth(s)));
            }

            // Fade back to the flat colour.
            _group.alpha = 1f - Mathf.Clamp01((t - FadeOut) / FadeLen);
        }

        // ---------- letters ----------

        private void HideLetters()
        {
            TMP_TextInfo info = title.textInfo;
            for (int i = 0; i < info.characterCount; i++) ScaleChar(info, i, 0f, 0f);
            title.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        private void Letters(float t)
        {
            if (title == null) return;
            title.ForceMeshUpdate();
            TMP_TextInfo info = title.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                float k = Mathf.Clamp01((t - LettersStart - i * LetterGap) / LetterPop);
                float s = k <= 0f ? 0f : BackOut(k);
                ScaleChar(info, i, s, Mathf.Clamp01(k * 3f));
            }
            title.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        private static void ScaleChar(TMP_TextInfo info, int i, float scale, float alpha)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) return;
            int m = c.materialReferenceIndex, v = c.vertexIndex;
            Vector3[] verts = info.meshInfo[m].vertices;
            Color32[] cols = info.meshInfo[m].colors32;
            Vector3 centre = (verts[v] + verts[v + 2]) * 0.5f;
            Vector3 pivot = new Vector3(centre.x, verts[v].y, 0f);   // grow from the baseline
            for (int j = 0; j < 4; j++)
            {
                verts[v + j] = pivot + (verts[v + j] - pivot) * scale;
                cols[v + j].a = (byte)(alpha * 255);
            }
        }

        // ---------- confetti ----------

        private void Burst()
        {
            Color[] palette =
            {
                new(1f, 0.82f, 0.2f), new(0.95f, 0.35f, 0.35f), new(0.35f, 0.7f, 1f),
                new(0.4f, 0.85f, 0.45f), new(0.65f, 0.5f, 1f), Color.white,
            };
            for (int i = 0; i < 46; i++)
            {
                var go = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(logo, false);
                var rt = (RectTransform)go.transform;
                Vector2 at = _stroke.PointAt(Random.value, out Vector2 dir);
                rt.anchoredPosition = at;
                rt.sizeDelta = new Vector2(Random.Range(14f, 24f), Random.Range(8f, 14f));
                var img = go.GetComponent<Image>();
                img.color = palette[i % palette.Length]; img.raycastTarget = false;
                Vector2 outward = at.sqrMagnitude > 1f ? at.normalized : Random.insideUnitCircle.normalized;
                _confetti.Add((rt, outward * Random.Range(420f, 900f) + new Vector2(0f, 300f), Random.Range(-720f, 720f)));
            }
        }

        private void UpdateConfetti()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
            for (int i = 0; i < _confetti.Count; i++)
            {
                var (rt, vel, spin) = _confetti[i];
                vel += new Vector2(0f, -1800f) * dt;
                vel *= 1f - 1.6f * dt;
                rt.anchoredPosition += vel * dt;
                rt.localRotation = Quaternion.Euler(0f, 0f, rt.localEulerAngles.z + spin * dt);
                _confetti[i] = (rt, vel, spin);
            }
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);

        private static float BackOut(float x)
        {
            const float c1 = 1.9f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
