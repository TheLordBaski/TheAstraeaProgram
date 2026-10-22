using TAP.Core;
using TAP.Parts;
using UnityEngine;
using UnityEngine.Rendering;

namespace TAP.Game
{
    /// <summary>Materials, the assembly building (hangar) and the CoM / CoT / CoP markers.</summary>
    public static class EditorVisuals
    {
        private static Material _overlayTemplate;

        /// <summary>Translucent unlit marker material; alwaysOnTop draws through parts.</summary>
        public static Material OverlayMaterial(Color c, bool alwaysOnTop)
        {
            if (_overlayTemplate == null)
            {
                _overlayTemplate = Resources.Load<Material>("Materials/Overlay");
                if (_overlayTemplate == null)
                {
                    var sh = Shader.Find("TAP/Overlay");
                    _overlayTemplate = sh != null ? new Material(sh) : PartMaterials.CreateLit("OverlayFallback", Color.white, 0, 0, false);
                }
            }
            var m = new Material(_overlayTemplate) { name = "Overlay" };
            m.SetColor("_Color", c);
            m.SetFloat("_ZTest", alwaysOnTop ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
            m.renderQueue = alwaysOnTop ? 3200 : 3100;
            return m;
        }

        // ------------------------------------------------------------------ hangar

        /// <summary>Loads the authored VAB at the rocket floor origin and lights the assembly workspace.</summary>
        public static GameObject BuildHangar(out Bounds assemblyBounds)
        {
            var root = VabModel.Create(assemblySpace: true);
            Vector3 min = Vector3.zero, max = Vector3.zero;
            foreach (var marker in root.GetComponentsInChildren<Transform>())
            {
                if (marker.name == "ClearVolumeMin") min = marker.position;
                if (marker.name == "ClearVolumeMax") max = marker.position;
            }
            assemblyBounds = new Bounds((min + max) * .5f,
                new Vector3(Mathf.Abs(max.x-min.x), Mathf.Abs(max.y-min.y), Mathf.Abs(max.z-min.z)));
            if (assemblyBounds.size.x < 1f) throw new System.InvalidOperationException("VAB clear-volume markers are missing");

            // Lighting: soft sky-like ambient, a key light from high above the door, fill lights.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.58f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
            RenderSettings.fog = false;
            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.transform.SetParent(root.transform, false);
            key.type = LightType.Directional;
            key.color = Color.white;
            key.intensity = 1.25f;
            key.shadows = LightShadows.None; // Interior fill must reach the craft through the enclosed roof.
            key.shadowStrength = 0.7f;
            key.transform.rotation = Quaternion.Euler(58f, -35f, 0f);
            var fill = new GameObject("FillLight").AddComponent<Light>();
            fill.transform.SetParent(root.transform, false);
            fill.type = LightType.Directional;
            fill.color = Color.white;
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);
            return root;
        }

        // ------------------------------------------------------------------ markers

        /// <summary>CoM (yellow ball), CoT (purple ball + exhaust arrow), CoP (cyan ball + restoring-force arrow).</summary>
        public sealed class CraftMarkers
        {
            private readonly GameObject _root;
            private readonly Transform _com, _cot, _cop, _cotArrow, _copArrow;

            public CraftMarkers()
            {
                _root = new GameObject("CraftMarkers");
                _com = Ball("CoM", new Color(1f, 0.85f, 0.15f, 0.9f), 0.55f);
                _cot = Ball("CoT", new Color(0.85f, 0.35f, 1f, 0.9f), 0.45f);
                _cop = Ball("CoP", new Color(0.25f, 0.85f, 1f, 0.9f), 0.45f);
                _cotArrow = Arrow("CoT arrow", new Color(0.85f, 0.35f, 1f, 0.85f));
                _copArrow = Arrow("CoP arrow", new Color(0.25f, 0.85f, 1f, 0.85f));
            }

            private Transform Ball(string name, Color c, float size)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(s.GetComponent<Collider>());
                s.name = name;
                s.layer = Layers.Ghost;
                s.transform.SetParent(_root.transform, false);
                s.transform.localScale = Vector3.one * size;
                var r = s.GetComponent<Renderer>();
                r.sharedMaterial = OverlayMaterial(c, true);
                r.shadowCastingMode = ShadowCastingMode.Off;
                return s.transform;
            }

            private Transform Arrow(string name, Color c)
            {
                var mb = new MeshBuilder();
                mb.CylinderBetween(0, Vector3.zero, new Vector3(0, 1.4f, 0), 0.06f, 10);
                mb.Lathe(0, new[] { new Vector2(0.18f, 1.4f), new Vector2(0.0f, 1.9f) }, 12);
                mb.Disc(0, 0.18f, 1.4f, false, 12);
                var go = new GameObject(name);
                go.layer = Layers.Ghost;
                go.transform.SetParent(_root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mb.ToMergedMesh(name);
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = OverlayMaterial(c, true);
                r.shadowCastingMode = ShadowCastingMode.Off;
                return go.transform;
            }

            public void Update(DesignStats s, Transform craftRoot, bool show)
            {
                bool any = show && s != null && s.PartCount > 0;
                _root.SetActive(any);
                if (!any) return;
                Vector3 o = craftRoot.position;
                _com.position = o + s.CoM;
                _cot.gameObject.SetActive(s.HasThrust);
                _cotArrow.gameObject.SetActive(s.HasThrust);
                if (s.HasThrust)
                {
                    _cot.position = o + s.CoT;
                    _cotArrow.position = o + s.CoT;
                    _cotArrow.rotation = Quaternion.FromToRotation(Vector3.up, -s.ThrustDir);
                }
                _cop.gameObject.SetActive(s.HasCoP);
                _copArrow.gameObject.SetActive(s.HasCoP);
                if (s.HasCoP)
                {
                    _cop.position = o + s.CoP;
                    _copArrow.position = o + s.CoP;
                    _copArrow.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.left);
                }
            }
        }
    }
}
