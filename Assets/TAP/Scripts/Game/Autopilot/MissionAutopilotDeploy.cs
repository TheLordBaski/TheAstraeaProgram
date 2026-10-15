using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TAP.Construction;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Deployables in a real vessel (PRT-03): panels, an antenna and a bay of the catalogue's later releases unfold in orbit, are saved in
    /// their state, and the ones that are out are torn off by fast air while a folded one is not.
    /// </summary>
    public sealed partial class MissionAutopilot
    {
        private static CraftDesign DeployRig(PartDatabase db)
        {
            var d = new CraftDesign { name = "Deploy Rig", description = "Deployables test: two folding panels, a dish and a service bay." };
            var a = new CraftAssembler(d, db);
            int core = a.AddRoot("probe_wren");
            int tank = a.AttachStack(core, "bottom", "tank_s1_short", "top");
            a.AttachStack(tank, "bottom", "bay_s1", "top");
            a.AttachRadial(tank, "solar_fold", 0, 0);
            a.AttachRadial(tank, "solar_fold", 0, 180);
            a.AttachRadial(tank, "antenna_dish", 0, 90);
            return d;
        }

        private IEnumerator DeployMission()
        {
            CaptureMessages();
            PartDatabase.ShowUnreleased = true; // the panels, the dish and the bay arrive with EA1 and EA2
            try
            {
                var db = PartDatabase.Instance;
                var home = Sim.System.HomeBody;

                SetPhase("unfolding in orbit");
                double r = home.Radius + 200000, vc = Math.Sqrt(home.GM / r);
                var orbit = CreateOrbitalHandle(DeployRig(db), "Deploy Rig", new Vector3d(r, 0, 0), new Vector3d(0, 0, vc), Quaternion.identity, home);
                if (!Sim.SwitchTo(orbit)) { Check("rig in orbit", false, "could not switch to the rig"); yield break; }
                yield return WaitSecondsAny(2);
                var rig = V;
                var panels = rig.Parts.Where(p => p.Def.id == "solar_fold").Select(p => p.GetModule<DeployableModule>()).ToList();
                var dish = rig.Parts.First(p => p.Def.id == "antenna_dish").GetModule<DeployableModule>();
                var bay = rig.Parts.First(p => p.Def.id == "bay_s1").GetModule<DeployableModule>();
                Check("every deployable part has the module", panels.Count == 2 && panels.All(p => p != null) && dish != null && bay != null,
                    $"{panels.Count(p => p != null)} panels, dish {dish != null}, bay {bay != null}");
                var actions = new List<PartAction>();
                panels[0].CollectActions(actions);
                Check("the part menu offers to deploy", actions.Count == 1 && actions[0].Label == "Deploy panel", string.Join(", ", actions.Select(a => a.Label)));

                panels[0].Extend(); dish.Extend(); bay.Extend();
                yield return WaitSecondsAny(14);
                Check("the panel, the dish and the bay unfold", panels[0].State == DeployState.Extended && dish.State == DeployState.Extended && bay.State == DeployState.Extended,
                    $"panel {panels[0].State} {panels[0].Progress:P0}, dish {dish.State}, bay {bay.State}");
                Check("the panel left folded stays folded", panels[1].State == DeployState.Retracted, panels[1].State.ToString());
                var record = Sim.CaptureAllRecords().First(x => x.name == "Deploy Rig");
                var saved = record.parts.Where(p => p.partId == "solar_fold").Select(p => p.modules.TryGetValue("Deployable", out var m) ? m["state"] : "none").ToList();
                Check("the state is saved with the vessel", saved.Contains("Extended") && saved.Contains("Retracted"), string.Join(", ", saved));

                panels[0].Retract();
                yield return WaitSecondsAny(7);
                Check("a panel retracts again", panels[0].State == DeployState.Retracted, panels[0].State.ToString());

                // Fast air: 250 m/s at 3 km is about 25 kPa. The panel and the dish that are out tear off, the folded panel does not.
                SetPhase("tearing off in fast air");
                double ra = home.Radius + 3000;
                var pos = new Vector3d(ra, 0, 0);
                var air = CreateOrbitalHandle(DeployRig(db), "Deploy Rig Air", pos, home.FrameVelocityAt(pos) + new Vector3d(0, 0, 250), Quaternion.LookRotation(Vector3.forward, Vector3.right), home);
                int m0 = _messages.Count;
                if (!Sim.SwitchTo(air)) { Check("rig in the air", false, "could not switch to the rig"); yield break; }
                var fast = V;
                var out1 = fast.Parts.First(p => p.Def.id == "solar_fold").GetModule<DeployableModule>();
                var out2 = fast.Parts.First(p => p.Def.id == "antenna_dish").GetModule<DeployableModule>();
                var folded = fast.Parts.Where(p => p.Def.id == "solar_fold").Select(p => p.GetModule<DeployableModule>()).First(m => m != out1);
                out1.Extend(); out2.Extend();
                double t0 = Sim.UT;
                while (V == fast && (out1.State != DeployState.Broken || out2.State != DeployState.Broken) && Sim.UT - t0 < 4) yield return null;
                string msg = FindMessage(m0, "torn off");
                Check("a panel and a dish that are out are torn off by fast air", out1.State == DeployState.Broken && out2.State == DeployState.Broken,
                    $"panel {out1.State}, dish {out2.State}, q {fast.DynamicPressure / 1000:F1} kPa, {msg}");
                Check("a folded panel is not", folded.State == DeployState.Retracted, folded.State.ToString());
            }
            finally
            {
                PartDatabase.ShowUnreleased = false;
            }
        }
    }
}
