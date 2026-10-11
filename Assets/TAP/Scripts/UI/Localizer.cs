using System.Collections.Generic;
using TAP.Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.UI
{
    /// <summary>
    /// Puts every text of every screen through <see cref="Loc.Translate"/> (FND-10): after the game has written its English into a
    /// label and before the frame is drawn, the label shows the player's language instead. It does nothing in English,
    /// unless an audit is on. Text typed into a field, and elements with the <c>no-loc</c> class (a clock, a readout), are left alone.
    /// </summary>
    [DefaultExecutionOrder(32000)] // after every script that writes text in this frame
    public sealed class Localizer : MonoBehaviour
    {
        private sealed class State { public string Source, Shown; }

        private readonly Dictionary<TextElement, State> _states = new Dictionary<TextElement, State>();
        private readonly List<TextElement> _buffer = new List<TextElement>();
        private readonly List<TextField> _fields = new List<TextField>();
        private readonly Dictionary<TextField, State> _placeholders = new Dictionary<TextField, State>();
        private string _language = "en";
        private int _version = -1;
        private float _nextPrune;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<Localizer>() != null) return;
            var go = new GameObject("Localizer");
            DontDestroyOnLoad(go);
            go.AddComponent<Localizer>();
        }

        private void LateUpdate()
        {
            if (!Loc.Active && _states.Count == 0) return;
            bool relang = _language != Loc.Signature || _version != Loc.Version;
            _language = Loc.Signature;
            _version = Loc.Version;
            for (int i = UIKit.Documents.Count - 1; i >= 0; i--)
            {
                var doc = UIKit.Documents[i];
                if (doc == null) { UIKit.Documents.RemoveAt(i); continue; }
                var root = doc.rootVisualElement;
                if (root == null) continue;
                _buffer.Clear();
                root.Query<TextElement>().ToList(_buffer);
                foreach (var e in _buffer) Process(e, relang);
                _fields.Clear();
                root.Query<TextField>().ToList(_fields);
                foreach (var f in _fields) Placeholder(f, relang);
            }
            if (Time.unscaledTime > _nextPrune)
            {
                _nextPrune = Time.unscaledTime + 5f;
                var gone = new List<TextElement>();
                foreach (var kv in _states) if (kv.Key.panel == null) gone.Add(kv.Key);
                foreach (var e in gone) _states.Remove(e);
            }
        }

        /// <summary>A text field's hint ("Search parts…") is text of the game, not of the player.</summary>
        private void Placeholder(TextField f, bool relang)
        {
            string current = f.textEdition.placeholder;
            if (string.IsNullOrEmpty(current)) return;
            if (!_placeholders.TryGetValue(f, out var state)) { state = new State(); _placeholders[f] = state; }
            else if (current == state.Shown)
            {
                if (!relang) return;
                current = state.Source;
            }
            state.Source = current;
            state.Shown = Loc.Active ? Loc.Translate(current) : current;
            if (state.Shown != f.textEdition.placeholder) f.textEdition.placeholder = state.Shown;
        }

        private void Process(TextElement e, bool relang)
        {
            if (e.ClassListContains("no-loc") || e.GetFirstAncestorOfType<TextField>() != null) return;
            string current = e.text;
            if (string.IsNullOrEmpty(current)) return;
            if (!_states.TryGetValue(e, out var state))
            {
                state = new State();
                _states[e] = state;
            }
            else if (current == state.Shown)
            {
                if (!relang) return; // still what the pass wrote
                current = state.Source;
            }
            state.Source = current;
            string shown = Loc.Active ? Loc.Translate(current) : current;
            state.Shown = shown;
            if (shown != e.text) e.text = shown;
        }
    }
}
