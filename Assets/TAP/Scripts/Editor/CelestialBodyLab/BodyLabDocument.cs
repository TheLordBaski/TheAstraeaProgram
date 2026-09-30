using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TAP.Core;

namespace TAP.EditorTools
{
    /// <summary>
    /// An authoring draft and its session history. Only Body is exported; neither history nor editor state belongs
    /// to the game's JSON. Edits to inherited lists copy the list, while edits to objects write sparse overrides.
    /// </summary>
    public sealed class BodyLabDocument
    {
        private JObject _system, _body, _original;
        private string _originalId, _saved, _initial;
        private bool _newBody;
        private readonly List<string> _undo = new List<string>(), _redo = new List<string>();
        private string _lastEdit;
        private long _lastEditTime;
        public event Action Changed;
        public Func<string, string> Presets { get; }
        public JObject Body => (JObject)_body.DeepClone();
        public bool Dirty => _body.ToString(Formatting.None) != _saved;
        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;
        public bool IsNew => _newBody;
        public string Id => (string)_body["id"];
        public IEnumerable<string> BodyIds => ((JArray)_system["bodies"]).Select(b => (string)b["id"]);
        public IEnumerable<string> MoonParentIds => ((JArray)_system["bodies"]).Where(b => (string)b["type"] != "star" && (string)b["type"] != "moon").Select(b => (string)b["id"]);

        public BodyLabDocument(string systemJson, string bodyId, Func<string, string> presets)
        {
            _system = JObject.Parse(systemJson);
            Presets = presets;
            _original = (JObject)((JArray)_system["bodies"]).First(b => (string)b["id"] == bodyId).DeepClone();
            _body = (JObject)_original.DeepClone();
            _originalId = bodyId;
            _saved = _body.ToString(Formatting.None);
            _initial = _saved;
        }

        /// <summary>A snapshot for Unity's session memory, never a game-data export or an asset.</summary>
        public JObject CaptureSession() => new JObject
        {
            ["system"] = _system.DeepClone(), ["body"] = Body, ["original"] = _original.DeepClone(),
            ["originalId"] = _originalId, ["saved"] = _saved, ["initial"] = _initial, ["newBody"] = _newBody,
            ["undo"] = new JArray(_undo), ["redo"] = new JArray(_redo)
        };

        public static BodyLabDocument RestoreSession(JObject state, Func<string, string> presets)
        {
            var d = new BodyLabDocument(state["system"].ToString(), (string)state["originalId"], presets);
            d._body = (JObject)state["body"].DeepClone(); d._original = (JObject)state["original"].DeepClone();
            d._saved = (string)state["saved"]; d._initial = (string)state["initial"];
            d._newBody = (bool)state["newBody"];
            d._undo.AddRange(state["undo"].Values<string>()); d._redo.AddRange(state["redo"].Values<string>());
            return d;
        }

        /// <summary>Creates an unsaved body in this system's context; the source body remains untouched.</summary>
        public BodyLabDocument NewBody(bool duplicate)
        {
            var d = new BodyLabDocument(_system.ToString(), _originalId, Presets);
            string id = "newbody";
            for (int i = 2; BodyIds.Contains(id); i++) id = "newbody" + i;
            var parent = ((JArray)_system["bodies"]).FirstOrDefault(b => (string)b["type"] == "star") ?? _system["bodies"][0];
            d._body = duplicate ? Body : new JObject
            {
                ["type"] = "moon", ["radius"] = 100000.0, ["gm"] = 1e10, ["rotationPeriod"] = 86400.0,
                ["parent"] = (string)parent["id"],
                ["orbit"] = new JObject { ["semiMajorAxis"] = Math.Max((double)parent["radius"] * 10, 1e7), ["eccentricity"] = 0.0 },
                ["terrain"] = new JObject
                {
                    ["seed"] = 1, ["minHeight"] = -1000.0, ["maxHeight"] = 2000.0,
                    ["layers"] = new JArray(new JObject { ["noise"] = new JObject { ["type"] = "fbm", ["frequency"] = 3.0, ["octaves"] = 4 }, ["amplitude"] = 1000.0 }),
                    ["colors"] = new JArray(new JObject { ["color"] = "#898680" }),
                    ["biomes"] = new JArray(new JObject { ["id"] = "surface", ["name"] = "Surface", ["color"] = "#898680" })
                },
                ["mapColor"] = new JArray(0.5, 0.5, 0.5)
            };
            d._body["id"] = id;
            d._body["displayName"] = duplicate ? ((string)_body["displayName"] ?? Id) + " Copy" : "New Body";
            d._newBody = true;
            d._saved = "";
            d._initial = d._body.ToString(Formatting.None);
            return d;
        }

        public JObject EffectiveBody()
        {
            var s = new JObject { ["bodies"] = new JArray(Body) };
            CelestialSystem.ResolveTerrainPresets(s, Presets);
            var body = (JObject)s["bodies"][0];
            NormalizeLists(body, typeof(BodyDefinition));
            return body;
        }

        private static void NormalizeLists(JObject value, Type type)
        {
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var token = value[field.Name];
                if (token == null || token.Type == JTokenType.Null) continue;
                Type ft = field.FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type element = ft.GetGenericArguments()[0];
                    if (token is JObject) value[field.Name] = token = new JArray(token.DeepClone());
                    if (token is JArray list)
                        foreach (var item in list) if (item is JObject obj) NormalizeLists(obj, element);
                }
                else if (token is JObject obj) NormalizeLists(obj, ft);
            }
        }

        /// <summary>Sets one schema path. A missing array is copied from the resolved preset before changing it.</summary>
        public void Set(string path, JToken value, bool coalesce = false)
        {
            var draft = Body;
            JObject effective;
            try { effective = EffectiveBody(); }
            catch (Exception e) when (e is FormatException || e is System.IO.IOException || e is JsonException)
            { effective = Body; } // An invalid preset name must remain editable.
            var parts = Regex.Matches(path, @"([^\.\[\]]+)|\[(\d+)\]");
            JToken target = draft, source = effective;
            for (int i = 0; i < parts.Count; i++)
            {
                bool index = parts[i].Groups[2].Success;
                string key = parts[i].Groups[1].Value;
                int n = index ? int.Parse(parts[i].Groups[2].Value) : 0;
                if (i == parts.Count - 1)
                {
                    if (index) ((JArray)target)[n] = value?.DeepClone() ?? JValue.CreateNull();
                    else ((JObject)target)[key] = value?.DeepClone() ?? JValue.CreateNull();
                    break;
                }
                JToken next = index ? target[n] : target[key];
                JToken inherited = source == null ? null : index ? source[n] : source[key];
                if (parts[i + 1].Groups[2].Success && inherited is JObject) inherited = new JArray(inherited.DeepClone());
                if (parts[i + 1].Groups[2].Success && next is JObject)
                {
                    next = new JArray(next.DeepClone());
                    if (index) target[n] = next; else target[key] = next;
                }
                if (next == null || next.Type == JTokenType.Null)
                {
                    bool array = parts[i + 1].Groups[2].Success;
                    next = array ? inherited?.DeepClone() ?? new JArray() : new JObject();
                    if (index) target[n] = next; else target[key] = next;
                }
                target = next;
                source = inherited;
            }
            Commit(draft, path, coalesce);
        }

        /// <summary>Removes an override, letting the preset or DTO default supply the value again.</summary>
        public void Remove(string path)
        {
            var draft = Body;
            var token = draft.SelectToken(path);
            if (token?.Parent is JProperty p) p.Remove();
            Commit(draft);
        }

        public void Commit(JObject body, string editKey = null, bool coalesce = false)
        {
            if (JToken.DeepEquals(body, _body)) return;
            long now = Stopwatch.GetTimestamp();
            bool sameGesture = coalesce && editKey != null && editKey == _lastEdit &&
                (now - _lastEditTime) / (double)Stopwatch.Frequency < 0.6;
            if (!sameGesture)
            {
                _undo.Add(_body.ToString(Formatting.None));
                if (_undo.Count > 128) _undo.RemoveAt(0);
            }
            _redo.Clear();
            _body = (JObject)body.DeepClone();
            _lastEdit = coalesce ? editKey : null;
            _lastEditTime = now;
            Changed?.Invoke();
        }

        public void EndGesture() => _lastEdit = null;
        public void Undo() => Travel(_undo, _redo);
        public void Redo() => Travel(_redo, _undo);
        private void Travel(List<string> from, List<string> to)
        {
            if (from.Count == 0) return;
            to.Add(_body.ToString(Formatting.None));
            _body = JObject.Parse(from[from.Count - 1]);
            from.RemoveAt(from.Count - 1);
            _lastEdit = null;
            Changed?.Invoke();
        }

        public void Revert() => Commit(JObject.Parse(_newBody ? _initial : _saved));

        private JObject EditedSystem(JObject source)
        {
            var system = (JObject)source.DeepClone();
            var bodies = (JArray)system["bodies"];
            if (_newBody) bodies.Add(Body);
            else
            {
                var original = bodies.FirstOrDefault(b => (string)b["id"] == _originalId);
                if (original == null) throw new FormatException("The source body was removed. Reload the system before saving.");
                original.Replace(Body);
                if (Id != _originalId)
                {
                    foreach (var b in bodies) if ((string)b["parent"] == _originalId) b["parent"] = Id;
                    if ((string)system["launchSite"]?["body"] == _originalId) system["launchSite"]["body"] = Id;
                }
            }
            return system;
        }

        public CelestialSystem BuildSystem() => ValidateAndBuild(EditedSystem(_system));

        public CelestialSystem BuildSourceSystem() => ValidateAndBuild(_system);

        public BodyLabDocument NewRandomBody(bool moon, int seed, string parentId = null)
        {
            var d = NewBody(false);
            d._body = BodyLabRandomizer.Generate(BuildSourceSystem(), d.Id, moon, seed, parentId);
            d._initial = d._body.ToString(Formatting.None);
            return d;
        }

        private CelestialSystem ValidateAndBuild(JObject system)
        {
            BodyLabValidation.CheckSystem(system);
            var resolved = (JObject)system.DeepClone();
            CelestialSystem.ResolveTerrainPresets(resolved, Presets);
            foreach (var token in (JArray)resolved["bodies"]) BodyLabValidation.CheckBody((JObject)token);
            return new CelestialSystem(resolved.ToObject<SystemDefinition>(), "lab");
        }

        /// <summary>Changes only this body in the latest source, preserving other edits and unknown JSON properties.</summary>
        public string SaveJson(string latestJson)
        {
            var latest = JObject.Parse(latestJson);
            if (!_newBody)
            {
                var current = ((JArray)latest["bodies"]).FirstOrDefault(b => (string)b["id"] == _originalId);
                if (!JToken.DeepEquals(current, _original))
                    throw new FormatException("This body changed on disk. Reload it before saving to avoid overwriting those changes.");
            }
            var output = EditedSystem(latest);
            ValidateAndBuild(output);
            return output.ToString(Formatting.Indented) + "\n";
        }

        public void MarkSaved(string systemJson)
        {
            _system = JObject.Parse(systemJson);
            _original = (JObject)((JArray)_system["bodies"]).First(b => (string)b["id"] == Id).DeepClone();
            _originalId = Id;
            _newBody = false;
            _saved = _body.ToString(Formatting.None);
            Changed?.Invoke();
        }
    }
}
