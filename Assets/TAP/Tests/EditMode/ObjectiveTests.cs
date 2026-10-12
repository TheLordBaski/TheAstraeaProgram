using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.TestTools;

namespace TAP.Tests
{
    /// <summary>The event bus and the data-driven objectives of the mission guide (FND-14).</summary>
    public class ObjectiveTests
    {
        private static CelestialSystem Home => CelestialSystem.LoadFromResources();
        private static CelestialSystem DebugSystem => Galaxy.LoadFromResources().LoadSystem("debug");

        private static ObjectiveContext Ctx(CelestialSystem sys, VesselState v = null, GameEvent e = null, params string[] done) => new ObjectiveContext
        {
            System = sys,
            Vessel = v,
            Event = e,
            IsDone = id => done.Contains(id),
        };

        [SetUp]
        public void Clear() => GameEvents.Clear();

        // ------------------------------------------------------------------ the bus

        [Test]
        public void Bus_TellsEveryListener_EvenWhenOneFails()
        {
            var heard = new List<GameEventKind>();
            Action<GameEvent> bad = _ => throw new InvalidOperationException("listener failed");
            Action<GameEvent> good = e => heard.Add(e.Kind);
            GameEvents.Raised += bad;
            GameEvents.Raised += good;
            try
            {
                LogAssert.Expect(LogType.Exception, new Regex("listener failed"));
                GameEvents.Raise(GameEventKind.Launched, 10, "v1", "Probe", "tellus");
            }
            finally { GameEvents.Raised -= bad; GameEvents.Raised -= good; }
            CollectionAssert.AreEqual(new[] { GameEventKind.Launched }, heard);
            Assert.AreEqual("Probe", GameEvents.Recent.Single().VesselName);
        }

        [Test]
        public void Bus_KeepsOnlyTheLastEvents()
        {
            for (int i = 0; i < 250; i++) GameEvents.Raise(GameEventKind.Staged, i, detail: i.ToString());
            Assert.AreEqual(100, GameEvents.Recent.Count);
            Assert.AreEqual("249", GameEvents.Recent.Last().Detail);
            Assert.AreEqual("150", GameEvents.Recent.First().Detail);
        }

        // ------------------------------------------------------------------ conditions

        [Test]
        public void Condition_NeedsTheEventItNames()
        {
            var landed = new GameEvent { Kind = GameEventKind.Landed, BodyId = Home.HomeMoon.Id };
            var c = new Condition { kind = "Landed", body = "$moon" };
            Assert.IsTrue(Objectives.Eval(c, Ctx(Home, e: landed)));
            Assert.IsFalse(Objectives.Eval(c, Ctx(Home, e: new GameEvent { Kind = GameEventKind.Landed, BodyId = Home.HomeBody.Id })), "another body");
            Assert.IsFalse(Objectives.Eval(c, Ctx(Home, e: new GameEvent { Kind = GameEventKind.Splashed, BodyId = Home.HomeMoon.Id })), "another event");
            Assert.IsFalse(Objectives.Eval(c, Ctx(Home)), "an event counts when it is raised, not after");
        }

        [Test]
        public void Condition_ReadsTheVessel()
        {
            var sys = Home;
            var inOrbit = new VesselState { BodyId = sys.HomeBody.Id, Situation = Situation.Orbiting, Altitude = 120000 };
            Assert.IsTrue(Objectives.Eval(new Condition { body = "$home", situation = "Orbiting" }, Ctx(sys, inOrbit)));
            Assert.IsTrue(Objectives.Eval(new Condition { situation = "Landed|Orbiting" }, Ctx(sys, inOrbit)), "alternatives");
            Assert.IsFalse(Objectives.Eval(new Condition { body = "$moon", situation = "Orbiting" }, Ctx(sys, inOrbit)), "right situation, wrong body");
            Assert.IsTrue(Objectives.Eval(new Condition { body = "$home", altitudeAbove = "space" }, Ctx(sys, inOrbit)));
            Assert.IsFalse(Objectives.Eval(new Condition { body = "$home", altitudeAbove = "space" }, Ctx(sys, new VesselState { BodyId = sys.HomeBody.Id, Altitude = 3000 })));
            Assert.IsFalse(Objectives.Eval(new Condition { launched = true }, Ctx(sys)), "no vessel, no state");
        }

        [Test]
        public void Condition_CombinesWithDoneAnyAndNot()
        {
            var sys = Home;
            var v = new VesselState { BodyId = sys.HomeBody.Id, Situation = Situation.Landed };
            var landedHome = new Condition { body = "$home", situation = "Landed" };
            Assert.IsTrue(Objectives.Eval(new Condition { any = new[] { new Condition { body = "$moon" }, landedHome } }, Ctx(sys, v)));
            Assert.IsFalse(Objectives.Eval(new Condition { not = landedHome }, Ctx(sys, v)));
            Assert.IsFalse(Objectives.Eval(new Condition { situation = "Landed", done = new[] { "launch" } }, Ctx(sys, v)), "launch is not done");
            Assert.IsTrue(Objectives.Eval(new Condition { situation = "Landed", done = new[] { "launch" } }, Ctx(sys, v, null, "launch")));
            Assert.IsFalse(Objectives.Eval(new Condition { situation = "Landed", notDone = new[] { "launch" } }, Ctx(sys, v, null, "launch")));
        }

        // ------------------------------------------------------------------ the sets

        [Test]
        public void Sets_ParseFromTheShippedFiles_AndCheckOutInTheHomeAndDebugSystems()
        {
            // No code names a body, so every set has to be sound for any system that has a home planet and a moon.
            foreach (var sys in new[] { Home, DebugSystem })
            {
                var sets = ObjectiveSets.Load(sys);
                Assert.AreEqual(Resources.LoadAll<TextAsset>("Data/Objectives").Length, sets.Count, "a set was left out for " + sys.Id);
                Assert.IsTrue(sets.Any(s => s.id == Objectives.DefaultSet));
            }
        }

        [Test]
        public void LunarSet_KeepsTheMilestoneIdsOfOlderSaves()
        {
            // Saves hold these ids in GameSave.milestones; the objectives that replaced the hard-coded list must not rename them.
            var lunar = ObjectiveSets.Load(Home).Single(s => s.id == Objectives.DefaultSet);
            CollectionAssert.AreEqual(new[]
            {
                "launch", "space", "orbit", "transfer", "luma_soi", "luma_orbit", "luma_land", "eva", "flag", "board", "luma_ascent", "return", "reentry", "landed_home", "recovered",
            }, lunar.objectives.Select(o => o.id).ToArray());
            Assert.AreEqual("launch", lunar.Key("launch", Objectives.DefaultSet), "the first set's ids are stored as they are");
            Assert.AreEqual("orbit_moon:launch", new ObjectiveSetDef { id = "orbit_moon" }.Key("launch", Objectives.DefaultSet));
        }

        [Test]
        public void Validate_FindsWhatIsWrong()
        {
            var set = Objectives.Parse(@"{ ""id"": ""bad"", ""title"": ""Bad"", ""objectives"": [
                { ""id"": ""a"", ""title"": ""A"", ""when"": { ""kind"": ""Teleported"", ""body"": ""nowhere"" } },
                { ""id"": ""a"", ""title"": ""A again"", ""when"": { ""situation"": ""Hovering"", ""done"": [ ""missing"" ] } },
                { ""id"": ""c"", ""title"": ""C"", ""when"": { ""altitudeAbove"": ""high"" } } ] }");
            string all = string.Join("\n", Objectives.Validate(set, Home));
            StringAssert.Contains("unknown event 'Teleported'", all);
            StringAssert.Contains("'nowhere' is not a body", all);
            StringAssert.Contains("used twice", all);
            StringAssert.Contains("unknown situation 'Hovering'", all);
            StringAssert.Contains("no objective 'missing'", all);
            StringAssert.Contains("not 'space' or a number", all);
        }

        [Test]
        public void Parse_RejectsAnUnknownField()
        {
            // A typo in a data file ("situtation") must not silently make an objective that is always done.
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => Objectives.Parse(@"{ ""id"": ""x"", ""title"": ""X"", ""objectives"": [ { ""id"": ""a"", ""title"": ""A"", ""when"": { ""situtation"": ""Landed"" } } ] }"));
        }

        [Test]
        public void ASetCanLeadToAnotherBody_WithoutCodeChanges()
        {
            // A guide for another system is a data file: land on a body by id, finish when a flag stands on it.
            var sys = DebugSystem;
            var rock = sys.Get("debug/testrock");
            Assert.NotNull(rock, "the debug system has a third body");
            string json = $@"{{ ""id"": ""rock"", ""title"": ""Mission: {{body:{rock.Id}}}"", ""objectives"": [
                {{ ""id"": ""land"", ""title"": ""Land on {{body:{rock.Id}}}"", ""hint"": ""Aim for {{body:{rock.Id}}}"", ""when"": {{ ""kind"": ""Landed"", ""body"": ""{rock.Id}"" }} }},
                {{ ""id"": ""flag"", ""title"": ""Plant a flag"", ""hint"": ""Walk out."", ""when"": {{ ""done"": [ ""land"" ], ""flagOn"": ""{rock.Id}"" }} }} ] }}";
            var set = Objectives.Parse(json);
            Assert.IsEmpty(Objectives.Validate(set, sys));
            Assert.AreEqual("Land on " + rock.Name, Objectives.Expand(sys, set.objectives[0].title));

            var done = new HashSet<string>();
            bool flagStands = false;
            ObjectiveContext Make(GameEvent e) => new ObjectiveContext { System = sys, Event = e, IsDone = done.Contains, FlagOn = id => flagStands && id == rock.Id };
            void Evaluate(GameEvent e)
            {
                bool changed = true;
                for (int pass = 0; changed && pass < 5; pass++)
                {
                    changed = false;
                    foreach (var o in set.objectives)
                        if (!done.Contains(o.id) && Objectives.Eval(o.when, Make(pass == 0 ? e : null))) { done.Add(o.id); changed = true; }
                }
            }
            Evaluate(new GameEvent { Kind = GameEventKind.Landed, BodyId = sys.HomeBody.Id });
            Assert.IsEmpty(done, "landing on the home planet is not landing on the rock");
            flagStands = true;
            Evaluate(null);
            Assert.IsEmpty(done, "the flag counts only after the landing");
            Evaluate(new GameEvent { Kind = GameEventKind.Landed, BodyId = rock.Id });
            CollectionAssert.AreEquivalent(new[] { "land", "flag" }, done, "one pass finishes the landing and the objective that was waiting on it");
        }

        [Test]
        public void Text_NamesTheBodiesOfTheSystem()
        {
            Assert.AreEqual($"Back to {Home.HomeBody.Name} from {Home.HomeMoon.Name}", Objectives.Expand(Home, "Back to {home} from {moon}"));
            Assert.AreEqual($"Back to {DebugSystem.HomeBody.Name} from {DebugSystem.HomeMoon.Name}", Objectives.Expand(DebugSystem, "Back to {home} from {moon}"));
            var home = Home.HomeBody;
            Assert.AreEqual($"{home.Atmosphere.Height / 1000:0} km and {home.Atmosphere.Height / 1000 + 10:0} km", Objectives.Expand(Home, "{space} km and {space+10} km"), "space begins at the top of the air");
            Assert.AreEqual("Plain text", Objectives.Expand(Home, "Plain text"));
        }

        [Test]
        public void Environments_ListEveryBodyOfTheSystem()
        {
            var all = EditorEnvironment.All();
            var sys = CelestialSystem.Default;
            foreach (var b in sys.Bodies.Where(b => !b.IsStar))
                Assert.IsTrue(all.Any(e => e.BodyId == b.Id), b.Name + " is a place to read the report for");
            Assert.AreEqual(sys.HomeBody.Id, all[0].BodyId, "the home planet comes first");
            Assert.AreEqual(all.Distinct().Count(), all.Count);
        }

        // ------------------------------------------------------------------ names in the code

        [Test]
        public void PresentationCode_NamesNoBody()
        {
            // What the player reads is built from the system ({moon}, Name), so a second system reads right. Names of starter crafts are data.
            var literal = new Regex("\"[^\"]*\\b(Tellus|Luma|Astraea)\\b(?! Pathfinder)[^\"]*\"");
            var offenders = new List<string>();
            string scripts = Path.Combine(Application.dataPath, "TAP", "Scripts");
            foreach (var dir in new[] { "UI", "Game/Flight", "Game/Objectives", "Game/Assembly", "Game/Localization" })
                foreach (var file in Directory.GetFiles(Path.Combine(scripts, dir), "*.cs", SearchOption.AllDirectories))
                {
                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        int comment = line.IndexOf("//", StringComparison.Ordinal);
                        if (comment >= 0 && line.Substring(0, comment).Count(ch => ch == '"') % 2 == 0) line = line.Substring(0, comment);
                        if (literal.IsMatch(line)) offenders.Add($"{Path.GetFileName(file)}:{i + 1}");
                    }
                }
            Assert.IsEmpty(offenders, "write {home} / {moon} (Objectives.Expand) or use the body's Name: " + string.Join(", ", offenders));
        }

        [Test]
        public void ObjectiveFiles_NameNoBodyInTheirTexts()
        {
            foreach (var t in Resources.LoadAll<TextAsset>("Data/Objectives"))
                Assert.IsFalse(Regex.IsMatch(t.text, "\"(title|hint)\"\\s*:\\s*\"[^\"]*\\b(Tellus|Luma)\\b(?! Pathfinder)"), t.name + " names a body in a text: use {home}, {moon} or {body:REF}");
        }
    }
}
