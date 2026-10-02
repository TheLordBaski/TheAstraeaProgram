using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using TAP.Core;

namespace TAP.EditorTools
{
    /// <summary>
    /// Authoring errors checked before compiling a preview or writing a system file: the rules the game checks when it
    /// loads a system (<see cref="SystemValidation"/>), every error at once.
    /// </summary>
    public static class BodyLabValidation
    {
        public static void CheckSystem(JObject system) => Throw(r => SystemValidation.CheckGraph(system, r));

        /// <summary>One body with its terrain presets resolved.</summary>
        public static void CheckBody(JObject json) => Throw(r =>
            SystemValidation.CheckBody(new ContentEntry(r, null, "body " + ((string)json["id"] ?? "(no id)"), json), json.ToObject<BodyDefinition>()));

        private static void Throw(Action<ContentReport> check)
        {
            var report = new ContentReport();
            check(report);
            if (report.HasErrors) throw new FormatException(string.Join("\n", report.Errors.Select(p => p.ToString())));
        }
    }
}
