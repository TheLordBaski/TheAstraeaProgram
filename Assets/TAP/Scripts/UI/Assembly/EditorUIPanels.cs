using System;
using System.Collections.Generic;
using System.Text;
using TAP.Construction;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TAP.UI
{
    public sealed partial class EditorUI
    {
        // engineer's report
        private Label _statsText, _warningsText;
        // staging
        private ScrollView _stageList;
        private Label _stagingTitle;
        private Button _resetStagingBtn;
        private readonly List<(VisualElement block, int stage)> _stageDropTargets = new List<(VisualElement, int)>();
        private readonly List<(VisualElement strip, int newStageNumber)> _insertTargets = new List<(VisualElement, int)>();
        // dialogs
        private VisualElement _help, _loadDialog, _flightsDialog, _partMenu;
        private ScrollView _loadList, _flightsList;
        private Label _partMenuText;
        private VisualElement _partMenuButtons;
        private int _partMenuIndex = -1;
        private Vector2 _partMenuAnchor;

        // ------------------------------------------------------------------ right panel

        private void BuildRightPanel()
        {
            _statsText = Q<Label>("stats");
            _warningsText = Q<Label>("warnings-text");
            _stagingTitle = Q<Label>("staging-title");
            _resetStagingBtn = Q<Button>("reset-staging");
            _resetStagingBtn.clicked += () => Ed.ResetStaging();
            UIKit.Tooltip(_resetStagingBtn, () => "Recompute the automatic staging order");
            _stageList = Q<ScrollView>("stages");
        }

        private void OnStatsUpdated()
        {
            var s = _scene.Stats;
            if (s == null) return;
            _envBtn.text = EnvName(_scene.Environment);
            var sb = new StringBuilder();
            string dim = UIKit.Hex(UIKit.TextDim);
            sb.Append($"<size=19><b>Δv {s.TotalDvEnv:N0} m/s</b></size>  <color=#{dim}>vac {s.TotalDvVac:N0} · sea level {s.TotalDvAsl:N0}</color>\n");
            Color twrCol = s.LaunchTwr <= 0 ? UIKit.TextDim : s.LaunchTwr < 1 ? UIKit.Bad : s.LaunchTwr < 1.2 ? UIKit.Warn : UIKit.Good;
            sb.Append($"Launch TWR <color=#{UIKit.Hex(twrCol)}><b>{(s.LaunchTwr > 0 ? s.LaunchTwr.ToString("0.00") : "--")}</b></color>   ");
            sb.Append($"Mass <b>{PartInfoText.Mass(s.WetMass)}</b> <color=#{dim}>(dry {PartInfoText.Mass(s.DryMass)})</color>\n");
            sb.Append($"Parts {s.PartCount} · Height {s.Height:0.0} m · Width {s.Width:0.0} m\n");
            sb.Append($"Crew seats {s.CrewSeats} <color=#{dim}>({s.CrewAvailable} available)</color> · EC {s.ElectricCharge:0}\n");
            sb.Append($"Monoprop {s.Monoprop:0} kg · Liquid {s.LiquidFuel:0} kg · Solid {s.SolidFuel:0} kg\n");
            if (s.HasCoP)
            {
                string stab = s.StabilityMargin < -0.3f ? $"<color=#{UIKit.Hex(UIKit.Good)}>stable</color>" :
                    s.StabilityMargin < 0.3f ? $"<color=#{UIKit.Hex(UIKit.Warn)}>marginal</color>" : $"<color=#{UIKit.Hex(UIKit.Bad)}>unstable</color>";
                sb.Append($"Aero: CoP {-s.StabilityMargin:+0.0;-0.0} m below CoM ({stab})\n");
            }
            sb.Append($"<size=13><color=#{dim}>Readouts for {EnvName(_scene.Environment)} (g = {s.Gravity:0.00} m/s²)</color></size>");
            _statsText.text = sb.ToString();

            var wb = new StringBuilder();
            foreach (var w in s.Warnings)
            {
                Color c = w.Blocking ? UIKit.Bad : w.Info ? UIKit.Accent : UIKit.Warn;
                string icon = w.Blocking ? "✖" : w.Info ? "ℹ" : "⚠";
                wb.Append($"<color=#{UIKit.Hex(c)}>{icon}</color> {w.Text}\n");
            }
            if (s.Warnings.Count == 0) wb.Append($"<color=#{UIKit.Hex(UIKit.Good)}>✔ No problems found.</color>");
            _warningsText.text = wb.ToString().TrimEnd('\n');
            _launchBtn.SetEnabled(!s.HasBlocking);
            RebuildStaging(s);
        }

        // ------------------------------------------------------------------ staging list

        private void RebuildStaging(DesignStats stats)
        {
            _stageList.Clear();
            _stageDropTargets.Clear();
            _insertTargets.Clear();
            var d = Ed.Design;
            int count = Ed.StageCount;
            _stagingTitle.text = Ed.AutoStaging ? "STAGING <size=12><color=#" + UIKit.Hex(UIKit.TextDim) + ">(automatic)</color></size>"
                                                : "STAGING <size=12><color=#" + UIKit.Hex(UIKit.Warn) + ">(custom)</color></size>";
            _resetStagingBtn.SetEnabled(!Ed.AutoStaging);
            if (count == 0)
            {
                _stageList.Add(UIKit.Label("No stageable parts (engines, decouplers, parachutes).", "text-dim wrap"));
                return;
            }
            // Same order and numbers as the flight staging stack: the first stage to fire (highest number) on top.
            for (int s = count - 1; s >= 0; s--)
            {
                AddInsertStrip(s + 1);
                var block = UIKit.Element("panel panel--light stage-block");
                var info = stats?.Stage(s);
                string num = s == count - 1 ? $"<b>Stage {s}</b> <size=12>(first)</size>" : $"<b>Stage {s}</b>";
                string dv = info != null && info.HasEngines && info.BurnTime > 0.05
                    ? $"  <color=#{UIKit.Hex(UIKit.Accent)}>Δv {info.DeltaVCurrent:N0} m/s</color> · TWR {(stats.Environment == EditorEnvironment.HomeSeaLevel ? info.TwrAsl : info.TwrVac):0.00} · {info.BurnTime:0} s"
                    : "";
                block.Add(UIKit.Label(num + dv, "stage-block__head"));
                var icons = UIKit.Element("stage-icons");
                var seenGroups = new HashSet<int>();
                for (int i = 0; i < d.parts.Count; i++)
                {
                    if (d.parts[i].stage != s) continue;
                    int g = d.parts[i].symmetryGroup;
                    if (g >= 0 && !seenGroups.Add(g)) continue; // one icon per symmetry group
                    AddStageIcon(icons, i);
                }
                block.Add(icons);
                _stageList.Add(block);
                _stageDropTargets.Add((block, s));
            }
            AddInsertStrip(0);
        }

        private void AddInsertStrip(int newStageNumber)
        {
            var strip = UIKit.Element("stage-insert");
            strip.pickingMode = PickingMode.Ignore;
            _stageList.Add(strip);
            _insertTargets.Add((strip, newStageNumber));
        }

        private void AddStageIcon(VisualElement parent, int partIndex)
        {
            var def = Ed.Db.Get(Ed.Design.parts[partIndex].partId);
            if (def == null) return;
            string iconName = def.engine != null ? (def.engine.type == "solid" ? "stage_srb" : "stage_engine") : def.decoupler != null ? "stage_decoupler" : "stage_chute";
            var bg = UIKit.Element("stage-icon");
            bg.Add(UIKit.Image(UIKit.Icon(iconName), IconColor(def), "stage-icon__glyph"));
            int count = Ed.SymmetryGroup(partIndex).Count;
            if (count > 1) bg.Add(UIKit.Label("×" + count, "stage-icon__badge"));
            bg.AddManipulator(new StageIconDrag(this, partIndex));
            UIKit.Tooltip(bg, () => def.title + (count > 1 ? $" ×{count}" : "") + "\n<size=13>Drag to another stage, or onto a gap to make a new stage</size>");
            parent.Add(bg);
        }

        private static Color IconColor(PartDefinition def)
        {
            if (def.engine != null) return def.engine.type == "solid" ? new Color(1f, 0.6f, 0.3f) : new Color(1f, 0.82f, 0.35f);
            if (def.decoupler != null) return new Color(0.55f, 0.85f, 1f);
            return new Color(0.5f, 1f, 0.6f);
        }

        internal void HighlightPart(int index, bool on)
        {
            Ed.UiHighlighted.Clear();
            if (on && index >= 0 && index < Ed.Design.parts.Count)
                foreach (int i in Ed.SymmetryGroup(index)) Ed.UiHighlighted.Add(i);
            Ed.RefreshHighlights();
        }

        /// <summary>The insert strip (a gap between stages, ±4 px) or stage block under a panel point.</summary>
        private (VisualElement target, int stage, bool insert) DropTargetAt(Vector2 panelPos)
        {
            foreach (var (strip, newStage) in _insertTargets)
            {
                var r = strip.worldBound;
                r.yMin -= 4; r.yMax += 4;
                if (r.Contains(panelPos)) return (strip, newStage, true);
            }
            foreach (var (block, stage) in _stageDropTargets)
                if (block.worldBound.Contains(panelPos)) return (block, stage, false);
            return (null, -1, false);
        }

        internal void ShowDropTarget(Vector2 panelPos)
        {
            var (target, _, _) = DropTargetAt(panelPos);
            foreach (var (strip, _) in _insertTargets) strip.EnableInClassList("drop-target", strip == target);
            foreach (var (block, _) in _stageDropTargets) block.EnableInClassList("drop-target", block == target);
        }

        internal void DropStageIcon(int partIndex, Vector2 panelPos)
        {
            var (target, stage, insert) = DropTargetAt(panelPos);
            ShowDropTarget(new Vector2(-1e6f, -1e6f));
            if (target == null) return;
            if (insert) Ed.MoveToNewStage(partIndex, stage);
            else if (Ed.Design.parts[partIndex].stage != stage) Ed.MoveToStage(partIndex, stage);
        }

        /// <summary>Drag handle for staging icons: a translucent copy follows the pointer and drops on a stage or gap.</summary>
        private sealed class StageIconDrag : PointerManipulator
        {
            private readonly EditorUI _ui;
            private readonly int _partIndex;
            private VisualElement _ghost;
            private Vector2 _start;
            private bool _pressed;

            public StageIconDrag(EditorUI ui, int partIndex)
            {
                _ui = ui;
                _partIndex = partIndex;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<PointerDownEvent>(OnDown);
                target.RegisterCallback<PointerMoveEvent>(OnMove);
                target.RegisterCallback<PointerUpEvent>(OnUp);
                target.RegisterCallback<PointerEnterEvent>(_ => _ui.HighlightPart(_partIndex, true));
                target.RegisterCallback<PointerLeaveEvent>(_ => { if (!_pressed) _ui.HighlightPart(_partIndex, false); });
                target.RegisterCallback<DetachFromPanelEvent>(_ => { _ui.HighlightPart(_partIndex, false); _ghost?.RemoveFromHierarchy(); });
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<PointerDownEvent>(OnDown);
                target.UnregisterCallback<PointerMoveEvent>(OnMove);
                target.UnregisterCallback<PointerUpEvent>(OnUp);
            }

            private void OnDown(PointerDownEvent e)
            {
                if (!CanStartManipulation(e)) return;
                _pressed = true;
                _start = e.position;
                target.CapturePointer(e.pointerId);
                e.StopPropagation();
            }

            private void OnMove(PointerMoveEvent e)
            {
                if (!_pressed || !target.HasPointerCapture(e.pointerId)) return;
                if (_ghost == null)
                {
                    if (((Vector2)e.position - _start).magnitude < 4f) return;
                    _ghost = new VisualElement();
                    _ghost.AddToClassList("stage-icon");
                    _ghost.AddToClassList("stage-ghost");
                    _ghost.pickingMode = PickingMode.Ignore;
                    foreach (var child in target.Children())
                    {
                        if (child is Image img) _ghost.Add(UIKit.Image(img.sprite, img.tintColor, "stage-icon__glyph"));
                        else if (child is Label l) _ghost.Add(UIKit.Label(l.text, "stage-icon__badge"));
                    }
                    _ui._root.Add(_ghost);
                }
                UIKit.Place(_ghost, e.position);
                _ui.ShowDropTarget(e.position);
            }

            private void OnUp(PointerUpEvent e)
            {
                if (!_pressed || !CanStopManipulation(e)) return;
                _pressed = false;
                target.ReleasePointer(e.pointerId);
                bool dragged = _ghost != null;
                _ghost?.RemoveFromHierarchy();
                _ghost = null;
                _ui.HighlightPart(_partIndex, false);
                if (dragged) _ui.DropStageIcon(_partIndex, e.position);
                e.StopPropagation();
            }
        }

        // ------------------------------------------------------------------ dialogs

        private void BuildDialogs()
        {
            _help = Q<VisualElement>("help-dialog");
            Q<Label>("help-text").text = HelpText;
            _loadDialog = Q<VisualElement>("load-dialog");
            _loadList = Q<ScrollView>("load-list");
            _flightsDialog = Q<VisualElement>("flights-dialog");
            _flightsList = Q<ScrollView>("flights-list");
            _root.Query<Button>(className: "dialog__close").ForEach(b =>
            {
                var dialog = UIKit.Ancestor(b, "dialog");
                b.clicked += () => UIKit.Show(dialog, false);
            });

            // Part menu (right click)
            _partMenu = Q<VisualElement>("part-menu");
            _partMenuText = Q<Label>("part-menu-text");
            _partMenuButtons = Q<VisualElement>("part-menu-buttons");
            UIKit.Show(_help, false);
            UIKit.Show(_loadDialog, false);
            UIKit.Show(_flightsDialog, false);
            UIKit.Show(_partMenu, false);
        }

        private void CloseDialogs()
        {
            UIKit.Show(_help, false);
            UIKit.Show(_loadDialog, false);
            UIKit.Show(_flightsDialog, false);
            UIKit.Show(_partMenu, false);
        }

        private void OpenLoadDialog()
        {
            _loadList.Clear();
            Section(_loadList, "Starter rockets (tested)");
            var starters = SaveStorage.LoadStarterCraft();
            if (starters.Count == 0) starters = StarterCraft.All(Ed.Db);
            foreach (var craft in starters) CraftRow(craft, null);
            Section(_loadList, "Your saved craft");
            var files = SaveStorage.ListCraftFiles();
            if (files.Count == 0) _loadList.Add(UIKit.Label("Nothing saved yet — press Save in the top bar.", "text-dim"));
            foreach (var f in files)
            {
                try { CraftRow(SaveStorage.LoadCraft(f), f); }
                catch (Exception e) { Debug.LogWarning($"Craft file {f} unreadable: {e.Message}"); }
            }
            UIKit.Show(_loadDialog, true);
        }

        private static void Section(VisualElement parent, string text) => parent.Add(UIKit.Label(text, "list-section"));

        private void CraftRow(CraftDesign craft, string path)
        {
            var row = UIKit.Element("panel panel--light list-row list-row--craft");
            var stats = DesignAnalysis.Analyze(craft, Ed.Db, EditorEnvironment.HomeSeaLevel);
            row.Add(UIKit.Label(
                $"<b>{craft.name}</b>  <size=13><color=#{UIKit.Hex(UIKit.TextDim)}>{craft.parts.Count} parts · {PartInfoText.Mass(stats.WetMass)} · Δv {stats.TotalDvVac:N0} m/s vac · TWR {stats.LaunchTwr:0.00}</color></size>\n" +
                $"<size=13>{craft.description}</size>", "list-row__text"));
            if (path != null)
            {
                Button del = null;
                del = UIKit.Button("Delete", () =>
                {
                    // Two-step confirmation: first click arms, second deletes.
                    if (del.text != "Sure?") { del.text = "Sure?"; del.AddToClassList("list-row__btn--armed"); return; }
                    SaveStorage.DeleteCraft(path);
                    Toast($"Deleted {craft.name}");
                    OpenLoadDialog();
                }, "list-row__btn");
                row.Add(del);
            }
            row.Add(UIKit.Button("Load", () =>
            {
                Ed.LoadDesign(SaveStorage.DeepClone(craft));
                Ed.FrameCraft();
                UIKit.Show(_loadDialog, false);
            }, "list-row__btn"));
            _loadList.Add(row);
        }

        private void OpenFlightsDialog()
        {
            _flightsList.Clear();
            var save = GameSession.Save;
            if (save == null) return;
            var sys = CelestialSystem.Default;
            foreach (var v in save.vessels)
            {
                if (v.kind != VesselKind.Ship && v.kind != VesselKind.EVA) continue;
                var row = UIKit.Element("panel panel--light list-row list-row--flight");
                row.Add(UIKit.Label($"<b>{v.name}</b>\n<size=13><color=#{UIKit.Hex(UIKit.TextDim)}>{VesselLabels.Describe(v, sys.Get(v.bodyId)?.Name)}</color></size>", "list-row__text"));
                string id = v.id;
                row.Add(UIKit.Button("Fly", () =>
                {
                    Ed.StoreSessionCraft();
                    GameSession.FlyVessel(id);
                }, "list-row__btn"));
                _flightsList.Add(row);
            }
            UIKit.Show(_flightsDialog, true);
        }

        // ------------------------------------------------------------------ part menu

        private void OpenPartMenu(int index)
        {
            if (index < 0 || index >= Ed.Design.parts.Count) return;
            _partMenuIndex = index;
            var mouse = Mouse.current;
            _partMenuAnchor = mouse != null ? mouse.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            RefreshPartMenu();
            UIKit.Show(_partMenu, true);
            UpdatePartMenuPosition();
        }

        private void RefreshPartMenu()
        {
            int index = _partMenuIndex;
            if (index < 0 || index >= Ed.Design.parts.Count) { UIKit.Show(_partMenu, false); return; }
            var rec = Ed.Design.parts[index];
            var def = Ed.Db.Get(rec.partId);
            if (def == null) { UIKit.Show(_partMenu, false); return; }
            _partMenuButtons.Clear();
            var sb = new StringBuilder(PartInfoText.Describe(def, Ed.Db, false));
            int group = Ed.SymmetryGroup(index).Count;
            if (group > 1) sb.Append($"\n<color=#{UIKit.Hex(UIKit.Accent)}>Symmetry group of {group} (changes apply to all)</color>");
            if (rec.stage >= 0) sb.Append($"\nStage {rec.stage}: activated by Space press #{Ed.StageCount - rec.stage}");
            _partMenuText.text = sb.ToString();

            if (def.engine != null)
            {
                double lim = Ed.GetSetting(index, "thrustLimit", def.engine.thrustLimit);
                var row = UIKit.Element("part-menu-row");
                row.Add(UIKit.Label($"Thrust limit {lim:0}%", "part-menu-label"));
                foreach (var step in new[] { -10, -1, 1, 10 })
                {
                    int st = step;
                    row.Add(UIKit.Button((step > 0 ? "+" : "") + step, () =>
                    {
                        double cur = Ed.GetSetting(index, "thrustLimit", def.engine.thrustLimit);
                        Ed.SetSetting(index, "thrustLimit", Math.Max(0, Math.Min(100, cur + st)));
                        RefreshPartMenu();
                    }, "part-menu-step"));
                }
                UIKit.Tooltip(row, () => def.engine.type == "solid"
                    ? "Solid boosters can't be throttled in flight: the limiter sets their fixed thrust (longer, gentler burn)."
                    : "Maximum thrust as a percentage of full throttle.");
                _partMenuButtons.Add(row);
            }
            if (index > 0)
            {
                var root = UIKit.Button("Make root part", () =>
                {
                    UIKit.Show(_partMenu, false);
                    Ed.MakeRoot(index);
                }, "part-menu-btn");
                UIKit.Tooltip(root, () => "Re-root the craft on this part (stack-attached parts only). Picking up the root takes the whole craft along; the parts that were above this one can then be picked up and moved.");
                _partMenuButtons.Add(root);
                _partMenuButtons.Add(UIKit.Button(group > 1 ? $"Delete ({group} parts + attached)" : "Delete (with attached parts)", () =>
                {
                    UIKit.Show(_partMenu, false);
                    Ed.DeletePart(index);
                }, "part-menu-btn"));
            }
            _partMenuButtons.Add(UIKit.Button("Close", () => UIKit.Show(_partMenu, false), "part-menu-btn"));
        }

        /// <summary>Beside the pointer; flipped left near the staging column and kept above the bottom edge.</summary>
        private void UpdatePartMenuPosition()
        {
            Vector2 anchor = UIKit.ScreenToPanel(_partMenuAnchor);
            Vector2 screen = UIKit.PanelSize(_root);
            Vector2 size = new Vector2(_partMenu.resolvedStyle.width, _partMenu.resolvedStyle.height);
            if (float.IsNaN(size.y)) size.y = 300;
            Vector2 p = anchor + new Vector2(18, 8);
            if (p.x + size.x > screen.x - 400) p.x = anchor.x - size.x - 18;
            if (p.y + size.y > screen.y - 8) p.y = screen.y - size.y - 8;
            UIKit.Place(_partMenu, p);
        }

        private const string HelpText =
            "<b>Building</b>\n" +
            "• Click a part in the list to pick it up, then click to attach it. Stack parts snap to the free nodes shown as green spheres; " +
            "radial parts (fins, legs, boosters on radial decouplers, RCS, batteries…) attach to the surface of the part under the cursor.\n" +
            "• <b>X</b> cycles radial symmetry (1, 2, 3, 4, 6, 8). Parts attached to a symmetric part (e.g. a nose cone on a booster) are copied onto every counterpart.\n" +
            "• <b>C</b> toggles angle snap (15° positions, 90° rotation steps). <b>W A S D Q E</b> rotate the held part, <b>Shift</b> for 5° steps, <b>Space</b> resets.\n" +
            "• Click a placed part to pick it up together with everything attached below/beside it. <b>Alt+click</b> copies it. " +
            "Clicking the root part picks up the whole craft to move it up or down (between the floor and the building's height limit); " +
            "right-click a part → <b>Make root part</b> to re-root the craft on it.\n" +
            "• Click empty space to <b>set the held parts aside</b>: they stay there greyed out, are not part of the craft (not launched, not in the readouts) " +
            "and can be built on, picked up and attached again.\n" +
            "• <b>Del</b> deletes the part under the cursor or the held part (or drop it on the parts list); <b>Esc</b> puts picked-up parts back.\n" +
            "• Right-click a part for its details and settings (thrust limiter), <b>Ctrl+Z / Ctrl+Y</b> undo / redo, <b>Ctrl+S</b> save.\n\n" +
            "<b>Camera</b>: right-drag orbit · scroll zoom · Shift+scroll or middle-drag move up/down · arrow keys · <b>F</b> frame the craft.\n\n" +
            "<b>Staging</b>: the top stage (highest number) fires first when you press Space in flight. Drag the icons between stages, or onto the gap between " +
            "two stages to create a new one. <b>Reset</b> restores the automatic order. Δv and TWR are shown per stage for the selected environment.\n\n" +
            "<b>Design checks</b>: the engineer's report lists problems (red blocks launch). Keep launch TWR above ~1.3, the centre of pressure (cyan) below " +
            "the centre of mass (yellow), a heat shield under crew pods that return from orbit, and a parachute. " +
            "Orbit takes about 3,400 m/s; a trip to a Luma landing and back needs roughly 6,000–6,300 m/s plus margin " +
            "(the Luma Pathfinder starter carries about 7,600 m/s in vacuum). Low thrust-to-weight upper stages are fine in space; " +
            "a lander needs a local TWR above ~1.5 at Luma (switch the readout to Luma surface).";
    }
}
