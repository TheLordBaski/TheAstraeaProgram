using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TAP.Core
{
    /// <summary>
    /// One entry of a content file (a part, a body, the launch site) while it is checked. Problems name the file, the
    /// entry and the field, and take their line and "found" text from the entry's JSON. A field gets one problem: the
    /// first rule it breaks.
    /// </summary>
    public sealed class ContentEntry
    {
        /// <summary>Where an entry's JSON comes from: a file, its JSON, and the field path that JSON stands for.</summary>
        public readonly struct Source
        {
            public readonly string File;
            public readonly JToken Json;
            /// <summary>The entry field this JSON is: "" for the entry itself, "terrain" for a terrain preset.</summary>
            public readonly string Path;

            public Source(string file, JToken json, string path = "")
            {
                File = file;
                Json = json;
                Path = path ?? "";
            }
        }

        public readonly ContentReport Report;
        public readonly string Name;
        /// <summary>The JSON the rules check (for a body, with its terrain presets resolved).</summary>
        public readonly JToken Json;
        private readonly List<Source> _sources = new List<Source>();
        private readonly HashSet<string> _fields = new HashSet<string>();

        public int ErrorCount { get; private set; }
        public bool HasErrors => ErrorCount > 0;
        /// <summary>The entry's first error, for saying why it can't be used.</summary>
        public ContentProblem FirstError { get; private set; }

        /// <param name="file">The file the entry is in (null when it isn't from a file).</param>
        /// <param name="name">"part eng_hornet", "body luma".</param>
        /// <param name="json">The entry's JSON (with line numbers when parsed by <see cref="ContentJson.Parse"/>).</param>
        public ContentEntry(ContentReport report, string file, string name, JToken json)
            : this(report, name, json, new Source(file, json)) { }

        /// <summary>
        /// An entry whose JSON was put together from several files (a body and the terrain presets it is built on). A
        /// problem points at the first source, in order, that holds the field, so at the file and line to edit.
        /// </summary>
        public ContentEntry(ContentReport report, string name, JToken json, params Source[] sources)
        {
            Report = report;
            Name = name;
            Json = json;
            _sources.AddRange(sources);
        }

        public void Error(string field, string expected, string found = null, string hint = null) =>
            Add(ContentSeverity.Error, field, expected, found, hint);

        public void Warning(string field, string expected, string found = null, string hint = null) =>
            Add(ContentSeverity.Warning, field, expected, found, hint);

        /// <summary>Whether this field, or a value inside it, already has a problem (or the whole entry has one).</summary>
        public bool Reported(string field)
        {
            field ??= "";
            if (_fields.Contains("")) return true;
            foreach (var f in _fields)
                if (f == field || f.StartsWith(field + ".", StringComparison.Ordinal) || f.StartsWith(field + "[", StringComparison.Ordinal))
                    return true;
            return false;
        }

        private void Add(ContentSeverity severity, string field, string expected, string found, string hint)
        {
            if (Reported(field)) return;
            _fields.Add(field ?? "");
            Locate(field, out string file, out JToken where, out bool exact);
            var p = new ContentProblem
            {
                Severity = severity,
                File = file,
                Line = LineOf(where),
                Entry = Name,
                Field = string.IsNullOrEmpty(field) ? null : field,
                Expected = expected,
                Found = found ?? (exact ? Describe(where) : "nothing"),
                Hint = hint,
            };
            // The report keeps one copy of a problem found twice (a body's own JSON and its resolved terrain); the entry
            // still counts it as its own.
            Report.Add(p);
            if (p.IsError)
            {
                ErrorCount++;
                FirstError ??= p;
            }
        }

        /// <summary>The value at a field path ("engine.ispVac", "nodes[2].pos"), or null when it is missing.</summary>
        public JToken At(string field)
        {
            if (Json == null) return null;
            if (string.IsNullOrEmpty(field)) return Json;
            try { return Json.SelectToken(field); }
            catch (JsonException) { return null; }
        }

        /// <summary>Whether the field is in the JSON (with any value, null included).</summary>
        public bool Has(string field) => At(field) != null;

        /// <summary>
        /// Finds the file and JSON that hold a field, or the nearest value enclosing it when the field is missing: the
        /// longest path first, sources in order, skipping a source whose value an earlier one replaces.
        /// </summary>
        private void Locate(string field, out string file, out JToken where, out bool exact)
        {
            field ??= "";
            exact = true;
            for (string path = field; ; path = Parent(path))
            {
                for (int k = 0; k < _sources.Count; k++)
                {
                    var t = Find(_sources[k], path);
                    if (t == null || Replaced(k, path)) continue;
                    file = _sources[k].File;
                    where = t;
                    return;
                }
                exact = false;
                if (path.Length == 0) break;
            }
            file = _sources.Count > 0 ? _sources[0].File : null;
            where = null;
        }

        /// <summary>A source's JSON at an entry field path, or null when the source doesn't have it.</summary>
        private static JToken Find(Source s, string path)
        {
            string sub;
            if (s.Path.Length == 0) sub = path;
            else if (path == s.Path) sub = "";
            else if (path.StartsWith(s.Path + ".", StringComparison.Ordinal)) sub = path.Substring(s.Path.Length + 1);
            else if (path.StartsWith(s.Path + "[", StringComparison.Ordinal)) sub = path.Substring(s.Path.Length);
            else return null;
            try { return sub.Length == 0 ? s.Json : s.Json?.SelectToken(sub); }
            catch (JsonException) { return null; }
        }

        /// <summary>
        /// Whether an earlier source replaces the value at this path: it has a list or a single value at the path or
        /// above it (objects merge, lists and values replace), so the later source's value isn't the one used.
        /// </summary>
        private bool Replaced(int k, string path)
        {
            for (int j = 0; j < k; j++)
                for (string p = path; ; p = Parent(p))
                {
                    var t = Find(_sources[j], p);
                    if (t != null && !(t is JObject)) return true;
                    if (p.Length == 0) break;
                }
            return false;
        }

        /// <summary>"nodes[2].pos" → "nodes[2]" → "nodes" → "".</summary>
        public static string Parent(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path[path.Length - 1] == ']')
            {
                int b = path.LastIndexOf('[');
                return b > 0 ? path.Substring(0, b) : "";
            }
            int dot = path.LastIndexOf('.');
            return dot > 0 ? path.Substring(0, dot) : "";
        }

        public static int LineOf(JToken t)
        {
            for (; t != null; t = t.Parent)
                if (t is IJsonLineInfo li && li.HasLineInfo()) return li.LineNumber;
            return 0;
        }

        /// <summary>What a person sees in the file: nothing, null, "text", 215000, true, a list of 2 values, an object.</summary>
        public static string Describe(JToken t)
        {
            if (t == null) return "nothing";
            switch (t.Type)
            {
                case JTokenType.Null: return "null";
                case JTokenType.String:
                    string s = (string)t;
                    return "\"" + (s.Length > 48 ? s.Substring(0, 48) + "…" : s) + "\"";
                case JTokenType.Boolean: return (bool)t ? "true" : "false";
                case JTokenType.Array:
                    int n = ((JArray)t).Count;
                    return n == 0 ? "an empty list" : "a list of " + ContentReport.Plural(n, "value");
                case JTokenType.Object: return ((JObject)t).Count == 0 ? "an empty object" : "an object";
                default: return t.ToString(Formatting.None);
            }
        }

        public static string Number(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);

        private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

        // ------------------------------------------------------------------ rules (each returns whether it passed)

        /// <summary>Reports an error (or warning) for the field unless the condition holds.</summary>
        public bool Check(bool ok, string field, string expected, bool error = true, string found = null, string hint = null)
        {
            if (!ok)
            {
                if (error) Error(field, expected, found, hint);
                else Warning(field, expected, found, hint);
            }
            return ok;
        }

        /// <param name="unit">What the number is in ("seconds", "metres"), said with what is expected.</param>
        public bool Positive(double v, string field, string unit = null) =>
            Check(IsFinite(v) && v > 0, field, WithUnit("a number above 0", unit));

        public bool NotNegative(double v, string field, string unit = null) =>
            Check(IsFinite(v) && v >= 0, field, WithUnit("a number of 0 or more", unit));

        public bool InRange(double v, double min, double max, string field, bool error = true, string unit = null) =>
            Check(IsFinite(v) && v >= min && v <= max, field, WithUnit($"a number from {Number(min)} to {Number(max)}", unit), error);

        private static string WithUnit(string expected, string unit) => string.IsNullOrEmpty(unit) ? expected : expected + " (" + unit + ")";

        public bool Text(string s, string field, string expected = "text") =>
            Check(!string.IsNullOrWhiteSpace(s), field, expected);

        public bool OneOf(string s, string field, IReadOnlyCollection<string> allowed, bool error = true, string hint = null)
        {
            foreach (var a in allowed) if (a == s) return true;
            return Check(false, field, "one of " + string.Join(", ", allowed), error, hint: hint);
        }

        /// <summary>A list of exactly <paramref name="length"/> numbers (not all zero, for a direction).</summary>
        public bool Vector(IReadOnlyList<double> v, int length, string field, bool direction = false, bool error = true)
        {
            string expected = $"a list of {length} numbers" + (direction ? ", not all 0" : "");
            bool ok = v != null && v.Count == length;
            if (ok && direction)
            {
                ok = false;
                foreach (var x in v) if (x != 0) ok = true;
            }
            if (ok) foreach (var x in v) if (!IsFinite(x)) ok = false;
            return Check(ok, field, expected, error);
        }

        public bool Vector(float[] v, int length, string field, bool direction = false, bool error = true) =>
            Vector(v == null ? null : Array.ConvertAll(v, x => (double)x), length, field, direction, error);

        /// <summary>Three numbers from 0 to 1: red, green and blue.</summary>
        public bool Color(float[] c, string field, bool error = true)
        {
            bool ok = c != null && c.Length == 3;
            if (ok) foreach (var x in c) if (!(x >= 0 && x <= 1)) ok = false;
            return Check(ok, field, "3 numbers from 0 to 1 (red, green, blue)", error);
        }
    }
}
