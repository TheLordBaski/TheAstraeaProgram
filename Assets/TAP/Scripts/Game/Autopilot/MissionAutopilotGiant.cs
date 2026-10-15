using System;
using System.Collections;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>A gas giant has no ground: a vessel that dives into it is crushed by the air, at the crush pressure above the datum.</summary>
    public sealed partial class MissionAutopilot
    {
        private IEnumerator GiantMission()
        {
            ExpectLosses = true;
            CaptureMessages();
            CelestialBody giant = Sim.System.Bodies.Find(b => b.IsGasGiant);
            if (giant == null) { Check("the system has a gas giant", false, "none"); yield break; }
            var db = PartDatabase.Instance;
            DevCheats.IgnoreHeat = true; // the dive tests the crush depth, not the heat of a fast entry
            SetPhase($"dive into {giant.Name}");
            double r = giant.Radius + giant.Atmosphere.Height * 0.6;
            var h = CreateOrbitalHandle(TestCraft.BareCapsule(db), "Plunger", new Vector3d(r, 0, 0), new Vector3d(-700, 0, 0),
                Quaternion.LookRotation(Vector3.right * -1, Vector3.up), giant);
            if (!Sim.SwitchTo(h)) { Check("probe above the giant", false, "could not switch to the probe"); yield break; }
            yield return WaitSecondsAny(1);
            int m0 = _messages.Count;
            Sim.SetPhysicsWarp(3);
            double lastAlt = double.NaN, lastBar = double.NaN, t0 = Sim.UT;
            while (V != null && Sim.UT - t0 < 1500)
            {
                lastAlt = V.Altitude;
                lastBar = V.StaticPressure / 1e5;
                yield return null;
            }
            Sim.SetPhysicsWarp(0);
            DevCheats.IgnoreHeat = false;
            string msg = FindMessage(m0, "crushed");
            Check($"a vessel diving into {giant.Name} is crushed by the air", msg != null, msg ?? "it was not crushed (it was last seen at " + (lastAlt / 1000).ToString("F1") + " km)");
            double expected = giant.Atmosphere.AltitudeForPressure(giant.CrushPressure);
            Check("it is crushed at the crush pressure, well above the datum", Math.Abs(lastAlt - expected) < giant.Atmosphere.ScaleHeight * 0.4 && lastAlt > 1000,
                $"last altitude {lastAlt / 1000:F1} km, {lastBar:F1} bar; the crush pressure ({giant.CrushPressure / 1e5:F1} bar) is at {expected / 1000:F1} km");
        }
    }
}
