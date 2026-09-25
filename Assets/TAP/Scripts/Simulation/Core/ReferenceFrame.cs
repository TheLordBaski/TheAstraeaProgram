using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Maps between Unity world space (float, small numbers near the active vessel) and the
    /// double-precision "true" frame: inertial, non-rotating, centred on <see cref="Body"/>.
    ///
    ///   truePos  = Origin + unityPos
    ///   trueVel  = Velocity + unityVel
    ///
    /// Origin advances with Velocity every physics step (a Galilean frame between rebases).
    /// Krakensbane rebases Velocity onto the active vessel; floating-origin rebases Origin.
    /// Changing the reference body (SOI transition) only changes Origin/Velocity/Body — unity
    /// coordinates of everything stay the same, so there is no visible jump.
    /// </summary>
    public sealed class ReferenceFrame
    {
        public CelestialBody Body { get; private set; }
        public Vector3d Origin;
        public Vector3d Velocity;
        /// <summary>Origin at the start of the last physics step (same coordinate system), for render interpolation.</summary>
        public Vector3d PreviousOrigin;

        public ReferenceFrame(CelestialBody body)
        {
            Body = body;
        }

        public Vector3d ToTrue(Vector3 unityPos) => Origin + unityPos;
        public Vector3d ToTrueVelocity(Vector3 unityVel) => Velocity + unityVel;
        public Vector3 ToUnity(Vector3d truePos) => (Vector3)(truePos - Origin);
        public Vector3 ToUnityVelocity(Vector3d trueVel) => (Vector3)(trueVel - Velocity);

        /// <summary>Unity position of a true position using the render-interpolated origin.</summary>
        public Vector3 ToUnityRender(Vector3d truePos, double alpha)
        {
            Vector3d o = Vector3d.Lerp(PreviousOrigin, Origin, alpha);
            return (Vector3)(truePos - o);
        }

        public Vector3d RenderOrigin(double alpha) => Vector3d.Lerp(PreviousOrigin, Origin, alpha);

        /// <summary>Re-centres the true frame on another body at time ut (no Unity-space changes).</summary>
        public void ChangeBody(CelestialBody newBody, double ut)
        {
            if (newBody == Body) return;
            // position of new body relative to old body
            Vector3d rel = newBody.GetPositionAtUT(ut) - Body.GetPositionAtUT(ut);
            Vector3d relV = newBody.GetVelocityAtUT(ut) - Body.GetVelocityAtUT(ut);
            Origin -= rel;
            PreviousOrigin -= rel;
            Velocity -= relV;
            Body = newBody;
        }

        /// <summary>Position of a body's centre relative to this frame's body (true coordinates).</summary>
        public Vector3d BodyPosition(CelestialBody b, double ut)
        {
            if (b == Body) return Vector3d.zero;
            return b.GetPositionAtUT(ut) - Body.GetPositionAtUT(ut);
        }

        public Vector3d BodyVelocity(CelestialBody b, double ut)
        {
            if (b == Body) return Vector3d.zero;
            return b.GetVelocityAtUT(ut) - Body.GetVelocityAtUT(ut);
        }
    }
}
