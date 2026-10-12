#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// The English table (FND-10): <c>Resources/Strings/en.json</c> lists every text the game shows, so a translator has the list and
    /// a test can tell a text that never reached it. This builds the list from the screens' files and the game data, and from
    /// the texts seen on screen during the <c>locale</c> autotest. Editor only.
    /// </summary>
    public static class LocalizationCatalog
    {
        public const string EnglishPath = "Assets/TAP/Resources/Strings/en.json";

        /// <summary>The texts that are in files rather than in code: screen layouts (UXML) and the game's data (parts, resources, bodies, biomes).</summary>
        public static SortedSet<string> FromFilesAndData()
        {
            var set = new SortedSet<string>(System.StringComparer.Ordinal);
            void Add(string text) { if (!string.IsNullOrWhiteSpace(text)) set.Add(Loc.PatternOf(text)); }

            foreach (var file in Directory.GetFiles("Assets/TAP/Resources/UI", "*.uxml"))
            {
                var doc = new XmlDocument();
                doc.Load(file);
                foreach (XmlNode node in doc.SelectNodes("//*"))
                    foreach (string attr in new[] { "text", "label", "tooltip" })
                        if (node.Attributes?[attr] != null) Add(node.Attributes[attr].Value);
            }
            foreach (var tip in LoadingTips.All) Add(tip);
            foreach (var c in ControlTable.All) Add(c.Label);
            foreach (var c in ControlTable.All.Select(c => c.Section).Distinct()) Add(c);
            foreach (var m in ControlTable.MouseRows) { Add(m.keys); Add(m.action); }
            var db = PartDatabase.Instance;
            foreach (var p in db.Parts) { Add(p.title); Add(p.description); Add(p.category); }
            foreach (var r in db.ResourceDefinitions) Add(r.name);
            var system = CelestialSystem.Default;
            foreach (var b in system.Bodies)
            {
                Add(b.Name);
                if (b.Terrain != null) foreach (var biome in b.Terrain.Biomes) Add(biome.Name);
            }
            foreach (var o in ObjectiveSets.All)
            {
                Add(Objectives.Expand(system, o.title));
                foreach (var obj in o.objectives) { Add(Objectives.Expand(system, obj.title)); Add(Objectives.Expand(system, obj.hint)); }
            }
            return set;
        }

        /// <summary>Writes the table: what it had, the files and data, and the texts in <paramref name="seen"/> (a harvest from a run).</summary>
        public static int Write(IEnumerable<string> seen)
        {
            var set = FromFilesAndData();
            foreach (var key in Loc.EnglishStrings.Keys) set.Add(key);
            if (seen != null) foreach (var s in seen) if (!string.IsNullOrWhiteSpace(s)) set.Add(s);
            var strings = new SortedDictionary<string, string>(System.StringComparer.Ordinal);
            foreach (var s in set) strings[s] = s;
            var table = new Dictionary<string, object> { ["name"] = "English", ["culture"] = "en-US", ["strings"] = strings };
            Directory.CreateDirectory(Path.GetDirectoryName(EnglishPath));
            File.WriteAllText(EnglishPath, SaveStorage.ToJson(table));
            return strings.Count;
        }
    }
}
#endif
