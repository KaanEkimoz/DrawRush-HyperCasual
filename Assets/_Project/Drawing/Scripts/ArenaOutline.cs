using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// A faint pencil line on the page where the play area ends, drawn along the inner faces of
    /// the arena's border colliders so it always matches where the player actually stops.
    /// </summary>
    public sealed class ArenaOutline : MonoBehaviour
    {
        [SerializeField] private Color color = new(0.11f, 0.13f, 0.38f, 0.28f);
        [SerializeField] private float lineWidth = 0.06f;
        [SerializeField] private float groundY = 0.012f;

        private void Awake()
        {
            float minX = float.MinValue, maxX = float.MaxValue, minZ = float.MinValue, maxZ = float.MaxValue;
            Vector3 centre = Vector3.zero;
            BoxCollider[] walls = GetComponentsInChildren<BoxCollider>();
            foreach (BoxCollider wall in walls) centre += wall.bounds.center / Mathf.Max(1, walls.Length);
            foreach (BoxCollider wall in walls)
            {
                Bounds b = wall.bounds;
                if (b.size.x < b.size.z)   // an east or west wall
                {
                    if (b.center.x < centre.x) minX = Mathf.Max(minX, b.max.x);
                    else maxX = Mathf.Min(maxX, b.min.x);
                }
                else if (b.center.z < centre.z) minZ = Mathf.Max(minZ, b.max.z);
                else maxZ = Mathf.Min(maxZ, b.min.z);
            }

            // On its own object: the line lies flat through its transform, the walls must not.
            var holder = new GameObject("Outline");
            holder.transform.SetParent(transform, false);
            holder.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var line = holder.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.alignment = LineAlignment.TransformZ;
            line.widthMultiplier = lineWidth;
            line.startColor = line.endColor = color;
            line.material = new Material(Shader.Find("Sprites/Default")) { name = "Auto_ArenaOutline" };
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.positionCount = 4;
            line.SetPositions(new[]
            {
                new Vector3(minX, groundY, minZ), new Vector3(minX, groundY, maxZ),
                new Vector3(maxX, groundY, maxZ), new Vector3(maxX, groundY, minZ),
            });
        }
    }
}
