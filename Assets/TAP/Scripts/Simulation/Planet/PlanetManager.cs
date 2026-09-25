using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Kinematic collision chunk near vessels. Moved every physics step with MovePosition/MoveRotation so
    /// PhysX sees the correct surface velocity of the rotating body.
    /// </summary>
    public sealed class ColliderChunk
    {
        public (int face, int iu, int iv) Key;
        /// <summary>UT of the pose the chunk was last moved to (teleport, not sweep, after any gap such as time warp).</summary>
        public double LastMoveUT = double.NaN;
        public GameObject Go;
        public Rigidbody Rb;
        public MeshCollider Collider;
        public Mesh Mesh;
        public Vector3d CenterBF;
        public double LastNeededUT;
        public volatile ChunkData Pending;
        public bool Ready;
    }

    /// <summary>A static structure fixed to a body's surface (launch pad, buildings).</summary>
    public sealed class SurfaceObject
    {
        public double LastMoveUT = double.NaN;
        public CelestialBody Body;
        public Vector3d PositionBF;
        public QuaternionD RotationBF;
        public GameObject Go;
        public Rigidbody Rb;
    }

    /// <summary>Terrain rendering for all bodies, collision chunks for the frame body, and surface structures.</summary>
    public sealed class PlanetManager : MonoBehaviour
    {
        public FlightSim Sim;
        public Camera Camera;
        public Material TerrainMaterial;
        public readonly Dictionary<CelestialBody, PlanetTerrain> Terrains = new Dictionary<CelestialBody, PlanetTerrain>();
        public readonly List<SurfaceObject> SurfaceObjects = new List<SurfaceObject>();
        public readonly List<AtmosphereShell> Atmospheres = new List<AtmosphereShell>();

        private readonly Dictionary<(int, int, int), ColliderChunk> _colliders = new Dictionary<(int, int, int), ColliderChunk>();
        private readonly ConcurrentQueue<ColliderChunk> _colliderDone = new ConcurrentQueue<ColliderChunk>();
        private CelestialBody _colliderBody;
        private double _nextNeedCheck;
        private GameObject _colliderRoot;
        private PhysicsMaterial _groundMat;

        public void Init(FlightSim sim, Camera cam, Material terrainMat)
        {
            Sim = sim;
            Camera = cam;
            TerrainMaterial = terrainMat;
            foreach (var b in sim.System.Bodies)
            {
                var go = new GameObject("Terrain " + b.Name);
                go.transform.SetParent(transform, false);
                var t = go.AddComponent<PlanetTerrain>();
                t.Init(b, terrainMat);
                Terrains[b] = t;
                if (b.Atmosphere != null)
                {
                    var shell = AtmosphereShell.Create(b, transform);
                    if (shell != null) Atmospheres.Add(shell);
                }
            }
            _colliderRoot = new GameObject("TerrainColliders");
            _colliderRoot.transform.SetParent(transform, false);
            _groundMat = new PhysicsMaterial("Ground") { dynamicFriction = 0.7f, staticFriction = 0.9f, bounciness = 0.02f, frictionCombine = PhysicsMaterialCombine.Average };
        }

        public void OnFrameBodyChanged(CelestialBody body)
        {
            if (_colliderBody == body) return;
            foreach (var c in _colliders.Values) if (c.Go != null) Destroy(c.Go);
            _colliders.Clear();
            _colliderBody = body;
        }

        /// <summary>Places all surface objects at their exact pose for UT (teleport), e.g. right after initialization.</summary>
        public void SyncSurfaceObjects(double ut)
        {
            var frame = Sim.Frame;
            foreach (var s in SurfaceObjects)
            {
                if (s.Go == null) continue;
                Vector3d wp = frame.BodyPosition(s.Body, ut) - frame.Origin + s.Body.RotationAtUT(ut) * s.PositionBF;
                Quaternion wr = (s.Body.RotationAtUT(ut) * s.RotationBF).ToQuaternion();
                s.Go.transform.SetPositionAndRotation((Vector3)wp, wr);
                if (s.Rb != null) { s.Rb.position = (Vector3)wp; s.Rb.rotation = wr; }
            }
            Physics.SyncTransforms();
        }

        public void OnOriginShift(Vector3 delta)
        {
            foreach (var c in _colliders.Values)
            {
                if (c.Rb == null) continue;
                c.Rb.position -= delta;
                c.Go.transform.position -= delta;
            }
            foreach (var s in SurfaceObjects)
            {
                if (s.Go == null) continue;
                if (s.Rb != null) s.Rb.position -= delta;
                s.Go.transform.position -= delta;
            }
        }

        // ------------------------------------------------------------------ visuals

        public void UpdateVisuals(double rut, double alpha)
        {
            if (Sim == null || Camera == null) return;
            var frame = Sim.Frame;
            Vector3d renderOrigin = frame.RenderOrigin(alpha);
            Vector3d camTrue = renderOrigin + (Vector3d)Camera.transform.position; // relative to frame body
            foreach (var kv in Terrains)
            {
                var b = kv.Key;
                Vector3d bodyPos = frame.BodyPosition(b, rut);
                Vector3d bodyCenterUnity = bodyPos - renderOrigin;
                kv.Value.UpdateTerrain(camTrue - bodyPos, bodyCenterUnity, b.RotationAtUT(rut));
            }
            foreach (var s in SurfaceObjects)
            {
                if (s.Go == null) continue;
                Vector3d wp = frame.BodyPosition(s.Body, rut) - renderOrigin + s.Body.RotationAtUT(rut) * s.PositionBF;
                Quaternion wr = (s.Body.RotationAtUT(rut) * s.RotationBF).ToQuaternion();
                // Visual transform only if not physics-driven this frame (kinematic interpolation handles it).
                if (s.Rb == null || Sim.Warp.OnRails || Sim.Paused)
                {
                    s.Go.transform.SetPositionAndRotation((Vector3)wp, wr);
                    if (s.Rb != null) { s.Rb.position = (Vector3)wp; s.Rb.rotation = wr; }
                }
            }
            foreach (var a in Atmospheres) a.UpdateShell(frame, rut, renderOrigin, camTrue);
        }

        // ------------------------------------------------------------------ physics

        /// <summary>Ensures collision chunks exist around loaded vessels and moves them to their pose at utNext.</summary>
        public void PreStepColliders(double utNext, double dt)
        {
            var frame = Sim.Frame;
            var body = frame.Body;
            if (_colliderBody != body) OnFrameBodyChanged(body);
            var terrain = Terrains[body];
            int level = terrain.CollisionLevel;

            if (Sim.UT >= _nextNeedCheck)
            {
                _nextNeedCheck = Sim.UT + 0.2;
                foreach (var v in Sim.LoadedVessels)
                {
                    if (v == null || v.Rb == null) continue;
                    if (v.AltitudeAGL > 4000 && v.Altitude > body.Terrain.MaxHeight + 3000) continue;
                    Vector3d pos = frame.ToTrue(v.Rb.worldCenterOfMass);
                    Vector3d dirBF = body.InertialToBodyFixed(pos, Sim.UT).normalized;
                    double chunkArc = Math.PI * 0.5 * body.Radius / (1 << level);
                    // speed-dependent lookahead
                    double speed = v.SurfaceSpeed;
                    double reach = Math.Max(chunkArc * 0.9, Math.Min(speed * 2.0, 1500));
                    Need(body, dirBF, level, utNext);
                    Vector3d e1 = Vector3d.Cross(dirBF, Vector3d.up);
                    if (e1.sqrMagnitude < 1e-6) e1 = Vector3d.Cross(dirBF, Vector3d.right);
                    e1 = e1.normalized;
                    Vector3d e2 = Vector3d.Cross(dirBF, e1).normalized;
                    double ang = reach / body.Radius;
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                        {
                            if (i == 0 && j == 0) continue;
                            Vector3d d = (dirBF + (e1 * i + e2 * j) * ang).normalized;
                            Need(body, d, level, utNext);
                        }
                }
                // Release chunks no longer needed.
                var remove = new List<(int, int, int)>();
                foreach (var kv in _colliders)
                    if (Sim.UT - kv.Value.LastNeededUT > 5) remove.Add(kv.Key);
                foreach (var k in remove)
                {
                    var c = _colliders[k];
                    if (c.Go != null) Destroy(c.Go);
                    if (c.Mesh != null) Destroy(c.Mesh);
                    _colliders.Remove(k);
                }
            }

            // Build finished chunk meshes (collider cooking on main thread).
            int built = 0;
            while (built < 4 && _colliderDone.TryDequeue(out var cc))
            {
                if (!_colliders.ContainsKey(cc.Key) || cc.Pending == null) continue;
                BuildColliderMesh(cc);
                built++;
            }

            // Synchronous fallback: a vessel very close to the ground must have its chunk now.
            foreach (var v in Sim.LoadedVessels)
            {
                if (v == null || v.Rb == null || v.AltitudeAGL > 300) continue;
                Vector3d pos = frame.ToTrue(v.Rb.worldCenterOfMass);
                Vector3d dirBF = body.InertialToBodyFixed(pos, Sim.UT).normalized;
                var key = KeyFor(dirBF, level);
                if (_colliders.TryGetValue(key, out var c) && !c.Ready)
                {
                    if (c.Pending == null)
                        c.Pending = Generate(body, key, level);
                    BuildColliderMesh(c);
                }
            }

            // Move all colliders to the end-of-step pose. A pose that doesn't continue from the previous step's target
            // (time warp, pause, a new chunk) is a teleport, never a sweep.
            bool teleported = false;
            double tol = dt * 0.5;
            QuaternionD rot = body.RotationAtUT(utNext);
            Quaternion q = rot.ToQuaternion();
            Vector3d originNext = frame.Origin + frame.Velocity * dt;
            foreach (var c in _colliders.Values)
            {
                if (!c.Ready || c.Rb == null) continue;
                Vector3 p = (Vector3)(rot * c.CenterBF - originNext);
                teleported |= MoveGround(c.Rb, c.Go.transform, p, q, !(Math.Abs(c.LastMoveUT - Sim.UT) <= tol));
                c.LastMoveUT = utNext;
            }
            foreach (var s in SurfaceObjects)
            {
                if (s.Rb == null || s.Body != body) continue;
                Vector3 p = (Vector3)(rot * s.PositionBF - originNext);
                teleported |= MoveGround(s.Rb, s.Go.transform, p, (rot * s.RotationBF).ToQuaternion(), !(Math.Abs(s.LastMoveUT - Sim.UT) <= tol));
                s.LastMoveUT = utNext;
            }
            if (teleported) Physics.SyncTransforms();
        }

        /// <summary>
        /// Normal steps move the ground kinematically (swept by continuous collision detection, so fast vessels
        /// can't tunnel). After a gap (time warp, pause, a new chunk) or a jump larger than any physical per-step
        /// motion (vessel switch, frame change) the move is a teleport: sweeping it would slam everything in between.
        /// Returns true for a teleport.
        /// </summary>
        private static bool MoveGround(Rigidbody rb, Transform t, Vector3 p, Quaternion q, bool afterGap)
        {
            if (afterGap || (rb.position - p).sqrMagnitude > 300f * 300f || Quaternion.Angle(rb.rotation, q) > 1f)
            {
                rb.position = p;
                rb.rotation = q;
                t.SetPositionAndRotation(p, q);
                return true;
            }
            rb.MovePosition(p);
            rb.MoveRotation(q);
            return false;
        }

        private (int, int, int) KeyFor(Vector3d dirBF, int level)
        {
            CubeSphere.FromSphere(dirBF, out int face, out double u, out double v);
            int n = 1 << level;
            int iu = Mathf.Clamp((int)Math.Floor((u + 1) * 0.5 * n), 0, n - 1);
            int iv = Mathf.Clamp((int)Math.Floor((v + 1) * 0.5 * n), 0, n - 1);
            return (face, iu, iv);
        }

        private void Need(CelestialBody body, Vector3d dirBF, int level, double utNext)
        {
            var key = KeyFor(dirBF, level);
            if (_colliders.TryGetValue(key, out var c)) { c.LastNeededUT = Sim.UT; return; }
            c = new ColliderChunk { Key = key, LastNeededUT = Sim.UT };
            _colliders[key] = c;
            TerrainWorkers.Enqueue(() =>
            {
                c.Pending = Generate(body, key, level);
                _colliderDone.Enqueue(c);
            });
        }

        private static ChunkData Generate(CelestialBody body, (int face, int iu, int iv) key, int level)
        {
            int n = 1 << level;
            double size = 2.0 / n;
            double u0 = -1 + key.iu * size, v0 = -1 + key.iv * size;
            return TerrainChunkGenerator.Generate(body.Terrain, body.Radius, key.face, u0, v0, size, 32, false, false);
        }

        /// <summary>
        /// Builds the collision chunks around <paramref name="truePos"/> (frame-body relative, inertial) right away,
        /// so a vessel loaded on the ground has something to stand on in its very first physics step.
        /// </summary>
        public void EnsureColliderNow(Vector3d truePos, double radius)
        {
            var body = Sim.Frame.Body;
            if (_colliderBody != body) OnFrameBodyChanged(body);
            if (!Terrains.TryGetValue(body, out var terrain)) return;
            int level = terrain.CollisionLevel;
            Vector3d dirBF = body.InertialToBodyFixed(truePos, Sim.UT).normalized;
            Vector3d e1 = Vector3d.Cross(dirBF, Vector3d.up);
            if (e1.sqrMagnitude < 1e-6) e1 = Vector3d.Cross(dirBF, Vector3d.right);
            e1 = e1.normalized;
            Vector3d e2 = Vector3d.Cross(dirBF, e1).normalized;
            double ang = radius / body.Radius;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    Vector3d d = (dirBF + (e1 * i + e2 * j) * ang).normalized;
                    var key = KeyFor(d, level);
                    if (!_colliders.TryGetValue(key, out var c))
                    {
                        c = new ColliderChunk { Key = key };
                        _colliders[key] = c;
                    }
                    c.LastNeededUT = Sim.UT;
                    if (c.Ready) continue;
                    if (c.Pending == null) c.Pending = Generate(body, key, level);
                    BuildColliderMesh(c);
                }
        }

        private void BuildColliderMesh(ColliderChunk c)
        {
            var d = c.Pending;
            if (d == null) return;
            c.Pending = null;
            // Already built synchronously: a late result from the background worker is discarded (a second collider
            // object for the same chunk would never be moved again).
            if (c.Ready) return;
            c.Mesh = new Mesh { name = "col" + c.Key };
            c.Mesh.SetVertices(d.Positions);
            c.Mesh.SetTriangles(d.Triangles, 0, true);
            c.CenterBF = d.CenterBF;
            c.Go = new GameObject("TerrainCollider " + c.Key);
            c.Go.layer = Layers.Terrain;
            c.Go.transform.SetParent(_colliderRoot.transform, false);
            var frame = Sim.Frame;
            var body = frame.Body;
            QuaternionD rot = body.RotationAtUT(Sim.UT);
            c.Go.transform.SetPositionAndRotation((Vector3)(rot * c.CenterBF - frame.Origin), rot.ToQuaternion());
            c.Rb = c.Go.AddComponent<Rigidbody>();
            c.Rb.isKinematic = true;
            c.Rb.interpolation = RigidbodyInterpolation.None;
            // The ground moves through Unity space at the vessel's speed (Krakensbane), so its motion must be swept.
            c.Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            c.Collider = c.Go.AddComponent<MeshCollider>();
            c.Collider.sharedMesh = c.Mesh;
            c.Collider.sharedMaterial = _groundMat;
            c.Ready = true;
        }

        /// <summary>Terrain height query that prefers the exact analytic function.</summary>
        public double TerrainHeight(CelestialBody b, Vector3d relPosInertial, double ut) => b.TerrainHeightAt(relPosInertial, ut);

        // ------------------------------------------------------------------ surface objects

        public SurfaceObject AddSurfaceObject(CelestialBody body, GameObject go, Vector3d posBF, QuaternionD rotBF, bool physics)
        {
            var so = new SurfaceObject { Body = body, Go = go, PositionBF = posBF, RotationBF = rotBF };
            go.transform.SetParent(transform, true);
            if (physics)
            {
                so.Rb = go.GetComponent<Rigidbody>();
                if (so.Rb == null) so.Rb = go.AddComponent<Rigidbody>();
                so.Rb.isKinematic = true;
                so.Rb.interpolation = RigidbodyInterpolation.Interpolate;
                so.Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }
            SurfaceObjects.Add(so);
            return so;
        }
    }
}
