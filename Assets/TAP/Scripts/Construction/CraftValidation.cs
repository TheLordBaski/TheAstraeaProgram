using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;

namespace TAP.Construction
{
    /// <summary>
    /// The rules of a craft file (the starter craft in Resources/Craft): parts the catalog has, a tree of parents, attach
    /// nodes the parts have, and poses the assembly building can place.
    /// </summary>
    public static class CraftValidation
    {
        /// <summary>Checks a craft file. Returns what could be read, or null when it can't be read at all.</summary>
        public static CraftDesign Check(string file, string json, PartDatabase db, ContentReport report)
        {
            var token = ContentJson.Parse(json, file, report);
            if (token == null) return null;
            string name = token is JObject o && o["name"] is JValue v && v.Type == JTokenType.String ? (string)v : null;
            var e = new ContentEntry(report, file, name != null ? "craft " + name : "craft", token);
            if (!(token is JObject))
            {
                e.Error("", "an object describing a craft");
                return null;
            }
            ContentJson.CheckShape(token, typeof(CraftDesign), e);
            var d = ContentJson.ToObject<CraftDesign>(token, SaveStorage.Settings);
            if (d == null) return null;
            e.Text(d.name, "name", "the craft's name");
            if (e.Check(d.parts != null && d.parts.Count > 0, "parts", "a list of parts, the root first"))
                CheckParts(e, d.parts, "parts", db);
            if (d.detached != null)
                for (int i = 0; i < d.detached.Count; i++)
                {
                    var g = d.detached[i];
                    if (g == null) continue;
                    string p = $"detached[{i}]";
                    e.Vector(g.pos, 3, p + ".pos");
                    e.Vector(g.rot, 4, p + ".rot");
                    if (e.Check(g.parts != null && g.parts.Count > 0, p + ".parts", "a list of parts, the group's root first"))
                        CheckParts(e, g.parts, p + ".parts", db);
                }
            return d;
        }

        private static void CheckParts(ContentEntry e, List<PartNodeRecord> parts, string path, PartDatabase db)
        {
            var uids = new HashSet<int>();
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                string f = $"{path}[{i}]";
                if (p == null)
                {
                    e.Error(f, "a part");
                    continue;
                }
                var def = db.Get(p.partId);
                if (def == null)
                    e.Error(f + ".partId", "the id of a part in " + PartDatabase.ResourcePath + ".json",
                        hint: db.IsBroken(p.partId) ? db.WhyMissing(p.partId) : null);
                e.Check(uids.Add(p.uid), f + ".uid", "a number no other part of the craft has", error: false);
                if (i == 0) e.Check(p.parent == -1, f + ".parent", "-1 (the first part is the root)");
                else if (e.Check(p.parent >= 0 && p.parent < parts.Count && p.parent != i, f + ".parent",
                             $"the index of another part of the list (0 to {parts.Count - 1})")
                         && ReachesRoot(parts, i, e, f))
                {
                    var parentDef = db.Get(parts[p.parent]?.partId);
                    if (def != null && parentDef != null) CheckAttachment(e, f, p, def, parentDef);
                }
                e.Vector(p.pos, 3, f + ".pos");
                if (e.Vector(p.rot, 4, f + ".rot"))
                {
                    double len = Math.Sqrt(p.rot.Sum(x => (double)x * x));
                    e.Check(Math.Abs(len - 1) < 1e-3, f + ".rot", "a rotation (a quaternion of length 1)", error: false,
                        found: "length " + ContentEntry.Number(len));
                }
                e.Check(p.stage >= -1, f + ".stage", "a stage of -1 (not staged) or more");
            }
        }

        /// <summary>Following the parents up from a part reaches the root without coming back.</summary>
        private static bool ReachesRoot(List<PartNodeRecord> parts, int i, ContentEntry e, string f)
        {
            var seen = new HashSet<int> { i };
            for (int k = parts[i].parent; k > 0; k = parts[k]?.parent ?? -1)
            {
                if (k >= parts.Count) return true; // reported at that part
                if (!seen.Add(k)) return e.Check(false, f + ".parent", "a chain of parents that ends at the root (part 0)", found: "a loop");
            }
            return true;
        }

        private static void CheckAttachment(ContentEntry e, string f, PartNodeRecord p, PartDefinition def, PartDefinition parent)
        {
            if (p.attachNode == "srf")
            {
                e.Check(def.surfaceAttach != null && def.surfaceAttach.allowed, f + ".attachNode",
                    "a node of " + def.id + " (" + Nodes(def) + "): it can't be attached to a surface");
                e.Check(parent.surfaceAttach == null || parent.surfaceAttach.onto, f + ".parentNode",
                    "a node of " + parent.id + " (" + Nodes(parent) + "): parts can't be attached to its surface", error: false);
                return;
            }
            e.Check(def.FindNode(p.attachNode) != null, f + ".attachNode", "a node of " + def.id + " (" + Nodes(def) + ")");
            e.Check(parent.FindNode(p.parentNode) != null, f + ".parentNode", "a node of " + parent.id + " (" + Nodes(parent) + ")");
        }

        private static string Nodes(PartDefinition d)
        {
            var ids = d.nodes?.Where(n => n != null).Select(n => n.id).ToList() ?? new List<string>();
            if (d.surfaceAttach != null && d.surfaceAttach.allowed) ids.Add("srf");
            return ids.Count == 0 ? "it has none" : string.Join(", ", ids);
        }
    }
}
