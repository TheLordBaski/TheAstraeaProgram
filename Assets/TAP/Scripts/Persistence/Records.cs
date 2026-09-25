using System;
using System.Collections.Generic;

namespace TAP.Persistence
{
    // Serializable data transfer objects. No behaviour, no Unity object references.
    // Vectors are stored as arrays (float[3] / double[3]) and quaternions as float[4] (x,y,z,w).

    /// <summary>One part in a craft design (VAB output). Index 0 is the root part.</summary>
    [Serializable]
    public class PartNodeRecord
    {
        public int uid;
        public string partId;
        /// <summary>Index of the parent part in the list (-1 for root).</summary>
        public int parent = -1;
        /// <summary>Attach node on the parent ("top", "bottom", ...) or "srf" for surface attachment.</summary>
        public string parentNode;
        /// <summary>Attach node on this part ("top", "bottom", ...) or "srf".</summary>
        public string attachNode;
        /// <summary>Position relative to the root part (vessel space, metres).</summary>
        public float[] pos = { 0, 0, 0 };
        /// <summary>Rotation relative to the root part.</summary>
        public float[] rot = { 0, 0, 0, 1 };
        /// <summary>Inverse stage index (higher fires first). -1 = not in staging.</summary>
        public int stage = -1;
        public int symmetryGroup = -1;
        /// <summary>Optional per-part settings (e.g. thrustLimit).</summary>
        public Dictionary<string, double> settings;
    }

    [Serializable]
    public class CraftDesign
    {
        public string name = "Untitled Craft";
        public string description = "";
        public int version = 1;
        public List<PartNodeRecord> parts = new List<PartNodeRecord>();
        /// <summary>Parts set aside in the assembly building, not connected to the craft (never launched).</summary>
        public List<DetachedAssembly> detached = new List<DetachedAssembly>();
    }

    /// <summary>A group of parts set aside in the assembly building (shown greyed out, not part of the craft).</summary>
    [Serializable]
    public class DetachedAssembly
    {
        /// <summary>Position of the group's root part in the building (world space, metres).</summary>
        public float[] pos = { 0, 0, 0 };
        /// <summary>Rotation of the group's root part in the building.</summary>
        public float[] rot = { 0, 0, 0, 1 };
        /// <summary>The parts, index 0 is the group's root; poses are relative to it (as in a craft design).</summary>
        public List<PartNodeRecord> parts = new List<PartNodeRecord>();
    }

    [Serializable]
    public class ResourceRecord
    {
        public string id;
        public double amount;
        public double max;
    }

    /// <summary>Full dynamic state of one part inside a vessel.</summary>
    [Serializable]
    public class PartRecord : PartNodeRecord
    {
        public double skinTemp = 290;
        public double internalTemp = 290;
        public List<ResourceRecord> resources = new List<ResourceRecord>();
        /// <summary>Per-module state blobs keyed by module name.</summary>
        public Dictionary<string, Dictionary<string, string>> modules = new Dictionary<string, Dictionary<string, string>>();
        public List<string> crew = new List<string>();
    }

    public enum VesselKind { Ship, Debris, EVA, Flag }

    public enum Situation { Prelaunch, Landed, Splashed, Flying, SubOrbital, Orbiting, Escaping }

    public enum SasMode { StabilityAssist, Prograde, Retrograde, Normal, AntiNormal, RadialIn, RadialOut, Target, AntiTarget, Maneuver }

    public enum SpeedMode { Surface, Orbit, Target }

    [Serializable]
    public class ControlRecord
    {
        public float throttle;
        public bool sas;
        public SasMode sasMode = SasMode.StabilityAssist;
        public bool rcs;
        public bool legsDeployed;
        public SpeedMode speedMode = SpeedMode.Surface;
    }

    [Serializable]
    public class ManeuverNodeRecord
    {
        public double ut;
        /// <summary>Delta-v components (m/s): prograde, normal, radial-out.</summary>
        public double prograde, normal, radial;
    }

    [Serializable]
    public class VesselRecord
    {
        public string id = Guid.NewGuid().ToString("N");
        public string name = "Vessel";
        public VesselKind kind = VesselKind.Ship;
        public Situation situation = Situation.Prelaunch;
        public string bodyId;
        public int rootIndex;
        public List<PartRecord> parts = new List<PartRecord>();
        public int currentStage;

        /// <summary>Orbit state (centre of mass) relative to the body, inertial frame.</summary>
        public double[] orbitPos;
        public double[] orbitVel;
        public double orbitEpoch;

        /// <summary>For landed/splashed vessels: root-part position in the body-fixed frame.</summary>
        public bool landed;
        public double[] landedPos;
        /// <summary>Root-part rotation relative to the body-fixed frame.</summary>
        public float[] landedRot;

        /// <summary>Root-part rotation in the inertial frame (for vessels on rails / in flight).</summary>
        public float[] rotation = { 0, 0, 0, 1 };
        public float[] angularVelocity = { 0, 0, 0 };
        /// <summary>Offset of the centre of mass from the root part, root space (to restore exactly).</summary>
        public float[] comOffset = { 0, 0, 0 };

        public ControlRecord control = new ControlRecord();
        public List<ManeuverNodeRecord> maneuverNodes = new List<ManeuverNodeRecord>();
        public string targetId;
        public double launchUT = -1;
        public string designName;
        /// <summary>For EVA: remaining jetpack propellant etc. are part module state; this marks the crew name.</summary>
        public string evaCrew;
    }

    public enum CrewStatus { Available, Assigned, EVA, Lost }

    [Serializable]
    public class CrewRecord
    {
        public string name;
        public CrewStatus status = CrewStatus.Available;
        public string vesselId;
        public int flights;
    }

    [Serializable]
    public class FlagRecordInfo
    {
        public string vesselId;
        public string bodyId;
        public string plaque;
        public double ut;
    }

    [Serializable]
    public class GameSave
    {
        public int version = 1;
        public string saveName = "Sandbox";
        public double ut;
        public string activeVesselId;
        public List<VesselRecord> vessels = new List<VesselRecord>();
        public List<CrewRecord> crew = new List<CrewRecord>();
        public List<string> missionLog = new List<string>();
        /// <summary>Mission milestones reached (ids from the mission tracker), in order.</summary>
        public List<string> milestones = new List<string>();
        public bool showGuide = true;
        /// <summary>Craft on the assembly floor when the game was saved.</summary>
        public CraftDesign editorCraft;
        public string savedAt;
        /// <summary>Scene to return to when loading ("flight" or "editor").</summary>
        public string scene = "flight";
    }
}
