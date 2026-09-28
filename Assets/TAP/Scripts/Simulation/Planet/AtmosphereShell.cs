using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>Glowing atmospheric limb seen from space (analytic ray/shell optical depth shader).</summary>
    public sealed class AtmosphereShell : MonoBehaviour
    {
        public CelestialBody Body;
        /// <summary>
        /// Drawn by the flight camera: the body is near. Far away the far view (ScaledSpace) draws its own copy, and this
        /// one stays hidden.
        /// </summary>
        public bool Local = true;
        private Material _mat;
        private MeshRenderer _mr;

        public static AtmosphereShell Create(CelestialBody body, Transform parent, int layer = Layers.Default)
        {
            var sh = Shader.Find("TAP/AtmosphereShell");
            var mat = Resources.Load<Material>("Materials/AtmosphereShell");
            if (mat == null && sh == null) return null;
            if (mat == null) mat = new Material(sh);
            var go = new GameObject("Atmosphere " + body.Name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AtmosphereShell>();
            s.Body = body;
            s._mat = new Material(mat);
            var mb = new MeshBuilder();
            mb.Sphere(0, Vector3.zero, 1f, 64, 40);
            var mesh = mb.ToMergedMesh("atmo_shell");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            s._mr = go.AddComponent<MeshRenderer>();
            s._mr.sharedMaterial = s._mat;
            s._mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            s._mr.receiveShadows = false;
            var c = body.Def.atmosphere.skyColor;
            s._mat.SetColor("_Color", new Color(c[0], c[1], c[2]) * 0.9f);
            return s;
        }

        public void UpdateShell(ReferenceFrame frame, double rut, Vector3d renderOrigin, Vector3d camTrue)
        {
            if (!Local) { Hide(); return; }
            Vector3d center = frame.BodyPosition(Body, rut) - renderOrigin;
            double camDist = (camTrue - frame.BodyPosition(Body, rut)).magnitude;
            var sim = FlightSim.Instance;
            var sun = sim != null ? sim.System.SunDirectionFrom(Body, Vector3d.zero, sim.RenderUT) : Vector3d.up;
            Place((Vector3)center, 1.0, camDist, (Vector3)sun);
        }

        /// <summary>
        /// Shows the shell around <paramref name="center"/> in a space of <paramref name="metresPerUnit"/> (1 in the
        /// flight scene), seen from <paramref name="camDist"/> metres from the body's centre. Only meaningful from high up
        /// or from space: inside the atmosphere the sky shader and the fog take over.
        /// </summary>
        public void Place(Vector3 center, double metresPerUnit, double camDist, Vector3 sunDir)
        {
            double atmoR = Body.Radius + Body.Atmosphere.Height;
            float vis = Mathf.Clamp01((float)((camDist - Body.Radius - Body.Atmosphere.Height * 0.55) / (Body.Atmosphere.Height * 0.6)));
            bool show = vis > 0.01f;
            _mr.enabled = show;
            if (!show) return;
            transform.position = center;
            transform.localScale = Vector3.one * (float)(atmoR / metresPerUnit);
            _mat.SetVector("_PlanetCenter", center);
            _mat.SetFloat("_PlanetRadius", (float)(Body.Radius / metresPerUnit));
            _mat.SetFloat("_AtmoRadius", (float)(atmoR / metresPerUnit));
            _mat.SetFloat("_Intensity", 1.6f * vis);
            _mat.SetVector("_SunDir", sunDir);
        }

        public void Hide()
        {
            if (_mr.enabled) _mr.enabled = false;
        }
    }
}
