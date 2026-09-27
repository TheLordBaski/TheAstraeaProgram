using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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
            return new Galaxy(JsonConvert.DeserializeObject<GalaxyDefinition>(ta.text));
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
