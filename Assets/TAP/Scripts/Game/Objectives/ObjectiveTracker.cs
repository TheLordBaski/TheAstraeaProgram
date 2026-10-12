using System;
using System.Collections.Generic;
using System.Linq;
using TAP.Core;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>The mission guides the game has (Resources/Data/Objectives), checked against the session system.</summary>
    public static class ObjectiveSets
    {
        private static List<ObjectiveSetDef> _all;
        private static CelestialSystem _system;

        public static IReadOnlyList<ObjectiveSetDef> All
        {
            get
            {
                var sys = CelestialSystem.Default;
                if (_all == null || _system != sys) { _all = Load(sys); _system = sys; }
                return _all;
            }
        }

        public static ObjectiveSetDef Get(string id) => All.FirstOrDefault(s => s.id == id) ?? All.FirstOrDefault(s => s.id == Objectives.DefaultSet) ?? All.FirstOrDefault();

        public static void Reset() => _all = null;

        /// <summary>Reads every file; a set with problems is left out and the problems are logged, as game data with errors is.</summary>
        public static List<ObjectiveSetDef> Load(CelestialSystem sys, Func<IEnumerable<(string name, string json)>> files = null)
        {
            var list = new List<ObjectiveSetDef>();
            var sources = files != null ? files() : Resources.LoadAll<TextAsset>("Data/Objectives").Select(t => (t.name, t.text));
            foreach (var (name, json) in sources)
            {
                try
                {
                    var set = Objectives.Parse(json);
                    var problems = Objectives.Validate(set, sys);
                    if (problems.Count > 0) { Debug.LogError($"Data/Objectives/{name}.json is left out:\n  " + string.Join("\n  ", problems)); continue; }
                    list.Add(set);
                }
                catch (Exception e) { Debug.LogError($"Data/Objectives/{name}.json is left out: {e.Message}"); }
            }
            return list.OrderBy(s => s.order).ThenBy(s => s.id, StringComparer.Ordinal).ToList();
        }
    }

    /// <summary>
    /// The mission guide's engine (FND-14): checks the objectives of the save's objective set against the game events and the active
    /// vessel, marks the ones that are done in the save (as milestones) and tells the player. What the objectives are, and what
    /// finishes them, is data (<see cref="ObjectiveSetDef"/>).
    /// </summary>
    public sealed class ObjectiveTracker : MonoBehaviour
    {
        public FlightSceneController Scene;
        public event Action<ObjectiveDef> ObjectiveReached;
        private float _timer;

        public string SetId => GameSession.Save?.objectiveSet ?? Objectives.DefaultSet;
        public ObjectiveSetDef Set => ObjectiveSets.Get(SetId);

        /// <summary>The ids of what is done: for the first set as they are, for others "set:id".</summary>
        public List<string> Reached => GameSession.Save?.milestones ?? new List<string>();

        public bool Has(string id) => Reached.Contains(Set.Key(id, Objectives.DefaultSet));

        public ObjectiveDef Current => Set?.objectives.FirstOrDefault(o => !Has(o.id));

        /// <summary>A text of the set with this system's names and numbers in it.</summary>
        public string Text(string text) => Objectives.Expand(CelestialSystem.Default, text);

        /// <summary>Switches to the next mission guide (what is done in each is kept).</summary>
        public void NextSet()
        {
            var all = ObjectiveSets.All;
            if (all.Count < 2 || GameSession.Save == null) return;
            int i = all.ToList().FindIndex(s => s.id == SetId);
            GameSession.Save.objectiveSet = all[(i + 1) % all.Count].id;
        }

        private void OnEnable() => GameEvents.Raised += OnEvent;
        private void OnDisable() => GameEvents.Raised -= OnEvent;

        private void OnEvent(GameEvent e) => Evaluate(e);

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < 0.5f || Scene == null || Scene.Sim == null) return;
            _timer = 0;
            Evaluate(null);
        }

        private ObjectiveContext Context(GameEvent e)
        {
            var sim = Scene.Sim;
            var v = sim.ActiveVessel;
            var ctx = new ObjectiveContext
            {
                System = sim.System,
                Event = e,
                IsDone = Has,
                Encounter = () => Scene.Trajectory?.FirstEncounter(true)?.Body.Id,
                FlagOn = bodyId => sim.Handles.Any(h => h.Kind == Persistence.VesselKind.Flag && h.Body != null && h.Body.Id == bodyId),
            };
            if (v != null)
                ctx.Vessel = new VesselState
                {
                    BodyId = v.MainBody.Id,
                    Situation = v.Situation,
                    Altitude = v.Altitude,
                    SurfaceSpeed = v.SurfaceSpeed,
                    Launched = v.Record.launchUT >= 0 && !v.IsFlag,
                    IsEva = v.IsEva,
                    IsFlag = v.IsFlag,
                    GroundContact = v.GroundContact,
                    Crew = v.CrewCount,
                };
            return ctx;
        }

        /// <summary>Marks every objective whose condition holds, in order, until nothing more changes (one landing can finish two).</summary>
        private void Evaluate(GameEvent e)
        {
            if (Scene == null || Scene.Sim == null || GameSession.Save == null) return;
            var set = Set;
            if (set == null) return;
            bool changed = true;
            for (int pass = 0; changed && pass < 20; pass++)
            {
                changed = false;
                var ctx = Context(pass == 0 ? e : null); // an event counts once
                foreach (var o in set.objectives)
                {
                    if (Has(o.id) || !Objectives.Eval(o.when, ctx)) continue;
                    Mark(set, o);
                    changed = true;
                }
            }
        }

        private void Mark(ObjectiveSetDef set, ObjectiveDef o)
        {
            GameSession.Save.milestones.Add(set.Key(o.id, Objectives.DefaultSet));
            string title = Text(o.title);
            GameSession.Log("Milestone: " + title);
            Scene.Notify($"✔ Milestone: {title}", true);
            ObjectiveReached?.Invoke(o);
        }
    }
}
