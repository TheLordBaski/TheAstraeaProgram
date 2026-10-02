using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TAP.Core;
using TAP.Parts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAP.Game
{
    /// <summary>
    /// Hot reload of the game data while playing in the editor (F8), so balancing is edit, F8, fly:
    /// <list type="bullet">
    /// <item>part stats change in place, also on parts already flying (an engine's thrust from its next step);</item>
    /// <item>a body's atmosphere, names, map colour, warp limits and a star's light change at once;</item>
    /// <item>models, attach nodes, what a part carries and its modules reach vessels built afterwards; a body's size,
    /// mass, orbit, spin and terrain, and the launch site, need a restart.</item>
    /// </list>
    /// New data with errors is reported and its entries keep their previous values.
    /// </summary>
    public static class ContentReload
    {
        /// <summary>Makes edited data files readable through Resources again (the editor sets it: a reimport).</summary>
        public static Action RefreshFiles;

        /// <summary>Hot reload works where the data files can change: playing in the editor.</summary>
        public static bool Available => RefreshFiles != null;

        /// <summary>Raised after a reload with a summary for the screen; the console has every detail.</summary>
        public static event Action<string> Reloaded;

        public sealed class Result
        {
            /// <summary>Changes in effect now.</summary>
            public readonly List<string> Now = new List<string>();
            /// <summary>Changes for vessels built from now on, or after a restart.</summary>
            public readonly List<string> Later = new List<string>();
            /// <summary>Entries that keep their previous values.</summary>
            public readonly List<string> Kept = new List<string>();
            public readonly ContentReport Problems = new ContentReport();
            public string Summary;
            public string Details;
        }

        public static Result Run()
        {
            RefreshFiles?.Invoke();
            var r = new Result();
            ReloadParts(r);
            ReloadSystem(r);
            r.Summary = Summarize(r);
            r.Details = Describe(r);
            if (r.Problems.HasErrors) Debug.LogError(r.Details);
            else if (r.Problems.WarningCount > 0) Debug.LogWarning(r.Details);
            else Debug.Log(r.Details);
            Reloaded?.Invoke(r.Summary);
            return r;
        }

        private static void ReloadParts(Result r)
        {
            string file = PartDatabase.ResourcePath + ".json";
            var ta = Resources.Load<TextAsset>(PartDatabase.ResourcePath);
            if (ta == null)
            {
                r.Kept.Add(file + " is missing: the parts keep their previous values");
                return;
            }
            var p = PartDatabase.Instance.Reload(file, ta.text);
            r.Problems.Add(p.Problems);
            if (p.Unreadable) r.Kept.Add(file + " can't be read: the parts keep their previous values");
            r.Now.AddRange(p.Changes);
            r.Later.AddRange(p.ForNewVessels.Select(c => c + " (vessels built from now on)"));
            r.Later.AddRange(p.Added.Select(a => "new part " + a + " (in the parts list when the assembly building opens)"));
            r.Kept.AddRange(p.Kept);
        }

        private static void ReloadSystem(Result r)
        {
            var sys = CelestialSystem.Loaded;
            if (sys == null) return; // loads fresh when it is needed
            var entry = Galaxy.Instance.Find(sys.Id);
            if (entry == null) return;
            string file = entry.file + ".json";
            var ta = Resources.Load<TextAsset>(entry.file);
            if (ta == null)
            {
                r.Kept.Add(file + " is missing: the bodies keep their previous values");
                return;
            }
            var report = new ContentReport();
            var def = SystemValidation.Check(file, ta.text, CelestialSystem.PresetLoader, report);
            r.Problems.Add(report);
            if (def == null || report.HasErrors)
            {
                r.Kept.Add(file + " has errors: the bodies keep their previous values");
                return;
            }
            // Built the way the game builds it, so values worked out at load (the launch site on its flat area) compare alike.
            CelestialSystem fresh;
            try { fresh = new CelestialSystem(def, sys.Id); }
            catch (FormatException e)
            {
                r.Kept.Add(file + " can't be built (" + e.Message + "): the bodies keep their previous values");
                return;
            }
            foreach (var nb in fresh.Def.bodies)
            {
                var body = sys.Get(nb.id);
                if (body == null)
                {
                    r.Later.Add($"new body {nb.id} (after a restart)");
                    continue;
                }
                bool live = false;
                foreach (var c in ContentUpdate.Differences(body.Def, nb))
                {
                    string top = c.Path.Split('.', '[')[0];
                    if (CelestialBody.LiveFields.Contains(top))
                    {
                        r.Now.Add($"{body.Name}: {c}");
                        live = true;
                    }
                    else r.Later.Add($"{body.Name}: {c} (after a restart)");
                }
                if (live) body.ApplyLive(nb);
            }
            foreach (var b in sys.Bodies)
                if (!fresh.Def.bodies.Exists(x => x.id == b.Id)) r.Later.Add($"{b.Name} is no longer in {file} (after a restart)");
            foreach (var c in ContentUpdate.Differences(new { sys.Def.name, sys.Def.startUT, sys.Def.sun, sys.Def.launchSite },
                         new { fresh.Def.name, fresh.Def.startUT, fresh.Def.sun, fresh.Def.launchSite }))
                r.Later.Add($"{c} (after a restart)");
        }

        private static string Summarize(Result r)
        {
            var parts = new List<string>();
            if (r.Now.Count == 1) parts.Add(r.Now[0]);
            else if (r.Now.Count > 1) parts.Add(r.Now[0] + $", and {r.Now.Count - 1} more");
            if (r.Later.Count > 0) parts.Add(ContentReport.Plural(r.Later.Count, "change") + " later (vessels built afterwards or a restart)");
            if (r.Problems.HasErrors)
            {
                var first = r.Problems.Errors.First();
                parts.Add(ContentReport.Plural(r.Problems.ErrorCount, "error") + ", previous values kept: " +
                          (string.IsNullOrEmpty(first.Entry) ? "" : first.Entry + ", ") + first.What);
            }
            else if (r.Kept.Count > 0) parts.Add(r.Kept[0]);
            return "Data reloaded (F8): " + (parts.Count == 0 ? "nothing changed." : string.Join(". ", parts.Select(s => s.TrimEnd('.'))) + ".");
        }

        private static string Describe(Result r)
        {
            var sb = new StringBuilder(r.Summary);
            void Section(string title, IEnumerable<string> lines)
            {
                var list = lines.ToList();
                if (list.Count == 0) return;
                sb.Append('\n').Append(title);
                foreach (var l in list) sb.Append("\n  ").Append(l);
            }
            Section("In effect now:", r.Now);
            Section("Later:", r.Later);
            Section("Kept:", r.Kept);
            Section("Problems:", r.Problems.Problems.Select(p => (p.IsError ? "Error: " : "Warning: ") + p));
            return sb.ToString();
        }
    }

    /// <summary>Listens for F8 in every scene while playing in the editor.</summary>
    public sealed class ContentReloadKey : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!ContentReload.Available) return;
            var go = new GameObject("ContentReload (F8)");
            DontDestroyOnLoad(go);
            go.AddComponent<ContentReloadKey>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame) ContentReload.Run();
        }
    }
}
