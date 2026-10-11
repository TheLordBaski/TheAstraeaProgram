using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TAP.Game;

namespace TAP.Tests
{
    /// <summary>FND-10: the string tables, the pseudo-locale, units and the helpers.</summary>
    public class LocalizationTests
    {
        [SetUp]
        public void SetUp() { GameSettings.Forget(); Loc.Reload(); Loc.Missing.Clear(); }

        [TearDown]
        public void TearDown() { Loc.Audit = false; GameSettings.Forget(); Loc.Reload(); }

        private static void Use(string language) { GameSettings.Current.gameplay.language = language; }

        [Test]
        public void Numbers_AreCutOutOfTheLookup_ButCodesAndNamesAreNot()
        {
            Loc.Audit = true; // names are marked only while a translation or audit is running
            Assert.AreEqual("Surface {0} m/s   Orbital {1} m/s", Loc.PatternOf("Surface 16.9 m/s   Orbital 175.0 m/s"));
            Assert.AreEqual("Hornet LV-215 Engine", Loc.PatternOf("Hornet LV-215 Engine"), "a part code is part of the name");
            Assert.AreEqual("Stage {0}  Δv {1} m/s", Loc.PatternOf("Stage 3  Δv 1,332 m/s"));
            Assert.AreEqual("{0}-{1}-{2}", Loc.PatternOf("2026-10-12"));
            Assert.AreEqual("T-0 (on pad)", Loc.PatternOf("T-0 (on pad)"));
            Assert.AreEqual("<color=#52C7FF>{0} km</color>", Loc.PatternOf("<color=#52C7FF>70 km</color>"), "tags are kept as they are");
            var args = new List<string>();
            Assert.AreEqual("{0} — Prelaunch on Tellus", Loc.PatternOf(Loc.Name("Meridian Orbiter") + " — Prelaunch on Tellus", args));
            Assert.AreEqual("Meridian Orbiter", args[0]);
        }

        [Test]
        public void LongProse_KeepsItsNumbers()
        {
            string prose = "Orbit takes about 3,400 m/s; a trip to a Luma landing and back needs roughly 6,000 to 6,300 m/s plus margin in total.";
            Assert.AreEqual(prose, Loc.PatternOf(prose));
        }

        [Test]
        public void PseudoLocale_KeepsTagsAndHoles_Accents_AndAddsAbout40Percent()
        {
            Use(Loc.Pseudo);
            string shown = Loc.Translate("<b>Pitch down</b> {0}".Replace("{0}", "12"));
            StringAssert.StartsWith("[", shown);
            StringAssert.EndsWith("]", shown);
            StringAssert.Contains("<b>", shown);
            StringAssert.Contains("</b>", shown);
            StringAssert.Contains("12", shown, "the number is put back");
            StringAssert.DoesNotContain("Pitch", shown);
            int letters = "Pitch down".Count(char.IsLetter);
            Assert.GreaterOrEqual(shown.Count(c => c == '~'), (int)(letters * 0.4), "padding for longer languages");
            Assert.AreEqual("Pitch down", Loc.Plain("Pitch down"));
        }

        [Test]
        public void NamesAreNotMarked_WhenNothingTranslates()
        {
            Assert.AreEqual("Meridian Orbiter", Loc.Name("Meridian Orbiter"), "no markers where no pass would take them out");
            Assert.AreEqual("Kodiak SR-300", Loc.Data("Kodiak SR-300"));
        }

        [Test]
        public void ANameOfThePlayer_IsNotTranslated()
        {
            Use(Loc.Pseudo);
            string shown = Loc.Translate("Loaded " + Loc.Name("Meridian Orbiter"));
            StringAssert.Contains("Meridian Orbiter", shown);
            StringAssert.DoesNotContain("Loaded", shown);
            Assert.IsFalse(shown.Contains('\uE000'), "the markers are taken out");
        }

        [Test]
        public void ALanguageFile_NeedsNoCodeChange_AndMayMoveTheNumbers()
        {
            Assert.IsTrue(Loc.LoadTable("xx", "{ \"name\": \"Testish\", \"culture\": \"de-DE\", \"strings\": { \"Surface {0} m/s   Orbital {1} m/s\": \"Orbit {1} m/s | Boden {0} m/s\", \"Pitch down\": \"Nase runter\" } }"));
            Use("xx");
            Assert.AreEqual("Nase runter", Loc.Translate("Pitch down"));
            Assert.AreEqual("Orbit 175.0 m/s | Boden 16.9 m/s", Loc.Translate("Surface 16.9 m/s   Orbital 175.0 m/s"));
            Assert.AreEqual("Not in the table", Loc.Translate("Not in the table"), "a text without a translation stays English");
            Assert.AreEqual("1.234,5", Loc.Number(1234.5, 1), "numbers follow the language's culture");
            Assert.IsTrue(Loc.AllLanguages.Any(l => l.code == "xx" && l.name == "Testish"));
        }

        [Test]
        public void AuditListsATextTheTableDoesNotKnow_AndNotOneItDoes()
        {
            Loc.Audit = true;
            Loc.Translate("Pitch down");
            Loc.Translate("A text that is certainly not in the table, nor ever will be");
            CollectionAssert.DoesNotContain(Loc.Missing, "Pitch down");
            CollectionAssert.Contains(Loc.Missing, "A text that is certainly not in the table, nor ever will be");
        }

        [Test]
        public void TheEnglishTable_HasEveryTextOfTheScreenFilesAndTheGameData()
        {
            var known = Loc.EnglishStrings;
            var missing = LocalizationCatalog.FromFilesAndData().Where(t => !known.ContainsKey(t)).ToList();
            Assert.IsEmpty(missing, "TAP → Localization → Update English Table adds these:\n" + string.Join("\n", missing.Take(20)));
        }

        [Test]
        public void Imperial_ConvertsTheNumbersInFrontOfAUnit_AsTheTextIsShown()
        {
            GameSettings.Current.gameplay.units = UnitSystem.Imperial;
            Assert.AreEqual("Altitude 262 ft  55.4 ft/s  79,366 lb", Loc.Translate("Altitude 80 m  16.9 m/s  36,000 kg"));
            Assert.AreEqual("12.4 mi", Loc.Translate("20.0 km"));
            Assert.AreEqual("in 5m 30s", Loc.Translate("in 5m 30s"), "minutes and seconds are not metres");
            Assert.AreEqual("Every 5 minutes", Loc.Translate("Every 5 minutes"));
            GameSettings.Current.gameplay.units = UnitSystem.Metric;
            Assert.AreEqual("80 m", Loc.Translate("80 m"));
        }

        [Test]
        public void Plurals_AndDates_FollowTheLanguage()
        {
            Assert.AreEqual("1 vessel", Loc.Plural(1, "vessel", "vessels"));
            Assert.AreEqual("0 vessels", Loc.Plural(0, "vessel", "vessels"));
            Assert.AreEqual("3 vessels", Loc.Plural(3, "vessel", "vessels"));
            Assert.AreEqual("10/12/2026", Loc.Date(new System.DateTime(2026, 10, 12)));
        }
    }
}
