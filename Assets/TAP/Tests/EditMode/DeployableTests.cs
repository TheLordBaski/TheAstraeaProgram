using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using TAP.Core;
using TAP.Parts;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>The deployables framework (PRT-03): one data-driven module for panels, antennas, bay doors and the rest.</summary>
    public class DeployableTests
    {
        private static PartDatabase Db => PartDatabase.Instance;
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void CleanUp()
        {
            foreach (var go in _made) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _made.Clear();
        }

        private static DeployableDefinition Def(double extend = 4, double retract = 3, double pressure = 0, string start = "retracted") =>
            new DeployableDefinition { extendTime = extend, retractTime = retract, breakPressure = pressure, startState = start };

        // ------------------------------------------------------------------ the state machine

        [Test]
        public void States_RetractedExtendingExtendedRetracting()
        {
            var d = new DeployableLogic(Def(4, 2));
            Assert.AreEqual(DeployState.Retracted, d.State);
            Assert.IsFalse(d.Retract(), "there is nothing to take in");
            Assert.IsTrue(d.Extend());
            Assert.AreEqual(DeployState.Extending, d.State);
            d.Advance(2, 0);
            Assert.AreEqual(0.5, d.Progress, 1e-9);
            Assert.AreEqual(DeployState.Extending, d.State);
            d.Advance(2, 0);
            Assert.AreEqual(DeployState.Extended, d.State);
            Assert.AreEqual(1, d.Progress, 1e-9);
            Assert.IsTrue(d.Retract());
            d.Advance(1, 0);
            Assert.AreEqual(0.5, d.Progress, 1e-9, "retracting takes 2 s");
            Assert.AreEqual(DeployState.Retracting, d.State);
            d.Advance(1, 0);
            Assert.AreEqual(DeployState.Retracted, d.State);
            Assert.AreEqual(0, d.Progress, 1e-9);
        }

        [Test]
        public void ACommandReversesAMotionInProgress()
        {
            var d = new DeployableLogic(Def(4, 4));
            d.Extend();
            d.Advance(1, 0);
            Assert.IsTrue(d.Retract(), "it can be taken back while it unfolds");
            d.Advance(1, 0);
            Assert.AreEqual(DeployState.Retracted, d.State);
            Assert.IsTrue(d.Toggle());
            Assert.AreEqual(DeployState.Extending, d.State);
            d.Advance(0.5, 0);
            Assert.IsTrue(d.Toggle(), "toggling an unfolding part folds it again");
            Assert.AreEqual(DeployState.Retracting, d.State);
        }

        [Test]
        public void APartThatCantRetract_StaysOut()
        {
            var d = new DeployableLogic(Def(2, 0));
            d.Extend();
            d.Advance(3, 0);
            Assert.AreEqual(DeployState.Extended, d.State);
            Assert.IsFalse(d.CanRetract);
            Assert.IsFalse(d.Retract());
            Assert.AreEqual(DeployState.Extended, d.State);
        }

        [Test]
        public void ItBreaksAboveItsPressure_OnlyWhileOut()
        {
            var d = new DeployableLogic(Def(4, 3, 15000));
            Assert.IsFalse(d.Advance(1, 50000), "folded away, it is safe in any air");
            d.Extend();
            Assert.IsFalse(d.Advance(1, 14000), "below the limit");
            Assert.IsTrue(d.Advance(1, 20000), "out at 20 kPa");
            Assert.AreEqual(DeployState.Broken, d.State);
            StringAssert.Contains("20.0 kPa", d.BrokenReason);
            Assert.IsFalse(d.Extend(), "a broken part stays broken");
            Assert.IsFalse(d.Retract());
        }

        [Test]
        public void AMotorWithoutPower_Stalls_AndOneWithoutALimitNeverBreaks()
        {
            var d = new DeployableLogic(Def(4, 3));
            d.Extend();
            d.Advance(2, 0, power: 0.5);
            Assert.AreEqual(0.25, d.Progress, 1e-9, "half the power, half the speed");
            d.Advance(100, 1e9, power: 0);
            Assert.AreEqual(0.25, d.Progress, 1e-9, "no power, no motion");
            Assert.AreEqual(DeployState.Extending, d.State, "and with no pressure limit, no breaking");
        }

        [Test]
        public void ItCanStartOut_AndSaveAndRestore()
        {
            var d = new DeployableLogic(Def(4, 3, 0, "extended"));
            Assert.AreEqual(DeployState.Extended, d.State);
            Assert.AreEqual(1, d.Progress);
            d.Restore(DeployState.Retracting, 0.4);
            Assert.AreEqual(0.4, d.Progress, 1e-9);
            d.Advance(0.6, 0);
            Assert.AreEqual(0.2, d.Progress, 1e-9);
        }

        // ------------------------------------------------------------------ curves and windows

        [Test]
        public void Curves_StartAtZeroAndEndAtOne_AndOvershootGoesPast()
        {
            foreach (string c in DeployableMotion.Curves)
            {
                Assert.AreEqual(0, DeployableMotion.Shape(c, 0), 1e-6, c);
                Assert.AreEqual(1, DeployableMotion.Shape(c, 1), 1e-6, c);
            }
            Assert.AreEqual(0.5f, DeployableMotion.Shape("linear", 0.5f), 1e-6);
            Assert.Less(DeployableMotion.Shape("easeIn", 0.5f), 0.5f);
            Assert.Greater(DeployableMotion.Shape("easeOut", 0.5f), 0.5f);
            Assert.Greater(DeployableMotion.Shape("overshoot", 0.8f), 1f, "it swings past the end and settles");
        }

        [Test]
        public void AMotionMovesOnlyInItsWindow_AndKeysOverrideTheCurve()
        {
            var m = new DeployableMotion { curve = "linear", start = 0.4f, end = 0.8f };
            Assert.AreEqual(0, m.Blend(0.2), 1e-6);
            Assert.AreEqual(0, m.Blend(0.4), 1e-6);
            Assert.AreEqual(0.5, m.Blend(0.6), 1e-6);
            Assert.AreEqual(1, m.Blend(0.9), 1e-6);
            var keyed = new DeployableMotion { keys = new[] { new[] { 0f, 0f }, new[] { 0.5f, 0.9f }, new[] { 1f, 1f } } };
            Assert.AreEqual(0.45, keyed.Blend(0.25), 1e-6);
            Assert.AreEqual(0.9, keyed.Blend(0.5), 1e-6);
            Assert.AreEqual(0.95, keyed.Blend(0.75), 1e-6);
        }

        // ------------------------------------------------------------------ the module, on a model with the named pieces

        private (Part part, DeployableModule module) Build(string partId, params string[] pieces)
        {
            var def = Db.GetAny(partId);
            Assert.IsNotNull(def?.deployable, partId + " has deployable data");
            var go = new GameObject(partId);
            _made.Add(go);
            var model = new GameObject("Model");
            model.transform.SetParent(go.transform, false);
            foreach (string path in pieces)
            {
                Transform parent = model.transform;
                foreach (string name in path.Split('/'))
                {
                    var child = parent.Find(name);
                    if (child == null)
                    {
                        child = new GameObject(name).transform;
                        child.SetParent(parent, false);
                    }
                    parent = child;
                }
            }
            var part = go.AddComponent<Part>();
            part.Def = def;
            part.InitVisuals();
            var module = new DeployableModule { Part = part };
            part.Modules.Add(module);
            module.OnInit();
            return (part, module);
        }

        private static void Run(DeployableModule m, double seconds, double pressure = 0)
        {
            for (double t = 0; t < seconds; t += 0.05) m.Advance(0.05, pressure);
            m.Show();
        }

        [Test]
        public void AFoldingPanel_DeploysAndRetracts_AndBreaksAt20kPa()
        {
            var (part, panel) = Build("solar_fold", "WingA", "WingB");
            var wingA = part.ModelRoot.Find("WingA");
            var wingB = part.ModelRoot.Find("WingB");
            Assert.AreEqual(DeployState.Retracted, panel.State);

            Assert.IsTrue(panel.Extend());
            Run(panel, 7);
            Assert.AreEqual(DeployState.Extended, panel.State);
            Assert.AreEqual(-90, Mathf.DeltaAngle(0, wingA.localEulerAngles.z), 0.1f, "the first wing folds out 90° one way");
            Assert.AreEqual(90, Mathf.DeltaAngle(0, wingB.localEulerAngles.z), 0.1f, "the second the other way");

            Assert.IsTrue(panel.Retract());
            Run(panel, 6);
            Assert.AreEqual(DeployState.Retracted, panel.State);
            Assert.AreEqual(0, Quaternion.Angle(Quaternion.identity, wingA.localRotation), 0.1f, "back to its rest pose");

            // Out in thick air, it tears off. Its own limit is 15 kPa; the plan's example is 20 kPa.
            panel.Extend();
            Run(panel, 2, pressure: 5000);
            Assert.AreEqual(DeployState.Extending, panel.State, "5 kPa is fine");
            Run(panel, 0.1, pressure: 20000);
            Assert.AreEqual(DeployState.Broken, panel.State);
            Assert.IsFalse(wingA.gameObject.activeSelf, "the wings are gone");
            Assert.IsFalse(wingB.gameObject.activeSelf);
            var actions = new List<PartAction>();
            panel.CollectActions(actions);
            Assert.IsEmpty(actions, "nothing is left to move");
        }

        [Test]
        public void TheSameModule_DrivesAnAntennaAndABayDoor_WithNoCodeChange()
        {
            // An antenna: the boom slides out, the dish tips up.
            var (antennaPart, antenna) = Build("antenna_dish", "Boom", "Dish");
            antenna.Extend();
            Run(antenna, 6);
            Assert.AreEqual(0.35f, antennaPart.ModelRoot.Find("Boom").localPosition.y, 1e-3f, "the boom slides 0.35 m");
            Assert.AreEqual(-90, Mathf.DeltaAngle(0, antennaPart.ModelRoot.Find("Dish").localEulerAngles.x), 0.1f);

            // A bay: two doors swing apart.
            var (bayPart, bay) = Build("bay_s1", "DoorL", "DoorR");
            var actions = new List<PartAction>();
            bay.CollectActions(actions);
            Assert.AreEqual("Open bay", actions.Single().Label);
            bay.Extend();
            Run(bay, 3);
            Assert.AreEqual(DeployState.Extended, bay.State);
            Assert.AreEqual(-110, Mathf.DeltaAngle(0, bayPart.ModelRoot.Find("DoorL").localEulerAngles.z), 0.1f);
            Assert.AreEqual(110, Mathf.DeltaAngle(0, bayPart.ModelRoot.Find("DoorR").localEulerAngles.z), 0.1f);
            actions.Clear();
            bay.CollectActions(actions);
            Assert.AreEqual("Close bay", actions.Single().Label);

            // A telescopic ladder scales; an inflatable shield can't be taken in once it is out.
            var (_, ladder) = Build("ladder_tele", "Ladder");
            ladder.Extend();
            Run(ladder, 4);
            Assert.AreEqual(DeployState.Extended, ladder.State);
            var (_, shield) = Build("shield_inflatable", "Bloom");
            shield.Extend();
            Run(shield, 5);
            Assert.IsFalse(shield.Retract(), "an inflatable shield stays inflated");
        }

        [Test]
        public void APlaceholderModelWithoutTheNamedPieces_StillKeepsItsState()
        {
            var (_, panel) = Build("solar_wing"); // no transforms in the model yet
            panel.Extend();
            Run(panel, 11);
            Assert.AreEqual(DeployState.Extended, panel.State);
        }

        [Test]
        public void WhileOut_ThePartIsMoreFragile_AndItSavesAndLoads()
        {
            var (part, panel) = Build("solar_fold", "WingA", "WingB");
            double own = part.Def.impactTolerance;
            Assert.AreEqual(own, part.ImpactTolerance, "folded, it has its own tolerance");
            panel.Extend();
            Run(panel, 1);
            Assert.AreEqual(6, part.ImpactTolerance, "out, it breaks at its deployed tolerance (6 m/s)");

            var saved = new Dictionary<string, string>();
            panel.Save(saved);
            var (_, again) = Build("solar_fold", "WingA", "WingB");
            again.Load(saved);
            Assert.AreEqual(DeployState.Extending, again.State);
            Assert.AreEqual(panel.Progress, again.Progress, 1e-9);

            var broken = new Dictionary<string, string> { ["state"] = "Broken", ["progress"] = "1", ["reason"] = "torn off" };
            var (_, gone) = Build("solar_fold", "WingA", "WingB");
            gone.Load(broken);
            Assert.AreEqual(DeployState.Broken, gone.State);
        }

        // ------------------------------------------------------------------ the data

        [Test]
        public void TheDeployableData_IsChecked()
        {
            var cat = JObject.Parse(Resources.Load<TextAsset>("Data/parts").text);
            var panel = ((JArray)cat["parts"]).First(p => (string)p["id"] == "solar_fold");
            var motions = (JArray)panel["deployable"]["motions"];
            motions[0]["kind"] = "spin";
            motions[1]["end"] = 0.1;
            panel["deployable"]["extendTime"] = 0;
            var report = new ContentReport();
            PartValidation.Check("deploy.json", cat.ToString(), report);
            string all = report.ToString();
            Assert.IsTrue(report.Errors.Any(p => p.Field == "deployable.motions[0].kind"), all);
            Assert.IsTrue(report.Errors.Any(p => p.Field == "deployable.motions[1].end"), all);
            Assert.IsTrue(report.Errors.Any(p => p.Field == "deployable.extendTime"), all);
        }

        [Test]
        public void EveryDeployablePartOfTheCatalogue_NamesItsPiecesOnce()
        {
            var parts = Db.AllParts.Where(p => p.deployable != null).ToList();
            Assert.GreaterOrEqual(parts.Count, 16, "panels, antennas, radiators, bays, ladders, airbrakes, drills, scanners and the shielded port");
            foreach (var p in parts)
            {
                var names = p.deployable.motions.Select(m => m.transform).ToList();
                Assert.AreEqual(names.Count, names.Distinct().Count(), p.id + " moves a piece twice");
            }
        }
    }
}
