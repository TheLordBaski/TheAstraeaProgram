using System;
using System.Collections.Generic;
using System.Linq;
using TAP.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// The settings dialog (FND-08), opened from the main menu, the space center and the pause menu. Every change applies at once and
    /// is saved; the owner of the screen under it closes it on Esc with <see cref="Close"/>. Layout: Resources/UI/Settings.uxml.
    /// </summary>
    public sealed class SettingsUI : MonoBehaviour
    {
        private static SettingsUI _instance;
        private VisualElement _dialog, _tabs;
        private ScrollView _list;
        private Label _note;
        private Button _reset;
        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<Action> _refresh = new List<Action>();
        private int _tab;
        private bool _resetArmed;

        private static readonly string[] Tabs = { "Graphics", "Audio", "Gameplay", "Interface" };

        public static bool IsOpen => _instance != null && UIKit.Shown(_instance._dialog);

        public static void Open()
        {
            if (_instance == null) _instance = Create();
            _instance.Refresh();
            UIKit.Show(_instance._dialog, true);
        }

        public static void Close()
        {
            if (_instance != null) UIKit.Show(_instance._dialog, false);
        }

        private static SettingsUI Create()
        {
            var doc = UIKit.CreateDocument("SettingsUI", 200, "Settings");
            var ui = doc.gameObject.AddComponent<SettingsUI>();
            var root = doc.rootVisualElement;
            ui._dialog = root.Q("settings-dialog");
            ui._tabs = root.Q("settings-tabs");
            ui._list = root.Q<ScrollView>("settings-list");
            ui._note = root.Q<Label>("settings-note");
            ui._reset = root.Q<Button>("settings-reset");
            root.Q<Button>("settings-close").clicked += Close;
            ui._reset.clicked += ui.Reset;
            for (int i = 0; i < Tabs.Length; i++)
            {
                int t = i;
                var b = UIKit.Button(Tabs[i], () => ui.ShowTab(t), "settings-tab");
                ui._tabs.Add(b);
                ui._tabButtons.Add(b);
            }
            ui._note.text = GameSettings.SafeMode
                ? "Safe mode: changes last for this session and are not saved."
                : "Changes apply at once and are saved.";
            ui.ShowTab(0);
            GameSettings.Changed += ui.Refresh;
            return ui;
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= Refresh;
            if (_instance == this) _instance = null;
        }

        private void ShowTab(int tab)
        {
            _tab = tab;
            _list.Clear();
            _refresh.Clear();
            for (int i = 0; i < _tabButtons.Count; i++) UIKit.SetOn(_tabButtons[i], i == tab);
            var s = GameSettings.Current;
            switch (tab)
            {
                case 0: BuildGraphics(s.graphics); break;
                case 1: BuildAudio(s.audio); break;
                case 2: BuildGameplay(s.gameplay); break;
                default: BuildInterface(s.ui); break;
            }
            Refresh();
        }

        private void Refresh()
        {
            foreach (var r in _refresh) r();
            _resetArmed = false;
            _reset.text = "Reset all to defaults";
        }

        private void Reset()
        {
            // Two clicks: the first arms the button, the second resets.
            if (!_resetArmed) { _resetArmed = true; _reset.text = "Sure? Click again"; return; }
            GameSettings.ResetToDefaults();
        }

        // ------------------------------------------------------------------ tabs

        private void BuildGraphics(GraphicsOptions g)
        {
            var sizes = Screen.resolutions.Select(r => (r.width, r.height)).Distinct().ToList();
            if (!sizes.Contains((g.width, g.height))) sizes.Add((g.width, g.height));
            sizes.Sort();
            Choice("Resolution", sizes.Select(r => $"{r.Item1} × {r.Item2}").ToArray(),
                () => sizes.IndexOf((g.width, g.height)), i => { g.width = sizes[i].Item1; g.height = sizes[i].Item2; GameSettings.Commit(); });
            Choice("Window mode", new[] { "Fullscreen", "Borderless window", "Windowed" }, () => (int)g.windowMode, i => { g.windowMode = (WindowMode)i; GameSettings.Commit(); });
            Choice("V-sync", new[] { "Off", "On" }, () => g.vsync ? 1 : 0, i => { g.vsync = i == 1; GameSettings.Commit(); });
            int[] caps = { 0, 30, 60, 90, 120, 144, 165, 240 };
            Choice("Frame cap (V-sync off)", caps.Select(c => c == 0 ? "None" : c + " fps").ToArray(),
                () => Mathf.Max(0, Array.IndexOf(caps, g.frameCap)), i => { g.frameCap = caps[i]; GameSettings.Commit(); });
            Choice("Quality preset", new[] { "Low", "Medium", "High", "Ultra", "Custom" }, () => (int)g.preset, i => { g.ApplyPreset((GraphicsPreset)i); GameSettings.Commit(); });
            Choice("Shadows", new[] { "Off", "Low", "Medium", "High" }, () => (int)g.shadows, i => Detail(g, () => g.shadows = (ShadowSetting)i));
            Choice("Anti-aliasing", new[] { "Off", "MSAA 2×", "MSAA 4×", "MSAA 8×" }, () => (int)g.antiAliasing, i => Detail(g, () => g.antiAliasing = (AntiAliasing)i));
            Slide("Render scale", 0.5f, 1.5f, () => g.renderScale, v => Detail(g, () => g.renderScale = Mathf.Round(v * 20f) / 20f), v => $"{v * 100:0}%");
            Choice("Terrain detail", new[] { "Low", "Medium", "High", "Ultra" }, () => g.terrainDetail, i => Detail(g, () => g.terrainDetail = i));
            Choice("Surface rocks", new[] { "Off", "Few", "Some", "Many" }, () => g.scatterDensity, i => Detail(g, () => g.scatterDensity = i));
            Choice("Planet quality", new[] { "Low", "Medium", "High", "Ultra" }, () => (int)g.planetQuality, i => Detail(g, () => g.planetQuality = (PlanetQuality)i));
            Choice("Clouds", new[] { "Off", "Layers", "Volumetric" }, () => (int)g.clouds, i => Detail(g, () => g.clouds = (PlanetCloudMode)i));
        }

        /// <summary>One detail setting changed: the preset may no longer match.</summary>
        private static void Detail(GraphicsOptions g, Action change)
        {
            change();
            g.MatchPreset();
            GameSettings.Commit();
        }

        private void BuildAudio(AudioOptions a)
        {
            Slide("Master volume", 0, 1, () => a.master, v => { a.master = v; GameSettings.Commit(); }, Percent);
            Slide("Music", 0, 1, () => a.music, v => { a.music = v; GameSettings.Commit(); }, Percent);
            Slide("Effects", 0, 1, () => a.effects, v => { a.effects = v; GameSettings.Commit(); }, Percent);
            Slide("Interface", 0, 1, () => a.interfaceSounds, v => { a.interfaceSounds = v; GameSettings.Commit(); }, Percent);
            Slide("Ambience", 0, 1, () => a.ambience, v => { a.ambience = v; GameSettings.Commit(); }, Percent);
        }

        private void BuildGameplay(GameplayOptions p)
        {
            int[] minutes = { 0, 1, 2, 5, 10, 15, 30 };
            Choice("Autosave in flight", minutes.Select(m => m == 0 ? "Off" : m == 1 ? "Every minute" : $"Every {m} minutes").ToArray(),
                () => Mathf.Max(0, Array.IndexOf(minutes, p.autosaveMinutes)), i => { p.autosaveMinutes = minutes[i]; GameSettings.Commit(); });
            Choice("Tooltips", new[] { "Off", "On" }, () => p.tooltips ? 1 : 0, i => { p.tooltips = i == 1; GameSettings.Commit(); });
            Slide("Mouse wheel zoom speed", 0.25f, 4f, () => p.zoomSpeed, v => { p.zoomSpeed = Mathf.Round(v * 20f) / 20f; GameSettings.Commit(); }, v => $"{v:0.0#}×");
        }

        private void BuildInterface(InterfaceOptions u)
        {
            Slide("Interface scale", 0.75f, 1.5f, () => u.uiScale, v => { u.uiScale = Mathf.Round(v * 20f) / 20f; GameSettings.Commit(); }, Percent);
            Slide("Flight display scale", 0.75f, 1.3f, () => u.hudScale, v => { u.hudScale = Mathf.Round(v * 20f) / 20f; GameSettings.Commit(); }, Percent);
        }

        private static string Percent(float v) => $"{v * 100:0}%";

        // ------------------------------------------------------------------ rows

        private void Choice(string label, string[] options, Func<int> get, Action<int> set)
        {
            var box = UIKit.Element("set-control");
            var value = UIKit.Label("", "set-value");
            void Step(int d) { int n = options.Length; set(((get() + d) % n + n) % n); }
            box.Add(UIKit.Button("◀", () => Step(-1), "set-step"));
            box.Add(value);
            box.Add(UIKit.Button("▶", () => Step(1), "set-step"));
            _refresh.Add(() => value.text = options[Mathf.Clamp(get(), 0, options.Length - 1)]);
            AddRow(label, box);
        }

        private void Slide(string label, float min, float max, Func<float> get, Action<float> set, Func<float, string> format)
        {
            var box = UIKit.Element("set-control");
            var slider = new Slider(min, max) { focusable = false };
            slider.AddToClassList("set-slider");
            var value = UIKit.Label("", "set-value set-value--narrow");
            slider.RegisterValueChangedCallback(e => set(e.newValue));
            box.Add(slider);
            box.Add(value);
            _refresh.Add(() => { slider.SetValueWithoutNotify(get()); value.text = format(get()); });
            AddRow(label, box);
        }

        private void AddRow(string label, VisualElement control)
        {
            var row = UIKit.Element("set-row");
            row.Add(UIKit.Label(label, "set-label"));
            row.Add(control);
            _list.Add(row);
        }
    }
}
