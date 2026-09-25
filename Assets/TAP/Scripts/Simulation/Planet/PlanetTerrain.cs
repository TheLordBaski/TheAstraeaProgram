using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>Background worker threads that generate terrain chunk geometry.</summary>
    public static class TerrainWorkers
    {
        private static readonly BlockingCollection<Action> Queue = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
        private static Thread[] _threads;
        public static int Pending => Queue.Count;

        public static void Enqueue(Action job)
        {
            EnsureStarted();
            Queue.Add(job);
        }

        private static void EnsureStarted()
        {
            if (_threads != null) return;
            int n = Mathf.Clamp(Environment.ProcessorCount - 2, 1, 6);
            _threads = new Thread[n];
            for (int i = 0; i < n; i++)
            {
                _threads[i] = new Thread(() =>
                {
                    foreach (var job in Queue.GetConsumingEnumerable())
                    {
                        try { job(); }
                        catch (Exception e) { Debug.LogException(e); }
                    }
                }) { IsBackground = true, Name = "TerrainWorker" + i, Priority = System.Threading.ThreadPriority.BelowNormal };
                _threads[i].Start();
            }
        }
    }

    public sealed class TerrainNode
    {
        public PlanetTerrain Owner;
        public int Face, Level;
        public double U0, V0, Size;
        public TerrainNode Parent;
        public TerrainNode[] Children;
        public Vector3d CenterDir;
        public double AngularRadius;
        public double ArcSize;

        public volatile ChunkData Pending;
        public int State; // 0 none, 1 requested, 2 ready
        public Mesh Mesh;
        public GameObject Go;
        public Vector3d MeshCenterBF;
        public double MinH, MaxH;
        public bool Shown;
        public int LastUsedFrame;
        public int Generation; // invalidation counter
    }

    /// <summary>
    /// Renders one body's surface as a chunked LOD quadtree over an equi-angular cube-sphere.
    /// A node is split only when all four children have meshes (no holes); skirts hide LOD cracks.
    /// </summary>
    public sealed class PlanetTerrain : MonoBehaviour
    {
        public CelestialBody Body;
        public int ChunkResolution = 32;
        public int MaxLevel = 12;
        public float SplitFactor = 2.2f;
        public Material Material;
        public int MeshBuildBudget = 12;

        private TerrainNode[] _roots;
        private readonly ConcurrentQueue<TerrainNode> _completed = new ConcurrentQueue<TerrainNode>();
        private readonly List<TerrainNode> _shownLastFrame = new List<TerrainNode>();
        private readonly List<TerrainNode> _shownThisFrame = new List<TerrainNode>();
        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private Vector3d _camBF;
        private double _camDist;
        private double _horizonAngle;
        private int _frame;
        public int VisibleChunks => _shownLastFrame.Count;
        public bool Hidden;

        public void Init(CelestialBody body, Material mat)
        {
            Body = body;
            Material = mat;
            double arc0 = Math.PI * 0.5 * body.Radius;
            MaxLevel = Mathf.Clamp(Mathf.RoundToInt((float)Math.Log(arc0 / (ChunkResolution * 3.2), 2)), 4, 16);
            _roots = new TerrainNode[6];
            for (int f = 0; f < 6; f++) _roots[f] = MakeNode(null, f, 0, -1, -1, 2);
        }

        public int CollisionLevel => MaxLevel;

        private TerrainNode MakeNode(TerrainNode parent, int face, int level, double u0, double v0, double size)
        {
            var n = new TerrainNode { Owner = this, Parent = parent, Face = face, Level = level, U0 = u0, V0 = v0, Size = size };
            n.CenterDir = CubeSphere.ToSphere(face, u0 + size * 0.5, v0 + size * 0.5);
            Vector3d corner = CubeSphere.ToSphere(face, u0, v0);
            n.AngularRadius = Vector3d.AngleRad(n.CenterDir, corner) * 1.05;
            n.ArcSize = size * Math.PI * 0.25 * Body.Radius;
            n.MinH = Body.Terrain.MinHeight;
            n.MaxH = Body.Terrain.MaxHeight;
            return n;
        }

        /// <summary>
        /// Updates LOD and chunk transforms. camTrue: camera position relative to the body centre (inertial);
        /// bodyCenterUnity: body centre in Unity space; bodyRot: body rotation (body-fixed -> inertial).
        /// </summary>
        public void UpdateTerrain(Vector3d camTrue, Vector3d bodyCenterUnity, QuaternionD bodyRot)
        {
            _frame++;
            _camBF = bodyRot.Inverse() * camTrue;
            _camDist = _camBF.magnitude;
            double r = Body.Radius + Math.Min(Body.Terrain.MinHeight, 0);
            _horizonAngle = _camDist > r ? Math.Acos(Math.Min(1, r / _camDist)) : Math.PI;
            BuildCompleted();
            _shownThisFrame.Clear();
            if (!Hidden)
                foreach (var root in _roots) Visit(root);

            Quaternion rot = bodyRot.ToQuaternion();
            foreach (var n in _shownThisFrame)
            {
                if (n.Go == null) continue;
                if (!n.Shown) { n.Go.SetActive(true); n.Shown = true; }
                Vector3d wp = bodyCenterUnity + bodyRot * n.MeshCenterBF;
                n.Go.transform.SetPositionAndRotation((Vector3)wp, rot);
                n.LastUsedFrame = _frame;
            }
            foreach (var n in _shownLastFrame)
                if (n.LastUsedFrame != _frame && n.Go != null && n.Shown) { n.Go.SetActive(false); n.Shown = false; }
            _shownLastFrame.Clear();
            _shownLastFrame.AddRange(_shownThisFrame);
            if (_frame % 120 == 0) Prune();
        }

        private bool IsVisible(TerrainNode n)
        {
            double ang = Vector3d.AngleRad(n.CenterDir, _camBF);
            double peak = Body.Terrain.MaxHeight > 0 ? Math.Acos(Math.Min(1, Body.Radius / (Body.Radius + Body.Terrain.MaxHeight))) : 0;
            return ang < _horizonAngle + peak + n.AngularRadius + 0.02;
        }

        private double DistanceTo(TerrainNode n)
        {
            double h = 0.5 * (n.MinH + n.MaxH);
            Vector3d c = n.CenterDir * (Body.Radius + h);
            double d = (c - _camBF).magnitude - n.ArcSize * 0.75;
            return Math.Max(0, d);
        }

        private void Visit(TerrainNode n)
        {
            if (!IsVisible(n)) return;
            if (n.State == 0) Request(n);
            bool wantSplit = n.Level < MaxLevel && DistanceTo(n) < n.ArcSize * SplitFactor;
            if (wantSplit)
            {
                if (n.Children == null)
                {
                    double h = n.Size * 0.5;
                    n.Children = new[]
                    {
                        MakeNode(n, n.Face, n.Level + 1, n.U0, n.V0, h),
                        MakeNode(n, n.Face, n.Level + 1, n.U0 + h, n.V0, h),
                        MakeNode(n, n.Face, n.Level + 1, n.U0, n.V0 + h, h),
                        MakeNode(n, n.Face, n.Level + 1, n.U0 + h, n.V0 + h, h),
                    };
                }
                bool allReady = true;
                foreach (var c in n.Children)
                {
                    if (c.State == 0) Request(c);
                    if (c.State != 2) allReady = false;
                }
                if (allReady)
                {
                    foreach (var c in n.Children) Visit(c);
                    return;
                }
            }
            if (n.State == 2) _shownThisFrame.Add(n);
        }

        private void Request(TerrainNode n)
        {
            n.State = 1;
            int gen = n.Generation;
            var body = Body;
            int res = ChunkResolution;
            bool ocean = body.HasOcean;
            TerrainWorkers.Enqueue(() =>
            {
                if (gen != n.Generation) return;
                n.Pending = TerrainChunkGenerator.Generate(body.Terrain, body.Radius, n.Face, n.U0, n.V0, n.Size, res, ocean, true);
                _completed.Enqueue(n);
            });
        }

        private void BuildCompleted()
        {
            int built = 0;
            float start = Time.realtimeSinceStartup;
            while (built < MeshBuildBudget && _completed.TryDequeue(out var n))
            {
                var d = n.Pending;
                n.Pending = null;
                if (d == null || n.State != 1) continue;
                if (n.Mesh == null) n.Mesh = new Mesh { name = $"terrain_{Body.Id}_{n.Face}_{n.Level}" };
                n.Mesh.Clear();
                n.Mesh.SetVertices(d.Positions);
                n.Mesh.SetNormals(d.Normals);
                n.Mesh.SetColors(d.Colors);
                n.Mesh.SetUVs(0, d.UV0);
                n.Mesh.SetUVs(1, d.UV1);
                n.Mesh.SetTriangles(d.Triangles, 0, false);
                n.Mesh.bounds = new Bounds((d.BoundsMin + d.BoundsMax) * 0.5f, d.BoundsMax - d.BoundsMin);
                n.MeshCenterBF = d.CenterBF;
                n.MinH = d.MinHeight;
                n.MaxH = d.MaxHeight;
                if (n.Go == null)
                {
                    n.Go = _pool.Count > 0 ? _pool.Pop() : CreateChunkObject();
                    n.Go.name = n.Mesh.name;
                    n.Go.GetComponent<MeshFilter>().sharedMesh = n.Mesh;
                    n.Go.SetActive(false);
                }
                n.Shown = false;
                n.State = 2;
                built++;
                if (Time.realtimeSinceStartup - start > 0.006f) break;
            }
        }

        private GameObject CreateChunkObject()
        {
            var go = new GameObject("chunk");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;
            go.layer = Layers.Default;
            return go;
        }

        /// <summary>Frees meshes of deep nodes that have not been used for a while.</summary>
        private void Prune()
        {
            foreach (var r in _roots) PruneNode(r);
        }

        private void PruneNode(TerrainNode n)
        {
            if (n.Children == null) return;
            bool anyRecent = false;
            foreach (var c in n.Children)
            {
                PruneNode(c);
                if (c.LastUsedFrame > _frame - 600 || c.Children != null || c.State == 1) anyRecent = true;
            }
            if (!anyRecent && n.Level >= 3)
            {
                foreach (var c in n.Children) Release(c);
                n.Children = null;
            }
        }

        private void Release(TerrainNode n)
        {
            n.Generation++;
            if (n.Go != null) { n.Go.SetActive(false); _pool.Push(n.Go); n.Go = null; }
            if (n.Mesh != null) { Destroy(n.Mesh); n.Mesh = null; }
            n.State = 0;
            n.Shown = false;
        }

        public void SetHidden(bool hidden)
        {
            Hidden = hidden;
            if (hidden)
                foreach (var n in _shownLastFrame) if (n.Go != null) { n.Go.SetActive(false); n.Shown = false; }
        }

        private void OnDestroy()
        {
            // Stop pending jobs writing into destroyed nodes.
            if (_roots != null) foreach (var r in _roots) Invalidate(r);
        }

        private void Invalidate(TerrainNode n)
        {
            n.Generation++;
            if (n.Mesh != null) Destroy(n.Mesh);
            if (n.Children != null) foreach (var c in n.Children) Invalidate(c);
        }
    }
}
