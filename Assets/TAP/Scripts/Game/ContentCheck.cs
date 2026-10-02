using System;
using Newtonsoft.Json.Linq;
using TAP.Construction;
using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Checks every content file the game ships, the way the game reads it: the galaxy, each star system with its terrain
    /// presets, the parts and the starter craft. Used by TAP → Validate Content and by a unit test.
    /// </summary>
    public static class ContentCheck
    {
        public sealed class Result
        {
            public readonly ContentReport Report = new ContentReport();
            public int Systems, Bodies, Presets, Resources, Parts, Craft;

            /// <summary>"2 systems with 9 bodies, 2 terrain presets, 5 resources, 38 parts and 3 starter craft".</summary>
            public string Counts =>
                $"{ContentReport.Plural(Systems, "system")} with {(Bodies == 1 ? "1 body" : Bodies + " bodies")}, " +
                $"{ContentReport.Plural(Presets, "terrain preset")}, {ContentReport.Plural(Resources, "resource")}, " +
                $"{ContentReport.Plural(Parts, "part")} and {Craft} starter craft";
        }

        public static Result All()
        {
            var r = new Result();
            var galaxyText = UnityEngine.Resources.Load<TextAsset>("Data/galaxy")?.text;
            var galaxy = galaxyText == null ? null : Galaxy.Load("Data/galaxy.json", galaxyText, r.Report, f => UnityEngine.Resources.Load<TextAsset>(f) != null);
            var systems = galaxy?.Systems ?? new[] { new Galaxy.SystemEntry { id = "home", file = "Data/system" } };
            foreach (var s in systems)
            {
                string text = string.IsNullOrEmpty(s.file) ? null : UnityEngine.Resources.Load<TextAsset>(s.file)?.text;
                if (text == null) continue; // the galaxy check reports it
                var def = SystemValidation.Check(s.file + ".json", text, CelestialSystem.PresetLoader, r.Report);
                if (def == null) continue;
                r.Systems++;
                r.Bodies += def.bodies.Count;
            }
            // Every terrain preset, also one no system uses yet.
            foreach (var ta in UnityEngine.Resources.LoadAll<TextAsset>("Data/Terrain"))
            {
                string file = SystemValidation.PresetFile(ta.name);
                var token = ContentJson.Parse(ta.text, file, r.Report);
                if (!(token is JObject preset)) continue;
                ContentJson.CheckShape(preset, typeof(TerrainDefinition), new ContentEntry(r.Report, file, "terrain preset " + ta.name, preset));
                r.Presets++;
            }
            var partsText = UnityEngine.Resources.Load<TextAsset>(PartDatabase.ResourcePath)?.text;
            var parts = PartValidation.Check(PartDatabase.ResourcePath + ".json", partsText, r.Report);
            if (parts == null) return r;
            r.Resources = parts.Catalog.resources.Count;
            r.Parts = parts.Catalog.parts.Count;
            var db = PartDatabase.FromCheck(parts, r.Report);
            foreach (var ta in UnityEngine.Resources.LoadAll<TextAsset>("Craft"))
                if (CraftValidation.Check("Craft/" + ta.name + ".json", ta.text, db, r.Report) != null) r.Craft++;
            return r;
        }

        /// <summary>
        /// Loads what a game needs (the parts, the session's star system) so their problems are known before play.
        /// Returns the error that keeps a game from starting, or null.
        /// </summary>
        public static string LoadGameData()
        {
            try
            {
                _ = PartDatabase.Instance;
                _ = CelestialSystem.Default;
                return null;
            }
            catch (ContentException e)
            {
                return e.Message;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return e.Message;
            }
        }
    }
}
