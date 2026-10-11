using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using TAP.Game;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.TestTools;

namespace TAP.Tests
{
    /// <summary>FND-12: the game log, unhandled exceptions and the problem report.</summary>
    public class BugReportTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "tap_report_" + Guid.NewGuid().ToString("N"));
            SaveStorage.RootOverride = _root;
            GameSettings.Forget();
            GameLog.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameLog.ResetForTests();
            SaveStorage.RootOverride = null;
            GameSession.Save = null;
            GameSession.LiveSaveProvider = null;
            GameSettings.Forget();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        /// <summary>The log is still open for writing: read it as another program (a bug report) would.</summary>
        private static string ReadLog()
        {
            using (var fs = new FileStream(GameLog.CurrentFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var r = new StreamReader(fs)) return r.ReadToEnd();
        }

        private static string[] Entries(string zip)
        {
            using (var z = ZipFile.OpenRead(zip)) return z.Entries.Select(e => e.FullName).OrderBy(n => n).ToArray();
        }

        private static string Read(string zip, string entry)
        {
            using (var z = ZipFile.OpenRead(zip))
            using (var r = new StreamReader(z.GetEntry(entry).Open())) return r.ReadToEnd();
        }

        [Test]
        public void TheLog_StartsWithTheVersionMachineAndSettings_AndHoldsWhatUnityLogs()
        {
            GameLog.Begin();
            Debug.Log("a line for the log");
            Debug.LogWarning("a warning for the log");
            GameLog.Flush();
            string text = ReadLog();
            StringAssert.Contains("The Astraea Program", text);
            StringAssert.Contains("OS:", text);
            StringAssert.Contains("GPU:", text);
            StringAssert.Contains("Settings:", text);
            StringAssert.Contains("[Log] a line for the log", text);
            StringAssert.Contains("[Warning] a warning for the log", text);
        }

        [Test]
        public void TheLogs_Rotate_AndTheLastFiveAreKept()
        {
            Directory.CreateDirectory(GameLog.Folder);
            for (int i = 0; i < 8; i++) File.WriteAllText(Path.Combine(GameLog.Folder, $"tap-20260101-00000{i}.log"), "old");
            GameLog.Begin();
            var files = Directory.GetFiles(GameLog.Folder, "tap-*.log");
            Assert.AreEqual(GameLog.KeptFiles, files.Length, "four old ones and this run's");
            Assert.IsFalse(files.Any(f => f.EndsWith("000000.log")), "the oldest are the ones that go");
        }

        [Test]
        public void AnUnhandledException_IsLoggedWithItsStack_AndRaisedOnceAMinute()
        {
            GameLog.Begin();
            int raised = 0;
            Action<GameLog.Entry> count = e => raised++;
            GameLog.ExceptionRaised += count;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("forced for the test"));
                    try { throw new InvalidOperationException("forced for the test"); } catch (Exception e) { Debug.LogException(e); }
                }
                GameLog.Pump();
            }
            finally { GameLog.ExceptionRaised -= count; }
            Assert.AreEqual(1, raised, "three times the same exception is one notice");
            Assert.AreEqual(3, GameLog.Exceptions.Count);
            GameLog.Flush();
            StringAssert.Contains("forced for the test", ReadLog());
        }

        [Test]
        public void TheReport_HoldsEverythingNeededToReproduce()
        {
            GameSession.NewGame("Report Test");
            GameSession.WritePersistent();
            GameSettings.Current.audio.master = 0.3f;
            GameSettings.Save();
            GameLog.Begin();
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("rocket exploded"));
            Debug.LogException(new InvalidOperationException("rocket exploded"));
            GameLog.Pump();
            Directory.CreateDirectory(Path.Combine(_root, "Mods"));
            Directory.CreateDirectory(Path.Combine(_root, "Mods", "FancyEngines"));

            string zip = BugReport.Create("The engines would not start.", new byte[] { 1, 2, 3 });

            var names = Entries(zip);
            Assert.IsTrue(names.Any(n => n.StartsWith("logs/tap-") && n.EndsWith(".log")), "the log: " + string.Join(", ", names));
            CollectionAssert.IsSupersetOf(names, new[] { "save/current-state.json", "save/persistent.json", "settings.json", "screenshot.png", "mods.txt", "system.txt", "report.txt" });
            StringAssert.Contains("Report Test", Read(zip, "save/current-state.json"), "the save as it is now");
            StringAssert.Contains("0.3", Read(zip, "settings.json"));
            StringAssert.Contains("FancyEngines", Read(zip, "mods.txt"));
            string report = Read(zip, "report.txt");
            StringAssert.Contains("The engines would not start.", report);
            StringAssert.Contains("rocket exploded", report, "the exception is named");
            StringAssert.Contains("rocket exploded", Read(zip, names.First(n => n.StartsWith("logs/tap-"))), "and is in the log with its stack");
        }

        [Test]
        public void TheReport_UsesTheFlightsLiveState_WhenThereIsOne()
        {
            GameSession.NewGame("Live");
            GameSession.LiveSaveProvider = () => new GameSave { saveName = "Live", ut = 12345 };
            string zip = BugReport.Create("");
            StringAssert.Contains("12345", Read(zip, "save/current-state.json"));
        }

        [Test]
        public void TheReport_WorksWithNothingToReport()
        {
            // main menu, no game, no log started: it still makes a file saying what it could
            string zip = BugReport.Create("");
            var names = Entries(zip);
            CollectionAssert.Contains(names, "report.txt");
            CollectionAssert.Contains(names, "system.txt");
        }
    }
}
