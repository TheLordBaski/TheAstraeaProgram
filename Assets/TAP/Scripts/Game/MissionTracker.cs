using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Watches the simulation for the milestones of the complete lunar mission and records them in the
    /// save. Drives the in-game mission guide (tutorial hints).
    /// </summary>
    public sealed class MissionTracker : MonoBehaviour
    {
        public sealed class Milestone
        {
            public string Id, Title, Hint;
            public Milestone(string id, string title, string hint) { Id = id; Title = title; Hint = hint; }
        }

        public static readonly Milestone[] Lunar =
        {
            new Milestone("launch", "Launch", "Pick a rocket in the assembly building (Luma Pathfinder is ready to fly), press Launch, then Space to ignite. Z = full throttle."),
            new Milestone("space", "Reach space (70 km)", "At ~60 m/s tap D to tip ~10° east, then select SAS Prograde. Stage (Space) when engines burn out."),
            new Milestone("orbit", "Achieve a stable orbit", "Cut throttle when apoapsis is ~80 km. In the map (M) click your orbit at Ap, drag the prograde handle until Pe > 70 km, then SAS → Maneuver and burn when the timer reaches 0."),
            new Milestone("transfer", "Plan a transfer to Luma", "In the map click Luma → Set as target. Add a node on your orbit and drag prograde (~860 m/s) and move it along the orbit until a Luma encounter appears."),
            new Milestone("luma_soi", "Enter Luma's sphere of influence", "Execute the transfer burn, then time warp (.) — warp stops automatically near dangerous events."),
            new Milestone("luma_orbit", "Orbit Luma", "Add a node at the Luma periapsis and drag retrograde until the orbit closes; burn."),
            new Milestone("luma_land", "Land on Luma", "Deploy legs (G). Burn retrograde to drop periapsis below the surface, then use SAS Retrograde (surface speed) and throttle to touch down below 3 m/s."),
            new Milestone("eva", "Walk on Luma (EVA)", "Right-click the capsule → EVA. WASD to walk, Space to jump, R for the jetpack."),
            new Milestone("flag", "Plant a flag", "Stand on the ground and press G."),
            new Milestone("board", "Board the lander", "Fly or walk to within 3 m of the hatch (jetpack R, Shift up) and press B."),
            new Milestone("luma_ascent", "Launch from Luma back to orbit", "Retract legs (G), full throttle, tip east and follow prograde until Ap ~20 km; circularise with a node."),
            new Milestone("return", "Return to Tellus", "Plan a node on the far side of Luma and burn prograde until the trajectory returns to Tellus with periapsis ~30 km."),
            new Milestone("reentry", "Survive reentry", "Decouple the lander (Space), hold SAS Retrograde so the heat shield faces the airflow."),
            new Milestone("landed_home", "Land safely", "Stage the parachute; it waits for safe pressure and opens fully at 1 km. Touch down under 6 m/s."),
            new Milestone("recovered", "Recover the capsule", "Press Esc → Recover vessel once landed or splashed down."),
        };

        public FlightSceneController Scene;
        public event Action<Milestone> MilestoneReached;
        private float _timer;
        private bool _wasHeating;
        private double _maxHeat;

        public List<string> Reached => GameSession.Save?.milestones ?? new List<string>();

        public bool Has(string id) => Reached.Contains(id);

        public Milestone Current
        {
            get
            {
                foreach (var m in Lunar) if (!Has(m.Id)) return m;
                return null;
            }
        }

        private void Mark(string id)
        {
            if (GameSession.Save == null || Has(id)) return;
            GameSession.Save.milestones.Add(id);
            var m = Array.Find(Lunar, x => x.Id == id);
            GameSession.Log("Milestone: " + (m?.Title ?? id));
            if (m != null) { Scene.Notify($"✔ Milestone: {m.Title}", true); MilestoneReached?.Invoke(m); }
        }

        private void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < 0.5f || Scene == null || Scene.Sim == null) return;
            _timer = 0;
            var sim = Scene.Sim;
            var v = sim.ActiveVessel;
            if (v == null) return;
            var body = v.MainBody;
            bool home = body == sim.System.Root;
            if (v.Record.launchUT >= 0 && !v.IsFlag) Mark("launch");
            if (home && v.Altitude > 70000) Mark("space");
            if (v.Orbit != null && v.Situation == Situation.Orbiting && home) Mark("orbit");
            if (home && Scene.Trajectory != null)
            {
                var enc = Scene.Trajectory.FirstEncounter(true);
                if (enc != null && enc.Body.Id == "luma" && Has("orbit")) Mark("transfer");
            }
            if (body.Id == "luma")
            {
                Mark("luma_soi");
                if (v.Situation == Situation.Orbiting) { if (Has("board")) Mark("luma_ascent"); else Mark("luma_orbit"); }
                if (v.Situation == Situation.Landed && !v.IsEva && !v.IsFlag) Mark("luma_land");
                if (v.IsEva && v.Situation == Situation.Landed || (v.IsEva && v.GroundContact)) Mark("eva");
                if (Has("eva") && !v.IsEva && v.CrewCount > 0) Mark("board");
            }
            foreach (var h in sim.Handles)
                if (h.Kind == VesselKind.Flag && h.Body.Id == "luma") { Mark("flag"); break; }
            if (home && Has("luma_soi")) Mark("return");
            if (home && Has("return"))
            {
                double maxT = 0;
                foreach (var p in v.Parts) maxT = Math.Max(maxT, p.SkinTemp);
                _maxHeat = Math.Max(_maxHeat, maxT);
                bool heating = v.InAtmosphere && v.Mach > 3;
                if (_wasHeating && !heating && v.CrewCount > 0) Mark("reentry");
                _wasHeating = heating;
                if ((v.Situation == Situation.Landed || v.Situation == Situation.Splashed) && v.CrewCount > 0 && v.SurfaceSpeed < 1)
                {
                    Mark("reentry");
                    Mark("landed_home");
                }
            }
        }

        /// <summary>Called by the scene when a vessel is recovered.</summary>
        public void OnRecovered(bool crewed)
        {
            if (crewed && Has("landed_home")) Mark("recovered");
        }
    }
}
