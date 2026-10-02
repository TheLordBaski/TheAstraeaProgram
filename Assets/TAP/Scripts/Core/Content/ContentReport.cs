using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TAP.Core
{
    public enum ContentSeverity { Error, Warning }

    /// <summary>
    /// One problem in a content file, said the way a person fixes it: the file and line, the entry (a part, a body), the
    /// field, what was expected and what was found. An error makes the entry unusable; a warning is worth a look.
    /// </summary>
    public sealed class ContentProblem
    {
        public ContentSeverity Severity;
        /// <summary>The file under Resources: "Data/parts.json", "Data/Terrain/luma.json". Null when not from a file.</summary>
        public string File;
        /// <summary>Line of the field, or of the nearest enclosing value when the field is missing; 0 when unknown.</summary>
        public int Line;
        /// <summary>"part eng_hornet", "body luma"; null for the file as a whole.</summary>
        public string Entry;
        /// <summary>"engine.ispVac", "orbit"; null for the entry as a whole.</summary>
        public string Field;
        public string Expected;
        public string Found;
        /// <summary>What it means or how to fix it: "did you mean thrustVac?".</summary>
        public string Hint;

        public bool IsError => Severity == ContentSeverity.Error;

        /// <summary>"Data/parts.json line 175".</summary>
        public string Place => string.IsNullOrEmpty(File) ? null : Line > 0 ? File + " line " + Line : File;

        /// <summary>"engine.ispVac: expected a number above 0, found nothing." (without the file and the entry).</summary>
        public string What
        {
            get
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(Field)) sb.Append(Field).Append(": ");
                sb.Append("expected ").Append(Expected).Append(", found ").Append(Found);
                if (!string.IsNullOrEmpty(Hint)) sb.Append(" (").Append(Hint).Append(')');
                return sb.Append('.').ToString();
            }
        }

        /// <summary>"Data/parts.json line 175: part eng_hornet, engine.ispVac: expected a number above 0, found nothing."</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Place != null) sb.Append(Place).Append(": ");
            if (!string.IsNullOrEmpty(Entry)) sb.Append(Entry).Append(string.IsNullOrEmpty(Field) ? ": " : ", ");
            return sb.Append(What).ToString();
        }
    }

    /// <summary>The problems found in content, in the order found. The same problem added twice is kept once.</summary>
    public sealed class ContentReport
    {
        private readonly List<ContentProblem> _problems = new List<ContentProblem>();
        private readonly HashSet<string> _keys = new HashSet<string>();

        public IReadOnlyList<ContentProblem> Problems => _problems;
        public int ErrorCount { get; private set; }
        public int WarningCount => _problems.Count - ErrorCount;
        public bool HasErrors => ErrorCount > 0;
        public bool IsClean => _problems.Count == 0;

        /// <summary>Adds a problem; false when the same one is already in the report.</summary>
        public bool Add(ContentProblem p)
        {
            if (!_keys.Add(p.Severity + " " + p)) return false;
            _problems.Add(p);
            if (p.IsError) ErrorCount++;
            return true;
        }

        public void Add(ContentReport other)
        {
            foreach (var p in other._problems) Add(p);
        }

        public IEnumerable<ContentProblem> Errors
        {
            get { foreach (var p in _problems) if (p.IsError) yield return p; }
        }

        public IEnumerable<ContentProblem> Warnings
        {
            get { foreach (var p in _problems) if (!p.IsError) yield return p; }
        }

        /// <summary>"1 error and 2 warnings", "no problems".</summary>
        public string Counts
        {
            get
            {
                if (IsClean) return "no problems";
                if (WarningCount == 0) return Plural(ErrorCount, "error");
                if (ErrorCount == 0) return Plural(WarningCount, "warning");
                return Plural(ErrorCount, "error") + " and " + Plural(WarningCount, "warning");
            }
        }

        public static string Plural(int n, string word) => n == 1 ? "1 " + word : n + " " + word + "s";

        /// <summary>Every problem on its own line, errors first.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var p in Errors) sb.Append("Error: ").Append(p).Append('\n');
            foreach (var p in Warnings) sb.Append("Warning: ").Append(p).Append('\n');
            return sb.ToString().TrimEnd('\n');
        }
    }

    /// <summary>Content that can't be used: the message lists every error.</summary>
    public sealed class ContentException : FormatException
    {
        public readonly ContentReport Report;

        public ContentException(string what, ContentReport report)
            : base(what + " (" + report.Counts + "):\n" + report)
        {
            Report = report;
        }
    }

    /// <summary>
    /// The content problems found while loading data this session. Each load logs what it found once: one console
    /// message for a file's errors (saying what they cost), one for its warnings. The main menu lists them.
    /// </summary>
    public static class ContentLog
    {
        public static ContentReport Session { get; private set; } = new ContentReport();

        /// <summary>Raised when a load added problems not seen before.</summary>
        public static event Action Changed;

        /// <summary>Forgets the problems found so far, so they are logged again when found again (tests).</summary>
        public static void Clear() => Session = new ContentReport();

        /// <summary>
        /// Keeps a load's problems for the session and logs the new ones. <paramref name="consequence"/> says what the
        /// errors cost, e.g. "parts with errors are left out until they are fixed".
        /// </summary>
        public static void Add(ContentReport report, string consequence)
        {
            var errors = new List<ContentProblem>();
            var warnings = new List<ContentProblem>();
            foreach (var p in report.Problems)
                if (Session.Add(p)) (p.IsError ? errors : warnings).Add(p);
            if (errors.Count > 0) Debug.LogError(Message("Content error", errors, consequence));
            if (warnings.Count > 0) Debug.LogWarning(Message("Content warning", warnings, null));
            if (errors.Count + warnings.Count > 0) Changed?.Invoke();
        }

        /// <summary>One problem on the first line; several as a count followed by the list.</summary>
        public static string Message(string kind, IReadOnlyList<ContentProblem> problems, string consequence)
        {
            string tail = string.IsNullOrEmpty(consequence) ? "" : " " + char.ToUpperInvariant(consequence[0]) + consequence.Substring(1) + ".";
            if (problems.Count == 1) return kind + ": " + problems[0] + tail;
            var sb = new StringBuilder(kind).Append("s (").Append(problems.Count).Append(").").Append(tail);
            foreach (var p in problems) sb.Append("\n  ").Append(p);
            return sb.ToString();
        }
    }
}
