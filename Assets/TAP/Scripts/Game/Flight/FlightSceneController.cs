using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Owns the flight scene: builds the simulation and presentation objects, spawns/resumes vessels,
    /// and implements scene-level actions (quicksave/load, revert, recover, leave).
    /// </summary>
    public sealed class FlightSceneController : MonoBehaviour
    {
        public static FlightSceneController Instance { get; private set; }

        public FlightSim Sim;
        public PlanetManager Planets;
        public FlightEffects Effects;
        public FlightCamera Camera;
        public SkyController Sky;
        public MapView Map;
        public TrajectoryService Trajectory;
        public FlightInput Input;
        public MissionTracker Tracker;
        public event Action<string, bool> Notification;
        public bool UiHidden;
        public event Action<bool> UiVisibilityChanged;

        private void Awake()
        {
            Instance = this;
        }

        public void Build()
        {
            GameSession.EnsureGame();
            var simGo = new GameObject("FlightSim");
            Sim = simGo.AddComponent<FlightSim>();
            Effects = new GameObject("Effects").AddComponent<FlightEffects>();
            Effects.Sim = Sim;
            Sim.Effects = Effects;

            Camera = FlightCamera.Create(Sim);
            Planets = new GameObject("Planets").AddComponent<PlanetManager>();
            var terrainMat = Resources.Load<Material>("Materials/Terrain");
            Planets.Init(Sim, Camera.Cam, terrainMat);
            Sim.Planets = Planets;
            Sky = SkyController.Create(Sim, Camera.Cam);
            LaunchSiteBuilder.Build(Sim, Planets);

            Trajectory = new GameObject("Trajectory").AddComponent<TrajectoryService>();
            Trajectory.Sim = Sim;
            Map = new GameObject("MapView").AddComponent<MapView>();
            Map.Init(Sim, Trajectory);

            Tracker = gameObject.AddComponent<MissionTracker>();
            Tracker.Scene = this;

            Input = gameObject.AddComponent<FlightInput>();
            Input.Sim = Sim;
            Input.Camera = Camera;
            Input.ToggleMap = ToggleMap;
            Input.Quicksave = Quicksave;
            Input.Quickload = Quickload;
            Input.ToggleUi = () => { UiHidden = !UiHidden; UiVisibilityChanged?.Invoke(!UiHidden); };

            Sim.CrewLost += (c, reason) => { GameSession.SetCrewStatus(c, CrewStatus.Lost, null); GameSession.Log($"{c} lost: {reason}"); };
            Sim.CrewStatusChanged += (c, vid) =>
            {
                if (vid == null) GameSession.SetCrewStatus(c, CrewStatus.Available, null);
                else if (vid == "") GameSession.SetCrewStatus(c, CrewStatus.EVA, null);
                else GameSession.SetCrewStatus(c, CrewStatus.Assigned, vid);
            };
            Sim.VesselRecovered += h => GameSession.Log($"Recovered {h.Name}");
            Sim.Message += (m, important) => Notification?.Invoke(m, important);

            StartFlight();
        }

        private void StartFlight()
        {
            var save = GameSession.Save;
            string activeId = save.activeVesselId;
            if (GameSession.Entry == FlightEntry.Launch && GameSession.PendingLaunch != null)
            {
                // Remove any vessel still sitting on the pad (KSP-style: the new launch replaces it).
                ClearLaunchPad(save);
                var rec = LaunchService.CreateLaunchRecord(GameSession.PendingLaunch, PartDatabase.Instance, save.ut);
                save.vessels.Add(rec);
                activeId = rec.id;
                GameSession.Log($"Rolled out {rec.name} to the launch pad");
                GameSession.PendingLaunch = null;
            }
            Sim.Initialize(save.ut, save.vessels, activeId);
            Planets.SyncSurfaceObjects(Sim.UT);
            if (Sim.ActiveVessel == null)
            {
                Notify("No active vessel. Returning to the assembly building.", true);
                GameSession.GoToEditor();
                return;
            }
            if (GameSession.Entry == FlightEntry.Launch)
            {
                SettleOnPad(Sim.ActiveVessel);
                save.activeVesselId = Sim.ActiveVessel.Id;
                GameSession.LaunchSnapshot = CaptureSave();
            }
            save.scene = "flight";
            if (!string.IsNullOrEmpty(GameSession.PendingMessage)) { Notify(GameSession.PendingMessage, true); GameSession.PendingMessage = null; }
        }

        private void ClearLaunchPad(GameSave save)
        {
            var sys = CelestialSystem.Default;
            var site = sys.Def.launchSite;
            var body = sys.Get(site.body);
            Vector3d pad = LaunchService.PadPositionBF(body, site);
            save.vessels.RemoveAll(v =>
            {
                if (!v.landed || v.landedPos == null || v.bodyId != body.Id) return false;
                var p = new Vector3d(v.landedPos[0], v.landedPos[1], v.landedPos[2]);
                bool onPad = (p - pad).magnitude < 60;
                if (onPad)
                {
                    foreach (var pr in v.parts) if (pr.crew != null) foreach (var c in pr.crew) GameSession.SetCrewStatus(c, CrewStatus.Available, null);
                    if (!string.IsNullOrEmpty(v.evaCrew)) GameSession.SetCrewStatus(v.evaCrew, CrewStatus.Available, null);
                    GameSession.Log($"{v.name} was cleared from the launch pad");
                }
                return onPad;
            });
        }

        /// <summary>Moves a freshly spawned vessel so its lowest collider rests exactly on the pad deck.</summary>
        private void SettleOnPad(Vessel v)
        {
            Physics.SyncTransforms();
            Vector3 up = -v.GravityAccel.normalized;
            float lowest = float.MaxValue;
            foreach (var c in v.GetComponentsInChildren<Collider>())
            {
                var b = c.bounds;
                // support along up of the AABB (conservative)
                Vector3 ext = b.extents;
                float s = Vector3.Dot(b.center, up) - (Mathf.Abs(up.x) * ext.x + Mathf.Abs(up.y) * ext.y + Mathf.Abs(up.z) * ext.z);
                lowest = Mathf.Min(lowest, s);
            }
            // Raycast the pad below the vessel's CoM to find the deck height.
            Vector3 com = v.WorldCoM;
            if (Physics.Raycast(com + up * 50, -up, out RaycastHit hit, 400, Layers.GroundMask, QueryTriggerInteraction.Ignore))
            {
                float deck = Vector3.Dot(hit.point, up);
                float delta = deck + 0.03f - lowest;
                v.transform.position += up * delta;
                v.Rb.position = v.transform.position;
                Physics.SyncTransforms();
            }
            v.LoadedUT = Sim.UT;
            Sim.CaptureState(v);
        }

        public void Notify(string msg, bool important = false) => Notification?.Invoke(msg, important);

        // ------------------------------------------------------------------ map

        public void ToggleMap()
        {
            SetMap(!Map.Active);
        }

        public void SetMap(bool on)
        {
            if (on) Map.FocusOn(Sim.ActiveHandle);
            Map.SetActive(on, Camera.Cam);
        }

        // ------------------------------------------------------------------ saving

        public GameSave CaptureSave()
        {
            var save = GameSession.Save;
            save.ut = Sim.UT;
            save.vessels = Sim.CaptureAllRecords();
            save.activeVesselId = Sim.ActiveVessel != null ? Sim.ActiveVessel.Id : save.activeVesselId;
            GameSession.ReconcileCrew();
            return SaveStorage.DeepClone(save);
        }

        public void Quicksave()
        {
            if (Sim.ActiveVessel == null) return;
            bool warping = Sim.Warp.OnRails;
            var s = CaptureSave();
            try
            {
                SaveStorage.WriteSave(s, "quicksave");
                Notify("Quicksaved (F9 to load)", true);
            }
            catch (Exception e) { Notify("Quicksave failed: " + e.Message, true); }
        }

        public void Quickload()
        {
            var s = SaveStorage.ReadSave(GameSession.Save.saveName, "quicksave");
            if (s == null) { Notify("No quicksave found", true); return; }
            GameSession.PendingMessage = "Quickload complete";
            GameSession.LoadFlightState(s, FlightEntry.Quickload);
        }

        public void RevertToLaunch()
        {
            if (GameSession.LaunchSnapshot == null) { Notify("Nothing to revert to", true); return; }
            GameSession.PendingMessage = "Flight reverted to launch";
            GameSession.LoadFlightState(SaveStorage.DeepClone(GameSession.LaunchSnapshot), FlightEntry.Resume);
        }

        public void RevertToEditor()
        {
            if (GameSession.PreLaunchSnapshot != null)
            {
                GameSession.Save = SaveStorage.DeepClone(GameSession.PreLaunchSnapshot);
                GameSession.ReconcileCrew();
            }
            GameSession.GoToEditor();
        }

        /// <summary>Leave the flight but keep every vessel (they continue on rails).</summary>
        public void LeaveToEditor()
        {
            if (Sim.Warp.OnRails) Sim.StopWarp();
            CaptureSave();
            GameSession.WritePersistent();
            GameSession.GoToEditor();
        }

        public void LeaveToMenu()
        {
            CaptureSave();
            GameSession.WritePersistent();
            GameSession.GoToMenu();
        }

        public void RecoverActive()
        {
            var v = Sim.ActiveVessel;
            if (v == null || !Sim.CanRecover(v)) return;
            string name = v.VesselName;
            bool crewed = v.CrewCount > 0;
            Tracker?.OnRecovered(crewed);
            Sim.Recover(v);
            GameSession.Log($"{name} recovered on {Sim.System.Root.Name}");
            if (Sim.ActiveVessel == null)
            {
                CaptureSave();
                GameSession.WritePersistent();
                GameSession.PendingMessage = $"{name} recovered. Crew returned to the roster.";
                GameSession.GoToEditor();
            }
        }

        private void OnApplicationQuit()
        {
            if (Sim != null && Sim.ActiveVessel != null)
            {
                CaptureSave();
                GameSession.WritePersistent();
            }
        }
    }
}
