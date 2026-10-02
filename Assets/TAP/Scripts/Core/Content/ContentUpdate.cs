using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace TAP.Core
{
    /// <summary>
    /// Updating loaded definitions from a new version of their file (hot reload) without replacing them: whoever holds a
    /// definition, a part in flight or its engine, sees the new values at once.
    /// </summary>
    public static class ContentUpdate
    {
        /// <summary>One value that differs between two versions: its field path, and the old and new values.</summary>
        public readonly struct Change
        {
            public readonly string Path, Before, After;

            public Change(string path, string before, string after)
            {
                Path = path;
                Before = before;
                After = after;
            }

            /// <summary>"engine.thrustVac 215000 → 250000".</summary>
            public override string ToString() => Path + " " + Before + " → " + After;
        }

        /// <summary>Writes a definition as its file holds it: data fields only (not computed properties), enums by name.</summary>
        private static readonly JsonSerializer Data = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DataOnly(),
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore,
        });

        private sealed class DataOnly : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var p = base.CreateProperty(member, memberSerialization);
                if (member is PropertyInfo info && !info.CanWrite) p.Ignored = true;
                return p;
            }
        }

        /// <summary>The values that differ between two versions of a definition, nested objects field by field, lists whole.</summary>
        public static List<Change> Differences(object before, object after)
        {
            var list = new List<Change>();
            Diff(before == null ? null : JToken.FromObject(before, Data), after == null ? null : JToken.FromObject(after, Data), "", list);
            return list;
        }

        private static void Diff(JToken a, JToken b, string path, List<Change> list)
        {
            if (a is JObject oa && b is JObject ob)
            {
                foreach (var p in oa.Properties()) Diff(p.Value, ob[p.Name], path.Length == 0 ? p.Name : path + "." + p.Name, list);
                foreach (var p in ob.Properties())
                    if (oa[p.Name] == null) Diff(null, p.Value, path.Length == 0 ? p.Name : path + "." + p.Name, list);
                return;
            }
            bool noneA = a == null || a.Type == JTokenType.Null, noneB = b == null || b.Type == JTokenType.Null;
            if (noneA && noneB || !noneA && !noneB && JToken.DeepEquals(a, b)) return;
            list.Add(new Change(path, Show(a), Show(b)));
        }

        private static string Show(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) return "nothing";
            if (t.Type == JTokenType.Float) return ContentEntry.Number((double)t);
            string s = t.ToString(Formatting.None);
            return s.Length > 60 ? s.Substring(0, 60) + "…" : s;
        }

        /// <summary>
        /// Copies a new version into a loaded definition of the same type: values and lists are replaced, and nested
        /// definitions (a part's engine) are updated in place the same way, so references to them stay valid.
        /// </summary>
        public static void CopyInto(object target, object source)
        {
            if (target == null || source == null || target.GetType() != source.GetType())
                throw new ArgumentException("A definition is copied from another of the same type");
            foreach (var f in target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (f.IsInitOnly) continue;
                object now = f.GetValue(target), next = f.GetValue(source);
                if (now != null && next != null && now.GetType() == next.GetType() && IsDefinition(f.FieldType)) CopyInto(now, next);
                else f.SetValue(target, next);
            }
        }

        /// <summary>A data class read from content (not a value, text or list).</summary>
        private static bool IsDefinition(Type t) =>
            t.IsClass && t.IsSerializable && t != typeof(string) && !t.IsArray && !typeof(IEnumerable).IsAssignableFrom(t);
    }
}
