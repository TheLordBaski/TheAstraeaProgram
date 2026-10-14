using NUnit.Framework;
using TAP.Game;
using TAP.Persistence;

namespace TAP.Tests
{
    public class FlightAudioTests
    {
        [Test]
        public void BuiltBanksProvideAllMusicLayersAndExpansionFoley()
        {
            Assert.AreEqual(FMOD.RESULT.OK, FMOD.Studio.System.create(out var studio));
            try
            {
                Assert.AreEqual(FMOD.RESULT.OK, studio.getCoreSystem(out var core));
                Assert.AreEqual(FMOD.RESULT.OK, core.setOutput(FMOD.OUTPUTTYPE.NOSOUND));
                Assert.AreEqual(FMOD.RESULT.OK, studio.initialize(64, FMOD.Studio.INITFLAGS.SYNCHRONOUS_UPDATE, FMOD.INITFLAGS.NORMAL, System.IntPtr.Zero));
                string directory = UnityEngine.Application.streamingAssetsPath + "/FMOD/";
                foreach (string bank in new[] { "Master", "Master.strings", "Flight" })
                    Assert.AreEqual(FMOD.RESULT.OK, studio.loadBankFile(directory + bank + ".bank", FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out _), bank);
                foreach (string name in new[] { "Flight", "FarHorizon", "NightDrift" })
                {
                    Assert.AreEqual(FMOD.RESULT.OK, studio.getEvent("event:/Music/" + name, out var music), name);
                    Assert.AreEqual(FMOD.RESULT.OK, music.isOneshot(out bool oneshot));
                    Assert.IsFalse(oneshot, name);
                    foreach (string layer in new[] { "Pad", "Strings", "Cello", "Horns", "Celeste", "Pizzicato", "Pulse" })
                        Assert.AreEqual(FMOD.RESULT.OK, music.getParameterDescriptionByName(layer, out _), name + "/" + layer);
                    Assert.AreEqual(FMOD.RESULT.OK, music.createInstance(out var instance));
                    Assert.AreEqual(FMOD.RESULT.OK, instance.setParameterByName("Pad", .5f));
                    Assert.AreEqual(FMOD.RESULT.OK, instance.start());
                    Assert.AreEqual(FMOD.RESULT.OK, studio.update());
                    Assert.AreEqual(FMOD.RESULT.OK, studio.flushCommands());
                    Assert.AreEqual(FMOD.RESULT.OK, instance.getPlaybackState(out var state));
                    Assert.AreEqual(FMOD.Studio.PLAYBACK_STATE.PLAYING, state, name);
                    instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    instance.release();
                }
                foreach (string name in new[] { "Dock_Latch_01", "Dock_Latch_02", "Undock_Release_01", "Undock_Release_02",
                    "Leg_Servo_01", "Leg_Servo_02", "Hatch_Open", "Hatch_Close", "Step_Metal_01", "Step_Metal_02", "Step_Metal_03",
                    "Step_Surface_01", "Step_Surface_02", "Step_Surface_03", "Jetpack_Loop" })
                {
                    Assert.AreEqual(FMOD.RESULT.OK, studio.getEvent("event:/Vessel/" + name, out var effect), name);
                    Assert.AreEqual(FMOD.RESULT.OK, effect.isOneshot(out bool oneshot));
                    Assert.AreEqual(name != "Jetpack_Loop", oneshot, name);
                }
            }
            finally { studio.release(); }
        }

        [TestCase(Situation.Prelaunch, true, 0f, 0, 0, 0f, false, FlightAudioPhase.Standby)]
        [TestCase(Situation.Flying, true, .1f, 4000, 200, 0f, false, FlightAudioPhase.Launch)]
        [TestCase(Situation.SubOrbital, true, .5f, 35000, 700, 0f, false, FlightAudioPhase.HighAtmosphere)]
        [TestCase(Situation.Orbiting, false, 2f, 100000, 0, 0f, false, FlightAudioPhase.Orbit)]
        [TestCase(Situation.Orbiting, false, 2f, 100000, 0, 0f, true, FlightAudioPhase.Transit)]
        [TestCase(Situation.SubOrbital, false, 2f, 100000, 0, 0f, false, FlightAudioPhase.Transit)]
        [TestCase(Situation.Escaping, false, 2f, 100000, 100, 0f, false, FlightAudioPhase.Transit)]
        [TestCase(Situation.Flying, false, 0f, 1000, -10, 0f, false, FlightAudioPhase.Touchdown)]
        [TestCase(Situation.SubOrbital, false, 0f, 1000, 30, 0f, false, FlightAudioPhase.Launch)]
        [TestCase(Situation.Landed, false, 0f, 0, 0, 0f, false, FlightAudioPhase.Touchdown)]
        [TestCase(Situation.Splashed, true, 0f, 0, 0, 0f, false, FlightAudioPhase.Touchdown)]
        [TestCase(Situation.SubOrbital, true, .4f, 25000, -700, .5f, false, FlightAudioPhase.Reentry)]
        [TestCase(Situation.Flying, true, .01f, 500, -700, .5f, false, FlightAudioPhase.Reentry)]
        [TestCase(Situation.Flying, true, .1f, 4000, 200, .5f, false, FlightAudioPhase.Launch)]
        public void ChoosesFlightContext(Situation situation, bool air, float fraction, double agl,
            double verticalSpeed, float heat, bool star, FlightAudioPhase expected)
        {
            Assert.AreEqual(expected, FlightAudio.SelectPhase(situation, air, fraction, agl, verticalSpeed, heat, star));
        }

        [Test]
        public void AtmosphericAndHeatingThresholdsHaveHysteresis()
        {
            Assert.AreEqual(FlightAudioPhase.Launch, FlightAudio.SelectPhase(Situation.Flying, true, .33f, 23000, 100, 0, false));
            Assert.AreEqual(FlightAudioPhase.HighAtmosphere, FlightAudio.SelectPhase(Situation.Flying, true, .33f, 23000, 100, 0, false, FlightAudioPhase.HighAtmosphere));
            Assert.AreEqual(FlightAudioPhase.HighAtmosphere, FlightAudio.SelectPhase(Situation.SubOrbital, true, .5f, 35000, -100, .03f, false));
            Assert.AreEqual(FlightAudioPhase.Reentry, FlightAudio.SelectPhase(Situation.SubOrbital, true, .5f, 35000, -100, .03f, false, FlightAudioPhase.Reentry));
        }
    }
}
