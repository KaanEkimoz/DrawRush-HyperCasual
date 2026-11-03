#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DrawRush.Enemy.EditorTools
{
    /// <summary>
    /// Gives the stationery enemies swinging limbs without a humanoid rig, which tears their box
    /// bodies: each limb is marked by hand and hung on its own bone where it meets the body, and
    /// the body never moves. The texture is no help here: navy pen lines run over everything.
    /// Marks are pixels of a 600x600 orthographic front view centred on (0, 0.5), 2 units wide,
    /// camera looking down -Z, so the image's left is +X.
    /// </summary>
    public static class LimbRigBaker
    {
        private const string Art = "Assets/_Project/Art/";
        private const float Soft = 0.07f;      // the joint blends into the body over this distance

        private readonly struct Mark
        {
            public readonly bool Arm, Left;
            public readonly Vector2 Pivot;
            public readonly RectInt[] Areas;
            public readonly string Part;   // "Arm"/"Leg" by default; "Blade" for the scissors

            public Mark(bool arm, bool left, float x, float y, params RectInt[] areas)
            {
                Arm = arm; Left = left; Pivot = new Vector2(x, y); Areas = areas; Part = arm ? "Arm" : "Leg";
            }

            private Mark(string part, bool left, float x, float y, RectInt[] areas)
            {
                Arm = false; Left = left; Pivot = new Vector2(x, y); Areas = areas; Part = part;
            }

            /// <summary>A scissor blade: turns in the picture plane around the screw.</summary>
            public static Mark Blade(bool left, float x, float y, params RectInt[] areas) => new("Blade", left, x, y, areas);
        }

        private static RectInt Px(int x0, int y0, int x1, int y1) => new(x0, y0, x1 - x0, y1 - y0);

        private static readonly (string prefab, bool inPlane, Mark[] marks)[] Enemies =
        {
            ("Doodle/stapler/P_stapler", false, new[]
            {
                new Mark(true, true, 206, 205, Px(100, 170, 204, 262)),
                new Mark(true, false, 412, 218, Px(412, 205, 500, 285)),
                new Mark(false, true, 212, 286, Px(150, 288, 228, 345)),
                new Mark(false, false, 365, 322, Px(345, 326, 415, 372)),
            }),
            ("Doodle/holepunch/P_holepunch", false, new[]
            {
                new Mark(true, true, 152, 270, Px(0, 232, 150, 370)),
                new Mark(true, false, 450, 262, Px(450, 232, 600, 355)),
                new Mark(false, true, 115, 388, Px(85, 385, 128, 452), Px(128, 408, 162, 452)),
                new Mark(false, false, 470, 388, Px(468, 378, 600, 450)),
            }),
            ("Doodle/sharpener/P_sharpener_v2", false, new[]
            {
                new Mark(true, true, 232, 290, Px(140, 282, 228, 408)),
                new Mark(true, false, 368, 290, Px(372, 282, 460, 408)),
                new Mark(false, true, 260, 388, Px(224, 390, 276, 452)),
                new Mark(false, false, 338, 388, Px(320, 390, 376, 452)),
            }),
            ("Doodle/pushpin/P_pushpin", false, new[]
            {
                new Mark(true, false, 378, 268, Px(380, 258, 440, 310)),
                new Mark(false, true, 292, 322, Px(225, 316, 290, 352), Px(160, 350, 255, 432)),
                new Mark(false, false, 330, 324, Px(322, 326, 405, 390)),
            }),
            ("Doodle/glue/P_glue", false, new[]
            {
                new Mark(true, true, 228, 305, Px(150, 298, 222, 408)),
                new Mark(true, false, 372, 305, Px(378, 298, 450, 408)),
                new Mark(false, true, 262, 390, Px(222, 392, 282, 452)),
                new Mark(false, false, 338, 390, Px(322, 392, 368, 452)),
            }),
            ("Doodle3D/gianteraser3d/P_gianteraser3d", false, new[]
            {
                new Mark(true, true, 215, 262, Px(172, 252, 224, 378)),
                new Mark(true, false, 385, 262, Px(377, 252, 425, 378)),
                new Mark(false, true, 255, 395, Px(210, 394, 278, 452)),
                new Mark(false, false, 345, 393, Px(322, 394, 392, 452)),
            }),
            ("Doodle/scissors/P_scissors_v2", false, new[]
            {
                // Blades first, so the legs win where the blade tips reach down between them.
                Mark.Blade(true, 300, 298, Px(258, 300, 300, 436)),
                Mark.Blade(false, 300, 298, Px(300, 300, 344, 436)),
                new Mark(true, true, 255, 272, Px(183, 262, 252, 368)),
                new Mark(true, false, 345, 272, Px(348, 262, 417, 368)),
                new Mark(false, true, 258, 385, Px(208, 385, 262, 452)),
                new Mark(false, false, 340, 385, Px(338, 385, 394, 445)),
            }),
            ("Doodle/eraser/P_eraser", false, new[]
            {
                new Mark(true, true, 230, 300, Px(146, 290, 228, 412)),
                new Mark(true, false, 370, 300, Px(372, 290, 456, 412)),
                new Mark(false, true, 255, 392, Px(226, 392, 280, 452)),
                new Mark(false, false, 345, 392, Px(320, 392, 374, 452)),
            }),
            ("Doodle/putty/P_putty", false, new[]
            {
                new Mark(true, true, 185, 295, Px(116, 280, 182, 394)),
                new Mark(true, false, 415, 295, Px(418, 280, 484, 394)),
                new Mark(false, true, 225, 390, Px(191, 392, 254, 452)),
                new Mark(false, false, 378, 390, Px(346, 392, 410, 452)),
            }),
        };

        /// <summary>Bake only the named prefabs (paths as in the table), e.g. a newly added enemy.</summary>
        public static string Bake(params string[] prefabs)
        {
            var report = new StringBuilder();
            foreach (var e in Enemies)
                if (System.Array.IndexOf(prefabs, e.prefab) >= 0) report.AppendLine(Bake(e.prefab, e.inPlane, e.marks));
            AssetDatabase.SaveAssets();
            return report.ToString();
        }
        [MenuItem("DrawRush/Bake Enemy Limbs")]
        public static string BakeAll()
        {
            var report = new StringBuilder();
            foreach (var e in Enemies) report.AppendLine(Bake(e.prefab, e.inPlane, e.marks));
            AssetDatabase.SaveAssets();
            return report.ToString();
        }

        private static Vector2 ToRoot(Vector2 px) => new((300f - px.x) / 300f, 0.5f + (300f - px.y) / 300f);

        private static bool Inside(Vector3 v, Mark m)
        {
            Vector2 px = new(300f - v.x * 300f, 300f - (v.y - 0.5f) * 300f);
            foreach (RectInt r in m.Areas)
                if (px.x >= r.xMin && px.x <= r.xMax && px.y >= r.yMin && px.y <= r.yMax) return true;
            return false;
        }

        private static string Bake(string prefab, bool inPlane, Mark[] marks)
        {
            string path = Art + prefab + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Mesh mesh = RootSpaceMesh(root, out Material material);
                Vector3[] v = mesh.vertices;

                var bones = new List<Transform>();
                var body = new GameObject("Body").transform;
                body.SetParent(root.transform, false);
                bones.Add(body);
                var weights = new BoneWeight[v.Length];
                for (int i = 0; i < v.Length; i++) weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1f };

                var limbs = new List<HopAnimator.Limb>();
                var line = new StringBuilder(Path.GetFileName(prefab)).Append(":");
                foreach (Mark mark in marks)
                {
                    Vector2 flat = ToRoot(mark.Pivot);
                    float nearZ = 0f, nearCount = 0f;
                    var members = new List<int>();
                    for (int i = 0; i < v.Length; i++)
                    {
                        if (!Inside(v[i], mark)) continue;
                        members.Add(i);
                        if (Vector2.Distance(v[i], flat) < 0.12f) { nearZ += v[i].z; nearCount++; }
                    }
                    if (members.Count == 0) { line.Append(" EMPTY"); continue; }
                    Vector3 pivot = new(flat.x, flat.y, nearCount > 0f ? nearZ / nearCount : 0f);
                    Vector3 tip = pivot;
                    foreach (int i in members) if (Vector3.Distance(v[i], pivot) > Vector3.Distance(tip, pivot)) tip = v[i];

                    var bone = new GameObject((mark.Left ? "L" : "R") + mark.Part).transform;
                    bone.SetParent(root.transform, false);
                    bone.localPosition = pivot;
                    bones.Add(bone);
                    foreach (int i in members)
                    {
                        float w = Mathf.SmoothStep(0f, 1f, Vector3.Distance(v[i], pivot) / Soft);
                        weights[i] = new BoneWeight { boneIndex0 = bones.Count - 1, weight0 = w, boneIndex1 = 0, weight1 = 1f - w };
                    }
                    Vector3 axis = inPlane || mark.Part == "Blade" ? Vector3.forward : Vector3.Cross((tip - pivot).normalized, Vector3.forward).normalized;
                    limbs.Add(new HopAnimator.Limb(bone, axis, mark.Left ^ mark.Arm ? 1f : -1f));
                    line.Append($" {bone.name}={members.Count}v");
                }

                // Feet on the floor, whatever the marks were read from.
                float lift = -mesh.bounds.min.y;
                for (int i = 0; i < v.Length; i++) v[i].y += lift;
                mesh.vertices = v;
                var bind = new Matrix4x4[bones.Count];
                for (int b = 0; b < bones.Count; b++)
                {
                    bones[b].localPosition += Vector3.up * (b == 0 ? 0f : lift);
                    bind[b] = Matrix4x4.Translate(-bones[b].localPosition);
                }
                mesh.boneWeights = weights;
                mesh.bindposes = bind;
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, Path.ChangeExtension(path, null).Replace("/P_", "/limbs_") + ".asset");

                var skin = new GameObject("Model").AddComponent<SkinnedMeshRenderer>();
                skin.transform.SetParent(root.transform, false);
                skin.sharedMesh = mesh;
                skin.sharedMaterial = material;
                skin.bones = bones.ToArray();
                skin.rootBone = body;
                skin.updateWhenOffscreen = true;
                skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                root.GetComponent<HopAnimator>().SetLimbs(limbs.ToArray());
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return line.Append($" lift {lift:F2}").ToString();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>The visual as one mesh in the prefab root's space; the old visual is removed.</summary>
        private static Mesh RootSpaceMesh(GameObject root, out Material material)
        {
            var skinned = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Mesh source;
            Matrix4x4 toRoot;
            if (skinned != null)
            {
                source = new Mesh();
                skinned.BakeMesh(source, true);
                Transform t = skinned.transform;
                toRoot = root.transform.worldToLocalMatrix * Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
                material = skinned.sharedMaterial;
            }
            else
            {
                var filter = root.GetComponentInChildren<MeshFilter>(true);
                source = filter.sharedMesh;
                toRoot = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                material = filter.GetComponent<Renderer>().sharedMaterial;
            }
            var mesh = new Mesh { name = root.name + "_limbs" };
            Vector3[] v = source.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = toRoot.MultiplyPoint3x4(v[i]);
            mesh.vertices = v;
            mesh.uv = source.uv;
            mesh.triangles = source.triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            for (int c = root.transform.childCount - 1; c >= 0; c--) Object.DestroyImmediate(root.transform.GetChild(c).gameObject);
            return mesh;
        }
    }
}
#endif
