using System;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>Save format versions, migration, atomic writes, backups and recovery (FND-05).</summary>
    public class SaveTests
    {
        private string _dir, _oldRoot;

        [SetUp]
        public void SetUp()
        {
            _oldRoot = SaveStorage.RootOverride;
            _dir = Path.Combine(Path.GetTempPath(), "tap-save-test-" + Guid.NewGuid().ToString("N"));
            SaveStorage.RootOverride = _dir;
        }

        [TearDown]
        public void TearDown()
        {
            SaveStorage.RootOverride = _oldRoot;
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        private static GameSave Make(double ut) => new GameSave { saveName = "Test", ut = ut, vessels = { new VesselRecord { name = "V" } } };

        /// <summary>Writes the save, then waits so the next backup gets a name of its own.</summary>
        private static void Write(double ut)
        {
            SaveStorage.WriteSave(Make(ut), "persistent");
            Thread.Sleep(5);
        }

        private static JObject Raw(string json)
        {
            using (var r = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
                return JObject.Load(r);
        }

        // ------------------------------------------------------------------ migration and the corpus

        [Test]
        public void Corpus_EverySaveLoadsWithTheSameVesselsCrewAndPositions()
        {
            string folder = Path.Combine(Application.dataPath, "TAP", "Tests", "Saves");
            var files = Directory.GetFiles(folder, "*.json");
            Assert.IsNotEmpty(files, "the corpus is empty: " + folder);
            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string text = File.ReadAllText(file);
                var raw = Raw(text);
                var save = SaveStorage.ParseSave(text);

                Assert.AreEqual(SaveMigration.Current, save.version, name);
                Assert.AreEqual((double)raw["ut"], save.ut, name);
                var rawVessels = (JArray)raw["vessels"];
                Assert.AreEqual(rawVessels.Count, save.vessels.Count, name + ": vessels");
                for (int i = 0; i < rawVessels.Count; i++)
                {
                    var rv = rawVessels[i];
                    var v = save.vessels[i];
                    Assert.AreEqual((string)rv["id"], v.id, name);
                    Assert.AreEqual((string)rv["name"], v.name, name);
                    Assert.AreEqual(((JArray)rv["parts"]).Count, v.parts.Count, name + ": " + v.name + " parts");
                    Assert.AreEqual(Doubles(rv["orbitPos"]), v.orbitPos, name + ": " + v.name + " orbit position");
                    Assert.AreEqual(Doubles(rv["orbitVel"]), v.orbitVel, name + ": " + v.name + " orbit velocity");
                    Assert.AreEqual(Doubles(rv["landedPos"]), v.landedPos, name + ": " + v.name + " landed position");
                }
                Assert.AreEqual(((JArray)raw["crew"]).Select(c => (string)c["name"]), save.crew.Select(c => c.name), name + ": crew");
                Assert.AreEqual(((JArray)raw["crew"]).Select(c => (string)c["status"]), save.crew.Select(c => c.status.ToString()), name + ": crew status");
                Assert.AreEqual("sandbox", save.mode, name);
            }
        }

        private static double[] Doubles(JToken t) => t?.Select(x => (double)x).ToArray();

        [Test]
        public void Migration_UpgradesAFormat1SaveAndANewSaveIsCurrent()
        {
            var save = SaveStorage.ParseSave("{\"version\":1,\"saveName\":\"Old\",\"ut\":100}");
            Assert.AreEqual(SaveMigration.Current, save.version);
            Assert.AreEqual("sandbox", save.mode);
            Assert.AreEqual(0, save.playTime);
            Assert.AreEqual(SaveMigration.Current, new GameSave().version);
            // A file with no version at all is the oldest format.
            Assert.AreEqual(SaveMigration.Current, SaveStorage.ParseSave("{\"ut\":5}").version);
        }

        [Test]
        public void Migration_RefusesASaveFromANewerGame()
        {
            string json = "{\"version\":" + (SaveMigration.Current + 1) + ",\"ut\":1}";
            Assert.Throws<SaveTooNewException>(() => SaveStorage.ParseSave(json));
        }

        [Test]
        public void Migration_KeepsTextThatLooksLikeADate()
        {
            var save = SaveStorage.ParseSave("{\"version\":1,\"missionLog\":[\"2026-10-11T12:00:00Z\"]}");
            Assert.AreEqual("2026-10-11T12:00:00Z", save.missionLog[0]);
        }

        // ------------------------------------------------------------------ writing

        [Test]
        public void Write_ARoundTripKeepsTheSave()
        {
            var save = Make(123456.789012345);
            save.playTime = 3600.5;
            save.mode = "sandbox";
            SaveStorage.WriteSave(save, "persistent");
            var back = SaveStorage.ReadSave("Test", "persistent");
            Assert.AreEqual(123456.789012345, back.ut);
            Assert.AreEqual(3600.5, back.playTime);
            Assert.AreEqual("V", back.vessels[0].name);
        }

        [Test]
        public void Write_AnInterruptedWriteLeavesTheOldSave()
        {
            Write(1);
            // A crash after the temporary file was started but before it replaced the save.
            string path = SaveStorage.SlotPath("Test", "persistent");
            File.WriteAllText(path + ".tmp", File.ReadAllText(path).Substring(0, 40));
            Assert.AreEqual(1, SaveStorage.ReadSave("Test", "persistent", out string notice).ut);
            Assert.IsNull(notice);
            // The next save goes through.
            Write(2);
            Assert.AreEqual(2, SaveStorage.ReadSave("Test", "persistent").ut);
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void Backups_KeepTheLastFiveAndDropTheOldest()
        {
            for (int i = 1; i <= 9; i++) Write(i);
            var backups = SaveStorage.Backups("Test");
            Assert.AreEqual(SaveStorage.KeptBackups, backups.Count);
            // Newest first: the save before the last one is 8, the oldest kept is 4.
            Assert.AreEqual(8, SaveStorage.ParseSave(File.ReadAllText(backups[0])).ut);
            Assert.AreEqual(4, SaveStorage.ParseSave(File.ReadAllText(backups[4])).ut);
        }

        [Test]
        public void Backups_AreOnlyForThePersistentSave()
        {
            SaveStorage.WriteSave(Make(1), "quicksave");
            SaveStorage.WriteSave(Make(2), "quicksave");
            Assert.IsEmpty(SaveStorage.Backups("Test"));
        }

        // ------------------------------------------------------------------ damage

        [Test]
        public void Damaged_ASaveIsRestoredFromTheNewestGoodBackup()
        {
            for (int i = 1; i <= 3; i++) Write(i);
            string path = SaveStorage.SlotPath("Test", "persistent");
            File.WriteAllText(path, File.ReadAllText(path).Substring(0, 30));

            var save = SaveStorage.ReadSave("Test", "persistent", out string notice);
            Assert.AreEqual(2, save.ut, "the backup made when save 3 was written");
            StringAssert.Contains("damaged", notice);
            Assert.IsTrue(File.Exists(path + ".damaged"));
            Assert.AreEqual(2, SaveStorage.ReadSave("Test", "persistent", out notice).ut, "the file on disk is good again");
            Assert.IsNull(notice);
        }

        [Test]
        public void Damaged_ABackupThatIsDamagedToo_IsSkipped()
        {
            for (int i = 1; i <= 3; i++) Write(i);
            File.WriteAllText(SaveStorage.Backups("Test")[0], "{ nope");
            File.WriteAllText(SaveStorage.SlotPath("Test", "persistent"), "");
            Assert.AreEqual(1, SaveStorage.ReadSave("Test", "persistent", out _).ut);
        }

        [Test]
        public void Damaged_WithNoBackup_Throws()
        {
            Write(1);
            File.WriteAllText(SaveStorage.SlotPath("Test", "persistent"), "{ \"ut\": ");
            Assert.Throws<SaveException>(() => SaveStorage.ReadSave("Test", "persistent"));
        }

        [Test]
        public void Damaged_ANewerSaveIsNotReplacedByABackup()
        {
            Write(1);
            Write(2);
            string path = SaveStorage.SlotPath("Test", "persistent");
            string newer = "{\"version\":" + (SaveMigration.Current + 1) + ",\"saveName\":\"Test\"}";
            File.WriteAllText(path, newer);
            Assert.Throws<SaveTooNewException>(() => SaveStorage.ReadSave("Test", "persistent"));
            Assert.AreEqual(newer, File.ReadAllText(path), "the newer game's save is left alone");
        }

        [Test]
        public void Damaged_AFileIsNeverRotatedIntoTheBackups()
        {
            Write(1);
            Write(2);
            string path = SaveStorage.SlotPath("Test", "persistent");
            File.WriteAllText(path, "{ broken");
            int before = SaveStorage.Backups("Test").Count;
            Write(3);   // would normally back up the file it replaces
            Assert.AreEqual(before, SaveStorage.Backups("Test").Count);
        }
    }
}
