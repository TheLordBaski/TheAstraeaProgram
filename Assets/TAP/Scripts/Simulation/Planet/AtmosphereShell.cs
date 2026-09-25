using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>Glowing atmospheric limb seen from space (analytic ray/shell optical depth shader).</summary>
    public sealed class AtmosphereShell : MonoBehaviour
    {
        public CelestialBody Body;
        private Material _mat;
        private MeshRenderer _mr;

        public static AtmosphereShell Create(CelestialBody body, Transform parent)
        {
            var sh = Shader.Find("TAP/AtmosphereShell");
            var mat = Resources.Load<Material>("Materials/AtmosphereShell");
            if (mat == null && sh == null) return null;
            if (mat == null) mat = new Material(sh);
            var go = new GameObject("Atmosphere " + body.Name);
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
            double atmoR = Body.Radius + Body.Atmosphere.Height;
            Vector3d center = frame.BodyPosition(Body, rut) - renderOrigin;
            double camDist = (camTrue - frame.BodyPosition(Body, rut)).magnitude;
            // Only meaningful from high up or space; inside we rely on the sky shader + fog.
            float vis = Mathf.Clamp01((float)((camDist - Body.Radius - Body.Atmosphere.Height * 0.55) / (Body.Atmosphere.Height * 0.6)));
            bool show = vis > 0.01f;
            _mr.enabled = show;
            if (!show) return;
            // Keep the sphere itself at float-friendly scale: place it relative to the camera if far away.
            transform.position = (Vector3)center;
            transform.localScale = Vector3.one * (float)atmoR;
            _mat.SetVector("_PlanetCenter", (Vector3)center);
            _mat.SetFloat("_PlanetRadius", (float)Body.Radius);
            _mat.SetFloat("_AtmoRadius", (float)atmoR);
            _mat.SetFloat("_Intensity", 1.6f * vis);
            var sun = FlightSim.Instance != null ? FlightSim.Instance.System.SunDirection : Vector3d.up;
            _mat.SetVector("_SunDir", (Vector3)sun);
        }
    }
}
