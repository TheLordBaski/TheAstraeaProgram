using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>
    /// Every star system the game knows (Resources/Data/galaxy.json). Each system is simulated on its own and body ids
    /// are unique within their system; <c>"home/tellus"</c> names a body across systems. 1.0 ships the home system only;
    /// more systems and travel between them come later (product/plan/20-multi-system-and-trade.md).
    /// </summary>
    public sealed class Galaxy
    {
        [Serializable]
        public class SystemEntry
        {
            public string id;
            public string name;
            /// <summary>Resources path of the system file, without extension.</summary>
            public string file;
            /// <summary>Position in the galaxy (light-years), for the galaxy map and interstellar travel.</summary>
            public double[] positionLy = { 0, 0, 0 };
            /// <summary>Test systems are never offered to players.</summary>
            public bool debug;
        }

        [Serializable]
        private class GalaxyDefinition
        {
            public string home = "home";
            public List<SystemEntry> systems = new List<SystemEntry>();
        }

        public const string PrefKey = "TAP.System";

        public readonly string HomeSystemId;
        public readonly IReadOnlyList<SystemEntry> Systems;

        private static Galaxy _instance;

        public static Galaxy Instance => _instance ?? (_instance = LoadFromResources());

        private Galaxy(GalaxyDefinition def)
        {
            HomeSystemId = def.home;
            Systems = def.systems;
        }

        public static Galaxy LoadFromResources(string path = "Data/galaxy")
        {
            var ta = Resources.Load<TextAsset>(path);
            if (ta == null)
            {
                // A project without a galaxy file is a single home system.
                var single = new GalaxyDefinition();
                single.systems.Add(new SystemEntry { id = "home", name = "Home", file = "Data/system" });
                return new Galaxy(single);
            }
            var report = new ContentReport();
            var galaxy = Load(path + ".json", ta.text, report, file => Resources.Load<TextAsset>(file) != null);
            ContentLog.Add(report, "no system can be loaded until they are fixed");
            if (galaxy == null || report.HasErrors) throw new ContentException(path + ".json can't be loaded", report);
            return galaxy;
        }

        /// <summary>
        /// Checks and reads a galaxy file: systems with ids of their own and files that exist, and a home system that is
        /// one of them. Null when it can't be read at all.
        /// </summary>
        /// <param name="systemFileExists">Whether a Resources path (without .json) holds a file; null skips the check.</param>
        public static Galaxy Load(string file, string json, ContentReport report, Func<string, bool> systemFileExists)
        {
            var token = ContentJson.Parse(json, file, report);
            if (token == null) return null;
            var e = new ContentEntry(report, file, null, token);
            if (!(token is JObject))
            {
                e.Error("", "an object with the home system and the list of systems");
                return null;
            }
            ContentJson.CheckShape(token, typeof(GalaxyDefinition), e);
            var def = ContentJson.ToObject<GalaxyDefinition>(token);
            if (def?.systems == null || def.systems.Count == 0)
            {
                e.Error("systems", "a list of at least one system");
                return null;
            }
            var ids = new List<string>();
            for (int i = 0; i < def.systems.Count; i++)
            {
                var s = def.systems[i];
                if (s == null) continue;
                string p = $"systems[{i}]";
                if (e.Text(s.id, p + ".id", "an id"))
                {
                    e.Check(!ids.Contains(s.id), p + ".id", "an id no other system has");
                    ids.Add(s.id);
                }
                if (e.Text(s.file, p + ".file", "the Resources path of a system file, without .json"))
                    e.Check(systemFileExists == null || systemFileExists(s.file), p + ".file",
                        "the Resources path of a system file, without .json", hint: "there is no such file");
                e.Check(!string.IsNullOrWhiteSpace(s.name), p + ".name", "the system's name", error: false);
                e.Vector(s.positionLy, 3, p + ".positionLy");
            }
            e.Check(ids.Contains(def.home), "home", "the id of one of the systems (" + string.Join(", ", ids) + ")");
            return new Galaxy(def);
        }

        public SystemEntry Find(string id)
        {
            foreach (var s in Systems)
                if (s.id == id) return s;
            return null;
        }

        /// <summary>Loads a system by id (the home system when the id is unknown).</summary>
        public CelestialSystem LoadSystem(string id)
        {
            var entry = Find(id) ?? Find(HomeSystemId);
            return CelestialSystem.LoadFromResources(entry.file, entry.id);
        }

        /// <summary>
        /// The system this session plays in: <c>-system &lt;id&gt;</c> on the command line, else the developer setting
        /// <see cref="PrefKey"/> (editor testing, e.g. the debug system), else the home system.
        /// </summary>
        public string SessionSystemId
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == "-system" && Find(args[i + 1]) != null) return args[i + 1];
                string pref = PlayerPrefs.GetString(PrefKey, "");
                return Find(pref) != null ? pref : HomeSystemId;
            }
        }
    }
}
