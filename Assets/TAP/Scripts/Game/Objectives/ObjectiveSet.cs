using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TAP.Core;
using TAP.Persistence;

namespace TAP.Game
{
    /// <summary>
    /// A condition of an objective (FND-14), written as data. Every field that is set must hold; <c>any</c> and <c>not</c> combine conditions.
    /// A body is written as an id ("home/tellus"), or <c>$home</c> (the home planet), <c>$moon</c> (its first moon) or <c>$star</c>.
    /// </summary>
    public sealed class Condition
    {
        /// <summary>A game event (see <see cref="GameEventKind"/>) that is being raised now. With it, <c>body</c> is the body of the event.</summary>
        public string kind;
        /// <summary>The body the vessel is at (or the event happened at).</summary>
        public string body;
        /// <summary>The vessel's situation, one or several joined by "|": "Orbiting", "Landed|Splashed".</summary>
        public string situation;
        public bool? launched, crewed, eva, flag, groundContact;
        /// <summary>"space" (above the body's atmosphere), or a height in metres.</summary>
        public string altitudeAbove;
        public double? surfaceSpeedBelow;
        /// <summary>The body the vessel's path first meets next.</summary>
        public string encounter;
        /// <summary>A flag stands on this body.</summary>
        public string flagOn;
        /// <summary>Objectives of the set that are done / not done.</summary>
        public string[] done, notDone;
        public Condition[] any;
        public Condition not;
    }

    public sealed class ObjectiveDef
    {
        /// <summary>Kept in saves; do not change it.</summary>
        public string id, title, hint;
        public Condition when;
    }

    /// <summary>A mission guide: objectives in order, each done when its condition holds. Text may use {body:REF}, {home}, {moon}, {space} and {space+N}.</summary>
    public sealed class ObjectiveSetDef
    {
        public string id, title;
        /// <summary>Where it comes in the list of sets.</summary>
        public int order;
        public ObjectiveDef[] objectives;
        /// <summary>The first set's ids are stored in saves as they are; other sets' as "set:id".</summary>
        public string Key(string objectiveId, string defaultSet) => id == defaultSet ? objectiveId : id + ":" + objectiveId;
    }

    /// <summary>What a condition looks at: the vessel, the event being raised, what is done.</summary>
    public sealed class VesselState
    {
        public string BodyId;
        public Situation Situation;
        public double Altitude, SurfaceSpeed;
        public bool Launched, IsEva, IsFlag, GroundContact;
        public int Crew;
    }

    public sealed class ObjectiveContext
    {
        public CelestialSystem System;
        public VesselState Vessel;
        public GameEvent Event;
        public Func<string, bool> IsDone = _ => false;
        /// <summary>The id of the body the vessel's path meets next (looked up only when a condition asks).</summary>
        public Func<string> Encounter = () => null;
        public Func<string, bool> FlagOn = _ => false;
    }

    public static class Objectives
    {
        public const string DefaultSet = "lunar";

        private static readonly JsonSerializerSettings Strict = new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error };

        public static ObjectiveSetDef Parse(string json) => JsonConvert.DeserializeObject<ObjectiveSetDef>(json, Strict);

        // ------------------------------------------------------------------ bodies and text

        public static CelestialBody Resolve(CelestialSystem sys, string reference)
        {
            if (string.IsNullOrEmpty(reference)) return null;
            switch (reference)
            {
                case "$home": return sys.HomeBody;
                case "$moon": return sys.HomeMoon;
                case "$star": return sys.Star;
                default: return sys.Get(reference);
            }
        }

        private static readonly Regex Token = new Regex(@"\{(body:[^{}]+|home|moon|space(?:\+\d+)?)\}", RegexOptions.Compiled);

        /// <summary>Puts the names and numbers of this system into a text: {body:$moon}, {home}, {moon}, {space}, {space+10}.</summary>
        public static string Expand(CelestialSystem sys, string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            return Token.Replace(text, m =>
            {
                string t = m.Groups[1].Value;
                if (t == "home") return sys.HomeBody.Name;
                if (t == "moon") return sys.HomeMoon?.Name ?? "the moon";
                if (t.StartsWith("body:")) return Resolve(sys, t.Substring(5))?.Name ?? t.Substring(5);
                double km = EventWatcher.SpaceAltitude(sys.HomeBody) / 1000.0;
                return (t == "space" ? km : km + int.Parse(t.Substring(6))).ToString("0");
            });
        }

        // ------------------------------------------------------------------ evaluating

        public static bool Eval(Condition c, ObjectiveContext ctx)
        {
            if (c == null) return true;
            var sys = ctx.System;
            CelestialBody wanted = c.body != null ? Resolve(sys, c.body) : null;
            if (c.body != null && wanted == null) return false;

            if (c.kind != null)
            {
                if (ctx.Event == null || !string.Equals(ctx.Event.Kind.ToString(), c.kind, StringComparison.OrdinalIgnoreCase)) return false;
                if (wanted != null && ctx.Event.BodyId != wanted.Id) return false;
            }

            bool needsVessel = c.situation != null || c.launched != null || c.crewed != null || c.eva != null || c.flag != null || c.groundContact != null
                               || c.altitudeAbove != null || c.surfaceSpeedBelow != null || (c.kind == null && c.body != null);
            var v = ctx.Vessel;
            if (needsVessel)
            {
                if (v == null) return false;
                if (c.kind == null && wanted != null && v.BodyId != wanted.Id) return false;
                if (c.situation != null && !c.situation.Split('|').Any(s => string.Equals(s.Trim(), v.Situation.ToString(), StringComparison.OrdinalIgnoreCase))) return false;
                if (c.launched != null && c.launched != v.Launched) return false;
                if (c.crewed != null && c.crewed != v.Crew > 0) return false;
                if (c.eva != null && c.eva != v.IsEva) return false;
                if (c.flag != null && c.flag != v.IsFlag) return false;
                if (c.groundContact != null && c.groundContact != v.GroundContact) return false;
                if (c.altitudeAbove != null)
                {
                    var at = sys.Get(v.BodyId);
                    double limit = c.altitudeAbove == "space" ? EventWatcher.SpaceAltitude(at) : double.Parse(c.altitudeAbove, System.Globalization.CultureInfo.InvariantCulture);
                    if (!(v.Altitude > limit)) return false;
                }
                if (c.surfaceSpeedBelow != null && !(v.SurfaceSpeed < c.surfaceSpeedBelow)) return false;
            }
            if (c.encounter != null)
            {
                var e = Resolve(sys, c.encounter);
                if (e == null || ctx.Encounter() != e.Id) return false;
            }
            if (c.flagOn != null)
            {
                var f = Resolve(sys, c.flagOn);
                if (f == null || !ctx.FlagOn(f.Id)) return false;
            }
            if (c.done != null && !c.done.All(ctx.IsDone)) return false;
            if (c.notDone != null && c.notDone.Any(ctx.IsDone)) return false;
            if (c.any != null && !c.any.Any(a => Eval(a, ctx))) return false;
            if (c.not != null && Eval(c.not, ctx)) return false;
            return true;
        }

        // ------------------------------------------------------------------ checking

        /// <summary>What is wrong with a set in this system: unknown events, situations, bodies and objectives. Empty when it is sound.</summary>
        public static List<string> Validate(ObjectiveSetDef set, CelestialSystem sys)
        {
            var problems = new List<string>();
            if (string.IsNullOrEmpty(set.id)) problems.Add("the set has no id");
            if (string.IsNullOrEmpty(set.title)) problems.Add($"{set.id}: no title");
            if (set.objectives == null || set.objectives.Length == 0) { problems.Add($"{set.id}: no objectives"); return problems; }
            var ids = new HashSet<string>();
            foreach (var o in set.objectives)
                if (string.IsNullOrEmpty(o.id) || !ids.Add(o.id)) problems.Add($"{set.id}: objective id '{o.id}' is empty or used twice");
            foreach (var o in set.objectives)
            {
                if (string.IsNullOrEmpty(o.title)) problems.Add($"{set.id}/{o.id}: no title");
                if (o.when == null) problems.Add($"{set.id}/{o.id}: no condition");
                else Check(o.when, $"{set.id}/{o.id}", ids, sys, problems);
            }
            return problems;
        }

        private static void Check(Condition c, string where, HashSet<string> ids, CelestialSystem sys, List<string> problems)
        {
            if (c.kind != null && !Enum.TryParse(c.kind, true, out GameEventKind _)) problems.Add($"{where}: unknown event '{c.kind}'");
            foreach (var (field, reference) in new[] { ("body", c.body), ("encounter", c.encounter), ("flagOn", c.flagOn) })
                if (reference != null && Resolve(sys, reference) == null) problems.Add($"{where}: {field} '{reference}' is not a body of this system");
            if (c.situation != null)
                foreach (var s in c.situation.Split('|'))
                    if (!Enum.TryParse(s.Trim(), true, out Situation _)) problems.Add($"{where}: unknown situation '{s}'");
            if (c.altitudeAbove != null && c.altitudeAbove != "space" && !double.TryParse(c.altitudeAbove, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                problems.Add($"{where}: altitudeAbove '{c.altitudeAbove}' is not 'space' or a number");
            foreach (var d in (c.done ?? new string[0]).Concat(c.notDone ?? new string[0]))
                if (!ids.Contains(d)) problems.Add($"{where}: there is no objective '{d}'");
            if (c.any != null) foreach (var a in c.any) Check(a, where, ids, sys, problems);
            if (c.not != null) Check(c.not, where, ids, sys, problems);
        }
    }
}
