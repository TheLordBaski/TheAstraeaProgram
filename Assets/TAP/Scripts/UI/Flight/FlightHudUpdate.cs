using System;
using System.Collections.Generic;
using System.Text;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using TAP.Parts;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    public sealed partial class FlightHud
    {
        private void Update()
        {
            if (Sim == null) return;
            var v = Sim.ActiveVessel;
            UpdateMessages();
            UpdateClock();
            if (v == null)
            {
                _altitude.text = "—";
                return;
            }
            bool eva = v.IsEva;
            UIKit.Show(_evaPanel, eva);
            UIKit.Show(_stagePanel, !eva);
            // The staging stack fills the space under the mission guide (both hug the left edge); its list scrolls.
            float screenH = _root.layout.height > 0 ? _root.layout.height : 1080;
            float stageH = Mathf.Clamp(screenH - MissionGuide.BottomFromTop - 16, 140, 520);
            if (Mathf.Abs(_stagePanel.resolvedStyle.height - stageH) > 1) _stagePanel.style.height = stageH;
            _evaPanel.style.maxHeight = Mathf.Max(90, screenH - MissionGuide.BottomFromTop - 16);
            UpdateAltimeter(v);
            UpdateNavballCluster(v);
            _slowTimer -= Time.unscaledDeltaTime;
            if (_slowTimer <= 0)
            {
                _slowTimer = 0.2f;
                UpdateResources(v);
                UpdateData(v);
                UpdateStaging(v);
                UpdateTarget(v);
                UpdateWarnings(v);
                if (eva) UpdateEva(v);
            }
            UpdateBurn(v);
            if (_bigNotice.text.Length > 0 && Time.unscaledTime > _bigNoticeUntil) _bigNotice.text = "";
        }

        private void UpdateMessages()
        {
            for (int i = _messageItems.Count - 1; i >= 0; i--)
            {
                var m = _messageItems[i];
                float left = m.until - Time.unscaledTime;
                if (left <= 0) { m.text.RemoveFromHierarchy(); _messageItems.RemoveAt(i); continue; }
                m.text.style.opacity = Mathf.Clamp01(left / 1.2f);
            }
        }

        private void UpdateClock()
        {
            var v = Sim.ActiveVessel;
            string met = v != null && v.Record.launchUT >= 0 ? "T+" + MathD.FormatDuration(Sim.UT - v.Record.launchUT) : "T-0 (on pad)";
            _clock.text = $"<b>{MathD.FormatUT(Sim.UT)}</b>\n<size=14><color=#{UIKit.Hex(UIKit.TextDim)}>MET</color> {met}</size>";
            string w;
            if (Sim.Warp.OnRails) w = $"{TimeWarp.RailsRates[Sim.Warp.RailsIndex]:0}x";
            else if (Sim.Warp.PhysicsIndex > 0) w = $"{TimeWarp.PhysicsRates[Sim.Warp.PhysicsIndex]:0}x P";
            else w = "1x";
            if (Sim.Paused || UiState.Paused) w = "PAUSED";
            _warpText.text = w;
            _warpText.style.color = Sim.Warp.OnRails || Sim.Warp.PhysicsIndex > 0 ? UIKit.Warn : UIKit.Accent;
            int max = Sim.MaxRailsIndex(out string reason);
            _warpLimit.text = max < TimeWarp.RailsRates.Length - 1 && reason != null ? $"Warp limit {TimeWarp.RailsRates[max]:0}x: {reason}" : "";
        }

        private void UpdateAltimeter(Vessel v)
        {
            double alt = _altTerrain ? v.AltitudeAGL : v.Altitude;
            _altMode.text = _altTerrain ? "ALT (TERRAIN)" : "ALT (SEA)";
            _altitude.text = alt < 100000 ? $"{alt:N0} m" : $"{alt / 1000:N1} km";
            double vs = v.VerticalSpeed;
            string arrow = vs > 0.05 ? "▲" : vs < -0.05 ? "▼" : "•";
            _vspeed.text = $"{arrow} {Math.Abs(vs):N1} m/s   horiz {Math.Sqrt(Math.Max(0, v.SurfaceSpeed * v.SurfaceSpeed - vs * vs)):N1} m/s";
            string sit = v.Situation.ToString();
            if (v.Situation == Situation.SubOrbital) sit = "Sub-orbital";
            _situation.text = $"{v.VesselName} — {sit} {(v.Situation == Situation.Landed || v.Situation == Situation.Splashed || v.Situation == Situation.Prelaunch ? "on" : "at")} {v.MainBody.Name}";
        }

        private void UpdateNavballCluster(Vessel v)
        {
            var c = v.Ctrl;
            double spd;
            switch (c.SpeedMode)
            {
                case SpeedMode.Surface: _speedMode.text = "SURF"; spd = v.SurfaceSpeed; break;
                case SpeedMode.Orbit: _speedMode.text = "ORBIT"; spd = v.OrbitalSpeed; break;
                default: _speedMode.text = "TGT"; spd = v.SpeedModeVelocity.magnitude; break;
            }
            _speed.text = $"{spd:N1} m/s";
            UIKit.SetBar(_throttle, c.Throttle, Color.Lerp(new Color(0.3f, 0.8f, 0.4f), new Color(1f, 0.6f, 0.2f), c.Throttle));
            _throttleText.text = $"{c.Throttle * 100:0}%";
            float g = (float)v.GForce;
            UIKit.SetBar(_gmeter, g / 10f, g > 8 ? UIKit.Bad : g > 5 ? UIKit.Warn : UIKit.Accent);
            _gText.text = $"{g:0.0}g";
            UIKit.SetOn(_sasBtn, c.Sas);
            UIKit.SetOn(_rcsBtn, c.Rcs);
            UIKit.SetOn(_legsBtn, c.LegsDeployed);
            bool hasLegs = false;
            foreach (var p in v.Parts) if (p.Def.landingLeg != null) { hasLegs = true; break; }
            UIKit.Show(_legsBtn, hasLegs);
            foreach (var kv in _sasModes)
            {
                bool avail = kv.Key == SasMode.StabilityAssist || v.TryGetSasDirection(kv.Key, out _);
                bool enabled = v.HasSas && avail;
                if (kv.Value.enabledSelf != enabled) kv.Value.SetEnabled(enabled);
                UIKit.SetOn(kv.Value, c.Sas && c.SasMode == kv.Key);
            }
        }

        private void UpdateResources(Vessel v)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<color=#{UIKit.Hex(UIKit.TextDim)}><size=13>RESOURCES</size></color>");
            // Stage propellant: tanks reachable by ignited engines
            double sAmt = 0, sMax = 0;
            foreach (var p in v.Parts)
            {
                var e = p.GetModule<EngineModule>();
                if (e == null || !e.Ignited || e.IsSolid) continue;
                sAmt += v.Resources.Available(p, e.Def.propellant);
                sMax += v.Resources.Capacity(p, e.Def.propellant);
                break;
            }
            if (sMax > 0) sb.AppendLine(ResLine("Stage propellant", sAmt, sMax, "kg", new Color(0.95f, 0.72f, 0.25f)));
            foreach (var rd in PartDatabase.Instance.ResourceDefinitions)
            {
                v.Resources.Totals(rd.id, out double a, out double m);
                if (m <= 0) continue;
                sb.AppendLine(ResLine(rd.id == "LiquidFuel" ? "Liquid propellant (all)" : rd.name, a, m, rd.unit, rd.Color));
            }
            if (v.CrewCapacity > 0 || v.IsEva)
            {
                var names = new List<string>();
                foreach (var cr in v.AllCrew()) names.Add(cr.name);
                if (v.IsEva) names.Add(v.Record.evaCrew);
                sb.Append($"Crew {v.CrewCount}/{v.CrewCapacity}: {string.Join(", ", names)}");
            }
            _resources.text = sb.ToString().TrimEnd('\n', '\r');
        }

        private static string ResLine(string name, double a, double m, string unit, Color col)
        {
            float f = m > 0 ? (float)(a / m) : 0;
            int blocks = Mathf.RoundToInt(f * 14);
            string bar = new string('█', blocks) + new string('░', 14 - blocks);
            string color = f < 0.1f ? UIKit.Hex(UIKit.Bad) : UIKit.Hex(col);
            return $"<size=13>{name}</size>\n<color=#{color}>{bar}</color> <size=13>{a:N0}/{m:N0} {unit}</size>";
        }

        private void UpdateData(Vessel v)
        {
            var sb = new StringBuilder();
            string dim = UIKit.Hex(UIKit.TextDim);
            var body = v.MainBody;
            var o = v.Orbit;
            sb.AppendLine($"<color=#{dim}><size=13>ORBIT ({body.Name})</size></color>");
            bool landed = v.Situation == Situation.Landed || v.Situation == Situation.Prelaunch || v.Situation == Situation.Splashed;
            if (o != null && !landed)
            {
                double ap = o.ApoapsisRadius - body.Radius, pe = o.PeriapsisRadius - body.Radius;
                string apS = o.IsElliptic ? MathD.FormatDistance(ap) : "escape";
                sb.AppendLine($"Ap  {apS}   <size=12>{(o.IsElliptic ? "in " + MathD.FormatDuration(o.TimeToApoapsis(Sim.UT)) : "")}</size>");
                sb.AppendLine($"Pe  {MathD.FormatDistance(pe)}   <size=12>in {MathD.FormatDuration(o.TimeToPeriapsis(Sim.UT))}</size>");
                if (pe < 0) sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Warn)}>Trajectory intersects the surface</color>");
                sb.AppendLine($"Inc {o.Inclination * MathD.Rad2Deg:0.00}°   e {o.Eccentricity:0.0000}");
                sb.AppendLine(o.IsElliptic ? $"Period {MathD.FormatDuration(o.Period)}" : $"<color=#{UIKit.Hex(UIKit.Warn)}>Escape trajectory</color>");
                var traj = Scene.Trajectory;
                if (traj != null && traj.Current.Count > 1)
                {
                    var p0 = traj.Current[0];
                    if (p0.EndType == PatchEnd.SoiEnter) sb.AppendLine($"<color=#{UIKit.Hex(MapView.MoonPatchColor)}>{p0.NextBody.Name} encounter in {MathD.FormatDuration(p0.EndUT - Sim.UT)}</color>");
                    if (p0.EndType == PatchEnd.SoiExit) sb.AppendLine($"<color=#{UIKit.Hex(MapView.MoonPatchColor)}>Leaving {body.Name} SOI in {MathD.FormatDuration(p0.EndUT - Sim.UT)}</color>");
                }
            }
            else sb.AppendLine("—");
            sb.AppendLine($"<color=#{dim}><size=13>FLIGHT</size></color>");
            sb.AppendLine($"Surface {v.SurfaceSpeed:N1} m/s   Orbital {v.OrbitalSpeed:N1} m/s");
            if (body.Atmosphere != null && v.Altitude < body.Atmosphere.Height)
            {
                sb.AppendLine($"Pressure {v.StaticPressure / 1000:0.00} kPa   ρ {v.AirDensity:0.0000} kg/m³");
                sb.AppendLine($"Mach {v.Mach:0.00}   Q {v.DynamicPressure / 1000:0.0} kPa   {v.AirTemperature:0} K");
            }
            else sb.AppendLine("Vacuum");
            sb.AppendLine($"<color=#{dim}><size=13>VESSEL</size></color>");
            v.GetPropulsion(out double thrust, out double isp);
            double g = v.GravityAccel.magnitude;
            double twr = g > 0 && v.TotalMass > 0 ? thrust / (v.TotalMass * g) : 0;
            sb.AppendLine($"Mass {v.TotalMass / 1000:0.00} t   TWR {twr:0.00}");
            var dv = StageDeltaV(v);
            sb.AppendLine($"Δv stage {dv.stage:N0} m/s   total {dv.total:N0} m/s");
            sb.Append($"Parts {v.Parts.Count}   Stage {Math.Max(0, v.CurrentStage)}");
            _data.text = sb.ToString();
        }

        private float _dvTimer;
        private (double stage, double total) _dvCache;

        private (double stage, double total) StageDeltaV(Vessel v)
        {
            _dvTimer -= 0.2f;
            if (_dvTimer > 0) return _dvCache;
            _dvTimer = 1f;
            // The burn in progress (engines already lit) comes first, then the stages still to fire.
            var stages = v.ComputeStageDeltaV(v.GravityAccel.magnitude, v.StaticPressure / 101325.0);
            double total = 0, cur = 0;
            bool first = true;
            foreach (var s in stages)
            {
                total += s.DeltaVCurrent;
                if (first && s.HasEngines) { cur = s.DeltaVCurrent; first = false; }
            }
            _dvCache = (cur, total);
            return _dvCache;
        }

        private void UpdateBurn(Vessel v)
        {
            if (v.ManeuverNodes.Count == 0) { UIKit.Show(_burnPanel, false); return; }
            var info = ManeuverPlanner.Info(v, Sim.UT);
            UIKit.Show(_burnPanel, true);
            string t = info.TimeToBurnStart > 0 ? $"burn in <b>{MathD.FormatDuration(info.TimeToBurnStart)}</b>" : $"<color=#{UIKit.Hex(UIKit.Warn)}>BURN NOW</color> (node {MathD.FormatDuration(info.TimeToNode, true)})";
            string align = info.AngleToBurn > 5 ? $"<color=#{UIKit.Hex(UIKit.Warn)}>off by {info.AngleToBurn:0}°</color>" : $"aligned ({info.AngleToBurn:0.0}°)";
            _burnInfo.text = $"MANEUVER  Δv <b>{info.RemainingDeltaV:N1}</b> / {info.TotalDeltaV:N1} m/s\n" +
                             $"Burn time {(double.IsNaN(info.BurnTime) ? "N/A (no thrust)" : MathD.FormatDuration(info.BurnTime))}   {t}\n{align}";
        }

        private void UpdateTarget(Vessel v)
        {
            if (!v.TryGetTargetState(Sim.UT, out var tp, out var tv)) { UIKit.Show(_targetPanel, false); return; }
            UIKit.Show(_targetPanel, true);
            double dist = (tp - v.TruePosition).magnitude;
            double rel = (v.TrueVelocity - tv).magnitude;
            var sb = new StringBuilder();
            sb.AppendLine($"<color=#{UIKit.Hex(NavballWidget.Tgt)}>TARGET</color>  {v.TargetName}");
            sb.AppendLine($"Distance {MathD.FormatDistance(dist)}   Rel. speed {rel:N1} m/s");
            var traj = Scene.Trajectory;
            if (traj != null && traj.HasClosest)
                sb.AppendLine($"Closest approach {MathD.FormatDistance(traj.ClosestDistance)} in {MathD.FormatDuration(traj.ClosestUT - Sim.UT)}\n  rel. speed there {traj.ClosestRelSpeed:N1} m/s");
            var enc = traj?.FirstEncounter(traj.HasNodes);
            if (enc != null)
                sb.AppendLine($"<color=#{UIKit.Hex(MapView.MoonPatchColor)}>{enc.Body.Name} periapsis {MathD.FormatDistance(enc.Orbit.PeriapsisRadius - enc.Body.Radius)}{(traj.HasNodes ? " (planned)" : "")}</color>");
            sb.Append("<size=12>Clear target from the map view</size>");
            _targetInfo.text = sb.ToString();
        }

        private void UpdateWarnings(Vessel v)
        {
            var sb = new StringBuilder();
            int n = 0;
            foreach (var p in v.Parts)
            {
                // Share of the heat-up from room temperature to the limit (a suit at 290 K of its 380 K is fine).
                const double roomT = 300;
                double fs = (p.SkinTemp - roomT) / Math.Max(1, p.Def.skinMaxTemp - roomT);
                double fi = (p.InternalTemp - roomT) / Math.Max(1, p.Def.maxTemp - roomT);
                if (fs > 0.72 || fi > 0.72)
                {
                    bool skin = fs >= fi;
                    string c = Math.Max(fs, fi) > 0.9 ? UIKit.Hex(UIKit.Bad) : UIKit.Hex(UIKit.Warn);
                    sb.AppendLine($"<color=#{c}>⚠ {p.Def.title}: {(skin ? "skin" : "internal")} {(skin ? p.SkinTemp : p.InternalTemp):N0} / {(skin ? p.Def.skinMaxTemp : p.Def.maxTemp):N0} K</color>");
                    if (++n >= 4) break;
                }
            }
            foreach (var p in v.Parts)
            {
                if (p.SmoothedJointLoad > 0.75 && n < 5)
                {
                    sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Warn)}>⚠ {p.Def.title}: joint {p.LastLoadDescription} {p.SmoothedJointLoad:P0} of limit</color>");
                    n++;
                }
            }
            foreach (var p in v.Parts)
            {
                var ch = p.GetModule<ParachuteModule>();
                if (ch != null && ch.IsOpen && ch.CanopyLoad > ch.Def.maxLoad * 0.7 && n < 6)
                {
                    sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Warn)}>⚠ Parachute load {ch.CanopyLoad / 1000:N0}/{ch.Def.maxLoad / 1000:N0} kN</color>");
                    n++;
                }
            }
            if (v.GForce > 8) { sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Bad)}>⚠ High acceleration {v.GForce:0.0} g</color>"); n++; }
            if (!v.HasControl && !v.IsEva) { sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Warn)}>No control: no crew or powered probe core</color>"); n++; }
            v.Resources.Totals("Electric", out double ec, out double ecMax);
            if (ecMax > 0 && ec < ecMax * 0.05) { sb.AppendLine($"<color=#{UIKit.Hex(UIKit.Warn)}>Electric charge depleted — reaction wheels offline</color>"); n++; }
            UIKit.Show(_warnPanel, n > 0);
            if (n > 0) _warnings.text = sb.ToString().TrimEnd('\n', '\r');
        }

        private void UpdateEva(Vessel v)
        {
            var em = v.RootPart.GetModule<EvaModule>();
            if (em == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"<b>EVA: {v.Record.evaCrew}</b>   {(em.Grounded ? "on the ground" : "floating")}   g {v.GravityAccel.magnitude:0.00} m/s²");
            var pack = v.RootPart.GetResource("Monoprop");
            double packMax = pack != null && pack.Max > 0 ? pack.Max : 5.0;
            sb.AppendLine($"Jetpack [R]: {(em.JetpackOn ? $"<color=#{UIKit.Hex(UIKit.Good)}>ON</color>" : "off")}   propellant {em.Propellant:0.00}/{packMax:0.00} kg");
            var hatch = Sim.FindBoardableHatch(v, out float d, out float rs);
            if (hatch != null)
            {
                bool ok = d <= EvaModule.BoardRange && rs <= EvaModule.BoardMaxRelSpeed;
                sb.AppendLine(ok ? $"<color=#{UIKit.Hex(UIKit.Good)}>[B] Board {hatch.Vessel.VesselName}</color>  ({d:0.0} m)"
                                 : $"Nearest hatch: {hatch.Vessel.VesselName} {d:0.0} m away, rel. speed {rs:0.0} m/s (need < {EvaModule.BoardRange:0} m, < {EvaModule.BoardMaxRelSpeed:0.0} m/s)");
            }
            sb.Append(em.Grounded ? "[G] Plant flag   [Space] Jump   [Shift] Run" : "WASD translate, Shift/Ctrl up/down with jetpack");
            _evaInfo.text = sb.ToString();
        }

        // ------------------------------------------------------------------ staging stack

        private void UpdateStaging(Vessel v)
        {
            var sig = new StringBuilder();
            sig.Append(v.CurrentStage).Append('|').Append(v.Parts.Count);
            foreach (var p in v.Parts) if (p.Stage >= 0) sig.Append(p.Uid).Append(':').Append(p.Stage).Append(',');
            string s = sig.ToString();
            if (s != _stageSignature)
            {
                _stageSignature = s;
                RebuildStages(v);
            }
            // live bars
            foreach (var row in _stageRows) row.Refresh();
        }

        private void RebuildStages(Vessel v)
        {
            _stageList.Clear();
            _stageRows.Clear();
            // Order: highest stage (fires first) at the top.
            for (int s = v.MaxStage; s >= 0; s--)
            {
                var parts = v.Parts.FindAll(p => p.Stage == s);
                if (parts.Count == 0) continue;
                var row = new StageRow(s, parts, s == v.CurrentStage);
                _stageList.Add(row.Root);
                _stageRows.Add(row);
            }
        }
    }

    /// <summary>One stage group in the staging stack: stage number + part icons with fuel/heat bars.</summary>
    public sealed class StageRow
    {
        public readonly int Stage;
        public readonly VisualElement Root;
        private readonly List<(Part part, VisualElement bar)> _items = new List<(Part, VisualElement)>();

        public StageRow(int stage, List<Part> parts, bool current)
        {
            Stage = stage;
            Root = UIKit.Element("panel panel--light stage-row" + (current ? " stage-row--current" : ""));
            Root.Add(UIKit.Label($"{stage}", "stage-row__num"));
            var items = UIKit.Element("stage-row__items");
            items.pickingMode = PickingMode.Ignore;
            Root.Add(items);
            var groups = new Dictionary<string, List<Part>>();
            foreach (var p in parts)
            {
                if (!groups.TryGetValue(p.Def.id, out var l)) groups[p.Def.id] = l = new List<Part>();
                l.Add(p);
            }
            foreach (var kv in groups)
            {
                var first = kv.Value[0];
                string icon = first.Def.engine != null ? (first.Def.engine.type == "solid" ? "stage_srb" : "stage_engine")
                    : first.Def.decoupler != null ? "stage_decoupler" : first.Def.parachute != null ? "stage_chute" : "dot";
                var item = UIKit.Element("stage-item");
                item.pickingMode = PickingMode.Ignore;
                item.Add(UIKit.Image(UIKit.Icon(icon), UIKit.TextColor, "stage-item__icon"));
                var text = UIKit.Element("stage-item__text");
                text.pickingMode = PickingMode.Ignore;
                text.Add(UIKit.Label($"{first.Def.title}{(kv.Value.Count > 1 ? " ×" + kv.Value.Count : "")}", "stage-item__title"));
                var bar = UIKit.Bar(false, "stage-item__bar");
                bar.pickingMode = PickingMode.Ignore;
                text.Add(bar);
                item.Add(text);
                items.Add(item);
                _items.Add((first, bar));
            }
            UIKit.Tooltip(Root, () => TooltipText(current, stage));
            Refresh();
        }

        private string TooltipText(bool current, int stage)
        {
            string t = current ? "Next stage to fire (Space)" : $"Stage {stage}";
            foreach (var (p, _) in _items)
            {
                var ch = p != null && !p.Destroyed ? p.GetModule<ParachuteModule>() : null;
                if (ch == null) continue;
                if (ch.State == ParachuteModule.ChuteState.Stowed || ch.State == ParachuteModule.ChuteState.Armed)
                {
                    var s = ch.DeploySafety(out string why);
                    string verdict = s == ParachuteModule.Safety.Safe ? "safe to deploy" : s == ParachuteModule.Safety.Risky ? "RISKY to deploy"
                        : s == ParachuteModule.Safety.Unsafe ? "UNSAFE: would tear or burn" : "waiting for air";
                    t += $"\n{p.Def.title}: {verdict} ({why})";
                }
                else t += $"\n{p.Def.title}: {ch.State}{(ch.FailReason != null ? " — " + ch.FailReason : "")}";
            }
            return t;
        }

        public void Refresh()
        {
            foreach (var (p, bar) in _items)
            {
                if (p == null || p.Destroyed) { UIKit.SetBar(bar, 0, Color.gray); continue; }
                var e = p.GetModule<EngineModule>();
                if (e != null && p.Vessel != null)
                {
                    double a = p.Vessel.Resources.Available(p, e.Def.propellant), m = p.Vessel.Resources.Capacity(p, e.Def.propellant);
                    float f = m > 0 ? (float)(a / m) : 0;
                    UIKit.SetBar(bar, f, e.Flameout ? UIKit.Bad : e.IsRunning ? UIKit.Good : new Color(0.95f, 0.72f, 0.25f));
                    continue;
                }
                var ch = p.GetModule<ParachuteModule>();
                if (ch != null)
                {
                    float f = ch.State == ParachuteModule.ChuteState.Failed ? 0 : 1;
                    Color c = ch.IsOpen ? UIKit.Good : UIKit.AccentDim;
                    if (ch.State == ParachuteModule.ChuteState.Stowed || ch.State == ParachuteModule.ChuteState.Armed)
                    {
                        // Deployment safety right now: green safe, amber risky, red unsafe (tears / burns).
                        var s = ch.DeploySafety(out _);
                        c = s == ParachuteModule.Safety.Unsafe ? UIKit.Bad : s == ParachuteModule.Safety.Risky ? UIKit.Warn
                          : s == ParachuteModule.Safety.Safe ? UIKit.Good : UIKit.AccentDim;
                    }
                    UIKit.SetBar(bar, f, c);
                    continue;
                }
                float heat = (float)(p.SkinTemp / p.Def.skinMaxTemp);
                UIKit.SetBar(bar, Mathf.Clamp01(heat), heat > 0.8f ? UIKit.Bad : UIKit.AccentDim);
            }
        }
    }
}
