using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TAP.Core;
using TAP.Parts;
using TAP.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TAP.EditorTools
{
    /// <summary>
    /// One bounded worker produces CPU data using the game's generator. Unity objects are made on the editor thread;
    /// revision checks prevent an older result replacing a newer draft. All preview objects are temporary.
    /// </summary>
    public sealed class BodyLabPreview : IDisposable
    {
        public enum ViewMode { Surface, Biomes, Height }
        public sealed class Result
        {
            public int Revision, Width, Resolution;
            public CelestialBody Body;
            public ChunkData[] Globe;
            public ChunkData Patch;
            public Color32[][] GlobeBiomes, GlobeHeights;
            public Color32[] PatchBiomes, PatchHeights, SurfaceMap, BiomeMap, HeightMap;
            public double[] Shares;
            public double MinHeight, MaxHeight, Latitude, Longitude, PatchWidth;
        }

        private Task<Result> _task;
        private CancellationTokenSource _cancel;
        private int _revision;
        private readonly GameObject _root;
        private GameObject _globe, _patch, _shell;
        private Camera _camera;
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly List<(Mesh mesh, Color32[] surface, Color32[] biomes, Color32[] heights)> _colours = new List<(Mesh, Color32[], Color32[], Color32[])>();
        private Material _surface, _diagnostic;
        private RenderTexture _render;
        private Texture2D _surfaceMap, _biomeMap, _heightMap;
        private float _yaw = 110, _pitch = 15, _distance = 34;
        public Result Current { get; private set; }
        public bool Busy => _task != null;
        public string Error { get; private set; }
        public bool PatchView { get; private set; }
        public ViewMode Mode { get; private set; }
        public RenderTexture Image => _render;
        public Texture2D Map => Mode == ViewMode.Biomes ? _biomeMap : Mode == ViewMode.Height ? _heightMap : _surfaceMap;

        public BodyLabPreview(Scene scene)
        {
            _root = NewObject("Body Lab Preview (temporary)", null);
            SceneManager.MoveGameObjectToScene(_root, scene);
            var go = NewObject("Preview Camera", _root.transform);
            _camera = go.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.cullingMask = 1 << 31;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.025f, 0.035f, 0.06f);
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = 1000;
            _camera.fieldOfView = 42;
            _camera.allowHDR = true;
            _surface = Material("Materials/Terrain", "TAP/Terrain");
            _diagnostic = Material("Materials/Terrain", "TAP/Terrain");
            foreach (var mat in new[] { _surface, _diagnostic })
            {
                mat.SetFloat("_SkyAmbient", 0);
                mat.SetColor("_SpaceAmbient", PlanetTerrain.SpaceAmbient);
                mat.SetVector("_BodySunDir", new Vector4(0.6f, 0.7f, -0.35f, 1));
            }
            _diagnostic.SetFloat("_DetailStrength", 0);
            _diagnostic.SetFloat("_WaterSpecular", 0);
        }

        private static GameObject NewObject(string name, Transform parent)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave, layer = 31 };
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private Material Material(string resource, string shader)
        {
            var basis = Resources.Load<Material>(resource);
            var mat = basis != null ? new Material(basis) : new Material(Shader.Find(shader));
            mat.hideFlags = HideFlags.HideAndDontSave;
            _materials.Add(mat);
            return mat;
        }

        public void Invalidate() { _revision++; _cancel?.Cancel(); }

        public bool Start(CelestialBody body, double latitude, double longitude, double patchWidth, bool refined)
        {
            if (Busy) return false;
            Error = null;
            int revision = _revision, resolution = refined ? 56 : 20, width = refined ? 256 : 96;
            _cancel = new CancellationTokenSource();
            var token = _cancel.Token;
            _task = Task.Run(() => Generate(body, latitude, longitude, patchWidth, resolution, width, revision, token), token);
            return true;
        }

        public static Result Generate(CelestialBody body, double latitude, double longitude, double patchWidth, int resolution, int width, int revision, CancellationToken cancel)
        {
            var r = new Result { Body = body, Revision = revision, Width = width, Resolution = resolution, Latitude = latitude, Longitude = longitude, PatchWidth = patchWidth };
            if (body.IsStar) return r;
            var t = body.Terrain;
            r.Globe = new ChunkData[6]; r.GlobeBiomes = new Color32[6][]; r.GlobeHeights = new Color32[6][];
            r.MinHeight = double.PositiveInfinity; r.MaxHeight = double.NegativeInfinity;
            for (int f = 0; f < 6; f++)
            {
                cancel.ThrowIfCancellationRequested();
                r.Globe[f] = TerrainChunkGenerator.Generate(t, body.Radius, f, -1, -1, 2, resolution, body.HasOcean, false);
                DiagnosticColours(t, r.Globe[f], out r.GlobeBiomes[f], out r.GlobeHeights[f]);
            }
            cancel.ThrowIfCancellationRequested();
            CubeSphere.FromSphere(Geo.FromLatLon(latitude, longitude), out int face, out double u, out double v);
            double size = Math.Min(2, patchWidth / body.Radius * 4 / Math.PI);
            double u0 = MathD.Clamp(u - size / 2, -1, 1 - size), v0 = MathD.Clamp(v - size / 2, -1, 1 - size);
            r.Patch = TerrainChunkGenerator.Generate(t, body.Radius, face, u0, v0, size, resolution, body.HasOcean, false);
            DiagnosticColours(t, r.Patch, out r.PatchBiomes, out r.PatchHeights);
            cancel.ThrowIfCancellationRequested();
            int height = width / 2;
            r.SurfaceMap = PlanetMaps.Surface(t, width, height);
            cancel.ThrowIfCancellationRequested();
            var indices = PlanetMaps.BiomeIndices(t, width, height);
            r.BiomeMap = PlanetMaps.Biomes(t, indices, width, height, r.SurfaceMap);
            r.HeightMap = new Color32[width * height];
            r.Shares = new double[t.Biomes.Count];
            double total = 0;
            for (int y = 0; y < height; y++)
            {
                cancel.ThrowIfCancellationRequested();
                double weight = Math.Cos(((y + 0.5) / height - 0.5) * Math.PI);
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    double h = t.Height(PlanetMaps.DirectionOf(x, y, width, height));
                    r.HeightMap[i] = HeightColour(t, h);
                    r.MinHeight = Math.Min(r.MinHeight, h); r.MaxHeight = Math.Max(r.MaxHeight, h);
                    r.Shares[indices[i]] += weight; total += weight;
                }
            }
            for (int i = 0; i < r.Shares.Length; i++) r.Shares[i] = 100 * r.Shares[i] / total;
            return r;
        }

        private static Color32 HeightColour(TerrainGenerator t, double height)
        {
            float value = (float)MathD.Clamp01((height - t.MinHeight) / (t.MaxHeight - t.MinHeight));
            return Color.Lerp(new Color(0.04f, 0.15f, 0.5f), new Color(1, 0.85f, 0.25f), value);
        }

        private static void DiagnosticColours(TerrainGenerator t, ChunkData data, out Color32[] biomes, out Color32[] heights)
        {
            biomes = new Color32[data.Positions.Length]; heights = new Color32[data.Positions.Length];
            for (int i = 0; i < data.Positions.Length; i++)
            {
                var dir = (data.CenterBF + (Vector3d)data.Positions[i]).normalized;
                biomes[i] = t.BiomeAt(dir).Color; biomes[i].a = 0;
                heights[i] = HeightColour(t, t.Height(dir)); heights[i].a = 0;
            }
        }

        /// <summary>Consumes a finished worker without blocking. Returns true only for a current, valid result.</summary>
        public bool Poll()
        {
            if (_task == null || !_task.IsCompleted) return false;
            var task = _task; _task = null; _cancel.Dispose(); _cancel = null;
            if (task.IsCanceled) return false;
            if (task.IsFaulted)
            {
                if (!(task.Exception.GetBaseException() is OperationCanceledException)) Error = task.Exception.GetBaseException().Message;
                return false;
            }
            if (task.Result.Revision != _revision) return false;
            Apply(task.Result);
            return true;
        }

        private void Apply(Result result)
        {
            ClearGeometry(); Current = result;
            _globe = NewObject("Globe", _root.transform); _patch = NewObject("Surface Patch", _root.transform);
            var body = result.Body;
            if (body.IsStar)
            {
                var mat = Material("Materials/Star", "TAP/Star");
                var c = body.Def.mapColor; mat.SetColor("_Color", new Color(c[0], c[1], c[2])); mat.SetFloat("_Intensity", 1.5f);
                var builder = new MeshBuilder(); builder.Sphere(0, Vector3.zero, 10, 64, 40);
                var mesh = builder.ToMergedMesh("Star preview"); _meshes.Add(mesh);
                AddRenderer(_globe, mesh, mat);
            }
            else
            {
                double scale = body.Radius / 10;
                _surface.SetFloat("_DistanceScale", (float)scale);
                for (int f = 0; f < 6; f++) AddChunk(_globe.transform, result.Globe[f], scale, Quaternion.identity, result.GlobeBiomes[f], result.GlobeHeights[f]);
                var dir = Geo.FromLatLon(result.Latitude, result.Longitude);
                Quaternion groundRotation = Quaternion.FromToRotation((Vector3)dir, Vector3.up);
                var patch = AddChunk(_patch.transform, result.Patch, result.PatchWidth / 10, groundRotation, result.PatchBiomes, result.PatchHeights);
                patch.transform.localPosition = Vector3.zero;
                _shell = NewObject("Atmosphere", _globe.transform);
                if (body.HasAtmosphere)
                {
                    var mat = Material("Materials/AtmosphereShell", "TAP/AtmosphereShell");
                    var builder = new MeshBuilder(); builder.Sphere(0, Vector3.zero, 1, 64, 40);
                    var mesh = builder.ToMergedMesh("Atmosphere preview"); _meshes.Add(mesh);
                    AddRenderer(_shell, mesh, mat);
                    _shell.transform.localScale = Vector3.one * (float)((body.Radius + body.Atmosphere.Height) / scale);
                    var c = body.Def.atmosphere.skyColor;
                    mat.SetColor("_Color", new Color(c[0], c[1], c[2]) * 0.9f);
                    mat.SetVector("_PlanetCenter", Vector3.zero); mat.SetFloat("_PlanetRadius", 10);
                    mat.SetFloat("_AtmoRadius", (float)((body.Radius + body.Atmosphere.Height) / scale));
                    mat.SetFloat("_Intensity", 1.6f); mat.SetVector("_SunDir", new Vector3(0.6f, 0.7f, -0.35f));
                }
                else _shell.SetActive(false);
                _surfaceMap = PlanetMaps.ToTexture(PlanetMaps.NorthUp(result.SurfaceMap, result.Width, result.Width / 2), result.Width, result.Width / 2, false);
                _biomeMap = PlanetMaps.ToTexture(PlanetMaps.NorthUp(result.BiomeMap, result.Width, result.Width / 2), result.Width, result.Width / 2, false);
                _heightMap = PlanetMaps.ToTexture(PlanetMaps.NorthUp(result.HeightMap, result.Width, result.Width / 2), result.Width, result.Width / 2, false);
                foreach (var t in new[] { _surfaceMap, _biomeMap, _heightMap }) t.hideFlags = HideFlags.HideAndDontSave;
            }
            SetView(PatchView, Mode);
        }

        private GameObject AddChunk(Transform parent, ChunkData data, double scale, Quaternion rotation, Color32[] biomes, Color32[] heights)
        {
            var go = NewObject("Terrain", parent);
            go.transform.localPosition = rotation * (Vector3)(data.CenterBF / scale);
            var positions = new Vector3[data.Positions.Length]; var normals = new Vector3[positions.Length];
            for (int i = 0; i < positions.Length; i++) { positions[i] = rotation * data.Positions[i] / (float)scale; normals[i] = rotation * data.Normals[i]; }
            var mesh = new Mesh { name = "Body lab terrain", hideFlags = HideFlags.HideAndDontSave, vertices = positions, normals = normals, colors32 = data.Colors, uv = data.UV0, uv2 = data.UV1, triangles = data.Triangles };
            mesh.RecalculateBounds(); _meshes.Add(mesh);
            AddRenderer(go, mesh, _surface);
            _colours.Add((mesh, data.Colors, biomes, heights));
            return go;
        }

        private static void AddRenderer(GameObject go, Mesh mesh, Material material)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        }

        public void SetView(bool patch, ViewMode mode)
        {
            PatchView = patch && Current?.Body.IsStar != true; Mode = mode;
            if (_globe == null) return;
            _globe.SetActive(!PatchView); _patch.SetActive(PatchView);
            foreach (var (mesh, surface, biomes, heights) in _colours) mesh.colors32 = mode == ViewMode.Biomes ? biomes : mode == ViewMode.Height ? heights : surface;
            foreach (var go in new[] { _globe, _patch })
                foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true))
                    if (renderer.gameObject != _shell && !Current.Body.IsStar) renderer.sharedMaterial = mode == ViewMode.Surface ? _surface : _diagnostic;
            if (_shell != null) _shell.SetActive(Current.Body.HasAtmosphere && mode == ViewMode.Surface);
        }

        public void Orbit(float dx, float dy) { _yaw += dx; _pitch = Mathf.Clamp(_pitch + dy, -85, 85); }
        public void Zoom(float amount) => _distance = Mathf.Clamp(_distance * Mathf.Exp(amount * 0.07f), PatchView ? 0.5f : 11, 150);
        public void Frame() { _yaw = PatchView ? 0 : 110; _pitch = PatchView ? 45 : 15; _distance = PatchView ? 17 : 34; }

        public void Render(int width, int height, double ut, float lightAngle)
        {
            if (_root == null || Current == null) return;
            width = Mathf.Clamp(width, 64, 1600); height = Mathf.Clamp(height, 64, 1200);
            if (_render == null || _render.width != width || _render.height != height)
            {
                if (_render != null) { _render.Release(); UnityEngine.Object.DestroyImmediate(_render); }
                _render = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { hideFlags = HideFlags.HideAndDontSave };
            }
            var rotation = Quaternion.Euler(_pitch, _yaw, 0);
            _camera.transform.position = rotation * new Vector3(0, 0, -_distance);
            _camera.transform.rotation = rotation;
            _camera.targetTexture = _render;
            _surface.SetFloat("_DistanceScale", (float)(PatchView ? Current.PatchWidth / 10 : Current.Body.Radius / 10));
            var sun = Quaternion.Euler(0, lightAngle, 0) * new Vector3(0.6f, 0.7f, -0.35f).normalized;
            foreach (var mat in new[] { _surface, _diagnostic }) mat.SetVector("_BodySunDir", new Vector4(sun.x, sun.y, sun.z, 1));
            _globe.transform.rotation = Quaternion.AngleAxis((float)(Current.Body.RotationAngleAtUT(ut) * MathD.Rad2Deg), Vector3.up);
            if (_shell != null && Current.Body.HasAtmosphere)
            {
                var mat = _shell.GetComponent<MeshRenderer>().sharedMaterial;
                mat.SetVector("_SunDir", sun);
            }
            _camera.Render();
        }

        private void ClearGeometry()
        {
            if (_globe != null) UnityEngine.Object.DestroyImmediate(_globe);
            if (_patch != null) UnityEngine.Object.DestroyImmediate(_patch);
            foreach (var mesh in _meshes) UnityEngine.Object.DestroyImmediate(mesh);
            _meshes.Clear(); _colours.Clear(); _shell = null;
            // The two terrain materials last for the session; atmosphere/star materials belong to each result.
            for (int i = _materials.Count - 1; i >= 2; i--) { UnityEngine.Object.DestroyImmediate(_materials[i]); _materials.RemoveAt(i); }
            foreach (var texture in new[] { _surfaceMap, _biomeMap, _heightMap }) if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            _surfaceMap = _biomeMap = _heightMap = null;
        }

        public void Dispose()
        {
            Invalidate();
            if (_task != null)
            {
                var cancel = _cancel;
                _task.ContinueWith(t => { if (t.IsFaulted) _ = t.Exception; cancel.Dispose(); }, TaskScheduler.Default);
                _task = null; _cancel = null;
            }
            ClearGeometry();
            foreach (var material in _materials) UnityEngine.Object.DestroyImmediate(material);
            _materials.Clear();
            if (_render != null) { _render.Release(); UnityEngine.Object.DestroyImmediate(_render); }
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }
    }
}
