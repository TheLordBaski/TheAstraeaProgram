using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Visual effects driven by the simulation: engine plumes (shape varies with pressure/throttle),
    /// exhaust smoke carried by the air, RCS puffs, explosions, separation puffs, reentry plasma
    /// and shredded parachutes.
    /// </summary>
    public sealed class FlightEffects : MonoBehaviour
    {
        public FlightSim Sim;

        private class PlumeFx
        {
            public EngineModule Engine;
            public Transform Root;
            public MeshRenderer Outer, Core;
            public ParticleSystem Smoke;
            public Light Glow;
            public float BaseRadius;
        }

        private class RcsFx
        {
            public RcsModule Module;
            public Transform[] Puffs;
            public MeshRenderer[] Renderers;
        }

        private class PlasmaFx
        {
            public Vessel Vessel;
            public ParticleSystem Particles;
            public Transform Sheath;
            public MeshRenderer SheathRenderer;
        }

        private readonly Dictionary<EngineModule, PlumeFx> _plumes = new Dictionary<EngineModule, PlumeFx>();
        private readonly Dictionary<RcsModule, RcsFx> _rcs = new Dictionary<RcsModule, RcsFx>();
        private readonly Dictionary<Vessel, PlasmaFx> _plasma = new Dictionary<Vessel, PlasmaFx>();
        private readonly List<ParticleSystem> _oneShots = new List<ParticleSystem>();
        private readonly List<(Light light, float until, float max)> _flashes = new List<(Light, float, float)>();
        private Mesh _coneMesh;
        private Material _plumeMat, _smokeMat, _fireMat, _sparkMat;
        private Texture2D _softTex;
        private MaterialPropertyBlock _mpb;
        private ParticleSystem.Particle[] _buf = new ParticleSystem.Particle[2048];

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _softTex = MakeSoftTexture(64);
            var plumeShader = Shader.Find("TAP/Plume");
            _plumeMat = Resources.Load<Material>("Materials/Plume");
            if (_plumeMat == null && plumeShader != null) _plumeMat = new Material(plumeShader);
            _smokeMat = MakeParticleMaterial(false);
            _fireMat = MakeParticleMaterial(true);
            _sparkMat = MakeParticleMaterial(true);
            _coneMesh = BuildCone();
        }

        private Material MakeParticleMaterial(bool additive)
        {
            var m = Resources.Load<Material>(additive ? "Materials/ParticleAdditive" : "Materials/ParticleSmoke");
            if (m != null) return new Material(m);
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            m = new Material(sh);
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", additive ? 2 : 0);
            m.SetTexture("_BaseMap", _softTex);
            m.renderQueue = 3100;
            return m;
        }

        private static Texture2D MakeSoftTexture(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "soft" };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2 - 1, dy = (y + 0.5f) / size * 2 - 1;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1 - r);
                    a = a * a * (3 - 2 * a);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            t.Apply();
            return t;
        }

        private static Mesh BuildCone()
        {
            // Unit plume along +Z: radius 1 at the nozzle expanding to 1.7 at z = 1. uv.y = along.
            var mb = new MeshBuilder();
            var prof = new List<Vector2>();
            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                prof.Add(new Vector2(1f + 0.7f * Mathf.Sqrt(t) - 0.9f * t * t, t));
            }
            mb.Transform = Matrix4x4.Rotate(Quaternion.FromToRotation(Vector3.up, Vector3.forward));
            mb.Lathe(0, prof, 20, true);
            var m = mb.ToMergedMesh("plume");
            // uv.y from profile index already 0..1
            return m;
        }

        // ------------------------------------------------------------------ per frame

        private void LateUpdate()
        {
            if (Sim == null) return;
            UpdatePlumes();
            UpdateRcs();
            UpdatePlasma();
            for (int i = _oneShots.Count - 1; i >= 0; i--)
            {
                var ps = _oneShots[i];
                if (ps == null) { _oneShots.RemoveAt(i); continue; }
                if (!ps.IsAlive(true)) { Destroy(ps.gameObject); _oneShots.RemoveAt(i); }
            }
            for (int i = _flashes.Count - 1; i >= 0; i--)
            {
                var f = _flashes[i];
                if (f.light == null) { _flashes.RemoveAt(i); continue; }
                float remain = f.until - Time.time;
                if (remain <= 0) { Destroy(f.light.gameObject); _flashes.RemoveAt(i); continue; }
                f.light.intensity = f.max * Mathf.Clamp01(remain / 0.6f);
            }
        }

        private void UpdatePlumes()
        {
            var alive = new HashSet<EngineModule>();
            foreach (var v in Sim.LoadedVessels)
            {
                if (v == null) continue;
                foreach (var p in v.Parts)
                {
                    var e = p.GetModule<EngineModule>();
                    if (e == null) continue;
                    alive.Add(e);
                    if (!_plumes.TryGetValue(e, out var fx))
                    {
                        if (!e.Ignited) continue;
                        fx = CreatePlume(e);
                        _plumes[e] = fx;
                    }
                    UpdatePlume(fx, v);
                }
            }
            var dead = new List<EngineModule>();
            foreach (var kv in _plumes) if (!alive.Contains(kv.Key) || kv.Value.Root == null) dead.Add(kv.Key);
            foreach (var d in dead)
            {
                var fx = _plumes[d];
                if (fx.Root != null) Destroy(fx.Root.gameObject);
                if (fx.Smoke != null)
                {
                    fx.Smoke.transform.SetParent(transform, true);
                    var em = fx.Smoke.emission; em.enabled = false;
                    _oneShots.Add(fx.Smoke);
                }
                _plumes.Remove(d);
            }
        }

        private PlumeFx CreatePlume(EngineModule e)
        {
            var nozzle = e.Part.transform.Find("Model/Bell/Nozzle");
            if (nozzle == null) nozzle = e.Part.transform.Find("Model/Nozzle");
            if (nozzle == null) nozzle = e.Part.transform;
            var root = new GameObject("Plume").transform;
            root.SetParent(nozzle, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity; // nozzle forward = exhaust direction
            var fx = new PlumeFx { Engine = e, Root = root };
            float exitR = e.Part.Def.model != null ? e.Part.Def.model.bellBottom * 0.5f : 0.4f;
            fx.BaseRadius = Mathf.Max(0.12f, exitR * 0.9f);
            var c = e.Def.exhaustColor;
            Color col = new Color(c[0], c[1], c[2]);
            fx.Outer = MakeConeRenderer(root, "Outer", col, 1.4f);
            fx.Core = MakeConeRenderer(root, "Core", Color.Lerp(col, Color.white, 0.6f), 3f);
            // Smoke for solids and for liquids in atmosphere
            var smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(nozzle, false);
            var ps = smokeGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = e.IsSolid ? 6f : 3.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(fx.BaseRadius * 3f, fx.BaseRadius * 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8, 20);
            main.maxParticles = 600;
            main.startColor = e.IsSolid ? new Color(0.85f, 0.83f, 0.8f, 0.55f) : new Color(0.9f, 0.9f, 0.9f, 0.3f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6;
            shape.radius = fx.BaseRadius;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 3.5f));
            var colt = ps.colorOverLifetime;
            colt.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 1) },
                new[] { new GradientAlphaKey(0.0f, 0), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1) });
            colt.color = grad;
            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0;
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = _smokeMat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
            fx.Smoke = ps;
            // light
            var lgo = new GameObject("Glow");
            lgo.transform.SetParent(root, false);
            lgo.transform.localPosition = new Vector3(0, 0, fx.BaseRadius * 2);
            fx.Glow = lgo.AddComponent<Light>();
            fx.Glow.type = LightType.Point;
            fx.Glow.color = col;
            fx.Glow.range = 25f * e.Def.exhaustScale;
            fx.Glow.shadows = LightShadows.None;
            return fx;
        }

        private MeshRenderer MakeConeRenderer(Transform parent, string name, Color c, float falloff)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = _coneMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _plumeMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", c);
            mpb.SetFloat("_Falloff", falloff);
            mr.SetPropertyBlock(mpb);
            return mr;
        }

        private void UpdatePlume(PlumeFx fx, Vessel v)
        {
            var e = fx.Engine;
            float thrustFrac = e.IsRunning ? Mathf.Clamp01(e.CurrentThrottle) : 0f;
            if (e.IsSolid && e.IsRunning) thrustFrac = 1f;
            float pAtm = (float)(v.StaticPressure / 101325.0);
            bool on = thrustFrac > 0.01f && !Sim.Warp.OnRails;
            fx.Outer.enabled = on;
            fx.Core.enabled = on;
            if (fx.Glow != null) { fx.Glow.enabled = on; fx.Glow.intensity = on ? 3f * thrustFrac : 0; }
            if (on)
            {
                float scale = e.Def.exhaustScale;
                float vac = 1f - Mathf.Clamp01(pAtm);
                float width = fx.BaseRadius * (1f + 1.2f * vac);
                float length = scale * (4.5f + 4f * thrustFrac) * (1f + 0.6f * vac);
                fx.Outer.transform.localScale = new Vector3(width, width, length);
                fx.Core.transform.localScale = new Vector3(width * 0.55f, width * 0.55f, length * 0.35f);
                float flick = 1f + 0.08f * Mathf.Sin(Time.time * 50f + e.GetHashCode());
                _mpb.Clear();
                fx.Outer.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_Intensity", (0.55f + 0.5f * thrustFrac) * flick * (0.6f + 0.4f * vac));
                fx.Outer.SetPropertyBlock(_mpb);
                fx.Core.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_Intensity", 1.2f * thrustFrac * flick);
                fx.Core.SetPropertyBlock(_mpb);
            }
            if (fx.Smoke != null)
            {
                var em = fx.Smoke.emission;
                float smokeRate = on && v.InAtmosphere ? (e.IsSolid ? 90f : 35f) * thrustFrac * Mathf.Clamp01(pAtm * 3f + 0.15f) : 0f;
                em.rateOverTime = smokeRate;
                var vol = fx.Smoke.velocityOverLifetime;
                Vector3 air = v.AirVelocityAt(fx.Smoke.transform.position);
                vol.x = air.x; vol.y = air.y; vol.z = air.z;
            }
        }

        private void UpdateRcs()
        {
            var alive = new HashSet<RcsModule>();
            foreach (var v in Sim.LoadedVessels)
            {
                if (v == null) continue;
                foreach (var p in v.Parts)
                {
                    var r = p.GetModule<RcsModule>();
                    if (r == null || r.NozzleThrottle == null) continue;
                    alive.Add(r);
                    if (!_rcs.TryGetValue(r, out var fx))
                    {
                        fx = new RcsFx { Module = r, Puffs = new Transform[r.NozzleThrottle.Length], Renderers = new MeshRenderer[r.NozzleThrottle.Length] };
                        for (int i = 0; i < fx.Puffs.Length; i++)
                        {
                            var n = r.Nozzle(i);
                            if (n == null) continue;
                            var go = new GameObject("RcsPuff");
                            go.transform.SetParent(n, false);
                            fx.Renderers[i] = MakeConeRenderer(go.transform, "cone", new Color(0.9f, 0.92f, 1f), 1.2f);
                            fx.Puffs[i] = go.transform;
                        }
                        _rcs[r] = fx;
                    }
                    for (int i = 0; i < fx.Puffs.Length; i++)
                    {
                        if (fx.Renderers[i] == null) continue;
                        float t = r.NozzleThrottle[i];
                        bool on = t > 0.05f && !Sim.Warp.OnRails;
                        fx.Renderers[i].enabled = on;
                        if (on)
                        {
                            fx.Renderers[i].transform.localScale = new Vector3(0.05f, 0.05f, 0.35f + 0.5f * t);
                            _mpb.Clear();
                            fx.Renderers[i].GetPropertyBlock(_mpb);
                            _mpb.SetFloat("_Intensity", 0.8f * t);
                            fx.Renderers[i].SetPropertyBlock(_mpb);
                        }
                    }
                }
            }
            var dead = new List<RcsModule>();
            foreach (var kv in _rcs) if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
            foreach (var d in dead) _rcs.Remove(d);
        }

        private void UpdatePlasma()
        {
            var alive = new HashSet<Vessel>();
            foreach (var v in Sim.LoadedVessels)
            {
                if (v == null || v.Rb == null || v.IsFlag) continue;
                alive.Add(v);
                double intensity = 0;
                if (v.InAtmosphere && !Sim.Warp.OnRails)
                {
                    double speed = v.SurfaceSpeed;
                    double h = Thermal.ConvectionK * System.Math.Sqrt(v.AirDensity) * speed;
                    double tRec = v.AirTemperature + 0.9 * speed * speed / 2008.0;
                    double flux = h * System.Math.Max(0, tRec - 900);
                    intensity = MathD.Clamp01((flux - 20000) / 250000);
                }
                if (!_plasma.TryGetValue(v, out var fx))
                {
                    if (intensity <= 0) continue;
                    fx = CreatePlasma(v);
                    _plasma[v] = fx;
                }
                Vector3 vRel = v.Rb.linearVelocity - v.AirVelocityAt(v.Rb.worldCenterOfMass);
                Vector3 dir = vRel.sqrMagnitude > 1 ? vRel.normalized : v.transform.up;
                // Leading point: most upstream part.
                Vector3 lead = v.Rb.worldCenterOfMass;
                float best = float.MinValue;
                float radius = 1f;
                foreach (var p in v.Parts)
                {
                    float s = Vector3.Dot(p.transform.position, dir);
                    if (s > best) { best = s; lead = p.transform.position; radius = Mathf.Max(0.6f, p.Def.diameter * 0.6f); }
                }
                fx.Sheath.position = lead + dir * radius * 0.3f;
                fx.Sheath.rotation = Quaternion.LookRotation(-dir);
                fx.Sheath.localScale = new Vector3(radius * 1.6f, radius * 1.6f, radius * 4f * (float)(0.6 + intensity));
                fx.SheathRenderer.enabled = intensity > 0.02;
                _mpb.Clear();
                fx.SheathRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_Intensity", (float)intensity * 1.2f);
                fx.SheathRenderer.SetPropertyBlock(_mpb);
                var em = fx.Particles.emission;
                em.rateOverTime = (float)(intensity * 220);
                fx.Particles.transform.position = lead + dir * radius * 0.2f;
                fx.Particles.transform.rotation = Quaternion.LookRotation(-dir);
                var sh = fx.Particles.shape;
                sh.radius = radius;
                var vol = fx.Particles.velocityOverLifetime;
                Vector3 air = v.AirVelocityAt(lead) * 0.25f + v.Rb.linearVelocity * 0.75f;
                vol.x = air.x; vol.y = air.y; vol.z = air.z;
            }
            var dead = new List<Vessel>();
            foreach (var kv in _plasma) if (!alive.Contains(kv.Key)) dead.Add(kv.Key);
            foreach (var d in dead)
            {
                var fx = _plasma[d];
                if (fx.Sheath != null) Destroy(fx.Sheath.gameObject);
                if (fx.Particles != null) Destroy(fx.Particles.gameObject);
                _plasma.Remove(d);
            }
        }

        private PlasmaFx CreatePlasma(Vessel v)
        {
            var fx = new PlasmaFx { Vessel = v };
            var sheath = new GameObject("PlasmaSheath");
            sheath.transform.SetParent(transform, false);
            fx.SheathRenderer = MakeConeRenderer(sheath.transform, "sheath", new Color(1f, 0.45f, 0.3f), 0.8f);
            fx.Sheath = sheath.transform;
            var go = new GameObject("PlasmaParticles");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSpeed = 0;
            main.maxParticles = 400;
            main.startColor = new Color(1f, 0.55f, 0.35f, 0.8f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.7f), 0), new GradientColorKey(new Color(1f, 0.3f, 0.5f), 1) },
                new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0f, 1) });
            col.color = grad;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = _fireMat;
            ps.Play();
            fx.Particles = ps;
            return fx;
        }

        // ------------------------------------------------------------------ one-shot effects

        public void SpawnExplosion(Vector3 pos, float size, Vector3 velocity)
        {
            size = Mathf.Max(0.5f, size);
            var ps = MakeBurst("Explosion", pos, _fireMat, 60, new Color(1f, 0.6f, 0.2f, 1f), 0.6f, 1.2f, size * 1.5f, size * 4f, 6f * size, velocity);
            var smoke = MakeBurst("ExplosionSmoke", pos, _smokeMat, 40, new Color(0.25f, 0.23f, 0.22f, 0.8f), 1.5f, 3.5f, size * 2f, size * 5f, 3f * size, velocity);
            var sparks = MakeBurst("Sparks", pos, _sparkMat, 50, new Color(1f, 0.85f, 0.5f, 1f), 0.5f, 1.4f, 0.08f, 0.2f, 25f, velocity);
            var lgo = new GameObject("Flash");
            lgo.transform.SetParent(transform, false);
            lgo.transform.position = pos;
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.7f, 0.35f);
            l.range = 40f * size;
            l.intensity = 8f;
            _flashes.Add((l, Time.time + 0.6f, 8f));
        }

        public void SpawnSeparation(Vector3 pos, Vector3 dir, float size)
        {
            MakeBurst("SeparationPuff", pos, _smokeMat, 25, new Color(0.9f, 0.9f, 0.9f, 0.6f), 0.8f, 1.8f, size * 0.4f, size * 1.2f, 4f, Vector3.zero);
        }

        public void SpawnCanopyShreds(Vector3 pos, float size, float[] color)
        {
            var c = color != null && color.Length >= 3 ? new Color(color[0], color[1], color[2], 1) : Color.white;
            MakeBurst("Shreds", pos, _smokeMat, 30, c, 1.5f, 3f, 0.3f, 1.0f, 8f, Vector3.zero);
        }

        private ParticleSystem MakeBurst(string name, Vector3 pos, Material mat, int count, Color color, float lifeMin, float lifeMax,
            float sizeMin, float sizeMax, float speed, Vector3 inheritVel)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startColor = color;
            main.maxParticles = count * 2;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = sizeMin * 0.5f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0f, 1) });
            col.color = grad;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.5f));
            if (inheritVel.sqrMagnitude > 0.01f)
            {
                var vol = ps.velocityOverLifetime;
                vol.enabled = true;
                vol.space = ParticleSystemSimulationSpace.World;
                vol.x = inheritVel.x; vol.y = inheritVel.y; vol.z = inheritVel.z;
            }
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            ps.Play();
            _oneShots.Add(ps);
            return ps;
        }

        // ------------------------------------------------------------------ frame shifts

        public void ShiftPositions(Vector3 delta)
        {
            var systems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in systems) ShiftSystem(ps, delta);
            foreach (var kv in _plumes)
                if (kv.Value.Smoke != null && kv.Value.Smoke.transform.parent != transform) ShiftSystem(kv.Value.Smoke, delta);
            foreach (var f in _flashes) if (f.light != null) f.light.transform.position += delta;
        }

        private void ShiftSystem(ParticleSystem ps, Vector3 delta)
        {
            if (ps.main.simulationSpace != ParticleSystemSimulationSpace.World) return;
            int n = ps.particleCount;
            if (n == 0) return;
            if (_buf.Length < n) _buf = new ParticleSystem.Particle[n * 2];
            n = ps.GetParticles(_buf);
            for (int i = 0; i < n; i++) _buf[i].position += delta;
            ps.SetParticles(_buf, n);
        }

        public void ShiftVelocity(Vector3 dv) { /* world particles follow the air velocity (set every frame) */ }
    }
}
