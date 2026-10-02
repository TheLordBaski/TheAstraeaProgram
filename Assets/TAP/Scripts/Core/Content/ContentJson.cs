using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TAP.Core
{
    /// <summary>
    /// Reading content JSON. The C# class a value is read into is its schema: <see cref="CheckShape"/> reports every
    /// field the class doesn't have and every value of the wrong kind (text for a number, an object for a list), and
    /// <see cref="ToObject{T}"/> then reads what it can. Rules about the values themselves live with each kind of content.
    /// </summary>
    public static class ContentJson
    {
        private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
        {
            LineInfoHandling = LineInfoHandling.Load,
            CommentHandling = CommentHandling.Ignore,
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
        };

        /// <summary>Parses a file with line numbers; a syntax error is reported and gives null.</summary>
        public static JToken Parse(string text, string file, ContentReport report)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                report.Add(new ContentProblem { Severity = ContentSeverity.Error, File = file, Expected = "JSON", Found = "an empty file" });
                return null;
            }
            try
            {
                return JToken.Parse(text, LoadSettings);
            }
            catch (JsonReaderException e)
            {
                var p = new ContentProblem
                {
                    Severity = ContentSeverity.Error, File = file, Line = e.LineNumber,
                    Expected = "valid JSON", Found = SyntaxError(e.Message),
                    Hint = "nothing in the file can be read until this is fixed",
                };
                var dup = Regex.Match(e.Message, "Property with the name '(.+?)' already exists");
                if (dup.Success)
                {
                    p.Expected = "each field once";
                    p.Found = $"\"{dup.Groups[1].Value}\" twice";
                }
                report.Add(p);
                return null;
            }
        }

        /// <summary>Newtonsoft's message without the path and position it appends (the line is reported separately).</summary>
        private static string SyntaxError(string message)
        {
            string m = Regex.Replace(message, @"\s*Path '.*$", "", RegexOptions.Singleline).Trim().TrimEnd('.');
            return "a syntax error: " + (m.Length > 0 ? char.ToLowerInvariant(m[0]) + m.Substring(1) : "unreadable");
        }

        /// <summary>Reads JSON into a type, leaving out values of the wrong kind (<see cref="CheckShape"/> reports them).</summary>
        public static T ToObject<T>(JToken token, JsonSerializerSettings settings = null)
        {
            var s = JsonSerializer.Create(settings ?? new JsonSerializerSettings());
            s.Error += (o, e) => e.ErrorContext.Handled = true;
            return token.ToObject<T>(s);
        }

        // ------------------------------------------------------------------ shape

        /// <summary>
        /// Checks a JSON value against the C# type it will be read into: every field must be one the type has (a field
        /// spelled with other capitals still loads, with a warning; an unknown field is ignored, with a warning and the
        /// nearest name), and every value of the right kind (an error).
        /// </summary>
        /// <param name="path">The value's field path within the entry ("" for the entry itself).</param>
        /// <param name="skip">Top-level fields not to check (checked as entries of their own).</param>
        public static void CheckShape(JToken token, Type type, ContentEntry entry, string path = "", ICollection<string> skip = null)
        {
            if (token == null) return;
            if (token.Type == JTokenType.Null)
            {
                if (type.IsValueType && Nullable.GetUnderlyingType(type) == null) entry.Error(path, Kind(type), "null");
                return;
            }
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(object) || typeof(JToken).IsAssignableFrom(type)) return;
            if (type == typeof(string))
            {
                if (token.Type != JTokenType.String) entry.Error(path, "text");
                return;
            }
            if (type == typeof(bool))
            {
                if (token.Type != JTokenType.Boolean) entry.Error(path, "true or false");
                return;
            }
            if (IsWhole(type))
            {
                bool ok = token.Type == JTokenType.Integer || token.Type == JTokenType.Float && Math.Abs((double)token % 1) == 0;
                if (!ok) entry.Error(path, "a whole number");
                return;
            }
            if (IsReal(type))
            {
                if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float) entry.Error(path, "a number");
                else if (double.IsNaN((double)token) || double.IsInfinity((double)token)) entry.Error(path, "a finite number");
                return;
            }
            if (type.IsEnum)
            {
                if (token.Type != JTokenType.String || !HasName(type, (string)token)) entry.Error(path, Kind(type));
                return;
            }
            var element = ElementType(type);
            if (element != null)
            {
                if (token is JArray list)
                    for (int i = 0; i < list.Count; i++) CheckShape(list[i], element, entry, $"{path}[{i}]");
                else entry.Error(path, "a list");
                return;
            }
            var values = DictionaryValueType(type);
            if (values != null)
            {
                if (token is JObject map)
                    foreach (var p in map.Properties()) CheckShape(p.Value, values, entry, Join(path, p.Name));
                else entry.Error(path, "an object");
                return;
            }
            if (!(token is JObject obj))
            {
                entry.Error(path, "an object");
                return;
            }
            var members = Members(type);
            foreach (var prop in obj.Properties())
            {
                if (path.Length == 0 && skip != null && skip.Contains(prop.Name)) continue;
                string p = Join(path, prop.Name);
                if (!members.TryGetValue(prop.Name, out var m))
                {
                    m = IgnoringCase(members, prop.Name);
                    if (m == null)
                    {
                        entry.Warning(p, "a field " + Describe(type) + " has", $"\"{prop.Name}\"", Suggest(members, prop.Name));
                        continue;
                    }
                    entry.Warning(p, $"\"{m.Name}\"", $"\"{prop.Name}\"", "it loads, but write field names with the capitals the format uses");
                }
                if (m.SingleOrList != null && prop.Value is JArray many)
                    for (int i = 0; i < many.Count; i++) CheckShape(many[i], m.SingleOrList, entry, $"{p}[{i}]");
                else CheckShape(prop.Value, m.SingleOrList ?? m.Type, entry, p);
            }
        }

        /// <summary>What a value of the type looks like: "a number", "one of Part, Stack, Vessel".</summary>
        public static string Kind(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type == typeof(string)) return "text";
            if (type == typeof(bool)) return "true or false";
            if (IsWhole(type)) return "a whole number";
            if (IsReal(type)) return "a number";
            if (type.IsEnum) return "one of " + string.Join(", ", Enum.GetNames(type));
            if (ElementType(type) != null) return "a list";
            return "an object";
        }

        private static string Join(string path, string name) => path.Length == 0 ? name : path + "." + name;

        private static bool IsWhole(Type t) => t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) || t == typeof(uint);

        private static bool IsReal(Type t) => t == typeof(double) || t == typeof(float) || t == typeof(decimal);

        private static bool HasName(Type enumType, string name)
        {
            foreach (var n in Enum.GetNames(enumType))
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static Type ElementType(Type t)
        {
            if (t.IsArray) return t.GetElementType();
            if (t.IsGenericType)
            {
                var g = t.GetGenericTypeDefinition();
                if (g == typeof(List<>) || g == typeof(IList<>) || g == typeof(IReadOnlyList<>) || g == typeof(IEnumerable<>))
                    return t.GetGenericArguments()[0];
            }
            return null;
        }

        private static Type DictionaryValueType(Type t)
        {
            if (!t.IsGenericType) return null;
            var g = t.GetGenericTypeDefinition();
            if ((g == typeof(Dictionary<,>) || g == typeof(IDictionary<,>)) && t.GetGenericArguments()[0] == typeof(string))
                return t.GetGenericArguments()[1];
            return null;
        }

        /// <summary>"EngineDefinition" → "an engine", for "a field an engine has".</summary>
        private static string Describe(Type type)
        {
            string name = Regex.Replace(type.Name, "(Definition|Def|Record)$", "");
            string words = Regex.Replace(name.Length > 0 ? name : type.Name, "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant();
            return ("aeiou".IndexOf(words[0]) >= 0 ? "an " : "a ") + words;
        }

        // ------------------------------------------------------------------ members

        private sealed class Member
        {
            public string Name;
            public Type Type;
            /// <summary>Set when the field takes one value or a list of them (<see cref="SingleOrListConverter{T}"/>).</summary>
            public Type SingleOrList;
        }

        private static readonly Dictionary<Type, Dictionary<string, Member>> MemberCache = new Dictionary<Type, Dictionary<string, Member>>();

        /// <summary>The fields Newtonsoft reads into a type: public fields and settable properties, by their JSON names.</summary>
        private static Dictionary<string, Member> Members(Type type)
        {
            lock (MemberCache)
            {
                if (MemberCache.TryGetValue(type, out var cached)) return cached;
                var map = new Dictionary<string, Member>();
                foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (!f.IsInitOnly || f.FieldType.IsClass) AddMember(map, f, f.FieldType);
                foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (p.CanWrite && p.GetSetMethod() != null && p.GetIndexParameters().Length == 0) AddMember(map, p, p.PropertyType);
                MemberCache[type] = map;
                return map;
            }
        }

        private static void AddMember(Dictionary<string, Member> map, MemberInfo info, Type type)
        {
            if (info.GetCustomAttribute<JsonIgnoreAttribute>() != null) return;
            string name = info.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? info.Name;
            Type single = null;
            var conv = info.GetCustomAttribute<JsonConverterAttribute>();
            if (conv != null && conv.ConverterType.IsGenericType && conv.ConverterType.GetGenericTypeDefinition() == typeof(SingleOrListConverter<>))
                single = conv.ConverterType.GetGenericArguments()[0];
            map[name] = new Member { Name = name, Type = type, SingleOrList = single };
        }

        private static Member IgnoringCase(Dictionary<string, Member> members, string name)
        {
            foreach (var m in members.Values)
                if (string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase)) return m;
            return null;
        }

        /// <summary>
        /// "did you mean thrustVac? the value is ignored", or only the second part when no field is close: the nearest
        /// field by edit distance to the name or to the field's start ("trust" is one edit from "thrust", in thrustVac).
        /// </summary>
        private static string Suggest(Dictionary<string, Member> members, string name)
        {
            string best = null, b = name.ToLowerInvariant();
            int bestStart = int.MaxValue, bestWhole = int.MaxValue;
            foreach (var m in members.Keys)
            {
                string a = m.ToLowerInvariant();
                int whole = Distance(a, b), start = whole;
                for (int k = Math.Max(1, b.Length - 1); k <= Math.Min(a.Length, b.Length + 1); k++)
                    start = Math.Min(start, Distance(a.Substring(0, k), b));
                if (start < bestStart || start == bestStart && whole < bestWhole)
                {
                    best = m;
                    bestStart = start;
                    bestWhole = whole;
                }
            }
            bool close = best != null && bestStart <= Math.Max(1, b.Length / 4);
            return close ? $"did you mean {best}? the value is ignored" : "the value is ignored";
        }

        private static int Distance(string a, string b)
        {
            var d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            return d[a.Length, b.Length];
        }
    }
}
