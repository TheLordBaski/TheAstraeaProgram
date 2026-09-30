using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TAP.Core;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TAP.EditorTools
{
    /// <summary>Controls generated from the game's DTO fields; editing writes only the changed schema path.</summary>
    public sealed class BodyLabFields
    {
        private readonly BodyLabDocument _document;
        private readonly Action _rebuild;
        private readonly Dictionary<string, bool> _folds;
        private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        public BodyLabFields(BodyLabDocument document, Action rebuild, Dictionary<string, bool> folds)
        {
            _document = document;
            _rebuild = rebuild;
            _folds = folds;
        }

        public void Draw(VisualElement parent, JObject effective)
        {
            ObjectFields(parent, typeof(BodyDefinition), effective, "", 0);
        }

        private void ObjectFields(VisualElement parent, Type type, JObject value, string path, int depth)
        {
            if (depth > 12) { parent.Add(new HelpBox("Nested terms are limited to 12 levels in the authoring window.", HelpBoxMessageType.Warning)); return; }
            object defaults = Activator.CreateInstance(type);
            string[] sources = type == typeof(TerrainLayerDef) ? new[] { "noise", "from", "constant", "craters", "rift", "canyons", "terraces", "dunes", "ellipsoid" }
                : type == typeof(TerrainFieldDef) ? new[] { "noise", "from", "sum", "term" }
                : type == typeof(TermDef) ? new[] { "of", "near", "sum" } : null;
            string selectedSource = sources == null ? null : ChoiceGroup(parent, value, path, "Source", sources, false, type);
            string[] shapes = type == typeof(TermDef) ? new[] { "smooth", "linear", "range", "below", "atLeast" }
                : type == typeof(TerrainLayerDef) || type == typeof(TerrainFieldDef) ? new[] { "smooth", "linear" } : null;
            string selectedShape = shapes == null ? null : ChoiceGroup(parent, value, path, "Shape", shapes, true, type);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (sources != null && sources.Contains(field.Name) && field.Name != selectedSource) continue;
                if (shapes != null && shapes.Contains(field.Name) && field.Name != selectedShape) continue;
                string p = path.Length == 0 ? field.Name : path + "." + field.Name;
                JToken token = value?[field.Name];
                if (field.Name == "preset" && type == typeof(TerrainDefinition)) token = _document.Body.SelectToken("terrain.preset");
                if (token == null)
                {
                    object def = field.GetValue(defaults);
                    if (def != null) token = JToken.FromObject(def, Serializer);
                }
                Field(parent, field.FieldType, token, p, field.Name, type, depth);
            }
        }

        private string ChoiceGroup(VisualElement parent, JObject value, string path, string label, string[] keys, bool optional, Type owner)
        {
            string selected = keys.FirstOrDefault(k => value?[k] != null && value[k].Type != JTokenType.Null);
            string empty = optional ? "None" : "Choose source";
            var choices = new List<string> { empty }; choices.AddRange(keys);
            var popup = new PopupField<string>(label, choices, selected ?? empty);
            popup.tooltip = string.Join("\n", keys.Select(k => k + ": " + BodyLabHelp.Describe(owner, k)));
            popup.name = path + "." + label.ToLowerInvariant(); parent.Add(popup);
            popup.RegisterValueChangedCallback(e =>
            {
                var obj = (JObject)_document.EffectiveBody().SelectToken(path)?.DeepClone() ?? new JObject();
                foreach (string key in keys) obj.Remove(key);
                if (e.newValue != empty)
                {
                    string key = e.newValue;
                    obj[key] = key == "from" || key == "of" ? new JValue(key == "of" ? "height" : FirstField())
                        : key == "near" ? new JValue("placeId")
                        : key == "constant" || key == "below" || key == "atLeast" ? new JValue(0.0)
                        : key == "smooth" || key == "linear" || key == "range" ? new JArray(0.0, 1.0)
                        : key == "ellipsoid" ? new JArray((double)_document.Body["radius"] * 1.1, (double)_document.Body["radius"], (double)_document.Body["radius"] * 0.9)
                        : key == "noise" ? new JObject { ["type"] = "fbm", ["frequency"] = 1.0, ["octaves"] = 4 }
                        : key == "rift" ? new JObject { ["path"] = new JArray(new JArray(0, -10), new JArray(0, 10)), ["width"] = 20000.0, ["depth"] = 2000.0 }
                        : key == "craters" ? new JObject { ["sizes"] = new JArray(new JObject { ["cell"] = 10000.0, ["chance"] = 0.5, ["depth"] = 0.2 }) }
                        : key == "canyons" ? new JObject { ["noise"] = new JObject { ["type"] = "fbm" }, ["width"] = 0.03, ["depth"] = 800.0 }
                        : key == "dunes" ? JObject.FromObject(new DuneDef(), Serializer)
                        : key == "terraces" ? JObject.FromObject(new TerraceDef(), Serializer)
                        : key == "term" ? new JObject { ["of"] = "latitude" }
                        : new JArray(new JObject { ["of"] = FirstField() });
                }
                _document.Set(path, obj); _rebuild();
            });
            return selected;
        }

        private string FirstField() => (string)_document.EffectiveBody().SelectToken("terrain.fields[0].id") ?? "fieldId";

        private void Field(VisualElement parent, Type type, JToken token, string path, string name, Type owner, int depth)
        {
            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
            {
                var row = Row(parent);
                var use = new Toggle("Use " + Label(owner, name)) { value = token != null && token.Type != JTokenType.Null, tooltip = BodyLabHelp.Describe(owner, name) };
                row.Add(use);
                use.RegisterValueChangedCallback(e => { _document.Set(path, e.newValue ? new JValue(0.0) : JValue.CreateNull()); _rebuild(); });
                if (use.value) Field(parent, nullable, token, path, name, owner, depth);
                return;
            }
            if (type == typeof(float[]) && name.EndsWith("Color", StringComparison.OrdinalIgnoreCase))
            {
                var a = token as JArray;
                if (a != null && a.Count == 3)
                {
                    var c = new ColorField(Label(owner, name)) { value = new Color((float)a[0], (float)a[1], (float)a[2]), showAlpha = false };
                    AddScalar(parent, c, path, name, owner);
                    c.RegisterValueChangedCallback(e => _document.Set(path, new JArray((double)e.newValue.r, (double)e.newValue.g, (double)e.newValue.b), true));
                    return;
                }
            }
            Type element = type.IsArray ? type.GetElementType() : type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0] : null;
            if (element != null) { List(parent, element, token, path, name, owner, depth); return; }
            if (type == typeof(string))
            {
                string current = token?.Type == JTokenType.Null ? "" : (string)token ?? "";
                var choices = Choices(owner, name);
                if (choices != null)
                {
                    if (!choices.Contains(current)) choices.Add(current);
                    var popup = new PopupField<string>(Label(owner, name), choices, current);
                    AddScalar(parent, popup, path, name, owner);
                    popup.RegisterValueChangedCallback(e => _document.Set(path, new JValue(e.newValue)));
                }
                else if (name == "color")
                {
                    ColorUtility.TryParseHtmlString(current, out var color);
                    var c = new ColorField(Label(owner, name)) { value = color, showAlpha = false };
                    AddScalar(parent, c, path, name, owner);
                    c.RegisterValueChangedCallback(e => _document.Set(path, new JValue("#" + ColorUtility.ToHtmlStringRGB(e.newValue)), true));
                }
                else
                {
                    var text = new TextField(Label(owner, name)) { value = current, multiline = name == "description" || name == "note" };
                    AddScalar(parent, text, path, name, owner);
                    text.RegisterValueChangedCallback(e => _document.Set(path, new JValue(e.newValue), true));
                }
                return;
            }
            if (type == typeof(bool))
            {
                var toggle = new Toggle(Label(owner, name)) { value = (bool?)token ?? false };
                AddScalar(parent, toggle, path, name, owner);
                toggle.RegisterValueChangedCallback(e => _document.Set(path, new JValue(e.newValue)));
                return;
            }
            if (type == typeof(int))
            {
                var number = new IntegerField(Label(owner, name)) { value = (int?)token ?? 0 };
                AddScalar(parent, number, path, name, owner);
                number.RegisterValueChangedCallback(e => _document.Set(path, new JValue(e.newValue), true));
                return;
            }
            if (type == typeof(double) || type == typeof(float))
            {
                var number = new DoubleField(Label(owner, name)) { value = (double?)token ?? 0 };
                AddScalar(parent, number, path, name, owner);
                number.RegisterValueChangedCallback(e => _document.Set(path, new JValue(e.newValue), true));
                return;
            }
            Object(parent, type, token as JObject, path, name, depth, owner);
        }

        private void AddScalar(VisualElement parent, VisualElement field, string path, string name, Type owner)
        {
            var row = Row(parent);
            field.style.flexGrow = 1;
            field.style.minWidth = 0;
            field.name = path;
            string property = name == "value" ? path.Split('.').Last().Split('[')[0] : name;
            field.tooltip = BodyLabHelp.Describe(owner, property) + "\nJSON: " + path;
            field.AddToClassList("unity-base-field__aligned");
            field.RegisterCallback<FocusOutEvent>(_ => _document.EndGesture());
            row.Add(field);
            var reset = new Button(() => { _document.Remove(path); _rebuild(); }) { text = "↶", tooltip = "Remove this override and use the inherited/default value." };
            reset.SetEnabled(!path.EndsWith("]", StringComparison.Ordinal) && _document.Body.SelectToken(path) != null);
            row.Add(reset);
        }

        private void Object(VisualElement parent, Type type, JObject value, string path, string name, int depth, Type owner)
        {
            var fold = Fold(parent, path, Label(owner, name), depth == 0 && (name == "terrain" || name == "orbit"));
            fold.tooltip = BodyLabHelp.Describe(owner, name);
            if (value == null)
            {
                fold.Add(new Button(() => { _document.Set(path, JToken.FromObject(Activator.CreateInstance(type), Serializer)); _folds[path] = true; _rebuild(); }) { text = "Add " + Label(owner, name) });
                return;
            }
            var row = Row(fold);
            var remove = new Button(() => { _document.Set(path, JValue.CreateNull()); _rebuild(); }) { text = "Disable", tooltip = "Inherited objects require Make Terrain Inline before they can be disabled." };
            bool inherited = path.StartsWith("terrain.", StringComparison.Ordinal) && _document.Body.SelectToken("terrain.preset") != null &&
                _document.EffectiveBody().SelectToken(path) != null;
            remove.SetEnabled(!inherited);
            row.Add(remove);
            row.Add(new Button(() => { _document.Remove(path); _rebuild(); }) { text = "Use inherited/default" });
            ObjectFields(fold, type, value, path, depth + 1);
        }

        private void List(VisualElement parent, Type element, JToken token, string path, string name, Type owner, int depth)
        {
            // Masks/when/sums also accept a single object. Keep that shape until this list is actually edited.
            var array = token is JArray a ? (JArray)a.DeepClone() : token is JObject ? new JArray(token.DeepClone()) : new JArray();
            var fold = Fold(parent, path, Label(owner, name) + " (" + array.Count + ")", depth == 0);
            fold.tooltip = BodyLabHelp.Describe(owner, name);
            var toolbar = Row(fold);
            toolbar.Add(new Button(() => ChangeList(path, array, x =>
            {
                if (x.Count == 0 && (name == "offset" || name == "normal" || name == "ellipsoid"))
                {
                    x.Add(name == "normal" ? 1.0 : 0.0); x.Add(0.0); x.Add(0.0);
                }
                else x.Add(DefaultElement(element, name));
            })) { text = "+ Add", tooltip = "Adds an entry at the end; order affects fields, layers, colours and biomes." });
            toolbar.Add(new Button(() => { _document.Set(path, new JArray()); _rebuild(); }) { text = "Clear" });
            toolbar.Add(new Button(() => { _document.Remove(path); _rebuild(); }) { text = "Use inherited/default" });
            for (int i = 0; i < array.Count; i++)
            {
                int index = i;
                string itemPath = path + "[" + i + "]";
                var entry = new VisualElement();
                entry.AddToClassList("body-lab-entry");
                fold.Add(entry);
                var row = Row(entry);
                string title = (string)(array[i] as JObject)?["id"] ?? (string)(array[i] as JObject)?["note"] ?? "Entry " + (i + 1);
                var label = new Label(title); label.style.flexGrow = 1; row.Add(label);
                var up = new Button(() => ChangeList(path, array, x => Swap(x, index, index - 1))) { text = "↑", tooltip = "Move earlier" }; up.SetEnabled(i > 0); row.Add(up);
                var down = new Button(() => ChangeList(path, array, x => Swap(x, index, index + 1))) { text = "↓", tooltip = "Move later" }; down.SetEnabled(i + 1 < array.Count); row.Add(down);
                row.Add(new Button(() => ChangeList(path, array, x => x.Insert(index + 1, x[index].DeepClone()))) { text = "Copy" });
                row.Add(new Button(() => ChangeList(path, array, x => x.RemoveAt(index))) { text = "×", tooltip = "Remove entry" });
                if (array[i] is JObject obj)
                {
                    var item = Fold(entry, itemPath, "Parameters", false);
                    ObjectFields(item, element, obj, itemPath, depth + 1);
                }
                else
                {
                    // Editing inside a singleton mask needs the array representation first.
                    if (!(token is JArray))
                        entry.Add(new Button(() => { _document.Set(path, array); _rebuild(); }) { text = "Edit list" });
                    else Field(entry, element, array[i], itemPath, "value", owner, depth + 1);
                }
            }
        }

        private void ChangeList(string path, JArray displayed, Action<JArray> edit)
        {
            // Read fresh values: scalar controls may have changed since the UI was built.
            var current = _document.EffectiveBody().SelectToken(path);
            var a = current is JArray list ? (JArray)list.DeepClone() : current is JObject ? new JArray(current.DeepClone()) : (JArray)displayed.DeepClone();
            edit(a);
            _document.Set(path, a);
            _folds[path] = true;
            _rebuild();
        }

        private static void Swap(JArray a, int i, int j)
        {
            var first = a[i].DeepClone(); var second = a[j].DeepClone(); a[i] = second; a[j] = first;
        }

        private Foldout Fold(VisualElement parent, string path, string label, bool open)
        {
            var fold = new Foldout { text = label, value = _folds.TryGetValue(path, out bool stored) ? stored : open };
            fold.RegisterValueChangedCallback(e => { if (e.target == fold) _folds[path] = e.newValue; });
            parent.Add(fold);
            return fold;
        }

        private static VisualElement Row(VisualElement parent)
        {
            var row = new VisualElement(); row.AddToClassList("body-lab-row"); parent.Add(row); return row;
        }

        private static JToken DefaultElement(Type type, string list)
        {
            if (type == typeof(double[])) return list == "temperatureCurve" ? new JArray(0.0, 288.15) : new JArray(0.0, 0.0);
            if (type.IsValueType) return new JValue(0.0);
            if (type == typeof(BiomeDef)) return new JObject { ["id"] = "newBiome", ["name"] = "New Biome", ["color"] = "#808080" };
            if (type == typeof(TerrainLayerDef)) return new JObject { ["constant"] = 0.0 };
            if (type == typeof(TerrainFieldDef)) return new JObject { ["id"] = "newField", ["noise"] = new JObject { ["type"] = "fbm" } };
            if (type == typeof(ColorRuleDef)) return new JObject { ["color"] = "#808080" };
            if (type == typeof(TermDef)) return new JObject { ["of"] = "height", ["atLeast"] = 0.0 };
            if (type == typeof(PlaceDef)) return new JObject { ["id"] = "newPlace", ["lat"] = 0.0, ["lon"] = 0.0 };
            if (type == typeof(CraterSizeDef)) return new JObject { ["cell"] = 10000.0, ["chance"] = 0.5, ["depth"] = 0.2 };
            return JToken.FromObject(Activator.CreateInstance(type), Serializer);
        }

        private static List<string> Choices(Type owner, string name)
        {
            if (owner == typeof(BodyDefinition) && name == "type") return new List<string> { "planet", "moon", "dwarf", "gasgiant", "star" };
            if (owner == typeof(NoiseDef) && name == "type") return new List<string> { "fbm", "ridged", "billow", "perlin" };
            if (owner == typeof(TerrainLayerDef) && name == "blend") return new List<string> { "add", "max", "min" };
            return null;
        }

        private static string Label(Type owner, string name) => BodyLabHelp.Label(owner, name);
    }
}
