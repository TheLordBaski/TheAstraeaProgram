using System;
using System.Collections.Generic;
using NUnit.Framework;
using TAP.Construction;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>Vehicle construction, staging and delta-v analysis (the assembly building's maths).</summary>
    public class ConstructionTests
    {
        private static PartDatabase Db => PartDatabase.Instance;

        [Test]
        public void StackAttach_MatesNodesExactly()
        {
            var d = new CraftDesign();
            var a = new CraftAssembler(d, Db);
            int pod = a.AddRoot("pod_kestrel");
            int tank = a.AttachStack(pod, "bottom", "tank_s1_long", "top");
            var podBottom = CraftAssembler.V(d.parts[pod].pos) + CraftAssembler.Q(d.parts[pod].rot) * Db.Get("pod_kestrel").FindNode("bottom").Position;
            var tankTop = CraftAssembler.V(d.parts[tank].pos) + CraftAssembler.Q(d.parts[tank].rot) * Db.Get("tank_s1_long").FindNode("top").Position;
            Assert.Less((podBottom - tankTop).magnitude, 1e-4f, "attached nodes must coincide");
            Vector3 podDir = CraftAssembler.Q(d.parts[pod].rot) * Db.Get("pod_kestrel").FindNode("bottom").Direction;
            Vector3 tankDir = CraftAssembler.Q(d.parts[tank].rot) * Db.Get("tank_s1_long").FindNode("top").Direction;
            Assert.Less((podDir + tankDir).magnitude, 1e-4f, "attached node directions must be opposite");
        }

        [Test]
        public void RadialSymmetry_PlacesEvenlySpacedCopiesInOneGroup()
        {
            var d = new CraftDesign();
            var a = new CraftAssembler(d, Db);
            int pod = a.AddRoot("pod_kestrel");
            int tank = a.AttachStack(pod, "bottom", "tank_s1_long", "top");
            var fins = a.AttachRadial(tank, "fin_fs1", -1.5f, 0, 3);
            Assert.AreEqual(3, fins.Count);
            int group = d.parts[fins[0]].symmetryGroup;
            Assert.GreaterOrEqual(group, 0);
            var angles = new List<float>();
            foreach (int f in fins)
            {
                Assert.AreEqual(group, d.parts[f].symmetryGroup);
                Assert.AreEqual(tank, d.parts[f].parent);
                Vector3 attach = CraftAssembler.V(d.parts[f].pos) + CraftAssembler.Q(d.parts[f].rot) * Db.Get("fin_fs1").surfaceAttach.Position;
                Vector3 rel = attach - CraftAssembler.V(d.parts[tank].pos);
                Assert.AreEqual(0.625f, new Vector2(rel.x, rel.z).magnitude, 1e-3f, "attach point sits on the tank surface");
                angles.Add(Mathf.Atan2(rel.x, rel.z) * Mathf.Rad2Deg);
            }
            angles.Sort();
            Assert.AreEqual(120f, angles[1] - angles[0], 0.01f);
            Assert.AreEqual(120f, angles[2] - angles[1], 0.01f);
        }

        [Test]
        public void RemoveSubtree_RemapsParents()
        {
            var d = new CraftDesign();
            var a = new CraftAssembler(d, Db);
            int pod = a.AddRoot("pod_kestrel");
            int dec = a.AttachStack(pod, "bottom", "dec_s1", "top");
            int tank = a.AttachStack(dec, "bottom", "tank_s1_long", "top");
            a.AttachStack(tank, "bottom", "eng_hornet", "top");
            int chute = a.AttachStack(pod, "top", "chute_main", "bottom");
            Assert.AreEqual(5, d.parts.Count);
            a.RemoveSubtree(dec);
            Assert.AreEqual(2, d.parts.Count, "decoupler, tank and engine removed");
            Assert.AreEqual("chute_main", d.parts[1].partId);
            Assert.AreEqual(0, d.parts[1].parent);
        }

        [Test]
        public void AutoStage_EnginesBelowFireFirst_ChuteLast()
        {
            var d = StarterCraft.Meridian(Db);
            int hornet = d.parts.FindIndex(p => p.partId == "eng_hornet");
            int ember = d.parts.FindIndex(p => p.partId == "eng_ember");
            int chute = d.parts.FindIndex(p => p.partId == "chute_main");
            Assert.Greater(d.parts[hornet].stage, d.parts[ember].stage, "first stage engine fires before the upper stage");
            Assert.AreEqual(0, d.parts[chute].stage, "parachute is the last stage");
            // The decoupler between the stages fires with the upper-stage engine (hot staging).
            var decs = d.parts.FindAll(p => p.partId == "dec_s1");
            Assert.IsTrue(decs.Exists(p => p.stage == d.parts[ember].stage));
        }

        [Test]
        public void DeltaV_SingleStageMatchesRocketEquation()
        {
            var d = new CraftDesign();
            var a = new CraftAssembler(d, Db);
            int probe = a.AddRoot("probe_wren");
            int tank = a.AttachStack(probe, "bottom", "tank_s1_long", "top");
            a.AttachStack(tank, "bottom", "eng_ember", "top");
            a.AutoStage();
            var stages = DeltaVCalculator.Compute(a.ToStageModel(), 0, 9.81, 0, Db);
            var e = Db.Get("eng_ember").engine;
            double m0 = a.TotalMass(), m1 = m0 - 2000; // the long tank holds 2000 kg of propellant
            double expected = e.ispVac * MathD.G0 * Math.Log(m0 / m1);
            Assert.AreEqual(expected, stages[0].DeltaVVac, 0.5);
            Assert.AreEqual(2000 / (e.thrustVac / (e.ispVac * MathD.G0)), stages[0].BurnTime, 0.1);
        }

        [Test]
        public void DeltaV_DecouplerBlocksFuelFlow()
        {
            // Engine below a decoupler can't drink from the tank above it.
            var d = new CraftDesign();
            var a = new CraftAssembler(d, Db);
            int probe = a.AddRoot("probe_wren");
            int tank = a.AttachStack(probe, "bottom", "tank_s1_long", "top");
            int dec = a.AttachStack(tank, "bottom", "dec_s1", "top");
            a.AttachStack(dec, "bottom", "eng_ember", "top");
            a.AutoStage();
            var s = DesignAnalysis.Analyze(d, Db, EditorEnvironment.TellusVacuum);
            Assert.AreEqual(0, s.TotalDvVac, 1e-6);
            Assert.IsTrue(s.Warnings.Exists(w => w.Text.Contains("no propellant")), "engine without reachable fuel is reported");
        }

        [Test]
        public void DesignAnalysis_StarterRocketsAreSound()
        {
            foreach (var c in StarterCraft.All(Db))
            {
                var s = DesignAnalysis.Analyze(c, Db, EditorEnvironment.TellusSeaLevel);
                Assert.IsFalse(s.HasBlocking, c.name + " has a blocking problem");
                Assert.Greater(s.LaunchTwr, 1.2, c.name + " launch TWR");
                Assert.Less(s.StabilityMargin, 0f, c.name + " must be aerodynamically stable (CoP below CoM)");
            }
            var lunar = DesignAnalysis.Analyze(StarterCraft.Pathfinder(Db), Db, EditorEnvironment.TellusVacuum);
            Assert.Greater(lunar.TotalDvVac, 6300, "Pathfinder needs the Δv for the full lunar mission");
        }

        [Test]
        public void CraftDesign_JsonRoundTripIsLossless()
        {
            var d = StarterCraft.Pathfinder(Db);
            string json = SaveStorage.ToJson(d);
            var back = SaveStorage.FromJson<CraftDesign>(json);
            Assert.AreEqual(d.parts.Count, back.parts.Count);
            for (int i = 0; i < d.parts.Count; i++)
            {
                Assert.AreEqual(d.parts[i].partId, back.parts[i].partId);
                Assert.AreEqual(d.parts[i].parent, back.parts[i].parent);
                Assert.AreEqual(d.parts[i].stage, back.parts[i].stage);
                Assert.AreEqual(d.parts[i].symmetryGroup, back.parts[i].symmetryGroup);
                for (int k = 0; k < 3; k++) Assert.AreEqual(d.parts[i].pos[k], back.parts[i].pos[k]);
                for (int k = 0; k < 4; k++) Assert.AreEqual(d.parts[i].rot[k], back.parts[i].rot[k]);
            }
            Assert.AreEqual(json, SaveStorage.ToJson(back));
        }

        [Test]
        public void GameSave_JsonRoundTripKeepsDoublePrecisionOrbits()
        {
            var save = new GameSave { ut = 123456.789012345 };
            var rec = new VesselRecord
            {
                name = "Test", bodyId = "tellus",
                orbitPos = new[] { 680000.123456789, -1234.987654321, 42.000000001 },
                orbitVel = new[] { 0.000123, 2279.123456789, -0.5 },
                orbitEpoch = 123456.789012345,
            };
            save.vessels.Add(rec);
            var back = SaveStorage.FromJson<GameSave>(SaveStorage.ToJson(save));
            Assert.AreEqual(save.ut, back.ut);
            for (int k = 0; k < 3; k++)
            {
                Assert.AreEqual(rec.orbitPos[k], back.vessels[0].orbitPos[k]);
                Assert.AreEqual(rec.orbitVel[k], back.vessels[0].orbitVel[k]);
            }
        }
    }
}
