#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using DrawRush.Collection;
using DrawRush.Core;
using DrawRush.Drawing;
using UnityEditor;
using UnityEngine;

namespace DrawRush.UI
{
    /// <summary>
    /// Bakes the hand-drawn art that has to match the game exactly, so it is drawn here instead of
    /// generated: the corner drop, the buttons, the hero's eyes and one sticker per level, traced
    /// from that level's real edges so the collection shows the shape the player actually drew.
    /// Everything is crayon hatching under a wobbly navy pen line, like the concept art.
    /// </summary>
    public static class DoodleSpriteBaker
    {
        private const string UiFolder = "Assets/_Project/Art/Doodle/ui/";
        private static readonly Color Navy = new(0.11f, 0.13f, 0.38f);
        private static readonly Color[] Crayons =
        {
            Hex(0xF9C52E), Hex(0xF58A3C), Hex(0xEF5B5B), Hex(0xF27FB2),
            Hex(0x8E6BE0), Hex(0x4C9DFF), Hex(0x34C7B5), Hex(0x6FCF5F),
        };

        // Shapes that have an obvious colour; the rest walk the crayon box.
        private static readonly Dictionary<string, int> Natural = new()
        {
            { "Heart", 2 }, { "Apple", 2 }, { "Cherries", 2 }, { "Watermelon", 7 }, { "Pear", 7 },
            { "Clover", 7 }, { "Lemon", 0 }, { "Banana", 0 }, { "Star", 0 }, { "Moon", 0 },
            { "Lightning", 0 }, { "Smiley", 0 }, { "Grin", 0 }, { "Sad Face", 5 }, { "Wow Face", 3 },
            { "Drop", 5 }, { "Flower", 3 }, { "Gem", 6 }, { "Diamond", 6 },
        };

        private static readonly HashSet<string> HasOwnFace = new() { "Smiley", "Grin", "Sad Face", "Wow Face" };

        private static Color Hex(int v) => new(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);

        private sealed class Sheet
        {
            public readonly int W, H;
            public readonly Color[] Px;
            public Sheet(int w, int h) { W = w; H = h; Px = new Color[w * h]; }

            public void Blend(int x, int y, Color c, float a)
            {
                if (x < 0 || y < 0 || x >= W || y >= H || a <= 0f) return;
                ref Color d = ref Px[y * W + x];
                float o = a + d.a * (1f - a);
                if (o <= 0f) return;
                d = new Color((c.r * a + d.r * d.a * (1f - a)) / o, (c.g * a + d.g * d.a * (1f - a)) / o,
                              (c.b * a + d.b * d.a * (1f - a)) / o, o);
            }

            /// <summary>One round dab. <paramref name="grain"/> > 0 breaks it up like wax on paper.</summary>
            public void Dab(float x, float y, float r, Color c, float alpha, float grain, bool[] clip = null)
            {
                int x0 = Mathf.FloorToInt(x - r - 1), x1 = Mathf.CeilToInt(x + r + 1);
                int y0 = Mathf.FloorToInt(y - r - 1), y1 = Mathf.CeilToInt(y + r + 1);
                for (int py = y0; py <= y1; py++)
                for (int px = x0; px <= x1; px++)
                {
                    if (px < 0 || py < 0 || px >= W || py >= H) continue;
                    if (clip != null && !clip[py * W + px]) continue;
                    float a = Mathf.Clamp01(r + 0.5f - Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y)));
                    if (grain > 0f) a *= Mathf.SmoothStep(0.3f - grain * 0.3f, 0.3f + grain * 0.3f, Mathf.PerlinNoise(px * 0.31f, py * 0.31f));
                    Blend(px, py, c, a * alpha);
                }
            }

            /// <summary>A hand-drawn line: resampled, wobbled off its path, pressure varying.</summary>
            public void Stroke(IList<Vector2> pts, float r, Color c, float wobble, float seed, float alpha = 1f, float grain = 0f, bool[] clip = null)
            {
                float s = 0f;
                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    float len = Vector2.Distance(pts[i], pts[i + 1]);
                    int n = Mathf.Max(1, Mathf.CeilToInt(len));
                    for (int k = 0; k < n; k++, s += len / n)
                    {
                        Vector2 p = Vector2.Lerp(pts[i], pts[i + 1], k / (float)n);
                        p.x += (Mathf.PerlinNoise(s * 0.025f, seed) - 0.5f) * 2f * wobble;
                        p.y += (Mathf.PerlinNoise(seed, s * 0.025f) - 0.5f) * 2f * wobble;
                        Dab(p.x, p.y, r * Mathf.Lerp(0.8f, 1.15f, Mathf.PerlinNoise(s * 0.05f, seed + 9f)), c, alpha, grain, clip);
                    }
                }
            }

            public void Save(string path)
            {
                var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
                t.SetPixels(Px);
                t.Apply();
                File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
            }
        }

        private static List<Vector2> Circle(Vector2 c, float r, float turns = 1f, float start = 0f)
        {
            var pts = new List<Vector2>();
            int n = Mathf.CeilToInt(64 * turns);
            for (int i = 0; i <= n; i++)
            {
                float a = start + i / (float)n * turns * Mathf.PI * 2f;
                pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            return pts;
        }

        /// <summary>Crayon scribbled back and forth across the clipped area.</summary>
        private static void Hatch(Sheet s, bool[] clip, Color c, float angleDeg, float spacing, float r, float seed, float alpha = 1f)
        {
            Vector2 dir = new(Mathf.Cos(angleDeg * Mathf.Deg2Rad), Mathf.Sin(angleDeg * Mathf.Deg2Rad));
            Vector2 n = new(-dir.y, dir.x), mid = new(s.W * 0.5f, s.H * 0.5f);
            float reach = s.W + s.H;
            for (float o = -reach; o < reach; o += spacing)
                s.Stroke(new[] { mid + n * o - dir * reach, mid + n * (o + spacing * 0.5f) + dir * reach }, r, c, spacing * 0.3f, seed + o, alpha, 0.8f, clip);
        }

        private static float BoxSd(Vector2 p, Vector2 half, float round)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * round;
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - round;
        }

        private static List<Vector2> BoxPath(Vector2 c, Vector2 half, float round)
        {
            var pts = new List<Vector2>();
            Vector2[] corners = { new(half.x, half.y), new(-half.x, half.y), new(-half.x, -half.y), new(half.x, -half.y) };
            for (int q = 0; q < 4; q++)
            {
                Vector2 k = new(Mathf.Sign(corners[q].x) * (half.x - round), Mathf.Sign(corners[q].y) * (half.y - round));
                for (int i = 0; i <= 8; i++)
                {
                    float a = (q + i / 8f) * Mathf.PI * 0.5f;
                    pts.Add(c + k + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * round);
                }
            }
            pts.Add(pts[0]);
            return pts;
        }

        [MenuItem("DrawRush/Bake Doodle Sprites")]
        public static void BakeAll()
        {
            BakeDrop();
            BakeButton();
            BakeEyes();
            BakeDeadEyes();
            BakePaper();
            BakePencilStroke();
            BakeSoundIcons();
            BakeCoffeeStain();
            BakeCollection();
            AssetDatabase.Refresh();
        }

        private static void BakeDrop()
        {
            var s = new Sheet(256, 256);
            Vector2 c = new(128f, 128f);
            var inside = new bool[256 * 256];
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
                inside[y * 256 + x] = (new Vector2(x, y) - c).magnitude < 80f + 7f * Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
            for (int i = 0; i < inside.Length; i++) if (inside[i]) s.Blend(i % 256, i / 256, Color.white, 0.55f);
            Hatch(s, inside, Color.white, 35f, 9f, 6f, 1f);
            s.Stroke(Circle(c, 84f, 1.12f, 0.6f), 5f, Navy, 3.5f, 2f);
            s.Stroke(Circle(c, 87f, 1.05f, 2.4f), 2.2f, Navy, 4f, 3f, 0.8f);
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f + 0.2f;
                Vector2 d = new(Mathf.Cos(a), Mathf.Sin(a));
                s.Stroke(new[] { c + d * 100f, c + d * (i % 2 == 0 ? 118f : 110f) }, 3.4f, Navy, 1f, i);
            }
            s.Save(UiFolder + "drop_doodle.png");
        }

        private static void BakeButton()
        {
            BakeButton("button_doodle", 800, 280);
            BakeButton("button_wide", 1200, 240);
        }

        private static void BakeButton(string name, int w, int h)
        {
            var s = new Sheet(w, h);
            Vector2 c = new(w * 0.5f - 6f, h * 0.5f + 8f), half = new(w * 0.5f - 34f, h * 0.5f - 40f);
            var inside = new bool[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float n = (Mathf.PerlinNoise(x * 0.012f, y * 0.012f) - 0.5f) * 10f;
                if (BoxSd(new Vector2(x, y) - c, half, 46f) + n < 0f) inside[y * w + x] = true;
                if (BoxSd(new Vector2(x, y) - (c + new Vector2(11f, -14f)), half, 46f) + n < 0f) s.Blend(x, y, Navy, 1f);
            }
            for (int i = 0; i < inside.Length; i++) if (inside[i]) s.Blend(i % w, i / w, Color.white, 1f);
            Hatch(s, inside, new Color(0.8f, 0.8f, 0.8f), 62f, 15f, 3.2f, 4f, 0.8f);
            s.Stroke(BoxPath(c, half, 46f), 7f, Navy, 4f, 5f);
            s.Stroke(BoxPath(c + new Vector2(2f, -2f), half, 46f), 2.5f, Navy, 6f, 6f, 0.75f);
            s.Save(UiFolder + name + ".png");
        }

        private static void BakeEyes()
        {
            var s = new Sheet(256, 128);
            foreach (float cx in new[] { 72f, 184f })
            {
                Vector2 c = new(cx, 64f);
                for (float r = 46f; r > 0f; r -= 2f) s.Dab(c.x, c.y, 2f, Color.white, 1f, 0f);
                foreach (var p in Circle(c, 44f)) for (float r = 0f; r < 44f; r += 3f) s.Dab(Vector2.Lerp(c, p, r / 44f).x, Vector2.Lerp(c, p, r / 44f).y, 3f, Color.white, 1f, 0f);
                Vector2 pupil = c + new Vector2(cx < 128f ? 8f : -8f, -4f);
                foreach (var p in Circle(pupil, 20f)) for (float r = 0f; r < 20f; r += 2f) s.Dab(Vector2.Lerp(pupil, p, r / 20f).x, Vector2.Lerp(pupil, p, r / 20f).y, 2.5f, Navy, 1f, 0f);
                s.Dab(pupil.x + 7f, pupil.y + 8f, 6f, Color.white, 1f, 0f);
                s.Stroke(Circle(c, 46f, 1.08f, 1f), 4.5f, Navy, 2f, cx);
            }
            s.Save(UiFolder + "hero_eyes.png");
        }

        /// <summary>Ruled paper that repeats without a seam: every pattern here wraps at the edge.</summary>
        private static void BakePaper()
        {
            const int S = 1024, Rules = 6;
            var s = new Sheet(S, S);
            Color page = new(0.975f, 0.96f, 0.915f), rule = new(0.66f, 0.8f, 0.93f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = x * Mathf.PI * 2f / S, v = y * Mathf.PI * 2f / S;
                float fibre = Mathf.Sin(u * 7f + Mathf.Sin(v * 5f) * 2f) * Mathf.Sin(v * 11f + Mathf.Sin(u * 3f) * 2f)
                            + Mathf.Sin(u * 23f + v * 17f) * 0.5f;
                Color c = page * (1f + fibre * 0.006f);
                float toRule = Mathf.Abs(Mathf.Repeat(y, S / (float)Rules) - S / (float)Rules * 0.5f);
                c = Color.Lerp(c, rule, Mathf.Clamp01(2.2f - toRule) * 0.75f);
                c.a = 1f;
                s.Px[y * S + x] = c;
            }
            s.Save("Assets/_Project/Art/Doodle/paper_lined.png");
        }

        /// <summary>A short strip of graphite: dense in the middle, broken at the edges, wrapping
        /// along its length so it can run down a stroke of any length.</summary>
        private static void BakePencilStroke()
        {
            const int W = 256, H = 64;
            var s = new Sheet(W, H);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x * Mathf.PI * 2f / W;
                float across = 1f - Mathf.Abs(y - H * 0.5f + Mathf.Sin(u * 2f) * 3f) / (H * 0.5f);
                float grain = Mathf.PerlinNoise(Mathf.Cos(u) * 9f + 20f + y * 0.09f, Mathf.Sin(u) * 9f + 20f + y * 0.35f);
                float a = Mathf.SmoothStep(0f, 1f, across * 2.4f) * Mathf.SmoothStep(0.32f, 0.62f, grain + across * 0.2f);
                s.Px[y * W + x] = new Color(1f, 1f, 1f, a);
            }
            s.Save(UiFolder + "pencil_stroke.png");
        }

        /// <summary>A coffee ring with nothing around it, so it never covers the page's lines.</summary>
        private static void BakeCoffeeStain()
        {
            const int S = 512;
            var s = new Sheet(S, S);
            Color coffee = new(0.55f, 0.35f, 0.17f);
            Vector2 c = new(256f, 256f);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Vector2 d = new Vector2(x, y) - c;
                float r = d.magnitude + (Mathf.PerlinNoise(x * 0.012f, y * 0.012f) - 0.5f) * 26f;
                float ring = Mathf.Clamp01(1f - Mathf.Abs(r - 190f) / 16f);
                float wash = r < 190f ? 0.07f : 0f;
                float wear = Mathf.SmoothStep(0.25f, 0.6f, Mathf.PerlinNoise(x * 0.02f + 9f, y * 0.02f + 3f));
                s.Px[y * S + x] = new Color(coffee.r, coffee.g, coffee.b, Mathf.Clamp01(ring * ring * 0.5f * wear + wash));
            }
            s.Save(UiFolder + "decor_coffee.png");
        }

        private enum PaperRule { Lined, Grid, Dots, Plain }

        /// <summary>The desk papers levels cycle through (see DeskThemes). All light, so every
        /// paint colour and ghost line stays readable.</summary>
        [MenuItem("DrawRush/Bake Desk Papers")]
        public static void BakeDeskPapers()
        {
            BakeThemePaper("paper_grid", new Color(0.97f, 0.975f, 0.98f), new Color(0.62f, 0.76f, 0.9f), PaperRule.Grid);
            BakeThemePaper("paper_dots", new Color(0.985f, 0.95f, 0.87f), new Color(0.72f, 0.62f, 0.5f), PaperRule.Dots);
            BakeThemePaper("paper_pink", new Color(0.99f, 0.9f, 0.92f), new Color(0.93f, 0.62f, 0.72f), PaperRule.Lined);
            BakeThemePaper("paper_mint", new Color(0.87f, 0.97f, 0.91f), new Color(0.55f, 0.82f, 0.7f), PaperRule.Grid);
            BakeThemePaper("paper_legal", new Color(1f, 0.96f, 0.68f), new Color(0.6f, 0.72f, 0.88f), PaperRule.Lined);
            BakeThemePaper("paper_sky", new Color(0.84f, 0.92f, 1f), new Color(0.58f, 0.72f, 0.92f), PaperRule.Dots);
            BakeThemePaper("paper_kraft", new Color(0.87f, 0.76f, 0.6f), Color.white, PaperRule.Plain);
            AssetDatabase.Refresh();
        }

        // Same fibre as the ruled page; every pattern wraps at the edge so the tile has no seam.
        private static void BakeThemePaper(string name, Color page, Color rule, PaperRule kind)
        {
            const int S = 1024, Rules = 6;
            float cell = S / (float)Rules;
            var s = new Sheet(S, S);
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = x * Mathf.PI * 2f / S, v = y * Mathf.PI * 2f / S;
                float fibre = Mathf.Sin(u * 7f + Mathf.Sin(v * 5f) * 2f) * Mathf.Sin(v * 11f + Mathf.Sin(u * 3f) * 2f)
                            + Mathf.Sin(u * 23f + v * 17f) * 0.5f;
                Color c = page * (1f + fibre * (kind == PaperRule.Plain ? 0.03f : 0.006f));
                float toX = Mathf.Abs(Mathf.Repeat(x, cell) - cell * 0.5f), toY = Mathf.Abs(Mathf.Repeat(y, cell) - cell * 0.5f);
                float ink = kind switch
                {
                    PaperRule.Lined => Mathf.Clamp01(2.2f - toY),
                    PaperRule.Grid => Mathf.Max(Mathf.Clamp01(2.2f - toX), Mathf.Clamp01(2.2f - toY)),
                    PaperRule.Dots => Mathf.Clamp01(6f - new Vector2(toX, toY).magnitude),
                    _ => 0f,
                };
                c = Color.Lerp(c, rule, ink * 0.75f);
                c.a = 1f;
                s.Px[y * S + x] = c;
            }
            s.Save("Assets/_Project/Art/Doodle/" + name + ".png");
        }

        /// <summary>The settings gear, drawn like the sound and music buttons.</summary>
        [MenuItem("DrawRush/Bake Settings Icon")]
        public static void BakeSettingsIcon()
        {
            const int Teeth = 8;
            var s = new Sheet(256, 256);
            Vector2 c = new(124f, 132f);
            var disc = new bool[256 * 256];
            for (int i = 0; i < disc.Length; i++) disc[i] = (new Vector2(i % 256, i / 256) - c).magnitude < 104f;
            for (int i = 0; i < disc.Length; i++)
                if ((new Vector2(i % 256, i / 256) - c - new Vector2(6f, -8f)).magnitude < 110f) s.Blend(i % 256, i / 256, Navy, 1f);
            for (int i = 0; i < disc.Length; i++) if (disc[i]) s.Blend(i % 256, i / 256, Color.white, 1f);
            Hatch(s, disc, Crayons[0], 35f, 11f, 5f, 2f);
            s.Stroke(Circle(c, 106f, 1.04f, 1f), 5f, Navy, 3f, 8f);

            var gear = new List<Vector2>();
            for (int i = 0; i <= Teeth * 4; i++)
            {
                float a = i / (Teeth * 4f) * Mathf.PI * 2f + 0.2f;
                float r = (i + 1) % 4 < 2 ? 76f : 54f;
                gear.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            s.Stroke(gear, 6f, Navy, 1.2f, 3f);
            s.Stroke(Circle(c, 22f, 1.05f, 0.5f), 6f, Navy, 1.2f, 4f);
            s.Save(UiFolder + "settings.png");
            AssetDatabase.Refresh();
        }

        private static void BakeSoundIcons()
        {
            var speaker = new List<List<Vector2>>
            {
                new() { new(62, 104), new(90, 104), new(128, 70), new(128, 186), new(90, 152), new(62, 152), new(62, 104) },
                Circle(new Vector2(128, 128), 38, 0.22f, -0.7f),
                Circle(new Vector2(128, 128), 62, 0.22f, -0.7f),
            };
            var note = new List<List<Vector2>>
            {
                Circle(new Vector2(88, 84), 19), Circle(new Vector2(170, 100), 19),
                new() { new(106, 86), new(106, 180), new(188, 196), new(188, 102) },
                new() { new(106, 156), new(188, 172) },
            };
            foreach (var (name, lines) in new[] { ("sound", speaker), ("music", note) })
            foreach (bool on in new[] { true, false })
            {
                var s = new Sheet(256, 256);
                Vector2 c = new(124f, 132f);
                var disc = new bool[256 * 256];
                for (int i = 0; i < disc.Length; i++) disc[i] = (new Vector2(i % 256, i / 256) - c).magnitude < 104f;
                for (int i = 0; i < disc.Length; i++)
                {
                    if ((new Vector2(i % 256, i / 256) - c - new Vector2(6f, -8f)).magnitude < 110f) s.Blend(i % 256, i / 256, Navy, 1f);
                }
                for (int i = 0; i < disc.Length; i++) if (disc[i]) s.Blend(i % 256, i / 256, Color.white, 1f);
                Hatch(s, disc, on ? Crayons[0] : new Color(0.8f, 0.8f, 0.82f), 35f, 11f, 5f, 2f);
                s.Stroke(Circle(c, 106f, 1.04f, 1f), 5f, Navy, 3f, 8f);
                foreach (var l in lines) s.Stroke(l, 5.5f, Navy, 1.5f, 3f);
                if (!on) s.Stroke(new[] { new Vector2(58, 198), new Vector2(192, 62) }, 8f, Crayons[2], 2f, 5f);
                s.Save(UiFolder + name + (on ? "_on.png" : "_off.png"));
            }
        }

        /// <summary>The hero's eyes once it is gone: two crossed-out circles.</summary>
        private static void BakeDeadEyes()
        {
            var s = new Sheet(256, 128);
            foreach (float cx in new[] { 72f, 184f })
            {
                Vector2 c = new(cx, 64f);
                Disc(s, c, 46f, Color.white);
                s.Stroke(Circle(c, 46f, 1.08f, 1f), 4.5f, Navy, 2f, cx);
                s.Stroke(new[] { c + new Vector2(-24f, -24f), c + new Vector2(24f, 24f) }, 7f, Navy, 1.5f, cx + 1f);
                s.Stroke(new[] { c + new Vector2(-24f, 24f), c + new Vector2(24f, -24f) }, 7f, Navy, 1.5f, cx + 2f);
            }
            s.Save(UiFolder + "hero_eyes_dead.png");
        }

        private static List<List<Vector2>> LevelLines(Transform level)
        {
            var lines = new List<List<Vector2>>();
            foreach (DrawEdgeAuthor a in level.GetComponentsInChildren<DrawEdgeAuthor>(true))
            {
                if (!a.IsValid) continue;
                Vector2 A = Flat(a.AnchorA.transform.position), B = Flat(a.AnchorB.transform.position);
                lines.Add(a.Waypoint != null ? Arc(A, Flat(a.Waypoint.position), B) : new List<Vector2> { A, B });
            }
            return lines;
        }

        private static Vector2 Flat(Vector3 p) => new(p.x, p.z);

        private static List<Vector2> Arc(Vector2 a, Vector2 m, Vector2 b)
        {
            float d = 2f * (a.x * (m.y - b.y) + m.x * (b.y - a.y) + b.x * (a.y - m.y));
            if (Mathf.Abs(d) < 1e-5f) return new List<Vector2> { a, b };
            float sa = a.sqrMagnitude, sm = m.sqrMagnitude, sb = b.sqrMagnitude;
            Vector2 c = new((sa * (m.y - b.y) + sm * (b.y - a.y) + sb * (a.y - m.y)) / d,
                            (sa * (b.x - m.x) + sm * (a.x - b.x) + sb * (m.x - a.x)) / d);
            float r = (a - c).magnitude, a0 = Mathf.Atan2(a.y - c.y, a.x - c.x);
            float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, Mathf.Atan2(b.y - c.y, b.x - c.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float toMid = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, Mathf.Atan2(m.y - c.y, m.x - c.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            // Take the way round that passes through the waypoint.
            if (Mathf.Sign(toMid) != Mathf.Sign(sweep) || Mathf.Abs(toMid) > Mathf.Abs(sweep)) sweep -= Mathf.Sign(sweep) * Mathf.PI * 2f;
            var pts = new List<Vector2>();
            for (int i = 0; i <= 24; i++)
            {
                float t = a0 + sweep * i / 24f;
                pts.Add(c + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r);
            }
            return pts;
        }

        private static void BakeCollection()
        {
            var lm = Object.FindFirstObjectByType<LevelManager>(FindObjectsInactive.Include);
            var root = (Transform)new SerializedObject(lm).FindProperty("levelsRoot").objectReferenceValue;
            var collection = AssetDatabase.LoadAssetAtPath<ShapeCollection>("Assets/_Project/Collection/ShapeCollection.asset");
            for (int i = 0; i < collection.Count; i++)
            {
                ShapeCollection.Entry e = collection.Get(i);
                if (e.Illustration == null || e.LevelIndex >= root.childCount) continue;
                var sheet = Sticker(LevelLines(root.GetChild(e.LevelIndex)), e.DisplayName, e.LevelIndex);
                if (sheet != null) sheet.Save(AssetDatabase.GetAssetPath(e.Illustration));
            }
        }

        /// <summary>Authored edge ends stop short of each other (the game snaps them at runtime):
        /// each end meets its nearest neighbour on another line, so the outline closes.</summary>
        private static void Weld(List<List<Vector2>> lines)
        {
            const float Reach = 70f;
            var ends = new List<(int line, int point)>();
            for (int i = 0; i < lines.Count; i++) { ends.Add((i, 0)); ends.Add((i, lines[i].Count - 1)); }
            var welded = new bool[ends.Count];
            Vector2 At((int line, int point) e) => lines[e.line][e.point];
            int Nearest(int e)
            {
                int best = -1;
                float bestDist = Reach;
                for (int o = 0; o < ends.Count; o++)
                {
                    if (ends[o].line == ends[e].line || welded[o]) continue;
                    float d = Vector2.Distance(At(ends[e]), At(ends[o]));
                    if (d < bestDist) { bestDist = d; best = o; }
                }
                return best;
            }
            for (int pass = 0; pass < 3; pass++)
            for (int e = 0; e < ends.Count; e++)
            {
                int o = Nearest(e);
                if (welded[e] || o < 0 || Nearest(o) != e) continue;
                Vector2 mid = (At(ends[e]) + At(ends[o])) * 0.5f;
                lines[ends[e].line][ends[e].point] = mid;
                lines[ends[o].line][ends[o].point] = mid;
                welded[e] = welded[o] = true;
            }
        }

        private static Sheet Sticker(List<List<Vector2>> lines, string name, int seed)
        {
            const int S = 512, Art = 400;
            const float Seal = 10f;           // wide enough to close any gap the weld left
            if (lines.Count == 0) return null;
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (var l in lines) foreach (Vector2 p in l) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
            float scale = Art / Mathf.Max(max.x - min.x, max.y - min.y, 0.01f);
            Vector2 centre = (min + max) * 0.5f;
            foreach (var l in lines) for (int i = 0; i < l.Count; i++) l[i] = (l[i] - centre) * scale + Vector2.one * (S * 0.5f);

            Weld(lines);

            // The pen line is the wall of a paint bucket: what the outside cannot reach is a region.
            var wall = new Sheet(S, S);
            foreach (var l in lines) wall.Stroke(l, Seal, Color.white, 0f, 0f);
            var label = new int[S * S];       // 0 unvisited, -1 outside, >0 region
            var queue = new Queue<int>();
            void Flood(int start, int id, List<int> members)
            {
                label[start] = id; queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int p = queue.Dequeue();
                    members?.Add(p);
                    int x = p % S, y = p / S;
                    if (x > 0) Visit(p - 1); if (x < S - 1) Visit(p + 1);
                    if (y > 0) Visit(p - S); if (y < S - 1) Visit(p + S);
                }
                void Visit(int q) { if (label[q] == 0 && wall.Px[q].a < 0.5f) { label[q] = id; queue.Enqueue(q); } }
            }
            Flood(0, -1, null);
            var regions = new List<List<int>>();
            for (int p = 0; p < label.Length; p++)
            {
                if (label[p] != 0 || wall.Px[p].a >= 0.5f) continue;
                var members = new List<int>();
                Flood(p, regions.Count + 1, members);
                if (members.Count > 80) regions.Add(members);
            }
            regions.Sort((a, b) => b.Count.CompareTo(a.Count));

            var s = new Sheet(S, S);
            var body = new bool[S * S];
            for (int p = 0; p < body.Length; p++) body[p] = label[p] != -1;
            // White paper cut-out with a hard little shadow.
            var rim = new List<Vector2>();
            for (int p = 0; p < body.Length; p++)
            {
                if (!body[p]) continue;
                int x = p % S, y = p / S;
                if (x % 3 == 0 && y % 3 == 0 && (!body[p - 3] || !body[p + 3] || !body[p - 3 * S] || !body[p + 3 * S])) rim.Add(new Vector2(x, y));
            }
            foreach (Vector2 p in rim) s.Dab(p.x + 5f, p.y - 7f, 19f, Navy, 1f, 0f);
            foreach (Vector2 p in rim) s.Dab(p.x, p.y, 19f, Color.white, 1f, 0f);
            for (int p = 0; p < body.Length; p++) if (body[p]) s.Blend(p % S, p / S, Color.white, 1f);

            int crayon = Natural.TryGetValue(name, out int natural) ? natural : seed % Crayons.Length;
            for (int r = 0; r < regions.Count; r++)
            {
                // Tiny regions are details (eyes): solid ink. Others share a natural shape's colour.
                bool detail = regions[r].Count < regions[0].Count * 0.08f;
                Color colour = detail ? Navy : Crayons[(crayon + (Natural.ContainsKey(name) ? 0 : r * 3)) % Crayons.Length];
                var clip = new bool[S * S];
                foreach (int p in regions[r])
                {
                    // Grown back under the pen line: the seal above ate this much of the region.
                    int x0 = p % S, y0 = p / S;
                    for (int dy = -8; dy <= 8; dy += 4)
                    for (int dx = -8; dx <= 8; dx += 4)
                    {
                        int q = (y0 + dy) * S + x0 + dx;
                        if (q >= 0 && q < clip.Length && !clip[q]) { clip[q] = true; s.Blend(q % S, q / S, colour, detail ? 1f : 0.5f); }
                    }
                }
                if (!detail) Hatch(s, clip, colour, 35f + r * 40f, 10f, 5f, seed + r);
            }
            for (int i = 0; i < lines.Count; i++) s.Stroke(lines[i], 5.5f, Navy, 2.5f, seed + i * 7f);
            for (int i = 0; i < lines.Count; i++) s.Stroke(lines[i], 2f, Navy, 4f, seed + i * 11f + 3f, 0.7f);
            if (regions.Count > 0 && !HasOwnFace.Contains(name)) Face(s, regions[0], seed);
            return s;
        }

        /// <summary>Googly eyes and a smile in the roomiest part of the shape.</summary>
        private static void Face(Sheet s, List<int> region, int seed)
        {
            const int S = 512;
            var inside = new bool[S * S];
            foreach (int p in region) inside[p] = true;
            // Peel the region from its edge: the last pixel left is the point furthest inside.
            var depth = new int[S * S];
            var queue = new Queue<int>();
            foreach (int p in region)
            {
                int x = p % S, y = p / S;
                if (x == 0 || y == 0 || x == S - 1 || y == S - 1 || !inside[p - 1] || !inside[p + 1] || !inside[p - S] || !inside[p + S])
                {
                    depth[p] = 1;
                    queue.Enqueue(p);
                }
            }
            int deepest = region[0];
            while (queue.Count > 0)
            {
                int p = queue.Dequeue();
                if (depth[p] > depth[deepest]) deepest = p;
                foreach (int q in new[] { p - 1, p + 1, p - S, p + S })
                    if (q >= 0 && q < inside.Length && inside[q] && depth[q] == 0) { depth[q] = depth[p] + 1; queue.Enqueue(q); }
            }
            Vector2 c = new(deepest % S, deepest / S);
            float size = Mathf.Clamp(depth[deepest] * 0.42f, 14f, 58f);
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 eye = c + new Vector2(side * size * 1.05f, size * 0.35f);
                Disc(s, eye, size * 0.8f, Color.white);
                s.Stroke(Circle(eye, size * 0.8f, 1.06f, 1f), Mathf.Max(2.5f, size * 0.09f), Navy, 1f, seed + side);
                Vector2 pupil = eye + new Vector2(-side * size * 0.14f, -size * 0.08f);
                Disc(s, pupil, size * 0.38f, Navy);
                Disc(s, pupil + Vector2.one * size * 0.13f, size * 0.11f, Color.white);
            }
            var mouth = new List<Vector2>();
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.Lerp(Mathf.PI * 1.15f, Mathf.PI * 1.85f, i / 12f);
                mouth.Add(c + new Vector2(0f, -size * 0.15f) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size * 0.75f);
            }
            s.Stroke(mouth, Mathf.Max(2.5f, size * 0.09f), Navy, 1f, seed + 2f);
        }

        private static void Disc(Sheet s, Vector2 c, float r, Color colour)
        {
            for (float d = r; d > 0f; d -= 1.5f)
                foreach (Vector2 p in Circle(c, d)) s.Dab(p.x, p.y, 1.8f, colour, 1f, 0f);
            s.Dab(c.x, c.y, 2f, colour, 1f, 0f);
        }
    }
}
#endif
