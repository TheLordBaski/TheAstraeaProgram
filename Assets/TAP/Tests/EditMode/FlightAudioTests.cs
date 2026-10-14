using NUnit.Framework;
using TAP.Game;
using TAP.Persistence;

namespace TAP.Tests
{
    public class FlightAudioTests
    {
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
