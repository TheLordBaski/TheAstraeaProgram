using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Text for the player (FND-10). The English text in the code is the key: a language is a JSON file in <c>Resources/Strings</c>
    /// (<c>{ "name": "Deutsch", "culture": "de-DE", "strings": { "Pitch down": "Nach unten" } }</c>), so adding one needs no code
    /// change. Every text on screen goes through <see cref="Translate"/> (the <c>Localizer</c> in TAP.UI does it for all UI
    /// text), with numbers masked: "Surface 16.9 m/s" looks up "Surface {0} m/s", and a translation may move the {0}.
    /// The pseudo-locale (<c>pseudo</c>) turns any text into accented letters, 40% longer, in brackets, to show text that is cut
    /// off and text that never reaches the translation.
    /// </summary>
    public static class Loc
    {
        public const string English = "en", Pseudo = "pseudo";

        /// <summary>Open for the player: the language codes and names found in Resources/Strings, English first, then the pseudo-locale.</summary>
        public static readonly List<(string code, string name)> Languages = new List<(string, string)>();

        private sealed class Table
        {
            public string Name = "English", Culture = "en-US";
            public Dictionary<string, string> Strings = new Dictionary<string, string>();
        }

        private static readonly Dictionary<string, Table> Tables = new Dictionary<string, Table>();
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
        private static Table _english;
        private static string _loaded;

        /// <summary>Record the texts that are not in the English table (a test run, <see cref="Missing"/>).</summary>
        public static bool Audit;
        /// <summary>Texts seen on screen whose pattern is not in the English table.</summary>
        public static readonly HashSet<string> Missing = new HashSet<string>();
        /// <summary>Every pattern of text the player was shown, for building the English table.</summary>
        public static readonly HashSet<string> Seen = new HashSet<string>();
        /// <summary>Raised when the language changes, so screens redo their text.</summary>
        public static int Version { get; private set; }

        public static string Language => GameSettings.Current.gameplay.language;

        /// <summary>The languages the player can choose: English, the pseudo-locale, and every file in Resources/Strings.</summary>
        public static IReadOnlyList<(string code, string name)> AllLanguages { get { Ensure(); return Languages; } }
        /// <summary>Text must be passed through <see cref="Translate"/>: another language, or an audit.</summary>
        public static bool Active => Audit || Language != English || Imperial;

        /// <summary>The player reads feet, miles and pounds: numbers in front of a unit are converted as the text is shown.</summary>
        public static bool Imperial => GameSettings.Current.gameplay.units == UnitSystem.Imperial;

        public static CultureInfo Culture
        {
            get
            {
                Ensure();
                var t = Current();
                try { return CultureInfo.GetCultureInfo(t?.Culture ?? "en-US"); } catch (CultureNotFoundException) { return CultureInfo.GetCultureInfo("en-US"); }
            }
        }

        // ------------------------------------------------------------------ tables

        /// <summary>Language and units: when either changes, what was translated has to be done again.</summary>
        public static string Signature => Language + (Imperial ? "+imperial" : "");

        private static void Ensure()
        {
            string key = Signature;
            if (_english != null && _loaded == key) return;
            Load(key);
        }

        /// <summary>Reads the tables again (a language was chosen, or a file was edited).</summary>
        public static void Reload()
        {
            _english = null;
            Tables.Clear();
            Cache.Clear();
            Load(Signature);
            Version++;
        }

        private static void Load(string signature)
        {
            _loaded = signature;
            Cache.Clear();
            if (_english == null)
            {
                Languages.Clear();
                Languages.Add((English, "English"));
                Languages.Add((Pseudo, "Pseudo-locale (for testing)"));
                _english = Read(English) ?? new Table();
                Tables[English] = _english;
                foreach (var asset in Resources.LoadAll<TextAsset>("Strings"))
                {
                    if (asset.name == English) continue;
                    var t = Parse(asset.text, asset.name);
                    if (t == null) continue;
                    Tables[asset.name] = t;
                    Languages.Add((asset.name, t.Name));
                }
            }
        }

        /// <summary>Adds a language from its JSON text, as if its file were in Resources/Strings (tools and tests).</summary>
        public static bool LoadTable(string code, string json)
        {
            Ensure();
            var t = Parse(json, code);
            if (t == null) return false;
            Tables[code] = t;
            Languages.RemoveAll(l => l.code == code);
            Languages.Add((code, t.Name));
            Cache.Clear();
            Version++;
            return true;
        }

        private static Table Read(string code)
        {
            var asset = Resources.Load<TextAsset>("Strings/" + code);
            return asset == null ? null : Parse(asset.text, code);
        }

        private static Table Parse(string json, string code)
        {
            try
            {
                var raw = SaveStorage.FromJson<Dictionary<string, object>>(json);
                var t = new Table { Name = raw.TryGetValue("name", out var n) ? n as string ?? code : code, Culture = raw.TryGetValue("culture", out var c) ? c as string ?? "en-US" : "en-US" };
                if (raw.TryGetValue("strings", out var s) && s is Newtonsoft.Json.Linq.JObject obj)
                    foreach (var p in obj.Properties()) t.Strings[p.Name] = (string)p.Value;
                return t;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"The language file Strings/{code}.json could not be read: {e.Message}");
                return null;
            }
        }

        private static Table Current()
        {
            string lang = Language;
            return Tables.TryGetValue(lang, out var t) ? t : _english;
        }

        /// <summary>Strings of the English table (the texts the game knows), for tools and tests.</summary>
        public static IReadOnlyDictionary<string, string> EnglishStrings { get { Ensure(); return _english.Strings; } }

        // ------------------------------------------------------------------ translating

        // Markers around text that comes from the player or the data (a craft name), kept out of the pattern.
        private const char NameOpen = '', NameClose = '';

        /// <summary>Data text that has its own translation (a part title): translated now, and not translated again with the line it is in.</summary>
        public static string Data(string english) => Active ? Name(Translate(english)) : english;

        /// <summary>Wraps text that is the player's or the data's own (a vessel name): it is not translated, and not part of the pattern.</summary>
        public static string Name(string text) => Active ? NameOpen + text + NameClose : text; // the markers are taken out by the translation: with none running, they would show

        // A number: not part of a word or a code ("F1", "D001", "LV-215"), with its sign, grouping and decimals.
        private static readonly Regex NumberRx = new Regex(@"(?<![A-Za-z0-9#_.])(?<![A-Za-z]-)-?\d[\d,]*(?:\.\d+)?(?![0-9])", RegexOptions.Compiled);
        private static readonly Regex NameRx = new Regex("\uE000[^\uE001]*\uE001", RegexOptions.Compiled);
        private static readonly Regex TagRx = new Regex(@"<[^<>]*>", RegexOptions.Compiled);
        private static readonly Regex HolesRx = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);

        /// <summary>The text for the player: translated, or in the pseudo-locale, or as it is. Safe to call every frame.</summary>
        public static string Translate(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            Ensure();
            if (!Cache.TryGetValue(text, out var result))
            {
                result = Convert(text);
                if (Cache.Count > 20000) Cache.Clear();
                Cache[text] = result;
            }
            return result;
        }

        /// <summary>The same as <see cref="Translate"/>, for code that sets text itself.</summary>
        public static string T(string english) => Translate(english);

        /// <summary>
        /// The pattern a text is looked up by: names and numbers cut out and replaced by {0}, {1}…, tags kept. The values go to
        /// <paramref name="args"/>.
        /// </summary>
        public static string PatternOf(string text, List<string> args = null)
        {
            args = args ?? new List<string>();
            // A long line is prose with fixed numbers in it ("about 3,400 m/s"): they stay in the text. A short one is a readout.
            bool maskNumbers = NameRx.Replace(TagRx.Replace(text, ""), "{}").Length <= MaskLimit;
            var sb = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (ch == NameOpen)
                {
                    int end = text.IndexOf(NameClose, i + 1);
                    if (end < 0) end = text.Length - 1;
                    sb.Append('{').Append(args.Count).Append('}');
                    args.Add(text.Substring(i + 1, end - i - 1));
                    i = end + 1;
                    continue;
                }
                if (ch == '<')
                {
                    var m = TagRx.Match(text, i);
                    if (m.Success && m.Index == i) { sb.Append(m.Value); i += m.Length; continue; }
                }
                int next = text.IndexOfAny(new[] { NameOpen, '<' }, i + 1);
                string chunk = next < 0 ? text.Substring(i) : text.Substring(i, next - i);
                i += chunk.Length;
                sb.Append(maskNumbers ? NumberRx.Replace(chunk, mm => { args.Add(mm.Value); return "{" + (args.Count - 1) + "}"; }) : chunk);
            }
            return sb.ToString();
        }

        /// <summary>Lines longer than this (without tags) keep their numbers in the pattern.</summary>
        public const int MaskLimit = 90;

        /// <summary>A text of several lines is translated line by line, so a panel's static lines match whatever its values are.</summary>
        private static string Convert(string text)
        {
            if (text.IndexOf('\r') >= 0) text = text.Replace("\r\n", "\n").Replace('\r', '\n'); // AppendLine on Windows
            if (text.IndexOf('\n') < 0) return ConvertLine(text);
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = ConvertLine(lines[i]);
            return string.Join("\n", lines);
        }

        private static string ConvertLine(string text)
        {
            if (text.Length == 0) return text;
            var args = new List<string>();
            string pattern = PatternOf(text, args);
            // only tags, holes and symbols: nothing to translate
            bool hasLetters = false;
            foreach (char c in HolesRx.Replace(TagRx.Replace(pattern, ""), "")) if (char.IsLetter(c)) { hasLetters = true; break; }
            if (!hasLetters) return TextWithout(text);

            string shown;
            if (Language == Pseudo) shown = PseudoText(pattern);
            else
            {
                var table = Current();
                bool hit = table.Strings.TryGetValue(pattern, out shown);
                if (!hit || string.IsNullOrEmpty(shown)) shown = pattern;
            }
            if (Audit || Language != English)
            {
                // The English table is the list of texts the game knows; the rest are texts that never reached it.
                Seen.Add(pattern);
                if (!_english.Strings.ContainsKey(pattern)) Missing.Add(pattern);
            }
            if (Imperial) ToImperial(ref shown, args);
            return Fill(shown, args);
        }

        /// <summary>The text with its name markers removed (for tests, and for text with nothing to translate).</summary>
        public static string Plain(string text) => text.IndexOf(NameOpen) < 0 ? text : text.Replace(NameOpen.ToString(), "").Replace(NameClose.ToString(), "");

        private static string TextWithout(string text) => Plain(text);

        private static string Fill(string pattern, List<string> args) =>
            args.Count == 0 ? pattern : HolesRx.Replace(pattern, m => int.TryParse(m.Groups[1].Value, out int k) && k < args.Count ? args[k] : m.Value);

        // ------------------------------------------------------------------ units

        // A number, one space, a metric unit that stands alone ("80 m", "16.9 m/s", "36,000 kg"); "5m" (minutes) and "5 minutes" are not.
        private static readonly Regex UnitRx = new Regex(@"\{(\d+)\} (km/s|m/s²|m/s|km|kg|kPa|m|t)(?![\p{L}/²])", RegexOptions.Compiled);

        private static void ToImperial(ref string pattern, List<string> args)
        {
            pattern = UnitRx.Replace(pattern, m =>
            {
                int k = int.Parse(m.Groups[1].Value);
                if (k >= args.Count || !double.TryParse(args[k].Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) return m.Value;
                string unit; double factor;
                switch (m.Groups[2].Value)
                {
                    case "m": unit = "ft"; factor = 3.280840; break;
                    case "km": unit = "mi"; factor = 0.621371; break;
                    case "m/s": unit = "ft/s"; factor = 3.280840; break;
                    case "m/s²": unit = "ft/s²"; factor = 3.280840; break;
                    case "km/s": unit = "mi/s"; factor = 0.621371; break;
                    case "kg": unit = "lb"; factor = 2.204623; break;
                    case "t": unit = "tn"; factor = 1.102311; break;
                    default: unit = "psi"; factor = 0.145038; break; // kPa
                }
                double converted = v * factor;
                int sourceDecimals = args[k].Contains(".") ? args[k].Length - args[k].IndexOf('.') - 1 : 0;
                double a = Math.Abs(converted);
                int decimals = Math.Min(sourceDecimals + 1, a >= 100 ? 0 : a >= 10 ? 1 : 2);
                args[k] = converted.ToString(args[k].Contains(",") || a >= 10000 ? "N" + decimals : "F" + decimals, CultureInfo.InvariantCulture);
                return "{" + k + "} " + unit;
            });
        }

        // ------------------------------------------------------------------ the pseudo-locale

        private const string Letters = "AaBbCcDdEeGgIiLlNnOoSsTtUuYyZz";
        private const string Fancy = "ÅåÞþÇçĐđÉéĜĝÎîĹĺÑñÖöŠšŢţÛûÝýŽž";

        /// <summary>Accented letters (tags and {0} holes left alone), about 40% longer, in brackets: "[Ĝöĺĩåţĥ ñåmé ~~~~]".</summary>
        public static string PseudoText(string pattern)
        {
            var sb = new StringBuilder("[");
            int letters = 0;
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (c == '<')
                {
                    int end = pattern.IndexOf('>', i);
                    if (end > i) { sb.Append(pattern, i, end - i + 1); i = end; continue; }
                }
                if (c == '{')
                {
                    int end = pattern.IndexOf('}', i);
                    if (end > i) { sb.Append(pattern, i, end - i + 1); i = end; continue; }
                }
                int k = Letters.IndexOf(c);
                if (k >= 0) { sb.Append(Fancy[k]); letters++; }
                else { sb.Append(c); if (char.IsLetter(c)) letters++; }
            }
            // Padding after the last tag-free character would sit inside a tag pair; it goes at the very end.
            int pad = Mathf.Max(1, Mathf.CeilToInt(letters * 0.4f));
            sb.Append(' ').Append('~', pad).Append(']');
            return sb.ToString();
        }

        // ------------------------------------------------------------------ formatting

        /// <summary>A number with the language's grouping and decimal sign.</summary>
        public static string Number(double v, int decimals = 0) => v.ToString("N" + decimals, Culture);

        /// <summary>"1 vessel", "3 vessels". The form is chosen by the English rule; the player's language translates both patterns ("{0} vessel", "{0} vessels").</summary>
        public static string Plural(int count, string one, string other) => $"{count} {(count == 1 ? one : other)}";

        public static string Date(DateTime when) => when.ToString("d", Culture);
    }
}
