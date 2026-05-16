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
    }
}
