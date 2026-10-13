using TAP.Persistence;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Pilot/autopilot inputs for a vessel. Rotation inputs follow the flight convention:
    ///   Pitch +1 = nose down (W), Yaw +1 = nose right (D), Roll +1 = roll right (E).
    /// Translation: X +1 = right (L), Y +1 = forward along nose (H), Z +1 = "down"/belly (N... see input map).
    /// </summary>
    public sealed class VesselControl
    {
        public float Pitch, Yaw, Roll;
        /// <summary>Trim: a steady pitch, yaw and roll the pilot has set, added to what the keys or sticks give.</summary>
        public float TrimPitch, TrimYaw, TrimRoll;
        public float TransX, TransY, TransZ;
        public float Throttle;
        public bool Sas;
        public bool Rcs;
        public SasMode SasMode = SasMode.StabilityAssist;
        public SpeedMode SpeedMode = SpeedMode.Surface;
        public bool LegsDeployed;

        /// <summary>Final torque command in the control frame (x: pitch axis, y: roll axis, z: yaw axis), each in [-1, 1].</summary>
        public Vector3 TorqueCommand;
        /// <summary>Final translation command in the control frame, each in [-1, 1].</summary>
        public Vector3 TranslationCommand;

        /// <summary>True while keys or sticks steer. Trim does not count: a stray trim must not switch the SAS hold off.</summary>
        public bool PilotRotating => Mathf.Abs(Pitch - TrimPitch) > 0.01f || Mathf.Abs(Yaw - TrimYaw) > 0.01f || Mathf.Abs(Roll - TrimRoll) > 0.01f;

        /// <summary>Pilot torque vector in the control frame, trim included (what steers with SAS off).</summary>
        public Vector3 PilotTorque => new Vector3(Pitch, -Roll, -Yaw);

        /// <summary>Pilot torque from keys and sticks only, without trim (what overrides SAS).</summary>
        public Vector3 StickTorque => new Vector3(Pitch - TrimPitch, -(Roll - TrimRoll), -(Yaw - TrimYaw));

        public void ClearInputs()
        {
            Pitch = Yaw = Roll = 0;
            TransX = TransY = TransZ = 0;
        }

        public ControlRecord ToRecord() => new ControlRecord
        {
            throttle = Throttle, sas = Sas, sasMode = SasMode, rcs = Rcs, legsDeployed = LegsDeployed, speedMode = SpeedMode,
            trimPitch = TrimPitch, trimYaw = TrimYaw, trimRoll = TrimRoll,
        };

        public void FromRecord(ControlRecord r)
        {
            if (r == null) return;
            Throttle = r.throttle;
            Sas = r.sas;
            SasMode = r.sasMode;
            Rcs = r.rcs;
            LegsDeployed = r.legsDeployed;
            SpeedMode = r.speedMode;
            TrimPitch = r.trimPitch;
            TrimYaw = r.trimYaw;
            TrimRoll = r.trimRoll;
        }
    }
}
