using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace TAP.Game
{
    /// <summary>
    /// The game's controls (FND-09): an Input System action asset made from <see cref="ControlTable"/>. Code asks for a control by id
    /// ("Flight/Stage"); players can rebind any of them, and the rebinds are saved per user in <c>controls.json</c> next to the
    /// settings. Bindings are by key position, so the layout is the same on QWERTY, QWERTZ and AZERTY keyboards.
    /// </summary>
    public static class Controls
    {
        /// <summary>One binding of a control: the entry that shows it, and the key (or stick) that rebinding changes.</summary>
        public readonly struct Slot
        {
            public readonly int Display, Key;
            public readonly string Default;
            public Slot(int display, int key, string def) { Display = display; Key = key; Default = def; }
        }

        public const string FileName = "controls.json";
        public static string FilePath => Path.Combine(SaveStorage.Root, FileName);

        private static InputActionAsset _asset;
        private static readonly Dictionary<string, InputAction> Actions = new Dictionary<string, InputAction>();
        private static readonly Dictionary<string, List<Slot>> Slots = new Dictionary<string, List<Slot>>();
        private static Dictionary<string, string> _overrides = new Dictionary<string, string>();

        public static event Action Changed;

        // ------------------------------------------------------------------ building

        private static void Ensure()
        {
            if (_asset != null) return;
            InputSystem.settings.shortcutKeysConsumeInput = true; // Ctrl+S saves; the S that rotates a part stays quiet
            _asset = ScriptableObject.CreateInstance<InputActionAsset>();
            _asset.name = "TAP controls";
            foreach (var group in ControlTable.All.GroupBy(c => c.Map))
            {
                var map = _asset.AddActionMap(group.Key);
                foreach (var def in group)
                {
                    bool axis = def.Kind == ControlKind.Axis;
                    var action = map.AddAction(def.Name, axis ? InputActionType.Value : InputActionType.Button, expectedControlLayout: axis ? "Axis" : "Button");
                    var slots = new List<Slot>();
                    foreach (var spec in def.Defaults) slots.Add(Bind(action, spec));
                    Actions[def.Id] = action;
                    Slots[def.Id] = slots;
                }
            }
            LoadOverrides();
            _asset.Enable();
        }

        /// <summary>Adds a binding from a spec: a path, or "Alt+Shift+&lt;Keyboard&gt;/x" for a key with modifiers held.</summary>
        private static Slot Bind(InputAction action, string spec)
        {
            var parts = spec.Split('+');
            string key = parts[parts.Length - 1];
            if (parts.Length == 1)
            {
                action.AddBinding(key);
                int i = action.bindings.Count - 1;
                return new Slot(i, i, key);
            }
            int composite = action.bindings.Count;
            if (parts.Length == 2) action.AddCompositeBinding("OneModifier").With("Modifier", Modifier(parts[0])).With("Binding", key);
            else action.AddCompositeBinding("TwoModifiers").With("Modifier1", Modifier(parts[0])).With("Modifier2", Modifier(parts[1])).With("Binding", key);
            return new Slot(composite, action.bindings.Count - 1, key);
        }

        private static string Modifier(string name) => "<Keyboard>/" + name.ToLowerInvariant();

        /// <summary>For tests: drop everything and build again, from the defaults.</summary>
        public static void Reset()
        {
            if (_asset != null) { _asset.Disable(); UnityEngine.Object.DestroyImmediate(_asset); }
            _asset = null;
            Actions.Clear();
            Slots.Clear();
            _overrides = new Dictionary<string, string>();
        }

        // ------------------------------------------------------------------ asking

        public static InputActionAsset Asset { get { Ensure(); return _asset; } }

        public static InputAction Action(string id)
        {
            Ensure();
            if (!Actions.TryGetValue(id, out var a)) throw new ArgumentException("There is no control called " + id);
            return a;
        }

        public static bool Down(string id) => Action(id).WasPressedThisFrame();
        public static bool Held(string id) => Action(id).IsPressed();
        public static bool Up(string id) => Action(id).WasReleasedThisFrame();

        /// <summary>A stick or lever, after the dead zone, sensitivity and inversion of its settings (-1 to 1).</summary>
        public static float Axis(string id)
        {
            float v = Action(id).ReadValue<float>();
            return GameSettings.Current.input.Axis(ControlTable.Find(id).Name).Apply(v);
        }

        public static IReadOnlyList<Slot> SlotsOf(string id) { Ensure(); return Slots[id]; }

        /// <summary>The control's key and stick names as the player sees them ("Space / A"), for the guide and the rebinding screen.</summary>
        public static string Display(string id)
        {
            var action = Action(id);
            var names = new List<string>();
            // The keyboard's keys when the control has any; a control only a stick or pad can use shows those.
            bool anyKeyboard = Slots[id].Any(s => action.bindings[s.Key].effectivePath.StartsWith("<Keyboard>"));
            foreach (var slot in Slots[id])
            {
                if (anyKeyboard && !action.bindings[slot.Key].effectivePath.StartsWith("<Keyboard>")) continue;
                string s = action.GetBindingDisplayString(slot.Display, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                if (!string.IsNullOrEmpty(s) && !names.Contains(s)) names.Add(s);
            }
            return names.Count == 0 ? "—" : string.Join(" / ", names);
        }

        /// <summary>One binding of a control as the player sees it ("Space"); a combination reads "Ctrl + Z".</summary>
        public static string DisplaySlot(string id, int slot)
        {
            string s = Action(id).GetBindingDisplayString(Slots[id][slot].Display, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            return Loc.Name(string.IsNullOrEmpty(s) ? "—" : s);
        }

        // ------------------------------------------------------------------ guide

        /// <summary>The F1 guide's lines for one heading: the keys now bound, and what they do. Made from the table, so a rebind shows at once.</summary>
        public static List<(string keys, string action)> GuideRows(string section)
        {
            Ensure();
            var rows = new List<(string, string)>();
            foreach (var def in ControlTable.All)
            {
                if (def.Section != section || def.Defaults.Length == 0) continue;
                rows.Add((Loc.Name(Display(def.Id)), def.Label));
            }
            foreach (var m in ControlTable.MouseRows) if (m.section == section) rows.Add((m.keys, m.action));
            return rows;
        }

        /// <summary>Replaces {Map/Name} in a text with the keys of that control, as bound now.</summary>
        public static string Expand(string template) =>
            System.Text.RegularExpressions.Regex.Replace(template, @"\{([A-Za-z]+/[A-Za-z0-9]+)\}", m => Loc.Name(Display(m.Groups[1].Value)));

        // ------------------------------------------------------------------ rebinding

        /// <summary>The path a binding slot is using now (its override, or the default).</summary>
        public static string PathOf(string id, int slot) => Action(id).bindings[Slots[id][slot].Key].effectivePath;

        /// <summary>Whether a slot has been rebound from its default.</summary>
        public static bool IsRebound(string id, int slot) => PathOf(id, slot) != Slots[id][slot].Default;

        /// <summary>
        /// The other controls that already use <paramref name="path"/> where this one is used (same modifiers, same screen).
        /// Flight and EVA share keys on purpose: only one of them is ever active.
        /// </summary>
        public static List<(ControlDef def, int slot)> Conflicts(string id, int slot, string path)
        {
            Ensure();
            var mine = ControlTable.Find(id);
            string signature = Signature(id, slot, path);
            var found = new List<(ControlDef, int)>();
            foreach (var other in ControlTable.All)
            {
                if (!Shares(mine, other)) continue;
                for (int i = 0; i < Slots[other.Id].Count; i++)
                {
                    if (other.Id == id && i == slot) continue;
                    if (Signature(other.Id, i, PathOf(other.Id, i)) == signature) found.Add((other, i));
                }
            }
            return found;
        }

        private static string Signature(string id, int slot, string path)
        {
            var a = Action(id);
            int display = Slots[id][slot].Display, key = Slots[id][slot].Key;
            if (display == key) return path;
            // modifiers are the parts between the composite and the key
            var mods = new List<string>();
            for (int i = display + 1; i < key; i++) mods.Add(a.bindings[i].effectivePath);
            return string.Join("+", mods) + "+" + path;
        }

        private static readonly string[] SceneOfFlight = { "Common", "Flight", "Eva", "Camera", "Map", "Dev" };
        private static readonly string[] SceneOfAssembly = { "Common", "Assembly", "EditorCamera", "Dev" };

        private static bool Shares(ControlDef a, ControlDef b)
        {
            if ((a.Map == "Flight" && b.Map == "Eva") || (a.Map == "Eva" && b.Map == "Flight")) return false;
            return (SceneOfFlight.Contains(a.Map) && SceneOfFlight.Contains(b.Map)) || (SceneOfAssembly.Contains(a.Map) && SceneOfAssembly.Contains(b.Map));
        }

        /// <summary>Rebinds a slot. Any control that used the path is left without it, unless <paramref name="keepConflicts"/> is set.</summary>
        public static void Rebind(string id, int slot, string path, bool keepConflicts = false)
        {
            Ensure();
            if (!keepConflicts)
                foreach (var (def, s) in Conflicts(id, slot, path))
                    Set(def.Id, s, "");
            Set(id, slot, path);
            SaveOverrides();
            Changed?.Invoke();
        }

        /// <summary>Takes one control, or all of them, back to its default bindings.</summary>
        public static void ResetToDefaults(string id = null)
        {
            Ensure();
            foreach (var def in ControlTable.All)
            {
                if (id != null && def.Id != id) continue;
                for (int i = 0; i < Slots[def.Id].Count; i++) Set(def.Id, i, Slots[def.Id][i].Default);
            }
            SaveOverrides();
            Changed?.Invoke();
        }

        private static void Set(string id, int slot, string path)
        {
            var s = Slots[id][slot];
            if (path == s.Default) Action(id).RemoveBindingOverride(s.Key);
            else Action(id).ApplyBindingOverride(s.Key, path);
            string key = id + "#" + slot;
            if (path == s.Default) _overrides.Remove(key); else _overrides[key] = path;
        }

        // ------------------------------------------------------------------ saving

        private static void LoadOverrides()
        {
            _overrides = new Dictionary<string, string>();
            if (!Application.isPlaying && SaveStorage.RootOverride == null) return; // tools and tests start from the defaults
            try
            {
                if (!File.Exists(FilePath)) return;
                var saved = SaveStorage.FromJson<Dictionary<string, string>>(File.ReadAllText(FilePath)) ?? new Dictionary<string, string>();
                foreach (var kv in saved)
                {
                    int hash = kv.Key.LastIndexOf('#');
                    if (hash < 0 || !Slots.TryGetValue(kv.Key.Substring(0, hash), out var slots)) continue; // a control that no longer exists
                    if (!int.TryParse(kv.Key.Substring(hash + 1), out int slot) || slot < 0 || slot >= slots.Count) continue;
                    Set(kv.Key.Substring(0, hash), slot, kv.Value);
                }
            }
            catch (Exception e) { Debug.LogWarning($"The controls file {FilePath} could not be read ({e.Message}); the default controls are used."); }
        }

        private static void SaveOverrides()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                if (_overrides.Count == 0) { if (File.Exists(FilePath)) File.Delete(FilePath); return; }
                SaveStorage.WriteAtomic(FilePath, SaveStorage.ToJson(_overrides));
            }
            catch (Exception e) { Debug.LogError("Could not save the controls: " + e.Message); }
        }
    }
}
