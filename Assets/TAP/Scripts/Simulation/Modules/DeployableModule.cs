using System;
using System.Collections.Generic;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// The module of every part that unfolds, extends or opens (PRT-03, <see cref="DeployableDefinition"/>): the same code moves a folding
    /// solar panel, an antenna dish and a bay door, from the transforms and curves in the part's data. It keeps the state
    /// (retracted, extending, extended, retracting, broken), tears the part's moving pieces off when it is out in too much air, lowers the
    /// part's impact tolerance while it is out, shows its actions in the part menu and offers the same calls to action groups.
    /// What the part does while it is out (power, signal, drag) belongs to the system that part is for.
    /// </summary>
    public sealed class DeployableModule : PartModule
    {
        public DeployableDefinition Def;
        public DeployableLogic Logic;

        private sealed class Piece
        {
            public DeployableMotion Motion;
            public Transform Target;
            public Vector3 RestPosition, RestScale;
            public Quaternion RestRotation;
        }

        private readonly List<Piece> _pieces = new List<Piece>();

        public override string ModuleName => "Deployable";

        public DeployState State => Logic.State;
        public double Progress => Logic.Progress;
        public bool IsOut => Logic.IsOut;

        public override void OnInit()
        {
            Def = Part.Def.deployable;
            Logic = new DeployableLogic(Def);
            FindPieces();
            Show();
        }

        private void FindPieces()
        {
            _pieces.Clear();
            var root = Part.ModelRoot != null ? Part.ModelRoot : Part.transform;
            foreach (var m in Def.motions)
            {
                var target = root.Find(m.transform);
                if (target == null) continue; // a placeholder model has no such piece yet: the state still works
                _pieces.Add(new Piece { Motion = m, Target = target, RestPosition = target.localPosition, RestRotation = target.localRotation, RestScale = target.localScale });
            }
        }

        // ------------------------------------------------------------------ commands (the part menu, and action groups later)

        public bool Extend() => Logic.Extend();
        public bool Retract() => Logic.Retract();
        public bool Toggle() => Logic.Toggle();

        // ------------------------------------------------------------------ simulation

        public override void OnPreStep(double dt)
        {
            double power = 1;
            var v = Vessel;
            if (Logic.Moving && Def.ecPerSecond > 0 && v != null)
            {
                double want = Def.ecPerSecond * dt;
                power = want > 0 ? Math.Min(1, v.Resources.Request(Part, "Electric", want) / want) : 1;
            }
            Advance(dt, v != null ? v.DynamicPressure : 0, power);
        }

        /// <summary>One step of the simulation: the motion, and the limits. Public so tests and tools can step it without a vessel.</summary>
        public void Advance(double dt, double dynamicPressure, double power = 1)
        {
            if (Logic.Advance(dt, dynamicPressure, power))
            {
                FlightSim.Instance?.Log($"{Part.Def.title} {Logic.BrokenReason}", true);
                Show();
            }
        }

        public override double ImpactTolerance(double baseTolerance) =>
            Logic != null && Logic.IsOut && Def.breakImpact > 0 ? Math.Min(baseTolerance, Def.breakImpact) : baseTolerance;

        // ------------------------------------------------------------------ the model

        public override void OnRenderUpdate(float dt) => Show();

        /// <summary>Puts the model's pieces where the progress says (and hides them when they have been torn off).</summary>
        public void Show()
        {
            if (Logic == null) return;
            bool broken = Logic.State == DeployState.Broken;
            foreach (var p in _pieces)
            {
                if (p.Target == null) continue;
                if (broken)
                {
                    if (p.Target.gameObject.activeSelf) p.Target.gameObject.SetActive(false);
                    continue;
                }
                float k = p.Motion.Blend(Logic.Progress);
                var a = Vec(p.Motion.from, p.Motion.kind == "scale" ? 1f : 0f);
                var b = Vec(p.Motion.to, 0f);
                var value = Vector3.LerpUnclamped(a, b, k);
                switch (p.Motion.kind)
                {
                    case "translate": p.Target.localPosition = p.RestPosition + value; break;
                    case "scale": p.Target.localScale = Vector3.Scale(p.RestScale, value); break;
                    default: p.Target.localRotation = p.RestRotation * Quaternion.Euler(value); break;
                }
            }
        }

        private static Vector3 Vec(float[] v, float fill) => v != null && v.Length >= 3 ? new Vector3(v[0], v[1], v[2]) : new Vector3(fill, fill, fill);

        // ------------------------------------------------------------------ saving

        public override void Save(Dictionary<string, string> s)
        {
            s["state"] = Logic.State.ToString();
            s["progress"] = F(Logic.Progress);
            if (Logic.BrokenReason != null) s["reason"] = Logic.BrokenReason;
        }

        public override void Load(Dictionary<string, string> s)
        {
            if (Enum.TryParse(GetString(s, "state", null), out DeployState state)) Logic.Restore(state, GetDouble(s, "progress"));
            if (state == DeployState.Broken) Logic.Break(GetString(s, "reason", "broken"));
            Show();
        }

        // ------------------------------------------------------------------ the part menu

        public override void CollectActions(List<PartAction> actions)
        {
            if (Logic.CanExtend) actions.Add(new PartAction(Def.extendLabel, () => Extend()));
            if (Logic.CanRetract) actions.Add(new PartAction(Def.retractLabel, () => Retract()));
        }

        public override void CollectInfo(List<string> info)
        {
            string text = Logic.State == DeployState.Broken ? $"Broken: {Logic.BrokenReason}" : Logic.State.ToString();
            if (Logic.Moving) text += $" ({Logic.Progress * 100:F0}%)";
            info.Add(text);
            if (Def.breakPressure > 0 && Logic.IsOut)
                info.Add($"Tears off above {Def.breakPressure / 1000:F0} kPa dynamic pressure");
        }
    }
}
