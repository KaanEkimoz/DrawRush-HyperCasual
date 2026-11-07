using System.Collections.Generic;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Spots on the page where the pencil cannot lay paint right now: a staple trap that is up, a
    /// wet ink blot. Sources add a disc and remove it again; the painter asks <see cref="Blocks"/>
    /// for both the brush and the point of the shape it would paint. Paint already on the page
    /// stays: a blocker only stops new paint.
    /// </summary>
    public static class PaintBlockers
    {
        private struct Disc { public Vector3 Center; public float Radius; }

        private static readonly Dictionary<int, Disc> Discs = new();
        private static int _next;

        /// <summary>Add a blocking disc; returns the handle to move or remove it with.</summary>
        public static int Add(Vector3 center, float radius)
        {
            int id = ++_next;
            Discs[id] = new Disc { Center = center, Radius = radius };
            return id;
        }

        public static void Remove(int id) => Discs.Remove(id);

        public static int Count => Discs.Count;

        /// <summary>True when <paramref name="point"/> lies on any blocking disc (ground distance).</summary>
        public static bool Blocks(Vector3 point)
        {
            foreach (Disc d in Discs.Values)
            {
                float dx = point.x - d.Center.x, dz = point.z - d.Center.z;
                if (dx * dx + dz * dz < d.Radius * d.Radius) return true;
            }
            return false;
        }

        public static void Clear() => Discs.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();
    }
}
