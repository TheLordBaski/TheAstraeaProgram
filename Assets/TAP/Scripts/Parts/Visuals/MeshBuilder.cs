using System.Collections.Generic;
using UnityEngine;

namespace TAP.Parts
{
    /// <summary>
    /// Small procedural mesh builder with one submesh per material slot.
    /// Used to generate part models, colliders and props deterministically at runtime.
    /// </summary>
    public sealed class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> UVs = new List<Vector2>();
        private readonly Dictionary<int, List<int>> _tris = new Dictionary<int, List<int>>();

        public Matrix4x4 Transform = Matrix4x4.identity;

        private List<int> Tris(int slot)
        {
            if (!_tris.TryGetValue(slot, out var l)) { l = new List<int>(); _tris[slot] = l; }
            return l;
        }

        public int AddVertex(Vector3 p, Vector3 n, Vector2 uv)
        {
            Vertices.Add(Transform.MultiplyPoint3x4(p));
            Normals.Add(Transform.MultiplyVector(n).normalized);
            UVs.Add(uv);
            return Vertices.Count - 1;
        }

        public void AddTriangle(int slot, int a, int b, int c)
        {
            var t = Tris(slot);
            t.Add(a); t.Add(b); t.Add(c);
        }

        public void AddQuad(int slot, int a, int b, int c, int d)
        {
            AddTriangle(slot, a, b, c);
            AddTriangle(slot, a, c, d);
        }

        /// <summary>
        /// Surface of revolution around +Y. Profile points are (radius, y) from bottom to top.
        /// Normals are smooth within each segment but split between profile segments (flat-shaded rings)
        /// unless <paramref name="smooth"/> is true.
        /// </summary>
        public void Lathe(int slot, IList<Vector2> profile, int segments, bool smooth = false, float angleStart = 0, float angleEnd = 360)
        {
            int n = profile.Count;
            if (n < 2) return;
            float a0 = angleStart * Mathf.Deg2Rad, a1 = angleEnd * Mathf.Deg2Rad;
            bool full = Mathf.Abs(angleEnd - angleStart) >= 359.9f;
            int cols = segments + 1;

            if (smooth)
            {
                // Shared vertices along the profile with averaged profile normals.
                var pn = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    Vector2 dPrev = i > 0 ? profile[i] - profile[i - 1] : profile[i + 1] - profile[i];
                    Vector2 dNext = i < n - 1 ? profile[i + 1] - profile[i] : dPrev;
                    Vector2 t = (dPrev.normalized + dNext.normalized).normalized;
                    pn[i] = new Vector2(t.y, -t.x); // outward for bottom->top profiles
                }
                int baseIdx = Vertices.Count;
                for (int i = 0; i < n; i++)
                {
                    for (int s = 0; s < cols; s++)
                    {
                        float u = (float)s / segments;
                        float a = Mathf.Lerp(a0, a1, u);
                        float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                        var p = new Vector3(profile[i].x * ca, profile[i].y, profile[i].x * sa);
                        var nn = new Vector3(pn[i].x * ca, pn[i].y, pn[i].x * sa);
                        AddVertex(p, nn, new Vector2(u, (float)i / (n - 1)));
                    }
                }
                for (int i = 0; i < n - 1; i++)
                    for (int s = 0; s < segments; s++)
                    {
                        int a = baseIdx + i * cols + s, b = a + 1, c = a + cols + 1, d = a + cols;
                        AddQuad(slot, a, d, c, b);
                    }
                return;
            }

            for (int i = 0; i < n - 1; i++)
            {
                Vector2 p0 = profile[i], p1 = profile[i + 1];
                Vector2 dir = (p1 - p0);
                if (dir.sqrMagnitude < 1e-12f) continue;
                dir.Normalize();
                var n2 = new Vector2(dir.y, -dir.x);
                int baseIdx = Vertices.Count;
                for (int s = 0; s < cols; s++)
                {
                    float u = (float)s / segments;
                    float a = Mathf.Lerp(a0, a1, u);
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    var nn = new Vector3(n2.x * ca, n2.y, n2.x * sa);
                    AddVertex(new Vector3(p0.x * ca, p0.y, p0.x * sa), nn, new Vector2(u, 0));
                    AddVertex(new Vector3(p1.x * ca, p1.y, p1.x * sa), nn, new Vector2(u, 1));
                }
                for (int s = 0; s < segments; s++)
                {
                    int a = baseIdx + s * 2, b = a + 1, c = a + 3, d = a + 2;
                    AddQuad(slot, a, b, c, d);
                }
            }
            if (!full) { /* open lathes are used for fairings only */ }
        }

        /// <summary>Flat disc cap at height y facing up (+Y) or down.</summary>
        public void Disc(int slot, float radius, float y, bool facingUp, int segments, float innerRadius = 0)
        {
            var n = facingUp ? Vector3.up : Vector3.down;
            if (innerRadius <= 0)
            {
                int c = AddVertex(new Vector3(0, y, 0), n, new Vector2(0.5f, 0.5f));
                int first = Vertices.Count;
                for (int s = 0; s <= segments; s++)
                {
                    float a = s * Mathf.PI * 2 / segments;
                    AddVertex(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius), n,
                        new Vector2(0.5f + 0.5f * Mathf.Cos(a), 0.5f + 0.5f * Mathf.Sin(a)));
                }
                for (int s = 0; s < segments; s++)
                {
                    if (facingUp) AddTriangle(slot, c, first + s + 1, first + s);
                    else AddTriangle(slot, c, first + s, first + s + 1);
                }
            }
            else
            {
                int first = Vertices.Count;
                for (int s = 0; s <= segments; s++)
                {
                    float a = s * Mathf.PI * 2 / segments;
                    float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                    AddVertex(new Vector3(ca * innerRadius, y, sa * innerRadius), n, new Vector2(0, 0));
                    AddVertex(new Vector3(ca * radius, y, sa * radius), n, new Vector2(1, 0));
                }
                for (int s = 0; s < segments; s++)
                {
                    int a = first + s * 2, b = a + 1, c = a + 3, d = a + 2;
                    if (facingUp) AddQuad(slot, a, d, c, b);
                    else AddQuad(slot, a, b, c, d);
                }
            }
        }

        /// <summary>Axis-aligned box (in the builder's current transform).</summary>
        public void Box(int slot, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var nn in n)
            {
                Vector3 u = Mathf.Abs(nn.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(nn, u);
                Vector3 c = center + Vector3.Scale(nn, h);
                Vector3 hu = Vector3.Scale(u, h), hv = Vector3.Scale(v, h);
                int a = AddVertex(c - hu - hv, nn, new Vector2(0, 0));
                int b = AddVertex(c + hu - hv, nn, new Vector2(1, 0));
                int cc = AddVertex(c + hu + hv, nn, new Vector2(1, 1));
                int d = AddVertex(c - hu + hv, nn, new Vector2(0, 1));
                AddQuad(slot, a, b, cc, d);
            }
        }

        /// <summary>Oriented box from a local frame (center, right, up, forward half-extents).</summary>
        public void OrientedBox(int slot, Vector3 center, Quaternion rot, Vector3 size)
        {
            var saved = Transform;
            Transform = saved * Matrix4x4.TRS(center, rot, Vector3.one);
            Box(slot, Vector3.zero, size);
            Transform = saved;
        }

        /// <summary>Cylinder between two points (for struts, pipes, poles).</summary>
        public void CylinderBetween(int slot, Vector3 a, Vector3 b, float radius, int segments, bool caps = true, float radiusB = -1)
        {
            if (radiusB < 0) radiusB = radius;
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-6f) return;
            var saved = Transform;
            Transform = saved * Matrix4x4.TRS(a, Quaternion.FromToRotation(Vector3.up, d / len), Vector3.one);
            Lathe(slot, new[] { new Vector2(radius, 0), new Vector2(radiusB, len) }, segments, true);
            if (caps)
            {
                Disc(slot, radius, 0, false, segments);
                Disc(slot, radiusB, len, true, segments);
            }
            Transform = saved;
        }

        /// <summary>Sphere (UV) centered at c.</summary>
        public void Sphere(int slot, Vector3 c, float radius, int segments = 16, int rings = 10)
        {
            var prof = new List<Vector2>();
            for (int i = 0; i <= rings; i++)
            {
                float t = Mathf.PI * i / rings - Mathf.PI / 2;
                prof.Add(new Vector2(Mathf.Cos(t) * radius + 1e-4f, Mathf.Sin(t) * radius));
            }
            var saved = Transform;
            Transform = saved * Matrix4x4.Translate(c);
            Lathe(slot, prof, segments, true);
            Transform = saved;
        }

        /// <summary>Extrudes a convex 2D polygon (in the XY plane) along Z by thickness.</summary>
        public void ExtrudePolygon(int slot, IList<Vector2> poly, float thickness, int edgeSlot = -1)
        {
            if (edgeSlot < 0) edgeSlot = slot;
            float hz = thickness * 0.5f;
            int n = poly.Count;
            // Faces
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? hz : -hz;
                var nn = side == 0 ? Vector3.forward : Vector3.back;
                int first = Vertices.Count;
                for (int i = 0; i < n; i++) AddVertex(new Vector3(poly[i].x, poly[i].y, z), nn, poly[i]);
                // Polygon is CCW in XY; Unity front faces are clockwise as seen by the viewer.
                for (int i = 1; i < n - 1; i++)
                {
                    if (side == 0) AddTriangle(slot, first, first + i, first + i + 1);
                    else AddTriangle(slot, first, first + i + 1, first + i);
                }
            }
            // Edges
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = poly[i], p1 = poly[(i + 1) % n];
                Vector2 e = (p1 - p0).normalized;
                var nn = new Vector3(e.y, -e.x, 0);
                int a = AddVertex(new Vector3(p0.x, p0.y, hz), nn, Vector2.zero);
                int b = AddVertex(new Vector3(p1.x, p1.y, hz), nn, Vector2.right);
                int c = AddVertex(new Vector3(p1.x, p1.y, -hz), nn, Vector2.one);
                int d = AddVertex(new Vector3(p0.x, p0.y, -hz), nn, Vector2.up);
                AddQuad(edgeSlot, a, d, c, b);
            }
        }

        public int SlotCount => _tris.Count;

        /// <summary>Builds a mesh; submesh order follows ascending slot ids. Returns the slot order.</summary>
        public Mesh ToMesh(string name, out int[] slotOrder)
        {
            var mesh = new Mesh { name = name };
            if (Vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, UVs);
            var slots = new List<int>(_tris.Keys);
            slots.Sort();
            mesh.subMeshCount = slots.Count;
            for (int i = 0; i < slots.Count; i++) mesh.SetTriangles(_tris[slots[i]], i, true);
            mesh.RecalculateBounds();
            slotOrder = slots.ToArray();
            return mesh;
        }

        /// <summary>Single-submesh mesh (all slots merged) — for colliders.</summary>
        public Mesh ToMergedMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            var all = new List<int>();
            foreach (var kv in _tris) all.AddRange(kv.Value);
            mesh.SetTriangles(all, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
