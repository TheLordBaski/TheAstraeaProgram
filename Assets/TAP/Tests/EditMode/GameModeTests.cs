using NUnit.Framework;
using TAP.Game;
using TAP.Persistence;

namespace TAP.Tests
{
    /// <summary>What each game mode allows (FND-06).</summary>
    public class GameModeTests
    {
        private GameSave _oldSave;

        [SetUp] public void SetUp() => _oldSave = GameSession.Save;
        [TearDown] public void TearDown() => GameSession.Save = _oldSave;

        [Test]
        public void Sandbox_AllowsEverythingAndOffersNoContracts()
        {
            var r = ModeRules.For(GameMode.Sandbox);
            Assert.IsTrue(r.PartAvailable("eng_hornet"));
            Assert.IsTrue(r.CanAfford(1e12));
            Assert.IsTrue(r.FacilityAllows("assembly", 3));
            Assert.IsTrue(r.CanHireCrew);
            Assert.IsFalse(r.ContractsOn);
        }

        [Test]
        public void Science_GatesPartsByResearchOnly()
        {
            var r = ModeRules.For(GameMode.Science);
            Assert.IsFalse(r.PartAvailable("eng_hornet"), "nothing is researched until the tech state exists");
            Assert.IsTrue(r.CanAfford(1e12));
            Assert.IsTrue(r.FacilityAllows("assembly", 3));
            Assert.IsTrue(r.CanHireCrew);
            Assert.IsFalse(r.ContractsOn);
        }

        [Test]
        public void Career_GatesEverything_AndOpensNothingBeforeItsStateExists()
        {
            var r = ModeRules.For(GameMode.Career);
            Assert.IsFalse(r.PartAvailable("eng_hornet"));
            Assert.IsTrue(r.CanAfford(0));
            Assert.IsFalse(r.CanAfford(1));
            Assert.IsTrue(r.FacilityAllows("assembly", 1));
            Assert.IsFalse(r.FacilityAllows("assembly", 2));
            Assert.IsFalse(r.CanHireCrew);
            Assert.IsTrue(r.ContractsOn);
        }

        [Test]
        public void Current_FollowsTheGameBeingPlayed()
        {
            GameSession.Save = null;
            Assert.AreSame(ModeRules.Sandbox, ModeRules.Current, "no game loaded");
            foreach (GameMode mode in System.Enum.GetValues(typeof(GameMode)))
            {
                GameSession.Save = new GameSave { mode = mode };
                Assert.AreSame(ModeRules.For(mode), ModeRules.Current);
                Assert.AreEqual(mode, ModeRules.Current.Mode);
            }
        }
    }
}
