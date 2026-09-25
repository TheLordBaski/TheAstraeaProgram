using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TAP.Construction;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TAP.UI
{
    public sealed partial class EditorUI
    {
        // engineer's report
        private TextMeshProUGUI _statsText, _warningsText;
        private RectTransform _warningsPanel;
        // staging
        private RectTransform _stageContent;
        private TextMeshProUGUI _stagingTitle;
        private UIKit.ButtonRef _resetStagingBtn;
        private readonly List<(RectTransform rect, int stage)> _stageDropTargets = new List<(RectTransform, int)>();
        private readonly List<(RectTransform rect, int newStageNumber)> _insertTargets = new List<(RectTransform, int)>();
        // dialogs
        private GameObject _help, _loadDialog, _flightsDialog, _partMenu;
        private RectTransform _loadContent, _flightsContent, _partMenuRect;
        private TextMeshProUGUI _partMenuText;
        private RectTransform _partMenuButtons;
        private int _partMenuIndex = -1;
        private Vector2 _partMenuAnchor;

        // ------------------------------------------------------------------ right panel

        private void BuildRightPanel()
        {
            var panel = UIKit.Panel(_root, "Engineer");
            var rt = panel.rectTransform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-8, -68);
            rt.sizeDelta = new Vector2(392, 212);
            var title = UIKit.Label(panel.transform, "ENGINEER'S REPORT", 15, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -8), new Vector2(300, 20));
            _statsText = UIKit.Label(panel.transform, "", 15, UIKit.TextColor);
            _statsText.textWrappingMode = TextWrappingModes.Normal;
            _statsText.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(_statsText.rectTransform, 12, 10, 32, 8);

            var warn = UIKit.Panel(_root, "Warnings");
            _warningsPanel = warn.rectTransform;
            _warningsPanel.anchorMin = new Vector2(1, 1);
            _warningsPanel.anchorMax = new Vector2(1, 1);
            _warningsPanel.pivot = new Vector2(1, 1);
            _warningsPanel.anchoredPosition = new Vector2(-8, -286);
            _warningsPanel.sizeDelta = new Vector2(392, 80);
            _warningsText = UIKit.Label(warn.transform, "", 14, UIKit.TextColor);
            _warningsText.textWrappingMode = TextWrappingModes.Normal;
            _warningsText.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(_warningsText.rectTransform, 12, 10, 8, 6);

            var st = UIKit.Panel(_root, "Staging");
            var srt = st.rectTransform;
            srt.anchorMin = new Vector2(1, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 0);
            srt.offsetMin = new Vector2(-8 - 392, 52);
            srt.offsetMax = new Vector2(-8, -374);
            _stagingTitle = UIKit.Label(st.transform, "STAGING", 15, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(_stagingTitle.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -8), new Vector2(250, 20));
            _resetStagingBtn = UIKit.Button(st.transform, "Reset", () => Ed.ResetStaging(), 14);
            UIKit.Place(_resetStagingBtn.Rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -5), new Vector2(76, 26));
            UIKit.Tooltip(_resetStagingBtn.Button.gameObject, () => "Recompute the automatic staging order");
            var scroll = UIKit.ScrollList(st.transform, out _stageContent, 2);
            var scr = (RectTransform)scroll.transform;
            scr.anchorMin = Vector2.zero;
            scr.anchorMax = Vector2.one;
            scr.offsetMin = new Vector2(6, 6);
            scr.offsetMax = new Vector2(-6, -36);
            var hint = UIKit.Label(st.transform, "Top stage fires first (Space). Drag icons between stages or onto a gap.", 13, UIKit.TextDim, TextAlignmentOptions.Center);
            hint.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(380, 34));
        }

        private void OnStatsUpdated()
        {
            var s = _scene.Stats;
            if (s == null) return;
            _envBtn.Text.text = EnvName(_scene.Environment);
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
            float h = Mathf.Clamp(_warningsText.GetPreferredValues(_warningsText.text, 370, 0).y + 16, 34, 220);
            _warningsPanel.sizeDelta = new Vector2(392, h);
            var srt = (RectTransform)_stageContent.parent.parent.parent;
            srt.offsetMax = new Vector2(-8, -(68 + 212 + 6 + h + 6));
            _launchBtn.SetInteractable(!s.HasBlocking);
            RebuildStaging(s);
        }

        // ------------------------------------------------------------------ staging list

        private void RebuildStaging(DesignStats stats)
        {
            foreach (Transform c in _stageContent) Destroy(c.gameObject);
            _stageDropTargets.Clear();
            _insertTargets.Clear();
            var d = Ed.Design;
            int count = Ed.StageCount;
            _stagingTitle.text = Ed.AutoStaging ? "STAGING <size=12><color=#" + UIKit.Hex(UIKit.TextDim) + ">(automatic)</color></size>"
                                                : "STAGING <size=12><color=#" + UIKit.Hex(UIKit.Warn) + ">(custom)</color></size>";
            _resetStagingBtn.SetInteractable(!Ed.AutoStaging);
            if (count == 0)
            {
                var none = UIKit.Label(_stageContent, "No stageable parts (engines, decouplers, parachutes).", 14, UIKit.TextDim);
                none.textWrappingMode = TextWrappingModes.Normal;
                UIKit.Size(none, 40);
                return;
            }
            // Same order and numbers as the flight staging stack: the first stage to fire (highest number) on top.
            for (int s = count - 1; s >= 0; s--)
            {
                AddInsertStrip(s + 1);
                var block = UIKit.Panel(_stageContent, "Stage " + s, UIKit.PanelLight);
                var v = UIKit.VLayout(block, 2, 6);
                v.childAlignment = TextAnchor.UpperLeft;
                var info = stats?.Stage(s);
                string num = s == count - 1 ? $"<b>Stage {s}</b> <size=12>(first)</size>" : $"<b>Stage {s}</b>";
                string dv = info != null && info.HasEngines && info.BurnTime > 0.05
                    ?$"  <color=#{UIKit.Hex(UIKit.Accent)}>Δv {info.DeltaVCurrent:N0} m/s</color> · TWR {(stats.Environment == EditorEnvironment.TellusSeaLevel ? info.TwrAsl : info.TwrVac):0.00} · {info.BurnTime:0} s"
                    : "";
                var header = UIKit.Label(block.transform, num + dv, 14, UIKit.TextColor);
                UIKit.Size(header, 20);
                var icons = UIKit.Rect(block.transform, "Icons");
                var gl = icons.gameObject.AddComponent<GridLayoutGroup>();
                gl.cellSize = new Vector2(44, 44);
                gl.spacing = new Vector2(4, 4);
                gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gl.constraintCount = 7;
                int n = 0;
                var seenGroups = new HashSet<int>();
                for (int i = 0; i < d.parts.Count; i++)
                {
                    if (d.parts[i].stage != s) continue;
                    int g = d.parts[i].symmetryGroup;
                    if (g >= 0 && !seenGroups.Add(g)) continue; // one icon per symmetry group
                    AddStageIcon(icons, i);
                    n++;
                }
                int rows = Mathf.Max(1, (n + 6) / 7);
                UIKit.Size(icons, rows * 48);
                _stageDropTargets.Add((block.rectTransform, s));
            }
            AddInsertStrip(0);
        }

        private void AddInsertStrip(int newStageNumber)
        {
            var strip = UIKit.Rect(_stageContent, "Insert");
            var img = strip.gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.0f);
            img.raycastTarget = false;
            UIKit.Size(img, 8);
            _insertTargets.Add((strip, newStageNumber));
        }

        private void AddStageIcon(RectTransform parent, int partIndex)
        {
            var def = Ed.Db.Get(Ed.Design.parts[partIndex].partId);
            if (def == null) return;
            string iconName = def.engine != null ? (def.engine.type == "solid" ? "stage_srb" : "stage_engine") : def.decoupler != null ? "stage_decoupler" : "stage_chute";
            var bg = UIKit.Panel(parent, "Icon", new Color(0.16f, 0.2f, 0.27f, 1f), false);
            var icon = UIKit.Image(bg.transform, UIKit.Icon(iconName), IconColor(def), "Glyph");
            UIKit.Stretch(icon.rectTransform, 5, 5, 5, 5);
            int count = Ed.SymmetryGroup(partIndex).Count;
            if (count > 1)
            {
                var badge = UIKit.Label(bg.transform, "×" + count, 12, UIKit.TextColor, TextAlignmentOptions.BottomRight, FontStyles.Bold);
                UIKit.Stretch(badge.rectTransform, 2, 3, 2, 1);
            }
            var drag = bg.gameObject.AddComponent<StageIconDrag>();
            drag.Ui = this;
            drag.PartIndex = partIndex;
            UIKit.Tooltip(bg.gameObject, () => def.title + (count > 1 ? $" ×{count}" : "") + "\n<size=13>Drag to another stage, or onto a gap to make a new stage</size>");
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

        internal void DropStageIcon(int partIndex, Vector2 screenPos)
        {
            foreach (var (rect, newStage) in _insertTargets)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null) ||
                    RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos + new Vector2(0, 4), null) ||
                    RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos - new Vector2(0, 4), null))
                {
                    Ed.MoveToNewStage(partIndex, newStage);
                    return;
                }
            }
            foreach (var (rect, stage) in _stageDropTargets)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, null))
                {
                    if (Ed.Design.parts[partIndex].stage != stage) Ed.MoveToStage(partIndex, stage);
                    return;
                }
            }
        }

        /// <summary>Drag handle for staging icons.</summary>
        public sealed class StageIconDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
        {
            public EditorUI Ui;
            public int PartIndex;
            private RectTransform _ghost;

            public void OnBeginDrag(PointerEventData e)
            {
                var src = (RectTransform)transform;
                _ghost = Instantiate(src, Ui._root);
                Destroy(_ghost.GetComponent<StageIconDrag>());
                foreach (var g in _ghost.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
                _ghost.sizeDelta = src.rect.size;
                var cg = _ghost.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.8f;
                cg.blocksRaycasts = false;
                OnDrag(e);
            }

            public void OnDrag(PointerEventData e)
            {
                if (_ghost == null) return;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(Ui._root, e.position, null, out var lp);
                _ghost.anchorMin = _ghost.anchorMax = new Vector2(0.5f, 0.5f);
                _ghost.pivot = new Vector2(0.5f, 0.5f);
                _ghost.anchoredPosition = lp;
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (_ghost != null) Destroy(_ghost.gameObject);
                _ghost = null;
                Ui.DropStageIcon(PartIndex, e.position);
            }

            public void OnPointerEnter(PointerEventData e) => Ui.HighlightPart(PartIndex, true);
            public void OnPointerExit(PointerEventData e) => Ui.HighlightPart(PartIndex, false);
            private void OnDisable() { if (Ui != null) Ui.HighlightPart(PartIndex, false); }
        }

        // ------------------------------------------------------------------ dialogs

        private void BuildDialogs()
        {
            _help = Dialog("Help", new Vector2(760, 640), out var helpBody, "ASSEMBLY BUILDING — CONTROLS");
            var ht = UIKit.Label(helpBody, HelpText, 15, UIKit.TextColor);
            ht.textWrappingMode = TextWrappingModes.Normal;
            ht.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(ht.rectTransform, 4, 4, 4, 4);

            _loadDialog = Dialog("Load", new Vector2(760, 620), out var loadBody, "LOAD CRAFT");
            var ls = UIKit.ScrollList(loadBody, out _loadContent, 6);
            UIKit.Stretch((RectTransform)ls.transform);

            _flightsDialog = Dialog("Flights", new Vector2(700, 520), out var fBody, "VESSELS IN FLIGHT");
            var fs = UIKit.ScrollList(fBody, out _flightsContent, 6);
            UIKit.Stretch((RectTransform)fs.transform);

            // Part menu (right click)
            var pm = UIKit.Panel(_root, "PartMenu", new Color(0.04f, 0.055f, 0.08f, 0.97f));
            _partMenu = pm.gameObject;
            _partMenuRect = pm.rectTransform;
            _partMenuRect.anchorMin = _partMenuRect.anchorMax = Vector2.zero;
            _partMenuRect.pivot = new Vector2(0, 1);
            _partMenuRect.sizeDelta = new Vector2(360, 300);
            _partMenuText = UIKit.Label(pm.transform, "", 14, UIKit.TextColor);
            _partMenuText.textWrappingMode = TextWrappingModes.Normal;
            _partMenuText.alignment = TextAlignmentOptions.TopLeft;
            _partMenuText.rectTransform.anchorMin = new Vector2(0, 1);
            _partMenuText.rectTransform.anchorMax = new Vector2(1, 1);
            _partMenuText.rectTransform.pivot = new Vector2(0.5f, 1);
            _partMenuText.rectTransform.anchoredPosition = new Vector2(0, -10);
            _partMenuText.rectTransform.sizeDelta = new Vector2(-24, 200);
            _partMenuButtons = UIKit.Rect(pm.transform, "Buttons");
            _partMenuButtons.anchorMin = new Vector2(0, 0);
            _partMenuButtons.anchorMax = new Vector2(1, 0);
            _partMenuButtons.pivot = new Vector2(0.5f, 0);
            _partMenuButtons.anchoredPosition = new Vector2(0, 8);
            _partMenuButtons.sizeDelta = new Vector2(-16, 80);
            UIKit.VLayout(_partMenuButtons, 4, 0);
            _partMenu.SetActive(false);
        }

        private GameObject Dialog(string name, Vector2 size, out RectTransform body, string title)
        {
            var shade = UIKit.Rect(_root, name + "Dialog");
            UIKit.Stretch(shade);
            var shadeImg = shade.gameObject.AddComponent<Image>();
            shadeImg.color = new Color(0, 0, 0, 0.45f);
            var panel = UIKit.Panel(shade, "Panel");
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var t = UIKit.Label(panel.transform, title, 18, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -12), new Vector2(size.x - 100, 26));
            var go = shade.gameObject;
            var close = UIKit.Button(panel.transform, "Close", () => go.SetActive(false), 15);
            UIKit.Place(close.Rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -8), new Vector2(80, 30));
            body = UIKit.Rect(panel.transform, "Body");
            UIKit.Stretch(body, 16, 16, 50, 16);
            go.SetActive(false);
            return go;
        }

        private void CloseDialogs()
        {
            _help.SetActive(false);
            _loadDialog.SetActive(false);
            _flightsDialog.SetActive(false);
            _partMenu.SetActive(false);
        }

        private void OpenLoadDialog()
        {
            foreach (Transform c in _loadContent) Destroy(c.gameObject);
            Section(_loadContent, "Starter rockets (tested)");
            var starters = SaveStorage.LoadStarterCraft();
            if (starters.Count == 0) starters = StarterCraft.All(Ed.Db);
            foreach (var craft in starters) CraftRow(craft, null);
            Section(_loadContent, "Your saved craft");
            var files = SaveStorage.ListCraftFiles();
            if (files.Count == 0)
            {
                var none = UIKit.Label(_loadContent, "Nothing saved yet — press Save in the top bar.", 14, UIKit.TextDim);
                UIKit.Size(none, 24);
            }
            foreach (var f in files)
            {
                try { CraftRow(SaveStorage.LoadCraft(f), f); }
                catch (Exception e) { Debug.LogWarning($"Craft file {f} unreadable: {e.Message}"); }
            }
            _loadDialog.SetActive(true);
        }

        private void Section(RectTransform parent, string text)
        {
            var l = UIKit.Label(parent, text, 15, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Size(l, 26);
        }

        private void CraftRow(CraftDesign craft, string path)
        {
            var row = UIKit.Panel(_loadContent, "Row", UIKit.PanelLight);
            UIKit.Size(row, 74);
            var stats = DesignAnalysis.Analyze(craft, Ed.Db, EditorEnvironment.TellusSeaLevel);
            var text = UIKit.Label(row.transform,
                $"<b>{craft.name}</b>  <size=13><color=#{UIKit.Hex(UIKit.TextDim)}>{craft.parts.Count} parts · {PartInfoText.Mass(stats.WetMass)} · Δv {stats.TotalDvVac:N0} m/s vac · TWR {stats.LaunchTwr:0.00}</color></size>\n" +
                $"<size=13>{craft.description}</size>", 15, UIKit.TextColor);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
            UIKit.Stretch(text.rectTransform, 10, path != null ? 190 : 100, 6, 4);
            var load = UIKit.Button(row.transform, "Load", () =>
            {
                Ed.LoadDesign(SaveStorage.DeepClone(craft));
                Ed.FrameCraft();
                _loadDialog.SetActive(false);
            }, 15);
            UIKit.Place(load.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(82, 34));
            if (path != null)
            {
                var del = UIKit.Button(row.transform, "Delete", null, 15);
                del.Button.onClick.AddListener(() =>
                {
                    // Two-step confirmation: first click arms, second deletes.
                    if (del.Text.text != "Sure?") { del.Text.text = "Sure?"; del.Background.color = new Color(0.5f, 0.15f, 0.12f); return; }
                    SaveStorage.DeleteCraft(path);
                    Toast($"Deleted {craft.name}");
                    OpenLoadDialog();
                });
                UIKit.Place(del.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-96, 0), new Vector2(82, 34));
            }
        }

        private void OpenFlightsDialog()
        {
            foreach (Transform c in _flightsContent) Destroy(c.gameObject);
            var save = GameSession.Save;
            if (save == null) return;
            var sys = CelestialSystem.Default;
            foreach (var v in save.vessels)
            {
                if (v.kind != VesselKind.Ship && v.kind != VesselKind.EVA) continue;
                var row = UIKit.Panel(_flightsContent, "Row", UIKit.PanelLight);
                UIKit.Size(row, 56);
                var body = sys.Get(v.bodyId);
                int crew = 0;
                foreach (var p in v.parts) crew += p.crew?.Count ?? 0;
                string kind = v.kind == VesselKind.EVA ? "EVA" : crew > 0 ? $"{crew} crew" : "uncrewed";
                var t = UIKit.Label(row.transform, $"<b>{v.name}</b>\n<size=13><color=#{UIKit.Hex(UIKit.TextDim)}>{SituationText(v.situation)} {body?.Name ?? v.bodyId} · {kind}</color></size>", 15);
                t.alignment = TextAlignmentOptions.Left;
                UIKit.Stretch(t.rectTransform, 10, 110, 4, 4);
                string id = v.id;
                var fly = UIKit.Button(row.transform, "Fly", () =>
                {
                    Ed.StoreSessionCraft();
                    save.activeVesselId = id;
                    GameSession.ResumeFlight();
                }, 15);
                UIKit.Place(fly.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(90, 34));
            }
            _flightsDialog.SetActive(true);
        }

        private static string SituationText(TAP.Persistence.Situation s)
        {
            switch (s)
            {
                case TAP.Persistence.Situation.Prelaunch: return "On the pad at";
                case TAP.Persistence.Situation.Landed: return "Landed on";
                case TAP.Persistence.Situation.Splashed: return "Splashed down on";
                case TAP.Persistence.Situation.Flying: return "Flying over";
                case TAP.Persistence.Situation.SubOrbital: return "Sub-orbital around";
                case TAP.Persistence.Situation.Orbiting: return "Orbiting";
                case TAP.Persistence.Situation.Escaping: return "Escaping";
            }
            return s.ToString();
        }

        // ------------------------------------------------------------------ part menu

        private void OpenPartMenu(int index)
        {
            if (index < 0 || index >= Ed.Design.parts.Count) return;
            _partMenuIndex = index;
            var mouse = Mouse.current;
            _partMenuAnchor = mouse != null ? mouse.position.ReadValue() : new Vector2(Screen.width / 2f, Screen.height / 2f);
            RefreshPartMenu();
            _partMenu.SetActive(true);
            UpdatePartMenuPosition();
        }

        private void RefreshPartMenu()
        {
            int index = _partMenuIndex;
            if (index < 0 || index >= Ed.Design.parts.Count) { _partMenu.SetActive(false); return; }
            var rec = Ed.Design.parts[index];
            var def = Ed.Db.Get(rec.partId);
            if (def == null) { _partMenu.SetActive(false); return; }
            foreach (Transform c in _partMenuButtons) Destroy(c.gameObject);
            var sb = new StringBuilder(PartInfoText.Describe(def, Ed.Db, false));
            int group = Ed.SymmetryGroup(index).Count;
            if (group > 1) sb.Append($"\n<color=#{UIKit.Hex(UIKit.Accent)}>Symmetry group of {group} (changes apply to all)</color>");
            if (rec.stage >= 0) sb.Append($"\nStage {rec.stage}: activated by Space press #{Ed.StageCount - rec.stage}");
            _partMenuText.text = sb.ToString();
            float textH = _partMenuText.GetPreferredValues(_partMenuText.text, 336, 0).y;
            _partMenuText.rectTransform.sizeDelta = new Vector2(-24, textH);

            int buttons = 0;
            if (def.engine != null)
            {
                double lim = Ed.GetSetting(index, "thrustLimit", def.engine.thrustLimit);
                var row = UIKit.Rect(_partMenuButtons, "Limiter");
                UIKit.Size(row, 30);
                UIKit.HLayout(row, 4, 0);
                var lbl = UIKit.Label(row, $"Thrust limit {lim:0}%", 14, UIKit.TextColor);
                UIKit.Size(lbl, 30, 150);
                foreach (var step in new[] { -10, -1, 1, 10 })
                {
                    int st = step;
                    var b = UIKit.Button(row, (step > 0 ? "+" : "") + step, () =>
                    {
                        double cur = Ed.GetSetting(index, "thrustLimit", def.engine.thrustLimit);
                        Ed.SetSetting(index, "thrustLimit", Math.Max(0, Math.Min(100, cur + st)));
                        RefreshPartMenu();
                    }, 14);
                    UIKit.Size(b.Button, 30, 42);
                }
                UIKit.Tooltip(row.gameObject, () => def.engine.type == "solid"
                    ? "Solid boosters can't be throttled in flight: the limiter sets their fixed thrust (longer, gentler burn)."
                    : "Maximum thrust as a percentage of full throttle.");
                buttons++;
            }
            if (index > 0)
            {
                var root = UIKit.Button(_partMenuButtons, "Make root part", () =>
                {
                    _partMenu.SetActive(false);
                    Ed.MakeRoot(index);
                }, 14);
                UIKit.Size(root.Button, 30);
                UIKit.Tooltip(root.Button.gameObject, () => "Re-root the craft on this part (stack-attached parts only). Picking up the root takes the whole craft along; the parts that were above this one can then be picked up and moved.");
                buttons++;
                var del = UIKit.Button(_partMenuButtons, group > 1 ? $"Delete ({group} parts + attached)" : "Delete (with attached parts)", () =>
                {
                    _partMenu.SetActive(false);
                    Ed.DeletePart(index);
                }, 14);
                UIKit.Size(del.Button, 30);
                buttons++;
            }
            var close = UIKit.Button(_partMenuButtons, "Close", () => _partMenu.SetActive(false), 14);
            UIKit.Size(close.Button, 30);
            buttons++;
            float h = textH + 24 + buttons * 34 + 8;
            _partMenuButtons.sizeDelta = new Vector2(-16, buttons * 34);
            _partMenuRect.sizeDelta = new Vector2(360, h);
        }

        private void UpdatePartMenuPosition()
        {
            float scale = _canvas.scaleFactor;
            Vector2 p = _partMenuAnchor / scale + new Vector2(18, -8);
            Vector2 size = _partMenuRect.sizeDelta;
            Vector2 screen = new Vector2(Screen.width, Screen.height) / scale;
            if (p.x + size.x > screen.x - 400) p.x = _partMenuAnchor.x / scale - size.x - 18;
            if (p.y - size.y < 8) p.y = size.y + 8;
            _partMenuRect.anchoredPosition = p;
        }

        private const string HelpText =
            "<b>Building</b>\n" +
            "• Click a part in the list to pick it up, then click to attach it. Stack parts snap to the free nodes shown as green spheres; " +
            "radial parts (fins, legs, boosters on radial decouplers, RCS, batteries…) attach to the surface of the part under the cursor.\n" +
            "• <b>X</b> cycles radial symmetry (1, 2, 3, 4, 6, 8). Parts attached to a symmetric part (e.g. a nose cone on a booster) are copied onto every counterpart.\n" +
            "• <b>C</b> toggles angle snap (15° positions, 90° rotation steps). <b>W A S D Q E</b> rotate the held part, <b>Shift</b> for 5° steps, <b>Space</b> resets.\n" +
            "• Click a placed part to pick it up together with everything attached below/beside it. <b>Alt+click</b> copies it. " +
            "Clicking the root part picks up the whole craft; right-click a part → <b>Make root part</b> to re-root the craft on it.\n" +
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
