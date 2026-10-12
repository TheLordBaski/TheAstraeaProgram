using System.Collections.Generic;
using TAP.Core;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Raises the game events that are changes of a vessel's state rather than something the simulation does at one moment (FND-14):
    /// reaching space, going into orbit, changing sphere of influence, landing and splashing down, surviving a reentry. It looks at every vessel
    /// twice a second and says nothing the first time it sees one, so loading a save in orbit is not "reaching orbit".
    /// </summary>
    public sealed class EventWatcher : MonoBehaviour
    {
        public FlightSim Sim;

        private sealed class Seen
        {
            public string Body;
            public Situation Situation;
            public bool InSpace, Heating;
        }

        private readonly Dictionary<string, Seen> _seen = new Dictionary<string, Seen>();
        private float _timer;

        /// <summary>Where space begins above a body: the top of its atmosphere, 70 km where there is none.</summary>
        public static double SpaceAltitude(CelestialBody body) => body?.Atmosphere?.Height ?? 70000;

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < 0.5f || Sim == null) return;
            _timer = 0;
            foreach (var h in Sim.Handles)
            {
                if (h.Kind == VesselKind.Flag || h.Body == null) continue;
                bool first = !_seen.TryGetValue(h.Id, out var s);
                if (first) _seen[h.Id] = s = new Seen { Body = h.Body.Id, Situation = h.Record.situation };
                var v = h.Loaded;
                bool inSpace = v != null && v.Altitude > SpaceAltitude(h.Body);
                bool heating = v != null && v.InAtmosphere && v.Mach > 3;
                if (!first)
                {
                    if (s.Body != h.Body.Id) Raise(GameEventKind.SoiChanged, h, s.Body);
                    if (s.Situation != h.Record.situation)
                    {
                        if (h.Record.situation == Situation.Orbiting) Raise(GameEventKind.Orbit, h);
                        else if (h.Record.situation == Situation.Landed) Raise(GameEventKind.Landed, h);
                        else if (h.Record.situation == Situation.Splashed) Raise(GameEventKind.Splashed, h);
                    }
                    if (inSpace && !s.InSpace) Raise(GameEventKind.ReachedSpace, h);
                    if (s.Heating && !heating && v != null && v.CrewCount > 0) Raise(GameEventKind.ReentrySurvived, h);
                }
                s.Body = h.Body.Id;
                s.Situation = h.Record.situation;
                s.InSpace = inSpace;
                s.Heating = heating;
            }
        }

        private void Raise(GameEventKind kind, VesselHandle h, string detail = null)
            => GameEvents.Raise(kind, Sim.UT, h.Id, h.Name, h.Body.Id, null, detail);
    }
}
