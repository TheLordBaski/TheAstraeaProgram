using System.IO;
using System.Linq;
using NUnit.Framework;
using TAP.Game;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAP.Tests
{
    /// <summary>FND-09: the control table, rebinding, the guide, trim and precision. Devices are simulated by the Input System test fixture.</summary>
    public class InputTests : InputTestFixture
    {
        private string _root;
        private Keyboard _kb;

        public override void Setup()
        {
            base.Setup();
            _root = Path.Combine(Path.GetTempPath(), "tap_input_" + System.Guid.NewGuid().ToString("N"));
            SaveStorage.RootOverride = _root;
            Controls.Reset();
            GameSettings.Forget();
            _kb = InputSystem.AddDevice<Keyboard>();
            Controls.Action("Flight/Stage"); // builds and enables the actions before any key goes down, as the game does at start-up
        }

        public override void TearDown()
        {
            Controls.Reset();
            SaveStorage.RootOverride = null;
            GameSettings.Forget();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            base.TearDown();
        }

        [Test]
        public void EveryControlIsBuilt_WithItsDefaults()
        {
            foreach (var def in ControlTable.All)
            {
                Assert.IsNotNull(Controls.Action(def.Id), def.Id);
                Assert.AreEqual(def.Defaults.Length, Controls.SlotsOf(def.Id).Count, def.Id);
            }
            Assert.AreEqual(ControlTable.All.Count, ControlTable.All.Select(c => c.Id).Distinct().Count(), "ids are unique");
        }

        [Test]
        public void NoTwoControlsShareAKeyWhereBothAreUsed()
        {
            foreach (var def in ControlTable.All)
                for (int i = 0; i < def.Defaults.Length; i++)
                {
                    var clash = Controls.Conflicts(def.Id, i, Controls.PathOf(def.Id, i));
                    Assert.IsEmpty(clash, $"{def.Id} #{i} ({def.Defaults[i]}) is also {string.Join(", ", clash.Select(c => c.def.Id))}");
                }
        }

        [Test]
        public void KeyboardBindingsFollowThePhysicalKeys()
        {
            // "<Keyboard>/w" is the key where W is on a US keyboard, on QWERTZ and AZERTY too. "#(w)" would follow the letter.
            foreach (var def in ControlTable.All)
                foreach (var d in def.Defaults)
                    Assert.IsFalse(d.Contains("#("), def.Id + " binds by the letter a key shows");
        }

        [Test]
        public void AKeyPress_ReachesTheControl_AndACombinationOnlyItsOwn()
        {
            Press(_kb.spaceKey);
            Assert.IsTrue(Controls.Down("Flight/Stage"));
            Release(_kb.spaceKey);

            // Ctrl+S saves the craft; the plain S that rotates a held part does not fire with it.
            Press(_kb.leftCtrlKey);
            Press(_kb.sKey);
            Assert.IsTrue(Controls.Down("Assembly/SaveCraft"));
            Assert.IsFalse(Controls.Down("Assembly/RotatePitchDown"));
        }

        [Test]
        public void EveryButton_CanBeRebound_AndFollowsTheNewKey()
        {
            foreach (var def in ControlTable.All.Where(c => c.Kind == ControlKind.Button))
            {
                for (int i = 0; i < def.Defaults.Length; i++)
                {
                    Controls.Rebind(def.Id, i, "<Keyboard>/u", keepConflicts: true);
                    Assert.AreEqual("<Keyboard>/u", Controls.PathOf(def.Id, i), def.Id);
                    Assert.IsTrue(Controls.IsRebound(def.Id, i), def.Id);
                }
                // With its modifiers held (a combination), the new key presses it.
                bool combo = def.Defaults.Any(d => d.Contains("+"));
                if (!combo)
                {
                    Press(_kb.uKey);
                    Assert.IsTrue(Controls.Down(def.Id), def.Id + " did not follow the new key");
                    Release(_kb.uKey);
                }
                Controls.ResetToDefaults(def.Id);
            }
        }

        [Test]
        public void EveryAxis_CanBeRebound()
        {
            foreach (var def in ControlTable.All.Where(c => c.Kind == ControlKind.Axis && c.Defaults.Length > 0))
            {
                Controls.Rebind(def.Id, 0, "<Gamepad>/rightStick/y", keepConflicts: true);
                Assert.AreEqual("<Gamepad>/rightStick/y", Controls.PathOf(def.Id, 0), def.Id);
                Controls.ResetToDefaults(def.Id);
                Assert.IsFalse(Controls.IsRebound(def.Id, 0), def.Id);
            }
        }

        [Test]
        public void Rebinding_TakesTheKeyFromTheControlThatUsedIt()
        {
            var clash = Controls.Conflicts("Flight/Stage", 0, "<Keyboard>/w");
            Assert.AreEqual("Flight/PitchDown", clash.Single().def.Id);
            Controls.Rebind("Flight/Stage", 0, "<Keyboard>/w");
            Assert.AreEqual("", Controls.PathOf("Flight/PitchDown", 0), "the control that lost the key has none until the player picks one");
            Assert.AreEqual("<Keyboard>/w", Controls.PathOf("Flight/Stage", 0));
        }

        [Test]
        public void FlightAndEvaShareKeys_Deliberately()
        {
            Assert.IsEmpty(Controls.Conflicts("Eva/Forward", 0, "<Keyboard>/w"));
        }

        [Test]
        public void Rebinds_AreSavedPerUser_AndResetGoesBackToTheDefaults()
        {
            Controls.Rebind("Flight/Stage", 0, "<Keyboard>/u");
            Assert.IsTrue(File.Exists(Controls.FilePath));

            Controls.Reset(); // a restart
            Assert.AreEqual("<Keyboard>/u", Controls.PathOf("Flight/Stage", 0));
            Press(_kb.uKey);
            Assert.IsTrue(Controls.Down("Flight/Stage"));
            Release(_kb.uKey);

            Controls.ResetToDefaults();
            Assert.AreEqual("<Keyboard>/space", Controls.PathOf("Flight/Stage", 0));
            Assert.IsFalse(File.Exists(Controls.FilePath), "nothing left to remember");
        }

        [Test]
        public void TheGuide_ShowsTheReboundKeys()
        {
            string before = string.Join("|", Controls.GuideRows(ControlTable.Flight).Select(r => r.keys + " " + r.action));
            StringAssert.Contains("Space", before.Substring(before.IndexOf("Activate next stage", System.StringComparison.Ordinal) - 40));
            Controls.Rebind("Flight/Stage", 0, "<Keyboard>/u");
            var after = Controls.GuideRows(ControlTable.Flight);
            Assert.IsTrue(after.Any(r => r.action == "Activate next stage" && r.keys.Contains("U")));
            Assert.IsFalse(after.Any(r => r.action == "Activate next stage" && r.keys.StartsWith("Space")));
            StringAssert.Contains("U", Controls.Expand("press {Flight/Stage}"));
        }

        [Test]
        public void EveryControlCodeAsksFor_IsInTheTable()
        {
            var asked = new System.Text.RegularExpressions.Regex("Controls\\.(?:Down|Held|Up|Axis|Action)\\(\"([A-Za-z]+/[A-Za-z0-9]+)\"\\)");
            var scripts = Directory.GetFiles(Path.Combine(Application.dataPath, "TAP", "Scripts"), "*.cs", SearchOption.AllDirectories);
            foreach (var file in scripts)
                foreach (System.Text.RegularExpressions.Match m in asked.Matches(File.ReadAllText(file)))
                    Assert.IsNotNull(ControlTable.Find(m.Groups[1].Value), $"{Path.GetFileName(file)} asks for {m.Groups[1].Value}, which is not in the table");
        }

        [Test]
        public void AStick_HasItsDeadZoneSensitivityAndInversion()
        {
            var pad = InputSystem.AddDevice<Gamepad>();
            Set(pad.leftStick, new Vector2(0f, 0.05f));
            Assert.AreEqual(0f, Controls.Axis("Flight/PitchAxis"), "inside the dead zone");
            Set(pad.leftStick, new Vector2(0f, 1f));
            Assert.AreEqual(1f, Controls.Axis("Flight/PitchAxis"), 1e-4f, "full travel is full input");
            GameSettings.Current.input.Axis("PitchAxis").invert = true;
            Assert.AreEqual(-1f, Controls.Axis("Flight/PitchAxis"), 1e-4f);
            GameSettings.Current.input.Axis("PitchAxis").invert = false;
            GameSettings.Current.input.Axis("PitchAxis").sensitivity = 0.5f;
            Assert.AreEqual(0.5f, Controls.Axis("Flight/PitchAxis"), 1e-4f);
        }

        [Test]
        public void Trim_HoldsAPitchWithoutInput()
        {
            var c = new VesselControl();
            for (int i = 0; i < 25; i++) FlightInput.ApplyAttitude(c, 1f, 0f, 0f, trimming: true, precision: false, dt: 0.1f); // 2.5 s with the modifier held
            Assert.AreEqual(0.5f, c.TrimPitch, 1e-4f);
            Assert.AreEqual(0.5f, c.Pitch, 1e-4f, "trimming does not also steer");
            FlightInput.ApplyAttitude(c, 0f, 0f, 0f, trimming: false, precision: false, dt: 0.1f); // keys released
            Assert.AreEqual(0.5f, c.Pitch, 1e-4f, "the trim holds the pitch");
            FlightInput.ApplyAttitude(c, -1f, 0f, 0f, trimming: false, precision: false, dt: 0.1f);
            Assert.AreEqual(-0.5f, c.Pitch, 1e-4f, "the pilot still steers on top of it");
        }

        [Test]
        public void Precision_CutsTheAttitudeInputToAQuarter()
        {
            var c = new VesselControl();
            FlightInput.ApplyAttitude(c, 1f, -1f, 0.5f, trimming: false, precision: true, dt: 0.02f);
            Assert.AreEqual(0.25f, c.Pitch, 1e-4f);
            Assert.AreEqual(-0.25f, c.Yaw, 1e-4f);
            Assert.AreEqual(0.125f, c.Roll, 1e-4f);
        }

        [Test]
        public void Trim_IsSavedWithTheVessel()
        {
            var c = new VesselControl { TrimPitch = 0.3f, TrimYaw = -0.2f, TrimRoll = 0.1f };
            var back = new VesselControl();
            back.FromRecord(SaveStorage.FromJson<ControlRecord>(SaveStorage.ToJson(c.ToRecord())));
            Assert.AreEqual((0.3f, -0.2f, 0.1f), (back.TrimPitch, back.TrimYaw, back.TrimRoll));
        }
    }
}
