using TAP.Core;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Stability assist: converts attitude error into normalized torque commands using the vessel's
    /// inertia and currently available control authority (wheels, gimbals, RCS), with rate limiting so
    /// low-authority vessels don't overshoot.
    /// </summary>
    public sealed class AttitudeController
    {
        private Quaternion _hold;
        private bool _holdValid;
        private SasMode _lastMode;
        public string LastError; // for UI when a mode is unavailable
        public float LastErrorAngleDeg;

        public void ResetHold() { _holdValid = false; }

        public Vector3 Update(Vessel v, double dt)
        {
            var ctrl = v.Ctrl;
            Vector3 pilot = ctrl.PilotTorque;
            LastError = null;
            if (!ctrl.Sas || !v.HasControl || !v.HasSas)
            {
                _holdValid = false;
                LastErrorAngleDeg = 0;
                return Clamp(pilot);
            }
            if (ctrl.SasMode != _lastMode) { _holdValid = false; _lastMode = ctrl.SasMode; }

            Quaternion ctrlRot = v.ControlRotation;
            Vector3 omegaWorld = v.Rb.angularVelocity;
            Vector3 omegaLocal = Quaternion.Inverse(ctrlRot) * omegaWorld;

            Vector3 errLocal = Vector3.zero;
            bool hasTarget = false;
            Vector3 nose = ctrlRot * Vector3.up;

            if (ctrl.SasMode == SasMode.StabilityAssist)
            {
                if (ctrl.PilotRotating)
                {
                    _holdValid = false;
                }
                else if (!_holdValid && omegaWorld.magnitude < 0.03f)
                {
                    _hold = ctrlRot;
                    _holdValid = true;
                }
                if (_holdValid)
                {
                    Quaternion qErr = _hold * Quaternion.Inverse(ctrlRot);
                    qErr.ToAngleAxis(out float ang, out Vector3 axis);
                    if (ang > 180) ang -= 360;
                    if (float.IsNaN(axis.x) || float.IsInfinity(axis.x)) axis = Vector3.up;
                    Vector3 errWorld = axis.normalized * ang * Mathf.Deg2Rad;
                    errLocal = Quaternion.Inverse(ctrlRot) * errWorld;
                    hasTarget = true;
                    LastErrorAngleDeg = Mathf.Abs(ang);
                }
            }
            else
            {
                Vector3 dir;
                if (v.TryGetSasDirection(ctrl.SasMode, out dir))
                {
                    Vector3 axis = Vector3.Cross(nose, dir);
                    float sin = axis.magnitude;
                    float cos = Vector3.Dot(nose, dir);
                    float ang = Mathf.Atan2(sin, cos);
                    if (sin < 1e-6f)
                    {
                        // aligned or exactly opposite: pick any perpendicular axis if opposite
                        axis = cos < 0 ? ctrlRot * Vector3.right : Vector3.zero;
                    }
                    else axis /= sin;
                    Vector3 errWorld = axis * ang;
                    errLocal = Quaternion.Inverse(ctrlRot) * errWorld;
                    errLocal.y = 0; // roll free in direction modes
                    hasTarget = true;
                    LastErrorAngleDeg = ang * Mathf.Rad2Deg;
                }
                else
                {
                    LastError = "SAS mode unavailable";
                }
            }

            Vector3 inertia = v.ControlFrameInertia;
            Vector3 authority = v.TorqueAuthority;
            Vector3 cmd = Vector3.zero;
            for (int k = 0; k < 3; k++)
            {
                float I = Mathf.Max(inertia[k], 1f);
                float tau = Mathf.Max(authority[k], 1f);
                float alphaMax = tau / I;
                float e = hasTarget ? errLocal[k] : 0f;
                float w = omegaLocal[k];
                float absE = Mathf.Abs(e);
                // Desired rate: linear near zero, sqrt(2*a*e) braking-curve further out, capped.
                float wCap = Mathf.Clamp(alphaMax * 3f, 0.15f, 1.2f);
                float wDes = Mathf.Sign(e) * Mathf.Min(wCap, Mathf.Min(absE * 2.2f, Mathf.Sqrt(2f * 0.55f * alphaMax * absE)));
                float tauResp = 0.18f;
                float aDes = (wDes - w) / tauResp;
                float c = aDes * I / tau;
                // Deadband to avoid chatter when settled.
                if (absE < 0.0015f && Mathf.Abs(w) < 0.0015f) c = 0;
                cmd[k] = Mathf.Clamp(c, -1f, 1f);
            }

            // Pilot overrides per axis.
            if (Mathf.Abs(pilot.x) > 0.01f) cmd.x = pilot.x;
            if (Mathf.Abs(pilot.y) > 0.01f) cmd.y = pilot.y;
            if (Mathf.Abs(pilot.z) > 0.01f) cmd.z = pilot.z;
            return Clamp(cmd);
        }

        private static Vector3 Clamp(Vector3 c) => new Vector3(Mathf.Clamp(c.x, -1, 1), Mathf.Clamp(c.y, -1, 1), Mathf.Clamp(c.z, -1, 1));
    }
}
