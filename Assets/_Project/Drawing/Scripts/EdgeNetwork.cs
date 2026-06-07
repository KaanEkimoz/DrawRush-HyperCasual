using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Collects the active level's authored edges (<see cref="DrawEdgeAuthor"/> "Kenar" prefabs)
    /// and tracks completion. Each author already knows its two endpoint anchors, so no neighbor
    /// graph is computed. When an edge fills, its author reveals its wall segment; when every
    /// edge fills, <see cref="AllCompleted"/> fires (the win condition listens).
    ///
    /// It also auto-detects corners (points where two-or-more edges share an endpoint) and
    /// spawns a vertical filler post at each. The post rises once every edge meeting at that
    /// corner is painted, closing the gap left between the straight wall segments. A vertical
    /// post is angle-agnostic, so this works for any shape (square, triangle, hexagon, …) with
    /// no per-corner authoring.
    ///
    /// Rebuilt on every enable so re-activating a level in the mega-scene starts from a fresh,
    /// unpainted edge set scoped to the active authors (FindObjectsByType excludes inactive).
    /// </summary>
    public sealed class EdgeNetwork : MonoBehaviour
    {
        /// <summary>Raised once when the last incomplete edge becomes complete.</summary>
        public event Action AllCompleted;

        [Tooltip("Fallback painted-line color when an edge's wall has no readable color.")]
        [SerializeField] private Color fallbackLineColor = new Color(0.1f, 1f, 0.8f, 1f);

        [Header("Corner posts")]
        [Tooltip("Generate rising filler posts where edges meet. Off = no corner pieces.")]
        [SerializeField] private bool generateCornerPosts = true;
        [Tooltip("Max distance between two anchors to count them as the same corner. Each edge " +
                 "has its own drops, so the two drops at a corner sit a little apart — this must " +
                 "be larger than that gap but smaller than an edge length.")]
        [SerializeField] private float cornerMergeDistance = 1.5f;
        [Tooltip("Post height (world units).")]
        [SerializeField] private float cornerHeight = 1.0f;
        [Tooltip("Post thickness (world units, X/Z).")]
        [SerializeField] private float cornerThickness = 0.45f;
        [Tooltip("Y of the post's base (ground level).")]
        [SerializeField] private float cornerBaseY = 0f;
        [Tooltip("Seconds the post takes to rise.")]
        [SerializeField] private float cornerRiseSeconds = 0.4f;

        private readonly List<DrawEdge> _edges = new();
        private readonly Dictionary<DrawEdge, DrawEdgeAuthor> _authors = new();
        private readonly List<Corner> _corners = new();
        private int _remaining;
        private Transform _cornerRoot;

        /// <summary>The edges built for the active level (read-only).</summary>
        public IReadOnlyList<DrawEdge> Edges => _edges;

        /// <summary>True once every edge is painted. False when there are no edges yet.</summary>
        public bool IsComplete => _edges.Count > 0 && _remaining == 0;

        private void OnEnable() => Rebuild();

        private void OnDisable() => UnsubscribeAll();

        /// <summary>Appends every edge touching <paramref name="anchor"/> to
        /// <paramref name="results"/> (cleared first). The paint controller uses this to pick
        /// which edge to slide along from a corner.</summary>
        public void GetEdgesTouching(DrawPart anchor, List<DrawEdge> results)
        {
            results.Clear();
            for (int i = 0; i < _edges.Count; i++)
                if (_edges[i].Contains(anchor)) results.Add(_edges[i]);
        }

        private void Rebuild()
        {
            UnsubscribeAll();
            _edges.Clear();
            _authors.Clear();
            _corners.Clear();
            _remaining = 0;

            var authors = UnityEngine.Object.FindObjectsByType<DrawEdgeAuthor>(FindObjectsSortMode.None);
            for (int i = 0; i < authors.Length; i++)
            {
                DrawEdgeAuthor author = authors[i];
                if (!author.IsValid) continue;

                var edge = new DrawEdge(author.AnchorA, author.AnchorB);
                edge.Completed += OnEdgeCompleted;
                _edges.Add(edge);
                _authors[edge] = author;
                author.View.Bind(edge, author.WallColor(fallbackLineColor));
            }
            _remaining = _edges.Count;

            BuildCorners();
        }

        private void OnEdgeCompleted(DrawEdge edge)
        {
            if (_authors.TryGetValue(edge, out DrawEdgeAuthor author)) author.Reveal();
            if (_remaining > 0) _remaining--;

            // Rise any corner whose every edge is now painted.
            for (int i = 0; i < _corners.Count; i++)
            {
                Corner c = _corners[i];
                if (c.Post == null || c.Revealed || !c.Edges.Contains(edge)) continue;
                if (AllEdgesComplete(c.Edges))
                {
                    c.Revealed = true;
                    c.Post.Reveal();
                }
            }

            if (_remaining == 0) AllCompleted?.Invoke();
        }

        private static bool AllEdgesComplete(List<DrawEdge> edges)
        {
            for (int i = 0; i < edges.Count; i++)
                if (!edges[i].IsComplete) return false;
            return true;
        }

        // --- corner detection + post spawning --------------------------------------------

        private void BuildCorners()
        {
            // Reset any posts spawned on a previous activation.
            if (_cornerRoot != null) SafeDestroy(_cornerRoot.gameObject);
            if (!generateCornerPosts || _edges.Count == 0) return;

            _cornerRoot = new GameObject("CornerPosts").transform;
            _cornerRoot.SetParent(transform, worldPositionStays: false);

            // Group edge endpoints that sit on the same spot into corners.
            float mergeSqr = cornerMergeDistance * cornerMergeDistance;
            foreach (DrawEdge edge in _edges)
            {
                AddEndpointToCorner(edge, edge.A, mergeSqr);
                AddEndpointToCorner(edge, edge.B, mergeSqr);
            }

            // A real corner has 2+ distinct edges meeting; spawn a post there.
            for (int i = 0; i < _corners.Count; i++)
            {
                Corner c = _corners[i];
                if (c.Edges.Count < 2) continue;
                c.Post = SpawnPost(c.Position, c.Edges);
            }
        }

        private void AddEndpointToCorner(DrawEdge edge, DrawPart anchor, float mergeSqr)
        {
            if (anchor == null) return;
            Vector3 p = anchor.Transform.position;
            for (int i = 0; i < _corners.Count; i++)
            {
                Corner c = _corners[i];
                if ((c.Position - p).sqrMagnitude <= mergeSqr)
                {
                    if (!c.Edges.Contains(edge)) c.Edges.Add(edge);
                    c.Sum += p;
                    c.Count++;
                    c.Position = c.Sum / c.Count;   // keep the corner at the midpoint of its drops
                    return;
                }
            }
            _corners.Add(new Corner { Position = p, Sum = p, Count = 1, Edges = { edge } });
        }

        private CornerPost SpawnPost(Vector3 corner, List<DrawEdge> edges)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CornerPost";
            // No physics collision needed; keep the trigger-free look, drop the collider.
            var col = go.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);

            go.transform.SetParent(_cornerRoot, worldPositionStays: false);
            go.transform.position = new Vector3(corner.x, cornerBaseY + cornerHeight * 0.5f, corner.z);
            go.transform.localScale = new Vector3(cornerThickness, cornerHeight, cornerThickness);

            // Match the wall material/color of one of the meeting edges.
            Material wallMat = null;
            Color wallCol = fallbackLineColor;
            if (edges.Count > 0 && _authors.TryGetValue(edges[0], out DrawEdgeAuthor a))
            {
                wallMat = a.WallMaterial();
                wallCol = a.WallColor(fallbackLineColor);
            }
            var rend = go.GetComponent<Renderer>();
            if (wallMat != null) rend.sharedMaterial = wallMat;
            else rend.material.color = wallCol;

            var post = go.AddComponent<CornerPost>();
            post.Init(cornerRiseSeconds, cornerHeight + 0.2f);
            return post;
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < _edges.Count; i++) _edges[i].Completed -= OnEdgeCompleted;
        }

        // Destroy works at runtime; editor-time (e.g. a manual Rebuild) needs DestroyImmediate.
        private static void SafeDestroy(UnityEngine.Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        private sealed class Corner
        {
            public Vector3 Position;   // running midpoint of the grouped endpoints
            public Vector3 Sum;
            public int Count;
            public readonly List<DrawEdge> Edges = new();
            public CornerPost Post;
            public bool Revealed;
        }
    }
}
