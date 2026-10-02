using System.Collections.Generic;
using System.Linq;
using TAP.Core;
using Newtonsoft.Json.Linq;

namespace TAP.Parts
{
    /// <summary>
    /// The rules of the part catalog (parts.json): each resource and part against its schema (the definition classes)
    /// and what it needs to fly. A part or resource with errors is left out and the rest load, so a broken entry gives
    /// one clear error at load instead of trouble in flight.
    /// </summary>
    public static class PartValidation
    {
        public sealed class Result
        {
            /// <summary>The resources and parts without errors.</summary>
            public PartCatalog Catalog;
            /// <summary>Parts left out because of errors: their title (when it could be read) and first error, by id.</summary>
            public readonly Dictionary<string, (string title, ContentProblem problem)> Broken = new Dictionary<string, (string, ContentProblem)>();
        }

        public static readonly string[] EngineTypes = { "liquid", "solid" };
        public static readonly string[] DecouplerKinds = { "stack", "radial" };
        /// <summary>The category of parts kept out of the parts list (EVA crew, flags).</summary>
        public const string HiddenCategory = "Hidden";

        /// <summary>Checks and reads a part catalog. Null when the file can't be read at all.</summary>
        public static Result Check(string file, string json, ContentReport report)
        {
            var token = ContentJson.Parse(json, file, report);
            if (token == null) return null;
            var top = new ContentEntry(report, file, null, token);
            if (!(token is JObject root))
            {
                top.Error("", "an object with resources, categories and parts");
                return null;
            }
            ContentJson.CheckShape(root, typeof(PartCatalog), top, "", new[] { "resources", "parts" });
            var result = new Result { Catalog = new PartCatalog() };
            result.Catalog.categories = ContentJson.ToObject<List<string>>(root["categories"] ?? new JArray()) ?? new List<string>();

            var resources = new Dictionary<string, ResourceDefinition>();
            var brokenResources = new HashSet<string>();
            if (root["resources"] is JArray rs)
                for (int i = 0; i < rs.Count; i++)
                {
                    string id = Id(rs[i]);
                    var e = new ContentEntry(report, file, id != null ? "resource " + id : $"resource #{i + 1}", rs[i]);
                    var r = Read<ResourceDefinition>(rs[i], e);
                    if (r != null) CheckResource(e, r, resources);
                    if (r == null || e.HasErrors)
                    {
                        if (id != null) brokenResources.Add(id);
                        continue;
                    }
                    resources[r.id] = r;
                    result.Catalog.resources.Add(r);
                }
            else top.Error("resources", "a list of resources");

            var ids = new HashSet<string>();
            if (root["parts"] is JArray ps)
                for (int i = 0; i < ps.Count; i++)
                {
                    string id = Id(ps[i]);
                    var e = new ContentEntry(report, file, id != null ? "part " + id : $"part #{i + 1}", ps[i]);
                    var p = Read<PartDefinition>(ps[i], e);
                    if (p != null) CheckPart(e, p, resources, brokenResources, result.Catalog.categories, ids);
                    if (p == null || e.HasErrors)
                    {
                        if (id != null && !result.Broken.ContainsKey(id) && result.Catalog.parts.All(x => x.id != id))
                            result.Broken[id] = (p?.title, e.FirstError);
                        continue;
                    }
                    result.Catalog.parts.Add(p);
                }
            else top.Error("parts", "a list of parts");
            return result;
        }

        private static string Id(JToken t) =>
            t is JObject o && o["id"] is JValue v && v.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)v) ? (string)v : null;

        private static T Read<T>(JToken token, ContentEntry e) where T : class
        {
            if (!(token is JObject))
            {
                e.Error("", "an object");
                return null;
            }
            ContentJson.CheckShape(token, typeof(T), e);
            return ContentJson.ToObject<T>(token, PartDatabase.JsonSettings);
        }

        private static string List(IEnumerable<string> ids) => string.Join(", ", ids);

        // ------------------------------------------------------------------ resources

        private static void CheckResource(ContentEntry e, ResourceDefinition r, Dictionary<string, ResourceDefinition> known)
        {
            if (e.Text(r.id, "id", "an id")) e.Check(!known.ContainsKey(r.id), "id", "an id no other resource has");
            e.Check(!string.IsNullOrWhiteSpace(r.name), "name", "the name players see", error: false);
            e.NotNegative(r.density, "density", "kg per unit");
            e.Color(r.color, "color");
        }

        /// <summary>A resource id the part refers to: a resource of the catalog without errors.</summary>
        private static void Resource(ContentEntry e, string id, string field, Dictionary<string, ResourceDefinition> resources, HashSet<string> broken)
        {
            if (id != null && resources.ContainsKey(id)) return;
            e.Error(field, "the id of a resource (" + List(resources.Keys) + ")", hint: id != null && broken.Contains(id) ? "that resource has errors" : null);
        }

        // ------------------------------------------------------------------ parts

        private static void CheckPart(ContentEntry e, PartDefinition p, Dictionary<string, ResourceDefinition> resources,
            HashSet<string> brokenResources, List<string> categories, HashSet<string> ids)
        {
            if (e.Text(p.id, "id", "an id"))
                e.Check(ids.Add(p.id), "id", "an id no other part has", hint: "the first part with this id is kept");
            e.Text(p.title, "title", "the name players see");
            if (p.category != HiddenCategory)
                e.Check(categories.Contains(p.category), "category", "one of the categories (" + List(categories) + ") or " + HiddenCategory,
                    error: false, hint: "the part is in no tab of the parts list");
            e.Positive(p.dryMass, "dryMass", "kg");
            e.Positive(p.diameter, "diameter", "metres");
            e.Positive(p.height, "height", "metres");
            if (p.model != null)
            {
                e.OneOf(p.model.type, "model.type", PartModelFactory.Types, error: false, hint: "the part is drawn as a tank");
                if (p.model.type != "eva" && p.model.type != "flag")
                {
                    e.Positive(p.model.diameter, "model.diameter", "metres");
                    e.Positive(p.model.height, "model.height", "metres");
                }
            }
            CheckNodes(e, p);
            if (p.surfaceAttach != null)
            {
                e.Vector(p.surfaceAttach.pos, 3, "surfaceAttach.pos");
                e.Vector(p.surfaceAttach.dir, 3, "surfaceAttach.dir", direction: true);
            }
            if (p.resources != null)
                for (int i = 0; i < p.resources.Count; i++)
                {
                    var r = p.resources[i];
                    string f = $"resources[{i}]";
                    if (r == null)
                    {
                        e.Error(f, "a resource amount: id, amount and max");
                        continue;
                    }
                    Resource(e, r.id, f + ".id", resources, brokenResources);
                    e.NotNegative(r.amount, f + ".amount");
                    if (r.max >= 0) e.Check(r.max >= r.amount, f + ".max", "a number of at least amount (" + ContentEntry.Number(r.amount) + ")",
                        hint: "leave it out to fill the part");
                }
            e.Positive(p.maxTemp, "maxTemp", "kelvin");
            e.Positive(p.skinMaxTemp, "skinMaxTemp", "kelvin");
            e.Positive(p.specificHeat, "specificHeat", "J per kg and kelvin");
            e.Positive(p.skinMassPerArea, "skinMassPerArea", "kg/m²");
            e.InRange(p.emissivity, 0, 1, "emissivity");
            e.Positive(p.impactTolerance, "impactTolerance", "m/s");
            e.Positive(p.breakingForce, "breakingForce", "newtons");
            e.Positive(p.breakingTorque, "breakingTorque", "newton-metres");
            if (p.drag != null)
            {
                e.NotNegative(p.drag.cdTop, "drag.cdTop");
                e.NotNegative(p.drag.cdBottom, "drag.cdBottom");
                e.NotNegative(p.drag.cdSide, "drag.cdSide");
            }
            if (p.engine != null) CheckEngine(e, p, resources, brokenResources);
            if (p.decoupler != null)
            {
                var d = p.decoupler;
                e.NotNegative(d.ejectionImpulse, "decoupler.ejectionImpulse", "newton-seconds");
                if (e.OneOf(d.kind, "decoupler.kind", DecouplerKinds) && d.kind == "stack")
                    e.Check(p.FindNode(d.explosiveNode) != null, "decoupler.explosiveNode", "the id of one of the part's nodes (" + NodeIds(p) + ")");
            }
            if (p.parachute != null)
            {
                var c = p.parachute;
                e.Positive(c.semiDeployedArea, "parachute.semiDeployedArea", "Cd·A, m²");
                e.Positive(c.deployedArea, "parachute.deployedArea", "Cd·A, m²");
                e.NotNegative(c.minPressure, "parachute.minPressure", "pascals");
                e.NotNegative(c.deployAltitude, "parachute.deployAltitude", "metres above the ground");
                e.Positive(c.semiDeployTime, "parachute.semiDeployTime", "seconds");
                e.Positive(c.deployTime, "parachute.deployTime", "seconds");
                e.Positive(c.maxLoad, "parachute.maxLoad", "newtons");
                e.Positive(c.maxCanopyTemp, "parachute.maxCanopyTemp", "kelvin");
                e.Positive(c.canopyDiameter, "parachute.canopyDiameter", "metres");
                e.Color(c.canopyColor, "parachute.canopyColor", error: false);
            }
            if (p.landingLeg != null)
            {
                var l = p.landingLeg;
                e.Positive(l.length, "landingLeg.length", "metres");
                e.InRange(l.splayDeg, 0, 90, "landingLeg.splayDeg", unit: "degrees");
                e.Positive(l.travel, "landingLeg.travel", "metres");
                e.Positive(l.spring, "landingLeg.spring", "N/m");
                e.NotNegative(l.damper, "landingLeg.damper", "N·s/m");
                e.Positive(l.impactTolerance, "landingLeg.impactTolerance", "m/s");
                e.Positive(l.maxLoad, "landingLeg.maxLoad", "newtons");
                e.Positive(l.footRadius, "landingLeg.footRadius", "metres");
                e.NotNegative(l.friction, "landingLeg.friction");
            }
            if (p.heatShield != null)
            {
                var h = p.heatShield;
                e.Positive(h.ablationStartTemp, "heatShield.ablationStartTemp", "kelvin");
                e.Positive(h.ablationHeat, "heatShield.ablationHeat", "J/kg");
                e.NotNegative(h.maxAblationRate, "heatShield.maxAblationRate", "kg/s");
                e.NotNegative(h.insulation, "heatShield.insulation", "W/K");
                e.Check(p.resources != null && p.resources.Exists(r => r?.id == "Ablator"), "resources", "Ablator for the heat shield",
                    error: false, hint: "a shield burns the Ablator its own part carries");
            }
            if (p.reactionWheel != null)
            {
                e.Positive(p.reactionWheel.torque, "reactionWheel.torque", "newton-metres");
                e.NotNegative(p.reactionWheel.ecPerSecond, "reactionWheel.ecPerSecond");
            }
            if (p.rcs != null) CheckRcs(e, p.rcs, resources, brokenResources);
            if (p.crew != null)
            {
                e.Check(p.crew.seats >= 1, "crew.seats", "a whole number of 1 or more");
                e.Vector(p.crew.hatchPos, 3, "crew.hatchPos");
                e.Vector(p.crew.hatchNormal, 3, "crew.hatchNormal", direction: true);
            }
            if (p.command != null) e.NotNegative(p.command.ecPerSecond, "command.ecPerSecond");
            if (p.dockingPort != null)
            {
                var d = p.dockingPort;
                e.Check(p.FindNode(d.nodeId) != null, "dockingPort.nodeId", "the id of one of the part's nodes (" + NodeIds(p) + ")");
                e.Positive(d.captureRange, "dockingPort.captureRange", "metres");
                e.Positive(d.captureSpeed, "dockingPort.captureSpeed", "m/s");
                e.InRange(d.captureAngleDeg, 0, 180, "dockingPort.captureAngleDeg", unit: "degrees");
                e.NotNegative(d.undockImpulse, "dockingPort.undockImpulse", "newton-seconds");
            }
            if (p.fin != null)
            {
                e.Positive(p.fin.area, "fin.area", "m²");
                e.NotNegative(p.fin.liftSlope, "fin.liftSlope", "per radian");
                e.Vector(p.fin.normal, 3, "fin.normal", direction: true);
                e.Check(p.fin.stallDeg > 0 && p.fin.stallDeg <= 90, "fin.stallDeg", "an angle above 0 and at most 90 degrees");
            }
        }

        private static string NodeIds(PartDefinition p) =>
            p.nodes == null || p.nodes.Count == 0 ? "it has none" : List(p.nodes.Where(n => n != null).Select(n => n.id));

        private static void CheckNodes(ContentEntry e, PartDefinition p)
        {
            if (p.nodes == null) return;
            var seen = new HashSet<string>();
            for (int i = 0; i < p.nodes.Count; i++)
            {
                var n = p.nodes[i];
                string f = $"nodes[{i}]";
                if (n == null)
                {
                    e.Error(f, "an attach node: id, pos, dir and size");
                    continue;
                }
                if (e.Text(n.id, f + ".id", "an id (top, bottom, …)") && e.Check(n.id != "srf", f + ".id", "an id other than srf", hint: "srf means surface attachment"))
                    e.Check(seen.Add(n.id), f + ".id", "an id no other node of this part has");
                e.Vector(n.pos, 3, f + ".pos");
                e.Vector(n.dir, 3, f + ".dir", direction: true);
                e.Check(n.size >= 0, f + ".size", "0 or more (0 = 0.625 m, 1 = 1.25 m, 2 = 2.5 m)", error: false);
            }
        }

        private static void CheckEngine(ContentEntry e, PartDefinition p, Dictionary<string, ResourceDefinition> resources, HashSet<string> brokenResources)
        {
            var g = p.engine;
            e.OneOf(g.type, "engine.type", EngineTypes);
            e.Positive(g.thrustVac, "engine.thrustVac", "newtons in vacuum");
            e.Positive(g.ispVac, "engine.ispVac", "seconds");
            e.Positive(g.ispAsl, "engine.ispAsl", "seconds");
            if (g.ispVac > 0 && g.ispAsl > 0)
                e.Check(g.ispAsl <= g.ispVac, "engine.ispAsl", "at most ispVac (" + ContentEntry.Number(g.ispVac) + ")", error: false,
                    hint: "an engine is less efficient in air than in vacuum");
            Resource(e, g.propellant, "engine.propellant", resources, brokenResources);
            if (g.type == "solid" && g.propellant != null && resources.ContainsKey(g.propellant))
                e.Check(p.resources != null && p.resources.Exists(r => r?.id == g.propellant), "resources", g.propellant + " for the solid motor",
                    error: false, hint: "a solid motor burns only what its own part carries");
            e.InRange(g.gimbalRange, 0, 90, "engine.gimbalRange", unit: "degrees");
            e.InRange(g.minThrottle, 0, 1, "engine.minThrottle");
            e.NotNegative(g.throttleResponse, "engine.throttleResponse", "throttle per second; 0 is instant");
            e.NotNegative(g.alternator, "engine.alternator", "electric charge per second");
            e.NotNegative(g.heatProduction, "engine.heatProduction", "watts");
            e.InRange(g.thrustLimit, 0, 100, "engine.thrustLimit", unit: "percent");
            e.Vector(g.thrustDir, 3, "engine.thrustDir", direction: true);
            e.Vector(g.nozzlePos, 3, "engine.nozzlePos");
            e.NotNegative(g.exhaustScale, "engine.exhaustScale");
            e.Vector(g.exhaustColor, 3, "engine.exhaustColor");
        }

        private static void CheckRcs(ContentEntry e, RcsDefinition r, Dictionary<string, ResourceDefinition> resources, HashSet<string> brokenResources)
        {
            e.Positive(r.thrust, "rcs.thrust", "newtons per nozzle");
            e.Positive(r.ispVac, "rcs.ispVac", "seconds");
            e.Positive(r.ispAsl, "rcs.ispAsl", "seconds");
            Resource(e, r.propellant, "rcs.propellant", resources, brokenResources);
            if (!e.Check(r.nozzles != null && r.nozzles.Length > 0, "rcs.nozzles", "a list of nozzles, each [x, y, z, dx, dy, dz]")) return;
            for (int k = 0; k < r.nozzles.Length; k++)
            {
                var n = r.nozzles[k];
                bool ok = n != null && n.Length == 6 && n.All(x => !float.IsNaN(x) && !float.IsInfinity(x)) && (n[3] != 0 || n[4] != 0 || n[5] != 0);
                e.Check(ok, $"rcs.nozzles[{k}]", "6 numbers: the position and the exhaust direction, not all 0");
            }
        }
    }
}
