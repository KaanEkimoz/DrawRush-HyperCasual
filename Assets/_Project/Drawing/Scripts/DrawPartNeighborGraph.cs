using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Pure-logic helper that builds a "nearest neighbors" graph over a set of anchor
    /// positions. Used to enforce polygon-edge connectivity so the chain rejects
    /// diagonal jumps — only the two closest anchors to a given corner are valid
    /// next steps. For convex layouts (square, triangle, hex) the two nearest
    /// neighbors are always the polygon-edge neighbors.
    /// </summary>
    public static class DrawPartNeighborGraph
    {
        /// <summary>
        /// For each position in <paramref name="positions"/>, returns the indices of
        /// its <paramref name="k"/> nearest other positions, ordered by ascending
        /// squared distance. If fewer than <c>k</c> other positions exist, returns
        /// all available ones.
        /// </summary>
        public static int[][] ComputeNearestNeighbors(IReadOnlyList<Vector3> positions, int k)
        {
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (k < 1) throw new ArgumentOutOfRangeException(nameof(k), "k must be >= 1");

            int n = positions.Count;
            var result = new int[n][];
            for (int i = 0; i < n; i++)
            {
                if (n == 1)
                {
                    result[i] = Array.Empty<int>();
                    continue;
                }

                var distances = new (int idx, float sqr)[n - 1];
                int d = 0;
                for (int j = 0; j < n; j++)
                {
                    if (j == i) continue;
                    distances[d++] = (j, (positions[i] - positions[j]).sqrMagnitude);
                }
                Array.Sort(distances, (a, b) => a.sqr.CompareTo(b.sqr));

                int take = Math.Min(k, distances.Length);
                result[i] = new int[take];
                for (int j = 0; j < take; j++) result[i][j] = distances[j].idx;
            }
            return result;
        }

        /// <summary>
        /// Collapses a (possibly directed/asymmetric) neighbor adjacency into the set of
        /// unique undirected edges. A pair (i, j) is emitted once if either i lists j or j
        /// lists i; each pair is normalized to (min, max) and deduplicated. Output is sorted
        /// ascending by (low, high) so callers — and tests — get a deterministic order.
        /// EdgeNetwork uses this to turn the anchors' neighbor graph into paintable edges
        /// without ever creating an edge twice (A–B and B–A are the same edge).
        /// </summary>
        public static (int A, int B)[] BuildUndirectedPairs(int[][] adjacency)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            var seen = new HashSet<long>();
            var pairs = new List<(int, int)>();
            for (int i = 0; i < adjacency.Length; i++)
            {
                int[] row = adjacency[i];
                if (row == null) continue;
                for (int n = 0; n < row.Length; n++)
                {
                    int j = row[n];
                    if (j == i || j < 0 || j >= adjacency.Length) continue;

                    int lo = Math.Min(i, j);
                    int hi = Math.Max(i, j);
                    long key = ((long)lo << 32) | (uint)hi;
                    if (seen.Add(key)) pairs.Add((lo, hi));
                }
            }
            pairs.Sort((a, b) => a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : a.Item2.CompareTo(b.Item2));
            return pairs.ToArray();
        }
    }
}
