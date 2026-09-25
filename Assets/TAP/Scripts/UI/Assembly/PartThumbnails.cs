using System.Collections.Generic;
using System.Text;
using TAP.Parts;
using UnityEngine;

namespace TAP.UI
{
    /// <summary>Renders part preview icons at runtime (one off-screen render per part, cached for the session).</summary>
    public static class PartThumbnails
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private const int ThumbLayer = 30;
        private const int Size = 128;
        private static Camera _cam;
        private static Light _light;

        public static Sprite Get(PartDefinition def)
        {
            if (def == null) return null;
            if (Cache.TryGetValue(def.id, out var s) && s != null) return s;
            s = Render(def);
            Cache[def.id] = s;
            return s;
        }

        private static void EnsureCamera()
        {
            if (_cam != null) return;
            var go = new GameObject("ThumbnailCamera");
            Object.DontDestroyOnLoad(go);
            _cam = go.AddComponent<Camera>();
            _cam.enabled = false;
            _cam.cullingMask = 1 << ThumbLayer;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0, 0, 0, 0);
            _cam.fieldOfView = 28f;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 200f;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            var lightGo = new GameObject("ThumbnailLight");
            lightGo.transform.SetParent(go.transform, false);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.intensity = 1.1f;
            _light.color = new Color(1f, 0.97f, 0.92f);
            _light.cullingMask = 1 << ThumbLayer;
            _light.shadows = LightShadows.None;
            _light.enabled = false;
            // The very first off-screen render after scene load can come back empty: warm up once.
            var warm = RenderTexture.GetTemporary(16, 16, 24, RenderTextureFormat.ARGB32);
            _cam.targetTexture = warm;
            _cam.Render();
            _cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(warm);
        }

        private static Sprite Render(PartDefinition def)
        {
            EnsureCamera();
            var part = PartModelFactory.Build(def, ThumbLayer);
            SetLayer(part, ThumbLayer);
            if (def.landingLeg != null)
            {
                var pivot = part.transform.Find("Model/LegPivot");
                if (pivot != null) pivot.localRotation = Quaternion.AngleAxis(-def.landingLeg.splayDeg, Vector3.right);
            }
            Vector3 place = new Vector3(0, -8000f, 0);
            part.transform.position = place;
            // Surface-attached parts face the camera side.
            if (def.surfaceAttach.allowed && def.nodes.Count == 0) part.transform.rotation = Quaternion.Euler(0, 200f, 0);
            Bounds b = new Bounds(place, Vector3.one * 0.1f);
            bool first = true;
            foreach (var r in part.GetComponentsInChildren<Renderer>())
            {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            float radius = Mathf.Max(0.2f, b.extents.magnitude);
            Vector3 dir = Quaternion.Euler(18f, -32f, 0) * Vector3.forward;
            float dist = radius / Mathf.Sin(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.02f;
            _cam.transform.position = b.center - dir * dist;
            _cam.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            _light.transform.rotation = Quaternion.Euler(35f, -60f, 0);
            _light.enabled = true;

            var rt = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32);
            _cam.targetTexture = rt;
            _cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "Thumb_" + def.id };
            tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            _cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            _light.enabled = false;
            part.SetActive(false);
            Object.Destroy(part);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }

        private static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayer(t.gameObject, layer);
        }
    }

    /// <summary>Human-readable part descriptions (palette tooltips, part menus).</summary>
    public static class PartInfoText
    {
        public static string Describe(PartDefinition d, PartDatabase db, bool full = true)
        {
            var sb = new StringBuilder();
            sb.Append("<b>").Append(d.title).Append("</b>\n");
            sb.Append($"<size=13><color=#{UIKit.Hex(UIKit.TextDim)}>{d.manufacturer} · {d.category}</color></size>\n");
            if (full && !string.IsNullOrEmpty(d.description)) sb.Append($"<size=14>{d.description}</size>\n");
            double wet = d.WetMass(db);
            sb.Append($"Mass: {Mass(d.dryMass)}");
            if (wet > d.dryMass + 0.01) sb.Append($" dry, {Mass(wet)} full");
            sb.Append($" · Ø {d.diameter:0.###} m\n");
            if (d.command != null)
                sb.Append(d.command.requiresCrew ? "Command: needs crew to control" : "Command: autonomous probe core").Append(d.command.sas ? " · SAS\n" : "\n");
            if (d.crew != null && d.crew.seats > 0) sb.Append($"Crew: {d.crew.seats} seat{(d.crew.seats > 1 ? "s" : "")} · hatch for EVA\n");
            if (d.engine != null)
            {
                var e = d.engine;
                double tAsl = e.thrustVac * e.ispAsl / e.ispVac;
                sb.Append($"Engine ({(e.type == "solid" ? "solid, can't throttle or stop" : "liquid, throttleable, restartable")}):\n");
                sb.Append($"  Thrust {e.thrustVac / 1000:0.#} kN vac / {tAsl / 1000:0.#} kN sea level\n");
                sb.Append($"  Isp {e.ispVac:0} s vac / {e.ispAsl:0} s sea level");
                if (e.gimbalRange > 0) sb.Append($" · gimbal ±{e.gimbalRange:0.#}°");
                sb.Append('\n');
                if (e.alternator > 0) sb.Append($"  Alternator {e.alternator:0.#} EC/s while running\n");
            }
            foreach (var r in d.resources)
            {
                var rd = db.GetResource(r.id);
                sb.Append($"Holds {r.amount:0.#} {(rd?.unit ?? "")} {rd?.name ?? r.id}\n");
            }
            if (d.decoupler != null)
                sb.Append(d.decoupler.kind == "radial" ? "Radial decoupler: drops side-mounted boosters\n" : "Stack decoupler: separates stages (blocks fuel flow)\n");
            if (d.parachute != null)
            {
                var p = d.parachute;
                sb.Append($"Parachute: opens below {p.minPressure / 1000:0.#} kPa, fully at {p.deployAltitude:0} m above ground\n");
                sb.Append($"  Tears above {p.maxLoad / 1000:0} kN canopy load or {p.maxCanopyTemp:0} K\n");
            }
            if (d.heatShield != null) sb.Append("Heat shield: ablator absorbs reentry heat; face it into the airflow\n");
            if (d.landingLeg != null) sb.Append($"Landing leg: G to deploy · breaks above {d.landingLeg.impactTolerance:0} m/s impact\n");
            if (d.reactionWheel != null) sb.Append($"Reaction wheel: {d.reactionWheel.torque / 1000:0.#} kN·m torque, uses {d.reactionWheel.ecPerSecond:0.##} EC/s\n");
            if (d.rcs != null) sb.Append($"RCS thrusters: {d.rcs.thrust / 1000:0.#} kN per nozzle, monopropellant (R to enable)\n");
            if (d.dockingPort != null) sb.Append("Docking port: joins two vessels (approach below 1 m/s)\n");
            if (d.fin != null) sb.Append("Fin: aerodynamic stability; mount low on the rocket\n");
            sb.Append($"<size=13><color=#{UIKit.Hex(UIKit.TextDim)}>Max temp {d.skinMaxTemp:0} K skin / {d.maxTemp:0} K internal · impact {d.impactTolerance:0} m/s</color></size>");
            if (full)
            {
                if (d.surfaceAttach.allowed) sb.Append($"\n<size=13><color=#{UIKit.Hex(UIKit.Accent)}>Can be attached to surfaces</color></size>");
            }
            return sb.ToString();
        }

        public static string Mass(double kg) => kg >= 1000 ? $"{kg / 1000:0.##} t" : $"{kg:0} kg";
    }
}
