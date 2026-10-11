using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TAP.Game
{
    public enum FlightEntry { Launch, Resume, Quickload }

    /// <summary>
    /// Scene-spanning game state: the persistent save (universe, crew), the craft being edited, and
    /// snapshots used for "revert" actions. Pure data + scene transitions; no simulation logic.
    /// </summary>
    public static class GameSession
    {
        public const string MenuScene = "MainMenu";
        public const string HubScene = "SpaceCenter";
        public const string EditorScene = "Assembly";
        public const string FlightScene = "Flight";

        public static GameSave Save;
        public static CraftDesign EditorCraft;
        public static CraftDesign PendingLaunch;
        public static GameSave PreLaunchSnapshot;   // before launch (revert to assembly)
        public static GameSave LaunchSnapshot;      // right after launch (restart flight)
        public static FlightEntry Entry = FlightEntry.Resume;
        public static string PendingMessage;

        /// <summary>
        /// Starts a scene load behind the loading screen and returns true; set by TAP.UI when it starts. When null (tests,
        /// before the UI exists) scenes load at once.
        /// </summary>
        public static Func<string, bool> AsyncLoader;
        /// <summary>The scene most recently asked for (the autotests check where a flow ended).</summary>
        public static string RequestedScene { get; private set; }

        /// <summary>Automated verification run requested from the command line (e.g. -autotest lunar).</summary>
        public static string AutoTest;
        public static string AutoTestReportPath;
        /// <summary>Craft file (or starter craft name) flown by the "ascent" test instead of the mission default.</summary>
        public static string AutoTestCraft;

        public static readonly string[] DefaultCrew =
        {
            "Ada Kestrel", "Bram Holloway", "Cyra Moss", "Dov Linden", "Esme Varga", "Finn Arden", "Gwen Tallis", "Hal Okafor",
        };

        public static bool HasGame => Save != null;

        public static void NewGame(string name = "Sandbox", GameMode mode = GameMode.Sandbox)
        {
            var sys = CelestialSystem.Default;
            Save = new GameSave { saveName = name, mode = mode, ut = sys.Def.startUT, scene = "editor" };
            foreach (var c in DefaultCrew) Save.crew.Add(new CrewRecord { name = c });
            EditorCraft = null;
            Log($"New {mode.ToString().ToLowerInvariant()} game started");
        }

        public static void EnsureGame()
        {
            if (Save == null) NewGame();
        }

        public static void Log(string msg)
        {
            if (Save == null) return;
            Save.missionLog.Add($"[{MathD.FormatUT(Save.ut)}] {msg}");
            if (Save.missionLog.Count > 400) Save.missionLog.RemoveAt(0);
        }

        // ------------------------------------------------------------------ crew

        public static CrewRecord Crew(string name) => Save?.crew.Find(c => c.name == name);

        public static List<string> TakeAvailableCrew(int count, string vesselId)
        {
            var list = new List<string>();
            if (Save == null) return list;
            foreach (var c in Save.crew)
            {
                if (list.Count >= count) break;
                if (c.status != CrewStatus.Available) continue;
                c.status = CrewStatus.Assigned;
                c.vesselId = vesselId;
                list.Add(c.name);
            }
            return list;
        }

        public static void SetCrewStatus(string name, CrewStatus status, string vesselId)
        {
            var c = Crew(name);
            if (c == null) return;
            c.status = status;
            c.vesselId = vesselId;
        }

        /// <summary>Makes roster statuses consistent with the vessels in the save.</summary>
        public static void ReconcileCrew()
        {
            if (Save == null) return;
            var aboard = new Dictionary<string, (string vessel, bool eva)>();
            foreach (var v in Save.vessels)
            {
                if (v.kind == VesselKind.EVA && !string.IsNullOrEmpty(v.evaCrew)) aboard[v.evaCrew] = (v.id, true);
                foreach (var p in v.parts)
                    if (p.crew != null) foreach (var c in p.crew) aboard[c] = (v.id, false);
            }
            foreach (var c in Save.crew)
            {
                if (c.status == CrewStatus.Lost) continue;
                if (aboard.TryGetValue(c.name, out var a))
                {
                    c.status = a.eva ? CrewStatus.EVA : CrewStatus.Assigned;
                    c.vesselId = a.vessel;
                }
                else
                {
                    c.status = CrewStatus.Available;
                    c.vesselId = null;
                }
            }
        }

        // ------------------------------------------------------------------ persistence

        /// <summary>Counts real time played into the save; the flight and assembly scenes call it every frame.</summary>
        public static void TickPlayTime(float seconds)
        {
            if (Save != null) Save.playTime += seconds;
        }

        public static void WritePersistent()
        {
            if (Save == null) return;
            try { SaveStorage.WriteSave(Save, "persistent"); }
            catch (Exception e) { Debug.LogError("Failed to write save: " + e.Message); }
        }

        public static bool LoadPersistent(string saveName)
        {
            var s = SaveStorage.ReadSave(saveName, "persistent", out string notice);
            if (s == null) return false;
            if (notice != null) PendingMessage = notice;
            Save = s;
            EditorCraft = s.editorCraft != null ? SaveStorage.DeepClone(s.editorCraft) : null;
            ReconcileCrew();
            return true;
        }

        // ------------------------------------------------------------------ scene flow

        public static void LoadScene(string scene)
        {
            Time.timeScale = 1;
            RequestedScene = scene;
            if (AsyncLoader == null || !AsyncLoader(scene)) SceneManager.LoadScene(scene);
        }

        public static void GoToMenu() => LoadScene(MenuScene);

        /// <summary>The space center: the root of every game loop. Saves first, so the hub always shows what the last scene left.</summary>
        public static void GoToHub()
        {
            if (Save != null) Save.scene = "hub";
            WritePersistent();
            LoadScene(HubScene);
        }

        public static void GoToEditor()
        {
            if (Save != null) Save.scene = "editor";
            WritePersistent();
            LoadScene(EditorScene);
        }

        public static void Launch(CraftDesign design)
        {
            EnsureGame();
            PendingLaunch = SaveStorage.DeepClone(design);
            EditorCraft = SaveStorage.DeepClone(design);
            PreLaunchSnapshot = SaveStorage.DeepClone(Save);
            Entry = FlightEntry.Launch;
            LoadScene(FlightScene);
        }

        public static void ResumeFlight()
        {
            Entry = FlightEntry.Resume;
            LoadScene(FlightScene);
        }

        /// <summary>Flies a vessel of the save (tracking station, pad, assembly building's list of flights).</summary>
        public static void FlyVessel(string vesselId)
        {
            Save.activeVesselId = vesselId;
            ResumeFlight();
        }

        public static void LoadFlightState(GameSave s, FlightEntry entry)
        {
            Save = s;
            ReconcileCrew();
            Entry = entry;
            LoadScene(FlightScene);
        }
    }
}
