using System;
using System.Collections.Generic;

namespace TAP.Parts
{
    /// <summary>
    /// A part that unfolds, extends or opens (PRT-03): solar panels, antennas, radiators, bay doors, ladders, airbrakes, drills, scanners,
    /// the shielded docking port. One data-driven module for all of them: it drives named transforms of the part's model along curves,
    /// and what the part does while out (power, signal, drag) is the business of the system it belongs to.
    /// </summary>
    [Serializable]
    public class DeployableDefinition
    {
        /// <summary>The action in the part menu that puts it out ("Deploy", "Extend", "Open"…).</summary>
        public string extendLabel = "Deploy";
        /// <summary>The action that takes it in ("Retract", "Close"…).</summary>
        public string retractLabel = "Retract";
        /// <summary>Seconds from retracted to extended.</summary>
        public double extendTime = 4;
        /// <summary>Seconds from extended to retracted; 0 means it can't be taken in again.</summary>
        public double retractTime = 3;
        /// <summary>"retracted" (the model's rest pose) or "extended" (for a part that starts out, such as a ladder).</summary>
        public string startState = "retracted";
        /// <summary>Dynamic pressure (Pa) above which the part tears off while it is out or moving. 0 means it never does.</summary>
        public double breakPressure;
        /// <summary>Impact speed (m/s) the part survives while out or moving; the part's own tolerance holds when retracted. 0 means no change.</summary>
        public double breakImpact;
        /// <summary>Electric charge per second while it moves.</summary>
        public double ecPerSecond;
        /// <summary>The pieces of the model it moves.</summary>
        public List<DeployableMotion> motions = new List<DeployableMotion>();
    }

    /// <summary>One transform of the model moved from its rest pose, in a window of the deployment, along a curve.</summary>
    [Serializable]
    public class DeployableMotion
    {
        /// <summary>The transform under the part's Model, by name or path ("WingA", "Boom/Dish"). Hand-made models keep these names.</summary>
        public string transform;
        /// <summary>"rotate" (euler degrees added to the rest rotation), "translate" (metres added to the rest position) or "scale" (factors times the rest scale).</summary>
        public string kind = "rotate";
        /// <summary>Value at the start; the default is no change (0, 0, 0; or 1, 1, 1 for scale).</summary>
        public float[] from;
        /// <summary>Value at the end.</summary>
        public float[] to = { 0, 0, 0 };
        /// <summary>"linear", "smooth", "easeIn", "easeOut" or "overshoot". Ignored when <see cref="keys"/> is set.</summary>
        public string curve = "smooth";
        /// <summary>Optional keyframes [[progress, value], …] of the blend from 0 to 1 and a bit beyond, each progress from 0 to 1 in order.</summary>
        public float[][] keys;
        /// <summary>The deployment progress at which the motion starts (0 to 1)...</summary>
        public float start;
        /// <summary>...and ends. Staggered windows fold a panel in stages.</summary>
        public float end = 1;

        public static readonly string[] Kinds = { "rotate", "translate", "scale" };
        public static readonly string[] Curves = { "linear", "smooth", "easeIn", "easeOut", "overshoot" };

        /// <summary>How far along the motion is at a deployment progress: 0 before its window, 1 after, shaped by the curve.</summary>
        public float Blend(double progress)
        {
            double span = Math.Max(1e-6, end - start);
            double t = Math.Min(1, Math.Max(0, (progress - start) / span));
            return keys != null && keys.Length > 0 ? Keyframes(keys, (float)t) : Shape(curve, (float)t);
        }

        /// <summary>The shape of a curve at t from 0 to 1 (0 at 0, 1 at 1).</summary>
        public static float Shape(string curve, float t)
        {
            switch (curve)
            {
                case "linear": return t;
                case "easeIn": return t * t;
                case "easeOut": return 1 - (1 - t) * (1 - t);
                case "overshoot":
                    // easeOutBack: it goes a little past the end and settles back.
                    const float c1 = 1.70158f, c3 = c1 + 1;
                    float u = t - 1;
                    return 1 + c3 * u * u * u + c1 * u * u;
                default: return t * t * (3 - 2 * t); // smooth
            }
        }

        /// <summary>Piecewise-linear value through [progress, value] keys; flat before the first and after the last.</summary>
        public static float Keyframes(float[][] keys, float t)
        {
            if (t <= keys[0][0]) return keys[0][1];
            for (int i = 1; i < keys.Length; i++)
            {
                if (t > keys[i][0]) continue;
                float span = keys[i][0] - keys[i - 1][0];
                float k = span > 1e-6f ? (t - keys[i - 1][0]) / span : 1;
                return keys[i - 1][1] + (keys[i][1] - keys[i - 1][1]) * k;
            }
            return keys[keys.Length - 1][1];
        }
    }

    public enum DeployState { Retracted, Extending, Extended, Retracting, Broken }

    /// <summary>
    /// The state machine of a deployable, apart from Unity: retracted, extending, extended, retracting, broken. The module steps it every
    /// physics step and shows its progress on the model.
    /// </summary>
    public sealed class DeployableLogic
    {
        public readonly DeployableDefinition Def;
        public DeployState State { get; private set; }
        /// <summary>0 retracted to 1 extended.</summary>
        public double Progress { get; private set; }
        public string BrokenReason { get; private set; }

        public DeployableLogic(DeployableDefinition def)
        {
            Def = def;
            if (def.startState == "extended") { State = DeployState.Extended; Progress = 1; }
        }

        public bool Moving => State == DeployState.Extending || State == DeployState.Retracting;
        /// <summary>Out, or on its way: the states in which the pressure and impact limits apply.</summary>
        public bool IsOut => State != DeployState.Retracted && State != DeployState.Broken;
        public bool CanExtend => State == DeployState.Retracted || State == DeployState.Retracting;
        public bool CanRetract => Def.retractTime > 0 && (State == DeployState.Extended || State == DeployState.Extending);

        public bool Extend()
        {
            if (!CanExtend) return false;
            State = DeployState.Extending;
            return true;
        }

        public bool Retract()
        {
            if (!CanRetract) return false;
            State = DeployState.Retracting;
            return true;
        }

        /// <summary>Does what the part menu's one button does: puts it out if it is in, takes it in if it is out.</summary>
        public bool Toggle() => CanExtend ? Extend() : Retract();

        /// <summary>Restores a saved state. A moving state continues from its progress.</summary>
        public void Restore(DeployState state, double progress)
        {
            State = state;
            Progress = Math.Min(1, Math.Max(0, progress));
            if (state == DeployState.Retracted) Progress = 0;
            if (state == DeployState.Extended) Progress = 1;
        }

        /// <summary>Steps <paramref name="dt"/> seconds. <paramref name="power"/> (0 to 1) is the share of the electric charge it asked for that it got: a starved motor stalls.</summary>
        /// <returns>True on the step it breaks.</returns>
        public bool Advance(double dt, double dynamicPressure, double power = 1)
        {
            if (State == DeployState.Broken) return false;
            if (IsOut && Def.breakPressure > 0 && dynamicPressure > Def.breakPressure)
            {
                Break($"torn off at {dynamicPressure / 1000:F1} kPa (limit {Def.breakPressure / 1000:F0} kPa)");
                return true;
            }
            if (State == DeployState.Extending)
            {
                Progress = Math.Min(1, Progress + dt * power / Math.Max(1e-6, Def.extendTime));
                if (Progress >= 1) State = DeployState.Extended;
            }
            else if (State == DeployState.Retracting)
            {
                Progress = Math.Max(0, Progress - dt * power / Math.Max(1e-6, Def.retractTime));
                if (Progress <= 0) State = DeployState.Retracted;
            }
            return false;
        }

        public void Break(string reason)
        {
            State = DeployState.Broken;
            BrokenReason = reason;
        }
    }
}
