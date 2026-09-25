using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAP.Game
{
    public enum MarkerType
    {
        Body, Vessel, Debris, Eva, Flag, Apoapsis, Periapsis, Node, Encounter, SoiExit, Impact, AtmosphereEntry,
        ClosestSelf, ClosestTarget, ActiveVessel,
    }

    public struct MapMarker
    {
        public MarkerType Type;
        public Vector3 Screen;
        public bool Visible;
        public string Label;
        public string Tooltip;
        public Color Color;
        public object Payload;
        public double UT;
    }

    /// <summary>
    /// Orbital map: bodies at 1/1000 scale around a focus point, patched-conic trajectory lines,
    /// other vessels/debris orbits, and markers (apsides, encounters, nodes, closest approach) for the UI.
    /// </summary>
    public sealed class MapView : MonoBehaviour
    {
        public const double Scale = 1000.0; // metres per map unit
        public FlightSim Sim;
        public TrajectoryService Trajectory;
        public Camera MapCamera;
        public bool Active;
        public readonly List<MapMarker> Markers = new List<MapMarker>();

        // Focus
        public CelestialBody FocusBody;
        public VesselHandle FocusVessel;
        public float CamDistance = 4000f; // map units
        public float CamYaw = 30, CamPitch = 35;
        private float _targetDistance = 4000f;

        private readonly Dictionary<CelestialBody, GameObject> _bodyObjects = new Dictionary<CelestialBody, GameObject>();
        private readonly Dictionary<CelestialBody, Material> _haloMats = new Dictionary<CelestialBody, Material>();
        private readonly List<OrbitLine> _currentLines = new List<OrbitLine>();
        private readonly List<OrbitLine> _plannedLines = new List<OrbitLine>();
        private readonly Dictionary<string, OrbitLine> _vesselLines = new Dictionary<string, OrbitLine>();
        private readonly Dictionary<CelestialBody, OrbitLine> _bodyLines = new Dictionary<CelestialBody, OrbitLine>();
        private Material _lineMat;
        private Material _mapSky;
        private Material _flightSky;
        private int _lastTrajVersion = -1;
        private Transform _root;

        public static readonly Color CurrentColor = new Color(0.35f, 0.85f, 1f, 1f);
        public static readonly Color PlannedColor = new Color(1f, 0.62f, 0.2f, 1f);
        public static readonly Color MoonPatchColor = new Color(0.75f, 1f, 0.45f, 1f);
        public static readonly Color OtherColor = new Color(0.6f, 0.62f, 0.66f, 0.55f);
        public static readonly Color TargetColor = new Color(1f, 0.35f, 0.9f, 0.9f);

        public void Init(FlightSim sim, TrajectoryService traj)
        {
            Sim = sim;
            Trajectory = traj;
            _root = new GameObject("MapRoot").transform;
            _root.SetParent(transform, false);
            var camGo = new GameObject("MapCamera");
            camGo.transform.SetParent(transform, false);
            MapCamera = camGo.AddComponent<Camera>();
            MapCamera.cullingMask = 1 << Layers.Map;
            MapCamera.nearClipPlane = 0.5f;
            MapCamera.farClipPlane = 5e6f;
            MapCamera.fieldOfView = 50;
            MapCamera.clearFlags = CameraClearFlags.Skybox;
            MapCamera.enabled = false;
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var lm = Resources.Load<Material>("Materials/OrbitLine");
            _lineMat = lm != null ? new Material(lm) : new Material(Shader.Find("TAP/OrbitLine"));
            _lineMat.SetFloat("_Width", 2.4f);
            var sky = Resources.Load<Material>("Materials/Sky");
            if (sky != null)
            {
                _mapSky = new Material(sky);
                _mapSky.SetFloat("_Atmosphere", 0);
                _mapSky.SetFloat("_StarBrightness", 0.9f);
            }
            foreach (var b in sim.System.Bodies) CreateBody(b);
            foreach (var b in sim.System.Bodies)
                if (b.Parent != null)
                {
                    var line = new OrbitLine(_root, _lineMat, Layers.Map);
                    var patch = new OrbitPatch { Orbit = b.Orbit, Body = b.Parent, StartUT = 0, EndUT = b.Orbit.Period, EndType = PatchEnd.None };
                    line.SamplePatch(patch, 256, double.MaxValue);
                    line.BuildMesh(Scale, new Color(0.55f, 0.58f, 0.62f, 0.5f));
                    _bodyLines[b] = line;
                }
            _root.gameObject.SetActive(false); // shown only while the map is open
        }

        private void CreateBody(CelestialBody b)
        {
            var go = new GameObject("Map " + b.Name);
            go.layer = Layers.Map;
            go.transform.SetParent(_root, false);
            go.AddComponent<MeshFilter>().sharedMesh = PlanetSphereMesh();
            var mr = go.AddComponent<MeshRenderer>();
            var mat = Resources.Load<Material>("Materials/Map_" + b.Id);
            if (mat == null)
            {
                mat = PartMaterials.CreateLit("map_" + b.Id, new Color(b.Def.mapColor[0], b.Def.mapColor[1], b.Def.mapColor[2]), 0, 0.1f, false);
            }
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _bodyObjects[b] = go;
            if (b.Atmosphere != null)
            {
                var sh = Resources.Load<Material>("Materials/AtmosphereShell");
                if (sh != null)
                {
                    var halo = new GameObject("Halo");
                    halo.layer = Layers.Map;
                    halo.transform.SetParent(go.transform, false);
                    var hmb = new MeshBuilder();
                    hmb.Sphere(0, Vector3.zero, 1f, 48, 32);
                    halo.AddComponent<MeshFilter>().sharedMesh = hmb.ToMergedMesh("halo");
                    var hm = new Material(sh);
                    var c = b.Def.atmosphere.skyColor;
                    hm.SetColor("_Color", new Color(c[0], c[1], c[2]));
                    hm.SetFloat("_Intensity", 1.6f);
                    halo.AddComponent<MeshRenderer>().sharedMaterial = hm;
                    float s = (float)((b.Radius + b.Atmosphere.Height) / b.Radius);
                    halo.transform.localScale = Vector3.one * s;
                    _haloMats[b] = hm;
                }
            }
        }

        private static Mesh _sphere;
        /// <summary>UV sphere whose texture mapping matches the equirectangular planet maps (lon 0 at +X, east towards -Z).</summary>
        private static Mesh PlanetSphereMesh()
        {
            if (_sphere != null) return _sphere;
            int seg = 96, rings = 48;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int j = 0; j <= rings; j++)
            {
                double lat = -90.0 + 180.0 * j / rings;
                for (int i = 0; i <= seg; i++)
                {
                    double lon = -180.0 + 360.0 * i / seg;
                    var d = (Vector3)TerrainGenerator.DirectionFromLatLon(lat, lon);
                    verts.Add(d);
                    norms.Add(d);
                    uvs.Add(new Vector2((float)i / seg, (float)j / rings));
                }
            }
            int row = seg + 1;
            for (int j = 0; j < rings; j++)
                for (int i = 0; i < seg; i++)
                {
                    int a = j * row + i, b = a + 1, c = a + row + 1, d = a + row;
                    // lon increases towards -Z (east); verify winding by face normal orientation at runtime below
                    tris.Add(a); tris.Add(d); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(b);
                }
            // Ensure outward-facing triangles (flip if the first triangle faces inward).
            Vector3 v0 = verts[tris[3 * row * 10]], v1 = verts[tris[3 * row * 10 + 1]], v2 = verts[tris[3 * row * 10 + 2]];
            Vector3 fn = Vector3.Cross(v1 - v0, v2 - v0);
            // In Unity a front-facing (clockwise) triangle's cross(v1-v0, v2-v0) points towards the viewer (outwards).
            if (Vector3.Dot(fn, v0) < 0)
                for (int k = 0; k < tris.Count; k += 3) { int t = tris[k + 1]; tris[k + 1] = tris[k + 2]; tris[k + 2] = t; }
            _sphere = new Mesh { name = "planet_sphere" };
            _sphere.SetVertices(verts);
            _sphere.SetNormals(norms);
            _sphere.SetUVs(0, uvs);
            _sphere.SetTriangles(tris, 0);
            _sphere.RecalculateBounds();
            return _sphere;
        }

        // ------------------------------------------------------------------ activation

        public void SetActive(bool on, Camera flightCam)
        {
            Active = on;
            UiState.MapActive = on;
            MapCamera.enabled = on;
            if (flightCam != null) flightCam.enabled = !on;
            _root.gameObject.SetActive(on);
            if (on)
            {
                _flightSky = RenderSettings.skybox;
                if (_mapSky != null) RenderSettings.skybox = _mapSky;
                RenderSettings.fog = false;
                if (FocusBody == null && FocusVessel == null) FocusVessel = Sim.ActiveHandle;
                _lastTrajVersion = -1;
                Trajectory.ForceUpdate();
            }
            else if (_flightSky != null) RenderSettings.skybox = _flightSky;
        }

        public void FocusOn(CelestialBody b)
        {
            FocusBody = b;
            FocusVessel = null;
            _targetDistance = (float)(b.Radius / Scale * 6);
        }

        public void FocusOn(VesselHandle h)
        {
            FocusVessel = h;
            FocusBody = null;
            _targetDistance = Mathf.Min(_targetDistance, 3000f);
        }

        public void CycleFocus()
        {
            var list = new List<object>();
            if (Sim.ActiveHandle != null) list.Add(Sim.ActiveHandle);
            foreach (var b in Sim.System.Bodies) list.Add(b);
            object cur = (object)FocusVessel ?? FocusBody;
            int i = list.IndexOf(cur);
            var next = list[(i + 1) % list.Count];
            if (next is VesselHandle h) FocusOn(h); else FocusOn((CelestialBody)next);
        }

        public Vector3d FocusAbsolute(double ut)
        {
            if (FocusVessel != null && Sim.Handles.Contains(FocusVessel)) return FocusVessel.AbsolutePosition(ut);
            if (FocusBody != null) return FocusBody.GetPositionAtUT(ut);
            var a = Sim.ActiveHandle;
            return a != null ? a.AbsolutePosition(ut) : Vector3d.zero;
        }

        public Vector3 ToMap(Vector3d absolute, Vector3d focus) => (Vector3)((absolute - focus) / Scale);

        // ------------------------------------------------------------------ update

        private void LateUpdate()
        {
            if (!Active || Sim == null) return;
            double ut = Sim.UT;
            Vector3d focus = FocusAbsolute(ut);
            HandleCamera();

            foreach (var kv in _bodyObjects)
            {
                var b = kv.Key;
                kv.Value.transform.position = ToMap(b.GetPositionAtUT(ut), focus);
                kv.Value.transform.rotation = b.RotationAtUT(ut).ToQuaternion();
                kv.Value.transform.localScale = Vector3.one * (float)(b.Radius / Scale);
                if (_haloMats.TryGetValue(b, out var hm))
                {
                    hm.SetVector("_PlanetCenter", kv.Value.transform.position);
                    hm.SetFloat("_PlanetRadius", (float)(b.Radius / Scale));
                    hm.SetFloat("_AtmoRadius", (float)((b.Radius + b.Atmosphere.Height) / Scale));
                    hm.SetVector("_SunDir", (Vector3)Sim.System.SunDirection);
                }
            }
            foreach (var kv in _bodyLines)
                kv.Value.Go.transform.position = ToMap(kv.Key.Parent.GetPositionAtUT(ut), focus);

            if (Trajectory.Version != _lastTrajVersion) RebuildTrajectoryLines();
            PositionTrajectoryLines(focus, ut);
            UpdateVesselLines(focus, ut);
            BuildMarkers(focus, ut);
        }

        private void HandleCamera()
        {
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse != null && !UiState.PointerOverUi)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    CamYaw += d.x * 0.3f;
                    CamPitch = Mathf.Clamp(CamPitch - d.y * 0.3f, -89f, 89f);
                }
                float s = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(s) > 0.01f) _targetDistance *= ScrollZoom.Factor(s, 0.7f);
            }
            if (kb != null && !UiState.KeyboardCaptured)
            {
                if (kb.tabKey.wasPressedThisFrame) CycleFocus();
                if (kb.backspaceKey.wasPressedThisFrame) FocusOn(Sim.ActiveHandle);
            }
            // Down to just above the focused body's surface, or to ~10 m from a focused vessel.
            float minDist = FocusBody != null ? (float)(FocusBody.Radius / Scale * 1.02) : 0.01f;
            _targetDistance = Mathf.Clamp(_targetDistance, minDist, 400000f);
            CamDistance = Mathf.Lerp(CamDistance, _targetDistance, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 8f));
            // Seen from the north (-Y, see Geo), so prograde orbits run counter-clockwise as on KSP's map.
            Quaternion rot = Quaternion.AngleAxis(180f, Vector3.right) * Quaternion.Euler(CamPitch, CamYaw, 0);
            MapCamera.transform.position = rot * new Vector3(0, 0, -CamDistance);
            MapCamera.transform.rotation = rot;
            MapCamera.nearClipPlane = Mathf.Max(0.001f, CamDistance * 0.0005f);
            MapCamera.farClipPlane = Mathf.Max(1e5f, CamDistance * 200f);
        }

        private void RebuildTrajectoryLines()
        {
            _lastTrajVersion = Trajectory.Version;
            Fill(_currentLines, Trajectory.Current, false);
            Fill(_plannedLines, Trajectory.HasNodes ? Trajectory.Planned : null, true);
        }

        private void Fill(List<OrbitLine> lines, List<OrbitPatch> patches, bool planned)
        {
            int n = patches?.Count ?? 0;
            while (lines.Count < n) lines.Add(new OrbitLine(_root, _lineMat, Layers.Map));
            for (int i = 0; i < lines.Count; i++)
            {
                if (i >= n) { lines[i].SetVisible(false); continue; }
                var p = patches[i];
                // Planned path: the arc before the first node is already drawn by the current path.
                if (planned && i == 0 && p.EndType == PatchEnd.Maneuver) { lines[i].SetVisible(false); continue; }
                lines[i].SetVisible(true);
                lines[i].SamplePatch(p, 320, p.Body.SOIRadius < 1e15 ? p.Body.SOIRadius * 1.02 : 60e6);
                Color c = planned ? PlannedColor : (i == 0 ? CurrentColor : MoonPatchColor);
                lines[i].BuildMesh(Scale, c);
            }
        }

        private void PositionTrajectoryLines(Vector3d focus, double ut)
        {
            foreach (var l in _currentLines) if (l.Go.activeSelf && l.Body != null) l.Go.transform.position = ToMap(l.Body.GetPositionAtUT(ut), focus);
            foreach (var l in _plannedLines) if (l.Go.activeSelf && l.Body != null) l.Go.transform.position = ToMap(l.Body.GetPositionAtUT(ut), focus);
        }

        private float _vesselLineTimer;
        private void UpdateVesselLines(Vector3d focus, double ut)
        {
            _vesselLineTimer += Time.unscaledDeltaTime;
            bool rebuild = _vesselLineTimer > 0.5f;
            if (rebuild) _vesselLineTimer = 0;
            var seen = new HashSet<string>();
            string targetId = Sim.ActiveVessel?.TargetId;
            foreach (var h in Sim.Handles)
            {
                if (h == Sim.ActiveHandle || h.Kind == VesselKind.Flag) continue;
                var o = h.CurrentOrbit;
                if (o == null) continue;
                seen.Add(h.Id);
                if (!_vesselLines.TryGetValue(h.Id, out var line))
                {
                    line = new OrbitLine(_root, _lineMat, Layers.Map);
                    _vesselLines[h.Id] = line;
                    rebuild = true;
                }
                if (rebuild)
                {
                    var patch = new OrbitPatch { Orbit = o, Body = h.Body, StartUT = ut, EndUT = ut + (o.IsElliptic ? o.Period : 86400 * 5), EndType = o.IsElliptic ? PatchEnd.None : PatchEnd.SoiExit };
                    line.SamplePatch(patch, 160, h.Body.SOIRadius < 1e15 ? h.Body.SOIRadius : 60e6);
                    bool isTarget = targetId == "vessel:" + h.Id;
                    line.BuildMesh(Scale, isTarget ? TargetColor : OtherColor);
                }
                line.SetVisible(true);
                line.Go.transform.position = ToMap(h.Body.GetPositionAtUT(ut), focus);
            }
            var remove = new List<string>();
            foreach (var kv in _vesselLines) if (!seen.Contains(kv.Key)) remove.Add(kv.Key);
            foreach (var r in remove) { _vesselLines[r].Destroy(); _vesselLines.Remove(r); }
        }

        // ------------------------------------------------------------------ markers

        private void Add(MarkerType t, Vector3d abs, Vector3d focus, string label, string tooltip, Color c, object payload = null, double ut = 0)
        {
            Vector3 w = ToMap(abs, focus);
            Vector3 s = MapCamera.WorldToScreenPoint(w);
            Markers.Add(new MapMarker { Type = t, Screen = s, Visible = s.z > 0, Label = label, Tooltip = tooltip, Color = c, Payload = payload, UT = ut });
        }

        private void BuildMarkers(Vector3d focus, double ut)
        {
            Markers.Clear();
            foreach (var b in Sim.System.Bodies)
                Add(MarkerType.Body, b.GetPositionAtUT(ut), focus, b.Name, $"{b.Name}\nRadius {b.Radius / 1000:F0} km  g {b.SurfaceGravity:F2} m/s²", Color.white, b);
            foreach (var h in Sim.Handles)
            {
                MarkerType t = h.Kind == VesselKind.Debris ? MarkerType.Debris : h.Kind == VesselKind.EVA ? MarkerType.Eva : h.Kind == VesselKind.Flag ? MarkerType.Flag : MarkerType.Vessel;
                if (h == Sim.ActiveHandle) t = MarkerType.ActiveVessel;
                Add(t, h.AbsolutePosition(ut), focus, h.Name, $"{h.Name}\n{h.Record.situation} at {h.Body.Name}", Color.white, h);
            }
            var list = Trajectory.HasNodes ? Trajectory.Planned : Trajectory.Current;
            AddPatchMarkers(Trajectory.Current, focus, ut, false);
            if (Trajectory.HasNodes) AddPatchMarkers(Trajectory.Planned, focus, ut, true);

            var v = Sim.ActiveVessel;
            if (v != null)
            {
                for (int i = 0; i < v.ManeuverNodes.Count; i++)
                {
                    var n = v.ManeuverNodes[i];
                    var p = FindPatch(Trajectory.HasNodes ? Trajectory.Planned : Trajectory.Current, n.ut, true);
                    if (p == null) continue;
                    double dv = Math.Sqrt(n.prograde * n.prograde + n.normal * n.normal + n.radial * n.radial);
                    Add(MarkerType.Node, PatchedConics.AbsolutePosition(p, n.ut), focus, $"Node {i + 1}", $"Maneuver {i + 1}\nΔv {dv:F1} m/s\nin {MathD.FormatDuration(n.ut - ut)}", new Color(0.3f, 0.6f, 1f), i, n.ut);
                }
                if (Trajectory.HasClosest && !string.IsNullOrEmpty(v.TargetId))
                {
                    var p = FindPatch(list, Trajectory.ClosestUT, false);
                    if (p != null)
                    {
                        string tip = $"Closest approach\n{MathD.FormatDistance(Trajectory.ClosestDistance)}\nin {MathD.FormatDuration(Trajectory.ClosestUT - ut)}\nrel. speed {Trajectory.ClosestRelSpeed:F1} m/s";
                        Add(MarkerType.ClosestSelf, PatchedConics.AbsolutePosition(p, Trajectory.ClosestUT), focus, "", tip, TargetColor, null, Trajectory.ClosestUT);
                        var tid = v.TargetId;
                        Vector3d tpos = Vector3d.zero;
                        if (tid.StartsWith("body:")) tpos = Sim.System.Get(tid.Substring(5)).GetPositionAtUT(Trajectory.ClosestUT);
                        else { var th = Sim.FindHandle(tid.Substring(7)); if (th != null) tpos = th.CurrentOrbit != null ? th.Body.GetPositionAtUT(Trajectory.ClosestUT) + th.CurrentOrbit.GetPositionAtUT(Trajectory.ClosestUT) : th.AbsolutePosition(Trajectory.ClosestUT); }
                        Add(MarkerType.ClosestTarget, tpos, focus, "", tip, TargetColor, null, Trajectory.ClosestUT);
                    }
                }
            }
        }

        private static OrbitPatch FindPatch(List<OrbitPatch> list, double t, bool preferEnding)
        {
            foreach (var p in list) if (t >= p.StartUT - 1e-6 && t <= p.EndUT + 1e-6) return p;
            return null;
        }

        private void AddPatchMarkers(List<OrbitPatch> patches, Vector3d focus, double ut, bool planned)
        {
            if (patches == null) return;
            for (int i = 0; i < patches.Count; i++)
            {
                var p = patches[i];
                if (planned && p.EndUT <= ut) continue;
                var o = p.Orbit;
                var b = p.Body;
                string pre = planned ? "Planned " : "";
                if (!double.IsNaN(p.ApoapsisUT))
                {
                    double ap = o.ApoapsisRadius - b.Radius;
                    Add(MarkerType.Apoapsis, PatchedConics.AbsolutePosition(p, p.ApoapsisUT), focus, $"Ap {MathD.FormatDistance(ap)}",
                        $"{pre}Apoapsis ({b.Name})\n{MathD.FormatDistance(ap)}\nin {MathD.FormatDuration(p.ApoapsisUT - ut)}", planned ? PlannedColor : CurrentColor, null, p.ApoapsisUT);
                }
                if (!double.IsNaN(p.PeriapsisUT))
                {
                    double pe = o.PeriapsisRadius - b.Radius;
                    Add(MarkerType.Periapsis, PatchedConics.AbsolutePosition(p, p.PeriapsisUT), focus, $"Pe {MathD.FormatDistance(pe)}",
                        $"{pre}Periapsis ({b.Name})\n{MathD.FormatDistance(pe)}\nin {MathD.FormatDuration(p.PeriapsisUT - ut)}", planned ? PlannedColor : (i == 0 ? CurrentColor : MoonPatchColor), null, p.PeriapsisUT);
                }
                if (p.EndType == PatchEnd.SoiEnter && i + 1 < patches.Count)
                {
                    var next = patches[i + 1];
                    double pe = next.Orbit.PeriapsisRadius - next.Body.Radius;
                    Add(MarkerType.Encounter, PatchedConics.AbsolutePosition(p, p.EndUT), focus, $"{next.Body.Name} encounter",
                        $"{pre}Enter {next.Body.Name} SOI in {MathD.FormatDuration(p.EndUT - ut)}\nPeriapsis {MathD.FormatDistance(pe)}", MoonPatchColor, null, p.EndUT);
                }
                if (p.EndType == PatchEnd.SoiExit)
                    Add(MarkerType.SoiExit, PatchedConics.AbsolutePosition(p, p.EndUT), focus, $"{b.Name} escape",
                        $"{pre}Leave {b.Name} SOI in {MathD.FormatDuration(p.EndUT - ut)}", MoonPatchColor, null, p.EndUT);
                if (p.EndType == PatchEnd.Impact)
                    Add(MarkerType.Impact, PatchedConics.AbsolutePosition(p, p.EndUT), focus, "Impact", $"{pre}Surface impact on {b.Name} in {MathD.FormatDuration(p.EndUT - ut)}", new Color(1f, 0.3f, 0.2f), null, p.EndUT);
                if (!double.IsNaN(p.AtmosphereEntryUT))
                    Add(MarkerType.AtmosphereEntry, PatchedConics.AbsolutePosition(p, p.AtmosphereEntryUT), focus, "Atmo",
                        $"{pre}Atmosphere entry in {MathD.FormatDuration(p.AtmosphereEntryUT - ut)}", new Color(0.5f, 0.8f, 1f), null, p.AtmosphereEntryUT);
            }
        }

        // ------------------------------------------------------------------ picking helpers for the UI

        /// <summary>Screen-space nearest point on the active vessel's current (or planned) path.</summary>
        public enum TrajectorySet { All, Current, Planned }

        public bool PickTrajectory(Vector2 screen, float maxPixels, out double ut) =>
            PickTrajectory(screen, maxPixels, Sim.UT, double.PositiveInfinity, TrajectorySet.All, out ut);

        /// <summary>
        /// Time of the point of the drawn trajectory closest to a screen position (interpolated along the line
        /// segments), limited to [minUT, maxUT] and to the current and/or planned (after-node) trajectory.
        /// </summary>
        public bool PickTrajectory(Vector2 screen, float maxPixels, double minUT, double maxUT, TrajectorySet set, out double ut)
        {
            ut = double.NaN;
            if (!Active) return false;
            float best = maxPixels;
            var lines = new List<OrbitLine>();
            if (set != TrajectorySet.Planned) lines.AddRange(_currentLines);
            if (set != TrajectorySet.Current && Trajectory.HasNodes) lines.AddRange(_plannedLines);
            foreach (var l in lines)
            {
                if (!l.Go.activeSelf || l.Body == null || l.PointsRel.Count == 0) continue;
                Vector3 origin = l.Go.transform.position;
                Vector3 prev = MapCamera.WorldToScreenPoint(origin + (Vector3)(l.PointsRel[0] / Scale));
                for (int i = 1; i < l.PointsRel.Count; i++)
                {
                    Vector3 cur = MapCamera.WorldToScreenPoint(origin + (Vector3)(l.PointsRel[i] / Scale));
                    Vector3 a = prev;
                    prev = cur;
                    if (a.z <= 0 || cur.z <= 0) continue;
                    Vector2 ab = (Vector2)cur - (Vector2)a;
                    float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(screen - (Vector2)a, ab) / ab.sqrMagnitude) : 0f;
                    float d = Vector2.Distance(screen, (Vector2)a + ab * t);
                    if (d >= best) continue;
                    double u = l.PointsUT[i - 1] + (l.PointsUT[i] - l.PointsUT[i - 1]) * t;
                    if (u <= minUT || u >= maxUT) continue;
                    best = d;
                    ut = u;
                }
            }
            return !double.IsNaN(ut);
        }

        /// <summary>Screen position and projected handle directions for a maneuver node.</summary>
        public bool NodeScreenFrame(ManeuverNodeRecord n, out Vector3 screen, out Vector2 pro, out Vector2 nrm, out Vector2 rad)
        {
            screen = Vector3.zero; pro = nrm = rad = Vector2.zero;
            var list = Trajectory.HasNodes ? Trajectory.Planned : Trajectory.Current;
            var p = FindPatch(list, n.ut, true);
            if (p == null) return false;
            Vector3d focus = FocusAbsolute(Sim.UT);
            p.Orbit.GetStateAtUT(n.ut, out var r, out var v);
            PatchedConics.OrbitalFrame(r, v, out var P, out var N, out var R);
            Vector3d abs = p.Body.GetPositionAtUT(n.ut) + r;
            Vector3 w = ToMap(abs, focus);
            screen = MapCamera.WorldToScreenPoint(w);
            if (screen.z <= 0) return false;
            float len = Mathf.Max(0.001f, CamDistance * 0.08f);
            Vector2 origin2 = screen;
            var cam = MapCamera;
            Vector2 S(Vector3d dir) { Vector3 s2 = cam.WorldToScreenPoint(w + (Vector3)dir * len); return (Vector2)s2 - origin2; }
            pro = S(P); nrm = S(N); rad = S(R);
            return true;
        }

        private void OnDestroy()
        {
            if (_flightSky != null && RenderSettings.skybox == _mapSky) RenderSettings.skybox = _flightSky;
        }
    }
}
