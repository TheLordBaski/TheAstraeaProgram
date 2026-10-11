using System;
using System.Collections.Generic;
using TAP.Parts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAP.Game
{
    /// <summary>A building of the space center that opens a screen when it is clicked.</summary>
    public sealed class Facility : MonoBehaviour
    {
        public string Id;
        public string DisplayName;
        /// <summary>World position of the building's marker.</summary>
        public Vector3 Anchor;
        internal Renderer[] Renderers;
    }

    /// <summary>
    /// The space center scene (FND-07): the launch complex as placeholder boxes with a slow camera, and the buildings that open
    /// the assembly building, the launch pad and the tracking station. HUB-01 and HUB-02 replace the look and the camera.
    /// </summary>
    public sealed class SpaceCenterScene : MonoBehaviour
    {
        public const string Assembly = "assembly", Pad = "pad", Tracking = "tracking";

        public Camera Cam;
        public readonly List<Facility> Facilities = new List<Facility>();
        public Facility Hover { get; private set; }
        public event Action<Facility> Clicked;
        /// <summary>Set by the UI: true while the pointer is over a panel or a marker, so the buildings behind it ignore it.</summary>
        public Func<bool> PointerOverUi;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly Vector3 Target = new Vector3(-20f, 20f, 0f);
        private MaterialPropertyBlock _block;
        private float _time;

        public static SpaceCenterScene Build()
        {
            Time.timeScale = 1f;
            var go = new GameObject("SpaceCenter");
            var sc = go.AddComponent<SpaceCenterScene>();
            sc._block = new MaterialPropertyBlock();
            sc.BuildLight();
            sc.BuildGround();
            sc.BuildPad();
            sc.BuildAssembly();
            sc.BuildTracking();
            sc.BuildCamera();
            foreach (var f in sc.Facilities) f.Renderers = f.GetComponentsInChildren<Renderer>();
            return sc;
        }

        // ------------------------------------------------------------------ site

        private void BuildLight()
        {
            var sky = Resources.Load<Material>("Materials/Sky");
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.4f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.21f, 0.2f);
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.intensity = 1.4f;
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, 140f, 0f);
        }

        private void BuildGround()
        {
            Box("Plain", transform, new Vector3(0, -1f, 0), new Vector3(20000, 2, 20000), new Color(0.3f, 0.36f, 0.24f), false);
            // The road from the assembly building past the pad, a spur to the tracking station and its apron.
            var tar = new Color(0.3f, 0.3f, 0.31f);
            Box("Road", transform, new Vector3(-12, 0.05f, 0), new Vector3(325, 0.2f, 14), tar, false);
            Box("Spur", transform, new Vector3(150, 0.05f, -35), new Vector3(14, 0.2f, 70), tar, false);
            Box("Apron", transform, new Vector3(180, 0.05f, -85), new Vector3(70, 0.2f, 50), new Color(0.34f, 0.34f, 0.35f), false);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.00022f;
            RenderSettings.fogColor = new Color(0.66f, 0.76f, 0.88f);
        }

        private void BuildPad()
        {
            var f = NewFacility(Pad, "Launch Pad", new Vector3(0, 70, 0));
            var t = f.transform;
            var concrete = new Color(0.62f, 0.62f, 0.6f);
            Cylinder("Deck", t, new Vector3(0, 0.5f, 0), new Vector3(46, 0.5f, 46), concrete);
            Box("FlameTrench", t, new Vector3(0, 1.1f, 0), new Vector3(8, 0.3f, 40), new Color(0.12f, 0.12f, 0.13f), false);
            // The rocket on the pad: a capsule-shaped placeholder.
            Cylinder("Rocket", t, new Vector3(0, 23, 0), new Vector3(3.7f, 21, 3.7f), new Color(0.93f, 0.93f, 0.9f));
            Cylinder("RocketNose", t, new Vector3(0, 45.5f, 0), new Vector3(3.7f, 1.6f, 3.7f), new Color(0.85f, 0.35f, 0.2f));
            // Service tower with a few crossbars.
            var steel = new Color(0.7f, 0.2f, 0.15f);
            Box("Tower", t, new Vector3(-12, 32, 0), new Vector3(4, 64, 4), steel);
            for (int i = 0; i < 4; i++)
                Box("Arm" + i, t, new Vector3(-6.5f, 18 + i * 10, 0), new Vector3(9, 1, 1.5f), steel);
            Box("Lightning", t, new Vector3(18, 35, 14), new Vector3(0.6f, 70, 0.6f), new Color(0.75f, 0.75f, 0.78f));
        }

        private void BuildAssembly()
        {
            var f = NewFacility(Assembly, "Assembly Building", new Vector3(-220, 75, 10));
            var t = f.transform;
            Box("Hall", t, new Vector3(-220, 29, 10), new Vector3(90, 58, 70), new Color(0.88f, 0.89f, 0.9f));
            Box("Roof", t, new Vector3(-220, 59, 10), new Vector3(92, 2, 72), new Color(0.35f, 0.37f, 0.4f));
            Box("Stripe", t, new Vector3(-220, 40, 10), new Vector3(91, 4, 71), new Color(0.2f, 0.4f, 0.75f));
            Box("Door", t, new Vector3(-174.6f, 22, 10), new Vector3(1, 44, 24), new Color(0.15f, 0.16f, 0.18f));
            Box("Annex", t, new Vector3(-220, 8, -50), new Vector3(60, 16, 28), new Color(0.78f, 0.8f, 0.82f));
        }

        private void BuildTracking()
        {
            var o = new Vector3(180, 0, -85);
            var f = NewFacility(Tracking, "Tracking Station", o + new Vector3(0, 42, 0));
            var t = f.transform;
            Box("Block", t, o + new Vector3(0, 6, 0), new Vector3(46, 12, 28), new Color(0.84f, 0.85f, 0.86f));
            Box("Band", t, o + new Vector3(0, 8, 0), new Vector3(46.5f, 2, 28.5f), new Color(0.2f, 0.4f, 0.75f));
            Cylinder("Mast", t, o + new Vector3(15, 15, 0), new Vector3(3.5f, 4, 3.5f), new Color(0.7f, 0.7f, 0.72f));
            // The dish tilts to the sky; its feed arm points along the dish axis.
            var tilt = Quaternion.Euler(-35f, 25f, 0f);
            var dishPos = o + new Vector3(15, 23, 0);
            Cylinder("Dish", t, dishPos, new Vector3(22, 0.8f, 22), new Color(0.9f, 0.9f, 0.92f)).transform.rotation = tilt;
            Cylinder("Feed", t, dishPos + tilt * Vector3.up * 6f, new Vector3(0.6f, 5, 0.6f), new Color(0.55f, 0.55f, 0.58f)).transform.rotation = tilt;
        }

        private void BuildCamera()
        {
            var go = new GameObject("HubCamera");
            go.transform.SetParent(transform, false);
            Cam = go.AddComponent<Camera>();
            Cam.fieldOfView = 45f;
            Cam.nearClipPlane = 1f;
            Cam.farClipPlane = 6000f;
            Cam.clearFlags = CameraClearFlags.Skybox;
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
            go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            PlaceCamera();
        }

        private void PlaceCamera()
        {
            // Sways ±20° around a view from the south-west, so the three buildings stay in frame.
            float yaw = 200f + 20f * Mathf.Sin(_time * 0.12f);
            var dir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Cam.transform.position = Target + dir * 480f + Vector3.up * 140f;
            Cam.transform.LookAt(Target);
        }

        // ------------------------------------------------------------------ pointer

        private void Update()
        {
            GameSession.TickPlayTime(Time.unscaledDeltaTime);
            _time += Time.unscaledDeltaTime;
            PlaceCamera();
            var mouse = Mouse.current;
            if (mouse == null) return;
            bool overUi = PointerOverUi != null && PointerOverUi();
            if (!overUi) SetHover(Pick(mouse.position.ReadValue()));
            if (!overUi && mouse.leftButton.wasPressedThisFrame && Hover != null) Clicked?.Invoke(Hover);
        }

        private Facility Pick(Vector2 screen)
        {
            if (!Physics.Raycast(Cam.ScreenPointToRay(screen), out var hit, 5000f)) return null;
            return hit.collider.GetComponentInParent<Facility>();
        }

        /// <summary>Highlights a building (null clears it); the pointer on a marker does the same as the pointer on the building.</summary>
        public void SetHover(Facility f)
        {
            if (f == Hover) return;
            if (Hover != null) Tint(Hover, null);
            Hover = f;
            if (f != null) Tint(f, new Color(0.35f, 0.8f, 1f));
        }

        private void Tint(Facility f, Color? c)
        {
            foreach (var r in f.Renderers)
            {
                if (r == null) continue;
                if (c == null) { r.SetPropertyBlock(null); continue; }
                r.GetPropertyBlock(_block);
                var baseColor = r.sharedMaterial.GetColor(BaseColor);
                _block.SetColor(BaseColor, Color.Lerp(baseColor, c.Value, 0.55f));
                r.SetPropertyBlock(_block);
            }
        }

        // ------------------------------------------------------------------ primitives

        private Facility NewFacility(string id, string name, Vector3 anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var f = go.AddComponent<Facility>();
            f.Id = id;
            f.DisplayName = name;
            f.Anchor = anchor;
            Facilities.Add(f);
            return f;
        }

        private static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Color c, bool collider = true)
            => Primitive(PrimitiveType.Cube, name, parent, pos, size, c, collider);

        /// <summary>A cylinder given by its diameter (x, z) and half height (y), like Unity's primitive.</summary>
        private static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 size, Color c)
            => Primitive(PrimitiveType.Cylinder, name, parent, pos, size, c, true);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 size, Color c, bool collider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = PartMaterials.CreateLit(name, c, 0f, 0.25f, false);
            return go;
        }
    }
}
