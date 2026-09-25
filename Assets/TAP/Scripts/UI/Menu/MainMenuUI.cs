using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Game;
using TAP.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TAP.UI
{
    /// <summary>Main menu: continue / new sandbox / load game / controls / quit, over a slowly turning view of Tellus and Luma.</summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        private RectTransform _root;
        private GameObject _loadDialog, _newDialog, _controls;
        private RectTransform _loadContent;
        private TMP_InputField _newName;
        private Transform _planet, _moon;
        private string _latestSave;

        public static MainMenuUI Create()
        {
            var canvas = UIKit.CreateCanvas("MainMenu", 10);
            var ui = canvas.gameObject.AddComponent<MainMenuUI>();
            ui._root = (RectTransform)canvas.transform;
            ui.BuildBackdrop();
            ui.Build();
            return ui;
        }

        // ------------------------------------------------------------------ 3D backdrop

        private void BuildBackdrop()
        {
            var camGo = new GameObject("MenuCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 5000f;
            camGo.tag = "MainCamera";
            var data = camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            camGo.transform.position = new Vector3(0, 0, -30);
            camGo.transform.rotation = Quaternion.identity;
            var sky = Resources.Load<Material>("Materials/Sky");
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.035f, 0.05f);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-0.8f, -0.25f, 0.55f));

            _planet = Body("Tellus", "Materials/Map_tellus", new Vector3(9f, -9f, 12f), 22f);
            _moon = Body("Luma", "Materials/Map_luma", new Vector3(-14f, 7f, 40f), 5f);
            // Thin atmosphere rim
            var shellMat = Resources.Load<Material>("Materials/AtmosphereShell");
            if (shellMat != null)
            {
                var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(shell.GetComponent<Collider>());
                shell.transform.SetParent(_planet, false);
                shell.transform.localScale = Vector3.one * 1.03f;
                shell.GetComponent<Renderer>().sharedMaterial = shellMat;
            }
        }

        private static Transform Body(string name, string mat, Vector3 pos, float diameter)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.name = name;
            s.transform.position = pos;
            s.transform.localScale = Vector3.one * diameter;
            var m = Resources.Load<Material>(mat);
            if (m != null) s.GetComponent<Renderer>().sharedMaterial = m;
            // Unity's sphere UVs map longitude around Y; tilt a little for a nicer look.
            s.transform.rotation = Quaternion.Euler(12f, 0, 8f);
            return s.transform;
        }

        private void Update()
        {
            if (_planet != null) _planet.Rotate(Vector3.up, -1.2f * Time.deltaTime, Space.Self);
            if (_moon != null) _moon.Rotate(Vector3.up, -0.6f * Time.deltaTime, Space.Self);
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                _loadDialog.SetActive(false);
                _newDialog.SetActive(false);
                _controls.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ menu

        private void Build()
        {
            var title = UIKit.Label(_root, "<size=46>THE</size> ASTRAEA <size=46>PROGRAM</size>", 86, Color.white, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -98), new Vector2(1200, 110));
            title.characterSpacing = 10;
            var sub = UIKit.Label(_root, "Build rockets. Reach orbit. Walk on Luma. Come home.", 24, UIKit.Accent);
            UIKit.Place(sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(116, -220), new Vector2(900, 36));

            var col = UIKit.Rect(_root, "Buttons");
            UIKit.Place(col, new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -300), new Vector2(420, 420));
            var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;

            _latestSave = FindLatestSave(out string savedAt);
            if (_latestSave != null)
            {
                var cont = MenuButton(col, $"Continue  <size=15><color=#{UIKit.Hex(UIKit.TextDim)}>{_latestSave} · {savedAt}</color></size>", () => Continue(_latestSave));
                cont.Background.color = new Color(0.12f, 0.42f, 0.26f, 1f);
            }
            MenuButton(col, "New sandbox", OpenNew);
            MenuButton(col, "Load game", OpenLoad);
            MenuButton(col, "Controls", () => _controls.SetActive(true));
            MenuButton(col, "Quit", () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });

            var foot = UIKit.Label(_root,
                "A rocket-only spaceflight sandbox · Tellus (R 600 km, 1 g, 70 km atmosphere) and its airless moon Luma (R 220 km, 0.16 g, 13,000 km away)\n" +
                "Original work built with Unity " + Application.unityVersion + " (URP)", 14, UIKit.TextDim);
            UIKit.Place(foot.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(112, 24), new Vector2(1400, 44));

            BuildNewDialog();
            BuildLoadDialog();
            BuildControls();
        }

        private UIKit.ButtonRef MenuButton(Transform parent, string label, Action onClick)
        {
            var b = UIKit.Button(parent, label, onClick, 24);
            UIKit.Size(b.Button, 58);
            b.Text.alignment = TextAlignmentOptions.Left;
            b.Text.margin = new Vector4(18, 0, 0, 0);
            return b;
        }

        private static string FindLatestSave(out string savedAt)
        {
            savedAt = null;
            string best = null;
            DateTime bestTime = DateTime.MinValue;
            foreach (var name in SaveStorage.ListSaves())
            {
                if (name == "AutoTest") continue;
                try
                {
                    string path = SaveStorage.SlotPath(name, "persistent");
                    if (!System.IO.File.Exists(path)) continue;
                    var t = System.IO.File.GetLastWriteTime(path);
                    if (t > bestTime) { bestTime = t; best = name; }
                }
                catch { }
            }
            if (best != null) savedAt = bestTime.ToString("yyyy-MM-dd HH:mm");
            return best;
        }

        private static void Continue(string saveName)
        {
            if (!GameSession.LoadPersistent(saveName))
            {
                GameSession.NewGame(saveName);
                GameSession.GoToEditor();
                return;
            }
            var s = GameSession.Save;
            bool hasActive = s.scene == "flight" && s.vessels.Exists(v => v.id == s.activeVesselId);
            if (hasActive) GameSession.ResumeFlight();
            else GameSession.GoToEditor();
        }

        // ------------------------------------------------------------------ dialogs

        private GameObject Dialog(string name, Vector2 size, string title, out RectTransform body)
        {
            var shade = UIKit.Rect(_root, name);
            UIKit.Stretch(shade);
            shade.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            var panel = UIKit.Panel(shade, "Panel");
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var t = UIKit.Label(panel.transform, title, 20, UIKit.Accent, TextAlignmentOptions.Left, FontStyles.Bold);
            UIKit.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -14), new Vector2(size.x - 120, 28));
            var go = shade.gameObject;
            var close = UIKit.Button(panel.transform, "Close", () => go.SetActive(false), 16);
            UIKit.Place(close.Rect, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10), new Vector2(84, 32));
            body = UIKit.Rect(panel.transform, "Body");
            UIKit.Stretch(body, 18, 18, 56, 18);
            go.SetActive(false);
            return go;
        }

        private void BuildNewDialog()
        {
            _newDialog = Dialog("NewDialog", new Vector2(620, 300), "NEW SANDBOX", out var body);
            var l = UIKit.Label(body, "Save name:", 18);
            UIKit.Place(l.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -8), new Vector2(200, 30));
            _newName = UIKit.Input(body, "Sandbox", null, 18);
            UIKit.Place((RectTransform)_newName.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -44), new Vector2(580, 40));
            var info = UIKit.Label(body, "Starts with an empty universe, 8 rocketeers and three tested starter rockets in the assembly building.", 15, UIKit.TextDim);
            info.textWrappingMode = TextWrappingModes.Normal;
            UIKit.Place(info.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -96), new Vector2(580, 50));
            var start = UIKit.Button(body, "Start", () =>
            {
                string name = SaveStorage.SanitizeFileName(string.IsNullOrWhiteSpace(_newName.text) ? "Sandbox" : _newName.text.Trim());
                if (SaveStorage.SlotExists(name, "persistent"))
                {
                    // Never overwrite silently: pick a free name.
                    int i = 2;
                    while (SaveStorage.SlotExists(name + " " + i, "persistent")) i++;
                    name = name + " " + i;
                }
                GameSession.NewGame(name);
                GameSession.WritePersistent();
                GameSession.GoToEditor();
            }, 20);
            UIKit.Place(start.Rect, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(160, 46));
            start.Background.color = new Color(0.12f, 0.42f, 0.26f, 1f);
        }

        private void OpenNew()
        {
            string name = "Sandbox";
            int i = 2;
            while (SaveStorage.SlotExists(name, "persistent")) name = "Sandbox " + i++;
            _newName.text = name;
            _newDialog.SetActive(true);
        }

        private void BuildLoadDialog()
        {
            _loadDialog = Dialog("LoadDialog", new Vector2(760, 560), "LOAD GAME", out var body);
            var scroll = UIKit.ScrollList(body, out _loadContent, 6);
            UIKit.Stretch((RectTransform)scroll.transform);
        }

        private void OpenLoad()
        {
            foreach (Transform c in _loadContent) Destroy(c.gameObject);
            var saves = SaveStorage.ListSaves();
            int shown = 0;
            foreach (var name in saves)
            {
                bool hasPersistent = SaveStorage.SlotExists(name, "persistent");
                bool hasQuick = SaveStorage.SlotExists(name, "quicksave");
                if (!hasPersistent && !hasQuick) continue;
                shown++;
                var row = UIKit.Panel(_loadContent, "Row", UIKit.PanelLight);
                UIKit.Size(row, 64);
                string detail = "";
                try
                {
                    var s = SaveStorage.ReadSave(name, hasPersistent ? "persistent" : "quicksave");
                    int ships = 0;
                    foreach (var v in s.vessels) if (v.kind == VesselKind.Ship) ships++;
                    detail = $"{s.savedAt} · T+{MathD.FormatUT(s.ut)} · {ships} vessel(s) · {s.milestones.Count} milestone(s)";
                }
                catch (Exception e) { detail = "unreadable: " + e.Message; }
                var t = UIKit.Label(row.transform, $"<b>{name}</b>\n<size=14><color=#{UIKit.Hex(UIKit.TextDim)}>{detail}</color></size>", 18);
                UIKit.Stretch(t.rectTransform, 12, 250, 6, 6);
                string n = name;
                if (hasPersistent)
                {
                    var b = UIKit.Button(row.transform, "Resume", () => Continue(n), 16);
                    UIKit.Place(b.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(110, 38));
                }
                if (hasQuick)
                {
                    var q = UIKit.Button(row.transform, "Quicksave", () =>
                    {
                        var s = SaveStorage.ReadSave(n, "quicksave");
                        if (s == null) return;
                        GameSession.EditorCraft = s.editorCraft;
                        GameSession.PendingMessage = "Quicksave loaded";
                        GameSession.LoadFlightState(s, FlightEntry.Quickload);
                    }, 16);
                    UIKit.Place(q.Rect, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-126, 0), new Vector2(110, 38));
                }
            }
            if (shown == 0)
            {
                var none = UIKit.Label(_loadContent, "No saved games yet.", 17, UIKit.TextDim);
                UIKit.Size(none, 30);
            }
            _loadDialog.SetActive(true);
        }

        private void BuildControls()
        {
            _controls = Dialog("Controls", new Vector2(980, 760), "CONTROLS", out var body);
            var scroll = UIKit.ScrollList(body, out var content, 2);
            UIKit.Stretch((RectTransform)scroll.transform);
            var sb = new System.Text.StringBuilder();
            sb.Append("<b><color=#").Append(UIKit.Hex(UIKit.Accent)).Append(">FLIGHT</color></b>\n");
            foreach (var (k, a) in Bindings.Flight) sb.Append($"<b>{k}</b>\t{a}\n");
            sb.Append("\n<b><color=#").Append(UIKit.Hex(UIKit.Accent)).Append(">EVA</color></b>\n");
            foreach (var (k, a) in Bindings.Eva) sb.Append($"<b>{k}</b>\t{a}\n");
            sb.Append("\n<b><color=#").Append(UIKit.Hex(UIKit.Accent)).Append(">MAP VIEW</color></b>\n");
            foreach (var (k, a) in Bindings.Map) sb.Append($"<b>{k}</b>\t{a}\n");
            sb.Append("\n<b><color=#").Append(UIKit.Hex(UIKit.Accent)).Append(">ASSEMBLY BUILDING</color></b>\n");
            foreach (var (k, a) in Bindings.Editor) sb.Append($"<b>{k}</b>\t{a}\n");
            var t = UIKit.Label(content, sb.ToString(), 16);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.alignment = TextAlignmentOptions.TopLeft;
            var fitter = t.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
