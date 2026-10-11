using System.Collections.Generic;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Space center screen (FND-07): a marker over each building, the tracking station's vessel list and a way back to the main
    /// menu. Layout: Resources/UI/SpaceCenter.uxml. HUB-02 replaces it with the finished hub.
    /// </summary>
    public sealed class SpaceCenterUI : MonoBehaviour
    {
        private SpaceCenterScene _scene;
        private VisualElement _root, _markers, _trackingDialog;
        private Label _note;
        private float _noteUntil;
        private readonly Dictionary<Facility, Button> _buttons = new Dictionary<Facility, Button>();

        public static SpaceCenterUI Create(SpaceCenterScene scene)
        {
            var doc = UIKit.CreateDocument("SpaceCenterUI", 10, "SpaceCenter");
            var ui = doc.gameObject.AddComponent<SpaceCenterUI>();
            ui._scene = scene;
            ui._root = doc.rootVisualElement;
            ui.Build();
            return ui;
        }

        private void Build()
        {
            _markers = _root.Q("hub-markers");
            _trackingDialog = _root.Q("tracking-dialog");
            _note = _root.Q<Label>("hub-note");
            var save = GameSession.Save;
            _root.Q<Label>("hub-info").text = $"{Loc.Name(save.saveName)} · {save.mode} · {Loc.Name(MathD.FormatUT(save.ut))}";
            _root.Q<Button>("hub-settings").clicked += SettingsUI.Open;
            _root.Q<Button>("hub-menu").clicked += () => { GameSession.WritePersistent(); GameSession.GoToMenu(); };
            _root.Query<Button>(className: "dialog__close").ForEach(b => b.clicked += () => UIKit.Show(_trackingDialog, false));

            foreach (var f in _scene.Facilities)
            {
                var facility = f;
                var b = UIKit.Button(Caption(f), () => Open(facility), "hub-marker");
                b.name = "bld-" + f.Id;
                b.RegisterCallback<PointerEnterEvent>(_ => _scene.SetHover(facility));
                b.RegisterCallback<PointerLeaveEvent>(_ => { if (_scene.Hover == facility) _scene.SetHover(null); });
                _markers.Add(b);
                _buttons[f] = b;
            }
            _scene.Clicked += Open;
            _scene.PointerOverUi = () => UIKit.PointerOverUi();

            if (!string.IsNullOrEmpty(GameSession.PendingMessage))
            {
                Note(GameSession.PendingMessage, 8f);
                GameSession.PendingMessage = null;
            }
        }

        private static string Caption(Facility f)
        {
            string hint = f.Id == SpaceCenterScene.Assembly ? "Build and launch rockets"
                : f.Id == SpaceCenterScene.Pad ? "Fly the rocket on the pad"
                : "Every vessel in flight";
            return $"<b>{f.DisplayName.ToUpperInvariant()}</b>\n<size=14><color=#{UIKit.Hex(UIKit.TextDim)}>{hint}</color></size>";
        }

        private void Note(string text, float seconds)
        {
            _note.text = text;
            UIKit.Show(_note, true);
            _noteUntil = Time.unscaledTime + seconds;
        }

        // ------------------------------------------------------------------ buildings

        private void Open(Facility f)
        {
            switch (f.Id)
            {
                case SpaceCenterScene.Assembly: GameSession.GoToEditor(); break;
                case SpaceCenterScene.Pad: OpenPad(); break;
                case SpaceCenterScene.Tracking: OpenTracking(); break;
            }
        }

        /// <summary>The pad flies the rocket waiting on it; with none, the way to get one is the assembly building.</summary>
        private void OpenPad()
        {
            var onPad = GameSession.Save.vessels.Find(v => v.kind == VesselKind.Ship && v.situation == Situation.Prelaunch && v.systemId == CelestialSystem.Default.Id);
            if (onPad != null) { GameSession.FlyVessel(onPad.id); return; }
            Note("Nothing is waiting on the launch pad. Build a rocket in the assembly building and launch it from there.", 6f);
        }

        private void OpenTracking()
        {
            var list = _root.Q<ScrollView>("tracking-list");
            list.Clear();
            var sys = CelestialSystem.Default;
            int shown = 0;
            foreach (var v in GameSession.Save.vessels)
            {
                if (v.kind != VesselKind.Ship && v.kind != VesselKind.EVA) continue;
                shown++;
                var row = UIKit.Element("panel panel--light tracking-row");
                row.Add(UIKit.Label($"<b>{Loc.Name(v.name)}</b>\n<size=14><color=#{UIKit.Hex(UIKit.TextDim)}>{VesselLabels.Describe(v, sys.Get(v.bodyId)?.Name)}</color></size>", "tracking-row__text"));
                string id = v.id;
                var fly = UIKit.Button("Fly", () => GameSession.FlyVessel(id), "tracking-row__btn");
                fly.name = "fly-" + id;
                fly.SetEnabled(v.systemId == sys.Id);
                row.Add(fly);
                list.Add(row);
            }
            if (shown == 0) list.Add(UIKit.Label("No vessels in flight. Launch one from the assembly building.", "text-dim"));
            UIKit.Show(_trackingDialog, true);
        }

        // ------------------------------------------------------------------ per frame

        private void Update()
        {
            if (Controls.Down("Common/Cancel")) { SettingsUI.Close(); UIKit.Show(_trackingDialog, false); }
            if (UIKit.Shown(_note) && Time.unscaledTime > _noteUntil) UIKit.Show(_note, false);
            // Markers follow their buildings.
            foreach (var kv in _buttons)
            {
                Vector3 s = _scene.Cam.WorldToScreenPoint(kv.Key.Anchor);
                bool visible = s.z > 0;
                UIKit.Show(kv.Value, visible);
                if (visible) UIKit.Place(kv.Value, UIKit.ScreenToPanel(new Vector2(s.x, s.y)));
                UIKit.SetOn(kv.Value, _scene.Hover == kv.Key);
            }
        }
    }
}
