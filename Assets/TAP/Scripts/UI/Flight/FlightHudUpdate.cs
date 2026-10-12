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
            using var timer = SimTimers.Measure(SimStep.Hud);
            var v = Sim.ActiveVessel;
            UpdateMessages();
            UpdateClock();
            if (v == null)
            {
                _altitude.text = "—";
                return;
            }
            bool eva = v.IsEva, map = UiState.MapActive;
            // the map has its own orbit and node panels in those corners
            _layout.Want("staging", !eva && !map);
            _layout.Want("eva", eva && !map);
            _layout.Want("mfd", !map);
            FitStackHeight();
            UpdatePfd(v);
            _slowTimer -= Time.unscaledDeltaTime;
            if (_slowTimer <= 0)
            {
                _slowTimer = 0.2f;
                UpdateStaging(v);
                UpdateWarnings(v);
                if (eva) UpdateEva(v);
                UpdatePage(v);
            }
            UpdateBurn(v);
            if (UIKit.Shown(_bigNotice) && Time.unscaledTime > _bigNoticeUntil) UIKit.Show(_bigNotice, false);
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

        /// <summary>The staging stack grows up from the bottom; it stops under whatever sits above it on the left (clock, mission guide).</summary>
        private void FitStackHeight()
        {
            var panel = UIKit.Shown(_stagePanel) ? _stagePanel : UIKit.Shown(_evaPanel) ? _evaPanel : null;
            if (panel == null) return;
            var me = panel.worldBound;
            float top = 16;
            foreach (var other in new[] { _clockPanel, MissionGuide.Panel })
            {
                if (other == null || other.panel == null || !UIKit.Shown(other)) continue;
                var r = other.worldBound;
                if (r.xMax > me.xMin && r.xMin < me.xMax && r.center.y < me.center.y) top = Mathf.Max(top, r.yMax + 10);
            }
            float k = panel.resolvedStyle.scale.value.x;
            float room = (me.yMax - top) / Mathf.Max(0.01f, k);
            if (panel == _stagePanel)
            {
                // the unit's frame and the list's header take ~66 px
                float max = Mathf.Max(80, room - 66);
                if (Mathf.Abs(_stageList.resolvedStyle.maxHeight.value - max) > 1) _stageList.style.maxHeight = max;
            }
            else
            {
                float max = Mathf.Max(90, room);
                if (Mathf.Abs(_evaPanel.resolvedStyle.maxHeight.value - max) > 1) _evaPanel.style.maxHeight = max;
            }
        }

        private void UpdateClock()
        {
            var v = Sim.ActiveVessel;
            _met.text = v != null && v.Record.launchUT >= 0 ? "T+" + MathD.FormatDuration(Sim.UT - v.Record.launchUT) : "T-0  ON PAD";
            _ut.text = Loc.Name(MathD.FormatUT(Sim.UT));
            bool warping = Sim.Warp.OnRails || Sim.Warp.PhysicsIndex > 0;
            string w;
            if (Sim.Paused || UiState.Paused) w = "PAUSED";
            else if (Sim.Warp.OnRails) w = $"{TimeWarp.RailsRates[Sim.Warp.RailsIndex]:N0}x";
            else if (Sim.Warp.PhysicsIndex > 0) w = $"{TimeWarp.PhysicsRates[Sim.Warp.PhysicsIndex]:0}x P";
            else w = "1x";
            _warpRate.text = w;
            _warpRate.EnableInClassList("warping", warping || Sim.Paused || UiState.Paused);
            int rails = Sim.Warp.OnRails ? Sim.Warp.RailsIndex : 0;
            for (int i = 0; i < _warpCells.Count; i++)
            {
                _warpCells[i].EnableInClassList("lit", Sim.Warp.PhysicsIndex == 0 && i < rails);
                UIKit.SetOn(_warpCells[i], Sim.Warp.PhysicsIndex == 0 && i == rails);
            }
            UIKit.SetOn(_physBtn, Sim.Warp.PhysicsIndex > 0);
            int max = Sim.MaxRailsIndex(out string reason);
            _warpLimit.text = max < TimeWarp.RailsRates.Length - 1 && reason != null ? $"LIMIT {TimeWarp.RailsRates[max]:N0}x: {reason.ToUpperInvariant()}" : "";
        }

        // ------------------------------------------------------------------ primary flight display

        private void UpdatePfd(Vessel v)
        {
            var c = v.Ctrl;
            double spd;
            switch (c.SpeedMode)
            {
                case SpeedMode.Surface: _speedMode.text = "SPD SURF"; spd = v.SurfaceSpeed; break;
                case SpeedMode.Orbit: _speedMode.text = "SPD ORB"; spd = v.OrbitalSpeed; break;
                default: _speedMode.text = "SPD TGT"; spd = v.SpeedModeVelocity.magnitude; break;
            }
            _speed.text = $"{spd:N1}";
            double vs = v.VerticalSpeed;
            _hspeed.text = $"H {Math.Sqrt(Math.Max(0, v.SurfaceSpeed * v.SurfaceSpeed - vs * vs)):N0} m/s";
            double alt = _altTerrain ? v.AltitudeAGL : v.Altitude;
            _altMode.text = _altTerrain ? "ALT AGL" : "ALT ASL";
            _altitude.text = alt < 100000 ? $"{alt:N0}" : $"{alt / 1000:N1} km";
            _vspeed.text = $"{(vs > 0.05 ? "▲" : vs < -0.05 ? "▼" : "•")} {Math.Abs(vs):N1} m/s";

            UIKit.SetBar(_throttle, c.Throttle, Green);
            _throttleText.text = $"{c.Throttle * 100:0}";
            float g = (float)v.GForce;
            UIKit.SetBar(_gmeter, g / 10f, g > 8 ? Red : g > 5 ? Amber : Cyan);
            _gText.text = $"{g:0.0}";

            UIKit.SetOn(_sasBtn, c.Sas);
            UIKit.SetOn(_rcsBtn, c.Rcs);
            UIKit.SetOn(_legsBtn, c.LegsDeployed);
            bool hasLegs = false;
            foreach (var p in v.Parts) if (p.Def.landingLeg != null) { hasLegs = true; break; }
            if (_legsBtn.enabledSelf != hasLegs) _legsBtn.SetEnabled(hasLegs);
            foreach (var kv in _sasModes)
            {
                bool avail = kv.Key == SasMode.StabilityAssist || v.TryGetSasDirection(kv.Key, out _);
                bool enabled = v.HasSas && avail;
                if (kv.Value.enabledSelf != enabled) kv.Value.SetEnabled(enabled);
                UIKit.SetOn(kv.Value, c.Sas && c.SasMode == kv.Key);
            }
        }

        // ------------------------------------------------------------------ orbit / maneuver display

        private void Set(string row, string text, Color? color = null)
        {
            var l = _rows[row];
            if (l.text != text) l.text = text;
            l.style.color = color.HasValue ? new StyleColor(color.Value) : new StyleColor(StyleKeyword.Null);
        }

        private void ShowRow(string row, bool on)
        {
            var e = _rowEls[row];
            if (UIKit.Shown(e) != on) UIKit.Show(e, on);
        }

        private void UpdatePage(Vessel v)
        {
            if (!UIKit.Shown(_mfdPanel)) return;
            switch (_page)
            {
                case "orb": UpdateOrbit(v); break;
                case "mnv": UpdateManeuver(v); break;
                case "tgt": UpdateTarget(v); break;
                case "res": UpdateResources(v); break;
                default: UpdateData(v); break;
            }
        }

        private static bool OnGround(Vessel v) => v.Situation == Situation.Landed || v.Situation == Situation.Prelaunch || v.Situation == Situation.Splashed;

        private void UpdateOrbit(Vessel v)
        {
            var body = v.MainBody;
            var o = v.Orbit;
            bool ground = OnGround(v);
            _orbTitle.text = "ORBIT · " + Loc.Name(body.Name.ToUpperInvariant());
            _plot.Set(ground ? null : o, body.Radius, body.Atmosphere?.Height ?? 0, Sim.UT);
            if (o == null || ground)
            {
                _orbState.text = ground ? "ON THE SURFACE" : "—";
                _orbState.style.color = Dim;
                foreach (var r in new[] { "ap", "tap", "pe", "tpe", "inc", "ecc", "per" }) Set("orb." + r, "—");
                _orbNote.text = "";
                return;
            }
            double ap = o.ApoapsisRadius - body.Radius, pe = o.PeriapsisRadius - body.Radius;
            _orbState.text = !o.IsElliptic ? "ESCAPE" : pe < 0 ? "SUB-ORBITAL" : body.Atmosphere != null && pe < body.Atmosphere.Height ? "IN ATMOSPHERE" : "STABLE";
            _orbState.style.color = !o.IsElliptic || pe < 0 ? Amber : Dim;
            Set("orb.ap", o.IsElliptic ? MathD.FormatDistance(ap) : "ESCAPE");
            Set("orb.tap", o.IsElliptic ? MathD.FormatDuration(o.TimeToApoapsis(Sim.UT)) : "—");
            Set("orb.pe", MathD.FormatDistance(pe), pe < 0 ? Amber : (Color?)null);
            Set("orb.tpe", MathD.FormatDuration(o.TimeToPeriapsis(Sim.UT)));
            Set("orb.inc", $"{o.Inclination * MathD.Rad2Deg:0.00}°");
            Set("orb.ecc", $"{o.Eccentricity:0.0000}");
            Set("orb.per", o.IsElliptic ? MathD.FormatDuration(o.Period) : "—");
            string note = "";
            var traj = Scene.Trajectory;
            if (traj != null && traj.Current.Count > 1)
            {
                var p0 = traj.Current[0];
                if (p0.EndType == PatchEnd.SoiEnter) note = $"{Loc.Name(p0.NextBody.Name)} encounter in {MathD.FormatDuration(p0.EndUT - Sim.UT)}";
                if (p0.EndType == PatchEnd.SoiExit) note = $"Leaving {Loc.Name(body.Name)} SOI in {MathD.FormatDuration(p0.EndUT - Sim.UT)}";
            }
            if (_orbNote.text != note) _orbNote.text = note;
        }

        private void UpdateManeuver(Vessel v)
        {
            bool has = v.ManeuverNodes.Count > 0;
            UIKit.Show(_mnvBody, has);
            UIKit.Show(_mnvEmpty, !has);
            _mnvState.text = has ? $"NODE 1/{v.ManeuverNodes.Count}" : "";
            if (!has) return;
            var info = ManeuverPlanner.Info(v, Sim.UT);
            bool now = info.TimeToBurnStart <= 0;
            _mnvTimeLabel.text = now ? "BURN NOW" : "BURN IN";
            _mnvTimeLabel.style.color = now ? Amber : Dim;
            _mnvTime.text = now ? "00:00" : MathD.FormatDuration(info.TimeToBurnStart);
            _mnvDv.text = $"{info.RemainingDeltaV:N1}";
            UIKit.SetBar(_mnvProgress, info.TotalDeltaV > 0 ? (float)(info.RemainingDeltaV / info.TotalDeltaV) : 0, Magenta);
            Set("mnv.burn", double.IsNaN(info.BurnTime) ? "NO THRUST" : MathD.FormatDuration(info.BurnTime), double.IsNaN(info.BurnTime) ? Amber : (Color?)null);
            Set("mnv.node", MathD.FormatDuration(info.TimeToNode, true));
            Set("mnv.align", $"{info.AngleToBurn:0.0}°", info.AngleToBurn > 5 ? Amber : Green);
            Set("mnv.total", $"{info.TotalDeltaV:N1} m/s");
        }

        private void UpdateTarget(Vessel v)
        {
            bool has = v.TryGetTargetState(Sim.UT, out var tp, out var tv);
            UIKit.Show(_tgtRows, has);
            UIKit.Show(_tgtEmpty, !has);
            _tgtTitle.text = has ? "TARGET · " + Loc.Name(v.TargetName.ToUpperInvariant()) : "TARGET";
            if (!has) return;
            Set("tgt.dist", MathD.FormatDistance((tp - v.TruePosition).magnitude));
            Set("tgt.rel", $"{(v.TrueVelocity - tv).magnitude:N1} m/s");
            var traj = Scene.Trajectory;
            bool close = traj != null && traj.HasClosest;
            foreach (var r in new[] { "tgt.ca", "tgt.cat", "tgt.carel" }) ShowRow(r, close);
            if (close)
            {
                Set("tgt.ca", MathD.FormatDistance(traj.ClosestDistance));
                Set("tgt.cat", MathD.FormatDuration(traj.ClosestUT - Sim.UT));
                Set("tgt.carel", $"{traj.ClosestRelSpeed:N1} m/s");
            }
            var enc = traj?.FirstEncounter(traj.HasNodes);
            ShowRow("tgt.enc", enc != null);
            if (enc != null) Set("tgt.enc", MathD.FormatDistance(enc.Orbit.PeriapsisRadius - enc.Body.Radius) + (traj.HasNodes ? " (planned)" : ""), Magenta);
        }

        private readonly List<(string id, Label name, VisualElement bar, Label val)> _resItems = new List<(string, Label, VisualElement, Label)>();

        private void UpdateResources(Vessel v)
        {
            // rows: the propellant the lit engines can reach, then every resource the vessel holds
            var lines = new List<(string id, string name, double a, double m, string unit, Color col)>();
            double sAmt = 0, sMax = 0;
            foreach (var p in v.Parts)
            {
                var e = p.GetModule<EngineModule>();
                if (e == null || !e.Ignited || e.IsSolid) continue;
                sAmt += v.Resources.Available(p, e.Def.propellant);
                sMax += v.Resources.Capacity(p, e.Def.propellant);
                break;
            }
            if (sMax > 0) lines.Add(("stage", "STAGE PROPELLANT", sAmt, sMax, "kg", Amber));
            foreach (var rd in PartDatabase.Instance.ResourceDefinitions)
            {
                v.Resources.Totals(rd.id, out double a, out double m);
                if (m > 0) lines.Add((rd.id, Loc.Data(rd.name).ToUpperInvariant(), a, m, rd.unit, rd.Color));
            }
            bool same = lines.Count == _resItems.Count;
            for (int i = 0; same && i < lines.Count; i++) same = lines[i].id == _resItems[i].id;
            if (!same)
            {
                _resRows.Clear();
                _resItems.Clear();
                foreach (var l in lines)
                {
                    var row = UIKit.Element("res-row");
                    row.pickingMode = PickingMode.Ignore;
                    var name = UIKit.Label(l.name, "res-row__name");
                    var bar = UIKit.Bar(false, "res-row__bar");
                    bar.pickingMode = PickingMode.Ignore;
                    var val = UIKit.Label("", "res-row__val no-loc");
                    row.Add(name); row.Add(bar); row.Add(val);
                    _resRows.Add(row);
                    _resItems.Add((l.id, name, bar, val));
                }
            }
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                float f = l.m > 0 ? (float)(l.a / l.m) : 0;
                UIKit.SetBar(_resItems[i].bar, f, f < 0.1f ? Red : l.col);
                _resItems[i].val.text = $"{l.a:N0} / {l.m:N0} {l.unit}";
            }
            _resCrew.text = v.CrewCapacity > 0 ? $"CREW {v.CrewCount}/{v.CrewCapacity}" : "";
        }

        private void UpdateData(Vessel v)
        {
            var body = v.MainBody;
            _dataTitle.text = Loc.Name(v.VesselName.ToUpperInvariant());
            string sit = v.Situation == Situation.SubOrbital ? "SUB-ORBITAL" : v.Situation.ToString().ToUpperInvariant();
            _dataState.text = $"{sit} · {Loc.Name(body.Name.ToUpperInvariant())}";
            Set("data.surf", $"{v.SurfaceSpeed:N1} m/s");
            Set("data.orb", $"{v.OrbitalSpeed:N1} m/s");
            bool air = body.Atmosphere != null && v.Altitude < body.Atmosphere.Height;
            foreach (var r in new[] { "data.mach", "data.q", "data.p", "data.temp" }) ShowRow(r, air);
            if (air)
            {
                Set("data.mach", $"{v.Mach:0.00}");
                Set("data.q", $"{v.DynamicPressure / 1000:0.0} kPa");
                Set("data.p", $"{v.StaticPressure / 1000:0.00} kPa");
                Set("data.temp", $"{v.AirTemperature:0} K");
            }
            v.GetPropulsion(out double thrust, out _);
            double g = v.GravityAccel.magnitude;
            Set("data.mass", $"{v.TotalMass / 1000:0.00} t");
            Set("data.twr", $"{(g > 0 && v.TotalMass > 0 ? thrust / (v.TotalMass * g) : 0):0.00}");
            var dv = StageDeltaV(v);
            Set("data.dvs", $"{dv.stage:N0} m/s");
            Set("data.dvt", $"{dv.total:N0} m/s");
            Set("data.parts", $"{v.Parts.Count}");
            Set("data.stage", $"{Math.Max(0, v.CurrentStage)}");
        }

        private float _dvTimer;
        private (double stage, double total) _dvCache;
        private readonly Dictionary<int, TAP.Parts.StageDeltaV> _dvByStage = new Dictionary<int, TAP.Parts.StageDeltaV>();

        /// <summary>Δv now (at this altitude): the burn in progress (engines already lit) first, then the stages still to fire. Once a second.</summary>
        private (double stage, double total) StageDeltaV(Vessel v)
        {
            if (Time.unscaledTime < _dvTimer) return _dvCache;
            _dvTimer = Time.unscaledTime + 1f;
            var stages = v.ComputeStageDeltaV(v.GravityAccel.magnitude, v.StaticPressure / 101325.0);
            double total = 0, cur = 0;
            bool first = true;
            _dvByStage.Clear();
            foreach (var s in stages)
            {
                total += s.DeltaVCurrent;
                _dvByStage[s.Stage] = s;
                if (first && s.HasEngines) { cur = s.DeltaVCurrent; first = false; }
            }
            _dvCache = (cur, total);
            return _dvCache;
        }

        // ------------------------------------------------------------------ burn strip, warnings, EVA

        private void UpdateBurn(Vessel v)
        {
            bool has = v.ManeuverNodes.Count > 0;
            // the strip repeats the maneuver page, so it shows only while that page is not on screen
            bool pageShown = _layout.IsShown("mfd") && _page == "mnv";
            _layout.Want("burn", has && !pageShown);
            if (!has || pageShown) return;
            var info = ManeuverPlanner.Info(v, Sim.UT);
            string t = info.TimeToBurnStart > 0 ? $"BURN IN <color=#{UIKit.Hex(White)}>{MathD.FormatDuration(info.TimeToBurnStart)}</color>"
                : $"<color=#{UIKit.Hex(Amber)}>BURN NOW</color>";
            string align = $"<color=#{UIKit.Hex(info.AngleToBurn > 5 ? Amber : Green)}>ALIGN {info.AngleToBurn:0.0}°</color>";
            string burn = double.IsNaN(info.BurnTime) ? "NO THRUST" : MathD.FormatDuration(info.BurnTime);
            _burnInfo.text = $"<color=#{UIKit.Hex(Dim)}>{t}  ·  Δv <color=#{UIKit.Hex(Magenta)}>{info.RemainingDeltaV:N1}</color> m/s  ·  {burn}  ·  {align}</color>";
        }

        private void UpdateWarnings(Vessel v)
        {
            var lines = new List<(string code, string text, bool bad)>();
            foreach (var p in v.Parts)
            {
                // Share of the heat-up from room temperature to the limit (a suit at 290 K of its 380 K is fine).
                const double roomT = 300;
                double fs = (p.SkinTemp - roomT) / Math.Max(1, p.Def.skinMaxTemp - roomT);
                double fi = (p.InternalTemp - roomT) / Math.Max(1, p.Def.maxTemp - roomT);
                if (fs > 0.72 || fi > 0.72)
                {
                    bool skin = fs >= fi;
                    lines.Add(("HEAT", $"{Loc.Data(p.Def.title)}: {(skin ? "skin" : "internal")} {(skin ? p.SkinTemp : p.InternalTemp):N0} / {(skin ? p.Def.skinMaxTemp : p.Def.maxTemp):N0} K", Math.Max(fs, fi) > 0.9));
                    if (lines.Count >= 4) break;
                }
            }
            foreach (var p in v.Parts)
                if (p.SmoothedJointLoad > 0.75 && lines.Count < 5)
                    lines.Add(("STRUCT", $"{Loc.Data(p.Def.title)}: joint {p.LastLoadDescription} {p.SmoothedJointLoad:P0} of limit", p.SmoothedJointLoad > 0.95));
            foreach (var p in v.Parts)
            {
                var ch = p.GetModule<ParachuteModule>();
                if (ch != null && ch.IsOpen && ch.CanopyLoad > ch.Def.maxLoad * 0.7 && lines.Count < 6)
                    lines.Add(("CHUTE", $"Parachute load {ch.CanopyLoad / 1000:N0} / {ch.Def.maxLoad / 1000:N0} kN", false));
            }
            if (v.GForce > 8) lines.Add(("G LOAD", $"High acceleration {v.GForce:0.0} g", true));
            if (!v.HasControl && !v.IsEva) lines.Add(("NO CTRL", "No crew or powered probe core", false));
            v.Resources.Totals("Electric", out double ec, out double ecMax);
            if (ecMax > 0 && ec < ecMax * 0.05) lines.Add(("ELEC", "Electric charge depleted: reaction wheels offline", false));

            _layout.Want("memo", lines.Count > 0);
            var sig = new StringBuilder();
            foreach (var l in lines) sig.Append(l.code).Append(l.text).Append(l.bad).Append('|');
            string s = sig.ToString();
            if (s == _memoSignature) return;
            _memoSignature = s;
            _memo.Clear();
            foreach (var l in lines)
            {
                var row = UIKit.Element("memo-line" + (l.bad ? " memo-line--bad" : ""));
                row.Add(UIKit.Label(l.code, "memo-line__code"));
                row.Add(UIKit.Label(l.text, "memo-line__text"));
                _memo.Add(row);
            }
        }

        private void UpdateEva(Vessel v)
        {
            var em = v.RootPart.GetModule<EvaModule>();
            if (em == null) return;
            var sb = new StringBuilder();
            sb.AppendLine($"<b>EVA: {v.Record.evaCrew}</b>   {(em.Grounded ? "on the ground" : "floating")}   g {v.GravityAccel.magnitude:0.00} m/s²");
            var pack = v.RootPart.GetResource("Monoprop");
            double packMax = pack != null && pack.Max > 0 ? pack.Max : 5.0;
            sb.AppendLine($"Jetpack [R]: {(em.JetpackOn ? $"<color=#{UIKit.Hex(Green)}>ON</color>" : "off")}   propellant {em.Propellant:0.00}/{packMax:0.00} kg");
            var hatch = Sim.FindBoardableHatch(v, out float d, out float rs);
            if (hatch != null)
            {
                bool ok = d <= EvaModule.BoardRange && rs <= EvaModule.BoardMaxRelSpeed;
                sb.AppendLine(ok ? $"<color=#{UIKit.Hex(Green)}>[B] Board {Loc.Name(hatch.Vessel.VesselName)}</color>  ({d:0.0} m)"
                                 : $"Nearest hatch: {Loc.Name(hatch.Vessel.VesselName)} {d:0.0} m away, rel. speed {rs:0.0} m/s (need < {EvaModule.BoardRange:0} m, < {EvaModule.BoardMaxRelSpeed:0.0} m/s)");
            }
            sb.Append(em.Grounded ? "[G] Plant flag   [Space] Jump   [Shift] Run" : "WASD translate, Shift/Ctrl up/down with jetpack");
            _evaInfo.text = sb.ToString();
        }

        // ------------------------------------------------------------------ staging stack

        private void UpdateStaging(Vessel v)
        {
            if (!UIKit.Shown(_stagePanel)) return;
            StageDeltaV(v);
            double total = 0;
            foreach (var s in _dvByStage.Values) total += s.DeltaVCurrent;
            _stageTotal.text = $"ΔV {total:N0}";
            var sig = new StringBuilder();
            sig.Append(v.CurrentStage).Append('|').Append(v.Parts.Count);
            foreach (var p in v.Parts) if (p.Stage >= 0) sig.Append(p.Uid).Append(':').Append(p.Stage).Append(',');
            string key = sig.ToString();
            if (key != _stageSignature)
            {
                _stageSignature = key;
                RebuildStages(v);
            }
            foreach (var row in _stageRows) row.Refresh(_dvByStage.TryGetValue(row.Stage, out var dv) ? dv : null);
        }

        private void RebuildStages(Vessel v)
        {
            _stageList.Clear();
            _stageRows.Clear();
            // KSP order: stage 0 on top, the next stage to fire at the bottom; a fired stage stays below it while its parts are on.
            for (int s = 0; s <= v.MaxStage; s++)
            {
                var parts = v.Parts.FindAll(p => p.Stage == s);
                if (parts.Count == 0) continue;
                var row = new StageRow(s, parts, s == v.CurrentStage, s > v.CurrentStage);
                _stageList.Add(row.Root);
                _stageRows.Add(row);
            }
            // keep the bottom (the next stage) in view
            _stageList.schedule.Execute(() => _stageList.scrollOffset = new Vector2(0, float.MaxValue));
        }
    }

    /// <summary>One stage in the staging stack: number, Δv and burn time, and its part icons with fuel (or chute safety) bars.</summary>
    public sealed class StageRow
    {
        public readonly int Stage;
        public readonly VisualElement Root;
        private readonly Label _dv, _unit, _burn;
        private readonly bool _current, _fired;
        private readonly List<(Part part, VisualElement bar)> _items = new List<(Part, VisualElement)>();
        private TAP.Parts.StageDeltaV _last;

        public StageRow(int stage, List<Part> parts, bool current, bool fired)
        {
            Stage = stage;
            _current = current;
            _fired = fired;
            Root = UIKit.Element("stg" + (current ? " stg--next" : "") + (fired ? " stg--fired" : ""));
            var head = UIKit.Element("fd-row");
            head.pickingMode = PickingMode.Ignore;
            head.Add(UIKit.Label($"{stage}", "stg__num no-loc"));
            head.Add(UIKit.Element("grow"));
            _dv = UIKit.Label("", "stg__dv no-loc");
            _unit = UIKit.Label("M/S", "stg__unit");
            head.Add(_dv);
            head.Add(_unit);
            Root.Add(head);
            var sub = UIKit.Element("fd-row stg__sub");
            sub.pickingMode = PickingMode.Ignore;
            sub.Add(UIKit.Label(current ? "NEXT · SPACE" : fired ? "ACTIVE" : "", "stg__state"));
            sub.Add(UIKit.Element("grow"));
            _burn = UIKit.Label("", "stg__burn no-loc");
            sub.Add(_burn);
            Root.Add(sub);
            var icons = UIKit.Element("stg__icons");
            icons.pickingMode = PickingMode.Ignore;
            Root.Add(icons);
            var groups = new Dictionary<string, List<Part>>();
            foreach (var p in parts)
            {
                if (!groups.TryGetValue(p.Def.id, out var l)) groups[p.Def.id] = l = new List<Part>();
                l.Add(p);
            }
            foreach (var kv in groups)
            {
                var first = kv.Value[0];
                bool engine = first.Def.engine != null;
                string icon = engine ? (first.Def.engine.type == "solid" ? "stage_srb" : "stage_engine")
                    : first.Def.decoupler != null ? "stage_decoupler" : first.Def.parachute != null ? "stage_chute" : "dot";
                Color tint = engine ? (first.Def.engine.type == "solid" ? new Color32(255, 138, 76, 255) : FlightHud.Amber)
                    : first.Def.decoupler != null ? FlightHud.Cyan : first.Def.parachute != null ? FlightHud.Green : FlightHud.White;
                var tile = UIKit.Element("stg-tile");
                tile.pickingMode = PickingMode.Ignore;
                tile.Add(UIKit.Image(UIKit.Icon(icon), tint, "stg-tile__icon"));
                if (kv.Value.Count > 1) tile.Add(UIKit.Label("×" + kv.Value.Count, "stg-tile__count no-loc"));
                var bar = UIKit.Bar(false, "stg-tile__bar");
                bar.pickingMode = PickingMode.Ignore;
                tile.Add(bar);
                icons.Add(tile);
                _items.Add((first, bar));
            }
            UIKit.Tooltip(Root, TooltipText);
        }

        private string TooltipText()
        {
            var sb = new StringBuilder(_current ? "Next stage to fire (Space)" : _fired ? $"Stage {Stage}, fired" : $"Stage {Stage}");
            if (_last != null && _last.HasEngines)
                sb.Append($"\nΔv {_last.DeltaVCurrent:N0} m/s (vacuum {_last.DeltaVVac:N0})   burn {MathD.FormatDuration(_last.BurnTime)}   TWR {_last.TwrAsl:0.00}");
            foreach (var (p, _) in _items)
            {
                if (p == null || p.Destroyed) continue;
                sb.Append($"\n{Loc.Data(p.Def.title)}");
                var ch = p.GetModule<ParachuteModule>();
                if (ch == null) continue;
                if (ch.State == ParachuteModule.ChuteState.Stowed || ch.State == ParachuteModule.ChuteState.Armed)
                {
                    var s = ch.DeploySafety(out string why);
                    string verdict = s == ParachuteModule.Safety.Safe ? "safe to deploy" : s == ParachuteModule.Safety.Risky ? "RISKY to deploy"
                        : s == ParachuteModule.Safety.Unsafe ? "UNSAFE: would tear or burn" : "waiting for air";
                    sb.Append($": {verdict} ({why})");
                }
                else sb.Append($": {ch.State}{(ch.FailReason != null ? " — " + ch.FailReason : "")}");
            }
            return sb.ToString();
        }

        public void Refresh(TAP.Parts.StageDeltaV dv)
        {
            _last = dv;
            bool has = dv != null && dv.HasEngines && dv.DeltaVCurrent > 0.5;
            _dv.text = has ? $"{dv.DeltaVCurrent:N0}" : "—";
            _dv.EnableInClassList("stg__none", !has);
            UIKit.Show(_unit, has);
            _burn.text = has ? MathD.FormatDuration(dv.BurnTime) : "";
            foreach (var (p, bar) in _items)
            {
                if (p == null || p.Destroyed) { UIKit.SetBar(bar, 0, Color.gray); continue; }
                var e = p.GetModule<EngineModule>();
                if (e != null && p.Vessel != null)
                {
                    double a = p.Vessel.Resources.Available(p, e.Def.propellant), m = p.Vessel.Resources.Capacity(p, e.Def.propellant);
                    float f = m > 0 ? (float)(a / m) : 0;
                    UIKit.SetBar(bar, f, e.Flameout ? FlightHud.Red : e.IsRunning ? FlightHud.Green : f < 0.25f ? FlightHud.Red : FlightHud.Amber);
                    continue;
                }
                var ch = p.GetModule<ParachuteModule>();
                if (ch != null)
                {
                    float f = ch.State == ParachuteModule.ChuteState.Failed ? 0 : 1;
                    Color c = ch.IsOpen ? FlightHud.Green : FlightHud.Dim;
                    if (ch.State == ParachuteModule.ChuteState.Stowed || ch.State == ParachuteModule.ChuteState.Armed)
                    {
                        // Deployment safety right now: green safe, amber risky, red unsafe (tears / burns).
                        var s = ch.DeploySafety(out _);
                        c = s == ParachuteModule.Safety.Unsafe ? FlightHud.Red : s == ParachuteModule.Safety.Risky ? FlightHud.Amber
                          : s == ParachuteModule.Safety.Safe ? FlightHud.Green : FlightHud.Dim;
                    }
                    UIKit.SetBar(bar, f, c);
                    continue;
                }
                float heat = (float)(p.SkinTemp / p.Def.skinMaxTemp);
                UIKit.SetBar(bar, Mathf.Clamp01(heat), heat > 0.8f ? FlightHud.Red : FlightHud.Dim);
            }
        }
    }
}
