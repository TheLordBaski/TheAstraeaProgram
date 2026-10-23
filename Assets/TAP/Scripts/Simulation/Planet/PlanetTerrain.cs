using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Background worker threads that generate terrain chunk geometry. Other work (colliders, the far view) goes first, in order.
    /// Surface chunks wait in a list and the lowest <see cref="TerrainNode.Priority"/> goes next, which their terrain re-ranks every
    /// frame (FND-20).
    /// </summary>
    public static class TerrainWorkers
    {
        private static readonly object Lock = new object();
        private static readonly Queue<Action> Jobs = new Queue<Action>();
        private static readonly List<TerrainNode> Chunks = new List<TerrainNode>();
        private static Thread[] _threads;
        private static bool _paused;
        /// <summary>First in, first out, with no look-ahead and nothing dropped: the order before FND-20, to compare against (<c>-terrain-fifo</c>).</summary>
        public static bool Fifo = Array.IndexOf(Environment.GetCommandLineArgs(), "-terrain-fifo") >= 0;
        /// <summary>Worker threads: all cores but two, 1 to 6, or <c>-terrain-workers n</c> (fewer fill the queue, to compare orders).</summary>
        public static readonly int Count = WorkerCount();
        /// <summary>Chunk requests dropped before a worker took them, because the LOD no longer wanted them (main thread).</summary>
        public static long Dropped;
        public static int Pending { get { lock (Lock) return Jobs.Count + Chunks.Count; } }

        public static void Enqueue(Action job)
        {
            lock (Lock)
            {
                Jobs.Enqueue(job);
                Start();
                Monitor.Pulse(Lock);
            }
        }

        /// <summary>Queues a chunk for its terrain's <see cref="PlanetTerrain.Build"/>; false if it is waiting already.</summary>
        public static bool Enqueue(TerrainNode n)
        {
            lock (Lock)
            {
                if (n.Queued) return false;
                n.Queued = true;
                Chunks.Add(n);
                Start();
                Monitor.Pulse(Lock);
                return true;
            }
        }

        /// <summary>Takes a chunk back off the queue; false if it was not waiting (a worker has it, or it was never queued).</summary>
        public static bool Remove(TerrainNode n)
        {
            lock (Lock)
            {
                if (!n.Queued) return false;
                Chunks.Remove(n);
                n.Queued = false;
                return true;
            }
        }

        /// <summary>Tests: while paused the workers take nothing, and <see cref="TakeNext"/> hands out what they would have.</summary>
        public static void Pause(bool paused)
        {
            lock (Lock)
            {
                _paused = paused;
                Monitor.PulseAll(Lock);
            }
        }

        public static TerrainNode TakeNext()
        {
            lock (Lock) return Chunks.Count > 0 ? Take() : null;
        }

        /// <summary>Tests: the chunks waiting now.</summary>
        public static List<TerrainNode> Waiting()
        {
            lock (Lock) return new List<TerrainNode>(Chunks);
        }

        private static int WorkerCount()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-terrain-workers");
            if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int n) && n > 0) return n;
            return Mathf.Clamp(Environment.ProcessorCount - 2, 1, 6);
        }

        // under the lock
        private static void Start()
        {
            if (_threads != null) return;
            _threads = new Thread[Count];
            for (int i = 0; i < Count; i++)
            {
                _threads[i] = new Thread(Work) { IsBackground = true, Name = "TerrainWorker" + i, Priority = System.Threading.ThreadPriority.BelowNormal };
                _threads[i].Start();
            }
        }

        private static void Work()
        {
            while (true)
            {
                Action job = null;
                TerrainNode chunk = null;
                lock (Lock)
                {
                    while (_paused || (Jobs.Count == 0 && Chunks.Count == 0)) Monitor.Wait(Lock);
                    if (Jobs.Count > 0) job = Jobs.Dequeue();
                    else chunk = Take();
                }
                try
                {
                    if (job != null) job();
                    else chunk.Owner.Build(chunk);
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        // under the lock: the lowest rank, or the oldest in Fifo
        private static TerrainNode Take()
        {
            int best = 0;
            if (!Fifo)
                for (int i = 1; i < Chunks.Count; i++)
                    if (Chunks[i].Priority < Chunks[best].Priority) best = i;
            var n = Chunks[best];
            if (Fifo) Chunks.RemoveAt(0);
            else
            {
                Chunks[best] = Chunks[Chunks.Count - 1];
                Chunks.RemoveAt(Chunks.Count - 1);
            }
            n.Queued = false;
            return n;
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

        public int State; // 0 none, 1 requested, 2 ready (a refresh after a terrain edit keeps it at 2)
        public Mesh Mesh;
        public GameObject Go;
        public Vector3d MeshCenterBF;
        public double MinH, MaxH;
        public bool Shown;
        public int LastUsedFrame;
        public int Generation; // bumped when the node is released or its ground changes: older results are dropped
        // the worker queue (FND-20): the rank (lower is built sooner), whether it waits there (TerrainWorkers' lock), the last frame the LOD wanted it
        public double Priority;
        public bool Queued;
        public int Wanted, Needed; // Needed: by the LOD at the camera itself, not only ahead
    }

    /// <summary>
    /// Renders one body's surface as a chunked LOD quadtree over an equi-angular cube-sphere.
    /// A node is split only when all four children have meshes (no holes); skirts hide LOD cracks.
    /// </summary>
    public sealed class PlanetTerrain : MonoBehaviour
    {
        /// <summary>
        /// The faint light on night sides seen from space, and on airless ground at night: dim, but enough to make out
        /// the ground after tone mapping. The far view uses the same, so nothing changes where it takes over.
        /// </summary>
        public static readonly Color SpaceAmbient = new Color(0.2f, 0.215f, 0.265f);

        public CelestialBody Body;
        public int ChunkResolution = 32;
        public int MaxLevel = 12;
        public float SplitFactor = 2.2f;
        /// <summary>The player's terrain detail setting: scales how near the terrain splits into finer chunks (1 = as designed).</summary>
        public static float DetailScale = 1f;
        /// <summary>LOD morphing between a chunk and its parent (TerrainMorph.hlsl); off only to compare against.</summary>
        public static bool MorphEnabled = true;
        private static readonly int MorphSplitId = Shader.PropertyToID("_TerrainMorphSplit");

        /// <summary>The chunk meshes' vertex layout: <see cref="TerrainVertex"/>.</summary>
        private static readonly UnityEngine.Rendering.VertexAttributeDescriptor[] VertexLayout =
        {
            new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Position, UnityEngine.Rendering.VertexAttributeFormat.Float32, 3),
            new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Normal, UnityEngine.Rendering.VertexAttributeFormat.Float32, 3),
            new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.Color, UnityEngine.Rendering.VertexAttributeFormat.UNorm8, 4),
            new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.TexCoord0, UnityEngine.Rendering.VertexAttributeFormat.Float32, 2),
            new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.TexCoord1, UnityEngine.Rendering.VertexAttributeFormat.Float32, 2),
            new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.TexCoord2, UnityEngine.Rendering.VertexAttributeFormat.Float16, 4),
            new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.TexCoord3, UnityEngine.Rendering.VertexAttributeFormat.Float16, 2),
            new UnityEngine.Rendering.VertexAttributeDescriptor(UnityEngine.Rendering.VertexAttribute.TexCoord4, UnityEngine.Rendering.VertexAttributeFormat.UNorm8, 4),
        };
        public Material Material;
        public void SetVisualMaterial(Material material)
        {
            Material = material;
            EnableMorph(material);
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial = material;
        }

        /// <summary>Only this terrain's chunks carry LOD morph data (UV2, UV3): its material alone may read them.</summary>
        private static void EnableMorph(Material material)
        {
            if (material != null) material.SetFloat(LodMorphId, 1);
        }
        private static readonly int LodMorphId = Shader.PropertyToID("_LodMorph");
        public int MeshBuildBudget = 12;

        private TerrainNode[] _roots;
        private readonly ConcurrentQueue<(TerrainNode node, ChunkData data, int gen)> _completed = new ConcurrentQueue<(TerrainNode, ChunkData, int)>();
        private readonly List<TerrainNode> _shownLastFrame = new List<TerrainNode>();
        private readonly List<TerrainNode> _shownThisFrame = new List<TerrainNode>();
        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private Vector3d _camBF;
        private double _camDist;
        private double _horizonAngle;
        /// <summary>How far ahead (s) the terrain asks for the chunks the camera will want, along its velocity (FND-20).</summary>
        public const double LookAhead = 1.5;
        private const double MaxLookAheadSpeed = 20000; // m/s; faster is a jump (another vessel, a new scene), not a flight
        private Vector3d _aheadBF, _viewBF, _velocityBF;
        private double _aheadHorizon, _viewCos, _viewSin, _lastTime = -1, _clock, _prefetchClock = double.NegativeInfinity, _peak;
        private int _prefetchFrame = -1;
        private readonly List<TerrainNode> _queued = new List<TerrainNode>(); // this terrain's chunks in TerrainWorkers' queue
        private int _frame;
        private int _camFace;
        private double _camU, _camV;
        public int VisibleChunks => _shownLastFrame.Count;
        /// <summary>Chunk meshes filled in <see cref="BuildCompleted"/> by every body's terrain since the game started (the benchmark reads it).</summary>
        public static long MeshUploads;
        /// <summary>The level of the chunk drawn under the camera, -1 when none is drawn there.</summary>
        public int LevelUnderCamera { get; private set; } = -1;
        /// <summary>The chunk under the camera is coarser than the LOD wants: its children are still being made.</summary>
        public bool DetailBehind { get; private set; }
        public bool Hidden;
        /// <summary>
        /// Everything in view is drawn: every root chunk facing the camera has its mesh. A node only splits once all its
        /// children are ready, so there are no holes deeper down. Until then the far view fills in behind.
        /// </summary>
        public bool Complete { get; private set; }

        public void Init(CelestialBody body, Material mat)
        {
            Body = body;
            Material = mat;
            EnableMorph(mat);
            double arc0 = Math.PI * 0.5 * body.Radius;
            MaxLevel = Mathf.Clamp(Mathf.RoundToInt((float)Math.Log(arc0 / (ChunkResolution * 3.2), 2)), 4, 16);
            // how far past the horizon the highest peaks still show
            _peak = body.Terrain.MaxHeight > 0 ? Math.Acos(Math.Min(1, body.Radius / (body.Radius + body.Terrain.MaxHeight))) : 0;
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
        /// bodyCenterUnity: body centre in Unity space; bodyRot: body rotation (body-fixed -> inertial); view: the camera, whose view
        /// cone is built first.
        /// </summary>
        public void UpdateTerrain(Vector3d camTrue, Vector3d bodyCenterUnity, QuaternionD bodyRot, Camera view = null)
        {
            double now = Time.realtimeSinceStartupAsDouble, dt = _lastTime < 0 ? 0 : now - _lastTime;
            _lastTime = now;
            Vector3d forward = view != null ? (Vector3d)view.transform.forward : Vector3d.zero;
            double half = view != null ? Math.Atan(Math.Tan(view.fieldOfView * 0.5 * MathD.Deg2Rad) * Math.Sqrt(1 + view.aspect * view.aspect)) : Math.PI;
            UpdateTerrain(camTrue, bodyCenterUnity, bodyRot, dt, forward, half);
        }

        /// <summary>
        /// As above, with dt: real seconds since the last update (0: unknown), forward: where the camera looks (inertial, Unity's axes; zero:
        /// everywhere), halfAngle: from there to a corner of the view (rad).
        /// </summary>
        public void UpdateTerrain(Vector3d camTrue, Vector3d bodyCenterUnity, QuaternionD bodyRot, double dt, Vector3d forward, double halfAngle)
        {
            _frame++;
            Vector3d last = _camBF;
            _camBF = bodyRot.Inverse() * camTrue;
            _camDist = _camBF.magnitude;
            double r = Body.Radius + Math.Min(Body.Terrain.MinHeight, 0);
            _horizonAngle = Horizon(_camDist, r);
            Vector3d velocity = dt > 0 ? (_camBF - last) / dt : Vector3d.zero;
            if (velocity.magnitude > MaxLookAheadSpeed) _velocityBF = Vector3d.zero;
            else if (dt > 0) _velocityBF = Vector3d.Lerp(_velocityBF, velocity, 1 - Math.Exp(-dt / 0.3)); // frame times jitter
            // Where the camera will be. A straight line overshoots a curving, slowing path, so it reaches at most twice the camera's
            // height, and a fall keeps at least half of that height (the LOD scales with it).
            double height = Math.Max(_camDist - Body.Radius, 1);
            Vector3d move = _velocityBF * LookAhead;
            if (move.magnitude > 2 * height) move = move.normalized * (2 * height);
            _aheadBF = _camBF + move;
            if (_aheadBF.magnitude < Body.Radius + 0.5 * height) _aheadBF = _aheadBF.normalized * (Body.Radius + 0.5 * height);
            _aheadHorizon = Horizon(_aheadBF.magnitude, r);
            // Moving less than a tenth of its height, the LOD ahead is the LOD here. The look-ahead runs ten times a second.
            _clock += dt;
            bool lookAhead = !TerrainWorkers.Fifo && (_aheadBF - _camBF).magnitude > 0.1 * height;
            bool prefetch = lookAhead && _clock - _prefetchClock >= 0.1;
            if (prefetch) { _prefetchClock = _clock; _prefetchFrame = _frame; }
            else if (!lookAhead) _prefetchFrame = -1;
            _viewBF = (bodyRot.Inverse() * forward).normalized;
            _viewCos = Math.Cos(halfAngle);
            _viewSin = Math.Sin(halfAngle);
            Shader.SetGlobalFloat(MorphSplitId, MorphEnabled ? SplitFactor * DetailScale : 0); // the split rule of Visit, for TerrainMorph.hlsl
            BuildCompleted();
            _shownThisFrame.Clear();
            CubeSphere.FromSphere(_camBF, out _camFace, out _camU, out _camV);
            LevelUnderCamera = -1;
            DetailBehind = false;
            bool complete = !Hidden;
            if (!Hidden)
                foreach (var root in _roots)
                {
                    Visit(root);
                    if (root.State != 2 && IsVisible(root)) complete = false;
                    if (prefetch) Prefetch(root);
                }
            Complete = complete;
            Rank();

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

        private static double Horizon(double dist, double r) => dist > r ? Math.Acos(Math.Min(1, r / dist)) : Math.PI;

        private bool IsVisible(TerrainNode n) => IsVisible(n, _camBF, _horizonAngle);

        private bool IsVisible(TerrainNode n, Vector3d eye, double horizon)
        {
            double ang = Vector3d.AngleRad(n.CenterDir, eye);
            return ang < horizon + _peak + n.AngularRadius + 0.02;
        }

        private double DistanceTo(TerrainNode n) => DistanceTo(n, _camBF);

        private double DistanceTo(TerrainNode n, Vector3d eye) => Math.Max(0, CentreDistance(n, eye) - n.ArcSize * 0.75);

        private double CentreDistance(TerrainNode n, Vector3d eye) => (n.CenterDir * (Body.Radius + 0.5 * (n.MinH + n.MaxH)) - eye).magnitude;

        private bool WantsSplit(TerrainNode n, Vector3d eye) => n.Level < MaxLevel && DistanceTo(n, eye) < n.ArcSize * SplitFactor * DetailScale;

        private void Split(TerrainNode n)
        {
            if (n.Children != null) return;
            double h = n.Size * 0.5;
            n.Children = new[]
            {
                MakeNode(n, n.Face, n.Level + 1, n.U0, n.V0, h),
                MakeNode(n, n.Face, n.Level + 1, n.U0 + h, n.V0, h),
                MakeNode(n, n.Face, n.Level + 1, n.U0, n.V0 + h, h),
                MakeNode(n, n.Face, n.Level + 1, n.U0 + h, n.V0 + h, h),
            };
        }

        /// <summary>
        /// The LOD wants this node's mesh this frame, at the camera (<paramref name="now"/>) or ahead: it is asked for if it has none,
        /// and kept while it waits.
        /// </summary>
        private void Want(TerrainNode n, bool now)
        {
            n.Wanted = _frame;
            if (now) n.Needed = _frame;
            if (n.State == 0) Request(n);
        }

        private void Visit(TerrainNode n)
        {
            if (!IsVisible(n)) return;
            Want(n, true);
            bool wantSplit = WantsSplit(n, _camBF);
            if (wantSplit)
            {
                Split(n);
                bool allReady = true;
                foreach (var c in n.Children)
                {
                    Want(c, true);
                    if (c.State != 2) allReady = false;
                }
                if (allReady)
                {
                    foreach (var c in n.Children) Visit(c);
                    return;
                }
            }
            if (n.State != 2) return;
            _shownThisFrame.Add(n);
            if (n.Face == _camFace && _camU >= n.U0 && _camU <= n.U0 + n.Size && _camV >= n.V0 && _camV <= n.V0 + n.Size)
            {
                LevelUnderCamera = n.Level;
                DetailBehind = wantSplit;
            }
        }

        /// <summary>
        /// Look-ahead (FND-20): asks for the chunks the LOD will want where the camera is going, <see cref="LookAhead"/> s along its
        /// velocity, before it gets there. Unlike <see cref="Visit"/> it goes on below any ready chunk, so the next level is asked for
        /// while the last one is still being made. These rank after what the camera needs now (<see cref="RankOf"/>), so they use
        /// idle workers. Nothing is drawn early: what is drawn still follows the camera, as the LOD morph needs.
        /// </summary>
        private void Prefetch(TerrainNode n)
        {
            if (!IsVisible(n, _aheadBF, _aheadHorizon)) return;
            Want(n, false);
            if (n.State != 2 || !WantsSplit(n, _aheadBF)) return;
            Split(n);
            foreach (var c in n.Children) Want(c, false);
            foreach (var c in n.Children) Prefetch(c);
        }

        private void Request(TerrainNode n)
        {
            if (n.State == 0) n.State = 1;
            n.Priority = RankOf(n);
            if (TerrainWorkers.Enqueue(n)) _queued.Add(n);
        }

        /// <summary>On a worker thread: makes a requested chunk. <see cref="BuildCompleted"/> drops it if the node changed meanwhile.</summary>
        public void Build(TerrainNode n)
        {
            int gen = n.Generation;
            _completed.Enqueue((n, TerrainChunkGenerator.Generate(Body.Terrain, Body.Radius, n.Face, n.U0, n.V0, n.Size, ChunkResolution, Body.HasOcean, true), gen));
        }

        /// <summary>
        /// Re-ranks this terrain's waiting chunks, and drops those the LOD no longer wants, now or at the last look-ahead (FND-20). A
        /// ready chunk waiting to be made again (new ground after a terrain edit) is never dropped: it would keep the old ground.
        /// </summary>
        private void Rank()
        {
            for (int i = _queued.Count - 1; i >= 0; i--)
            {
                var n = _queued[i];
                if (n.Queued && (TerrainWorkers.Fifo || n.Wanted == _frame || n.Wanted == _prefetchFrame || n.State == 2))
                {
                    if (!TerrainWorkers.Fifo) n.Priority = RankOf(n);
                    continue;
                }
                if (n.Queued && TerrainWorkers.Remove(n))
                {
                    n.State = 0;
                    TerrainWorkers.Dropped++;
                }
                _queued[i] = _queued[_queued.Count - 1];
                _queued.RemoveAt(_queued.Count - 1);
            }
        }

        /// <summary>
        /// A waiting chunk's rank, lower first: the camera's distance from its parent in the parent's arcs (the parent's error on
        /// screen), so all four children of a split rank alike and none holds the split up. A hundredth of the distance to the parent's
        /// middle orders the chunks the camera is over. Out of view counts four times as far; wanted only ahead, three arcs further.
        /// </summary>
        private double RankOf(TerrainNode n)
        {
            var p = n.Parent ?? n;
            bool now = n.Needed == _frame;
            Vector3d centre = p.CenterDir * (Body.Radius + 0.5 * (p.MinH + p.MaxH));
            double c = (centre - (now ? _camBF : _aheadBF)).magnitude, radius = p.ArcSize * 0.75;
            double rank = (Math.Max(0, c - radius) + 0.01 * c) / p.ArcSize;
            if (!InView(centre, radius)) rank *= 4;
            return now ? rank : rank + 3;
        }

        /// <summary>
        /// Whether a sphere is in the camera's view cone, or holds the camera. The cone's tip moves back until the cone touches the
        /// sphere, which leaves a point test with no trigonometry (this runs for every waiting chunk every frame).
        /// </summary>
        private bool InView(Vector3d centre, double radius)
        {
            if (_viewBF.sqrMagnitude == 0) return true;
            Vector3d to = centre - _camBF;
            if (to.sqrMagnitude <= radius * radius) return true;
            to += _viewBF * (radius / _viewSin);
            return Vector3d.Dot(to, _viewBF) > to.magnitude * _viewCos;
        }

        /// <summary>
        /// The ground changed within <paramref name="angle"/> (rad) of a body-fixed direction (a flat area came or went):
        /// every chunk there is made again. Chunks keep showing their old mesh until the new one is ready.
        /// </summary>
        public void Invalidate(Vector3d centerBF, double angle)
        {
            if (_roots == null) return;
            centerBF = centerBF.normalized;
            foreach (var r in _roots) Invalidate(r, centerBF, angle);
        }

        private void Invalidate(TerrainNode n, Vector3d c, double angle)
        {
            if (Vector3d.AngleRad(n.CenterDir, c) > n.AngularRadius + angle) return;
            if (n.State != 0)
            {
                n.Generation++;
                Request(n);
            }
            if (n.Children != null) foreach (var ch in n.Children) Invalidate(ch, c, angle);
        }

        private void BuildCompleted()
        {
            int built = 0;
            float start = Time.realtimeSinceStartup;
            while (built < MeshBuildBudget && _completed.TryDequeue(out var item))
            {
                var (n, d, gen) = item;
                if (d == null || gen != n.Generation || n.State == 0) { TerrainChunkGenerator.Release(d); continue; }
                // Meshes keep no CPU copy (UploadMeshData below), so a chunk made again (a terrain edit) gets a new one.
                if (n.Mesh != null) Destroy(n.Mesh);
                n.Mesh = new Mesh { name = $"terrain_{Body.Id}_{n.Face}_{n.Level}" };
                n.Mesh.SetVertexBufferParams(d.Vertices.Length, VertexLayout);
                n.Mesh.SetVertexBufferData(d.Vertices, 0, 0, d.Vertices.Length);
                n.Mesh.SetTriangles(d.Triangles, 0, false);
                n.Mesh.bounds = new Bounds((d.BoundsMin + d.BoundsMax) * 0.5f, d.BoundsMax - d.BoundsMin);
                n.Mesh.UploadMeshData(true);
                n.MeshCenterBF = d.CenterBF;
                n.MinH = d.MinHeight;
                n.MaxH = d.MaxHeight;
                TerrainChunkGenerator.Release(d); // the mesh holds its own copy now
                if (n.Go == null)
                {
                    n.Go = _pool.Count > 0 ? _pool.Pop() : CreateChunkObject();
                    n.Go.name = n.Mesh.name;
                    n.Go.GetComponent<MeshFilter>().sharedMesh = n.Mesh;
                    n.Go.SetActive(false);
                    n.Shown = false;
                }
                else n.Go.GetComponent<MeshFilter>().sharedMesh = n.Mesh;
                n.State = 2;
                built++;
                MeshUploads++;
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
                if (c.Wanted > _frame - 600 || c.Children != null || c.State == 1) anyRecent = true;
            }
            if (!anyRecent && n.Level >= 3)
            {
                foreach (var c in n.Children) Release(c);
                n.Children = null;
            }
        }

        private void Release(TerrainNode n)
        {
            TerrainWorkers.Remove(n);
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
            foreach (var n in _queued) TerrainWorkers.Remove(n);
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
