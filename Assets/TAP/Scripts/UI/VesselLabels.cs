using TAP.Persistence;

namespace TAP.UI
{
    /// <summary>Plain-language descriptions of saved vessels, for the lists that show them.</summary>
    public static class VesselLabels
    {
        public static string SituationText(Situation s)
        {
            switch (s)
            {
                case Situation.Prelaunch: return "On the pad at";
                case Situation.Landed: return "Landed on";
                case Situation.Splashed: return "Splashed down on";
                case Situation.Flying: return "Flying over";
                case Situation.SubOrbital: return "Sub-orbital around";
                case Situation.Orbiting: return "Orbiting";
                case Situation.Escaping: return "Escaping";
            }
            return s.ToString();
        }

        /// <summary>"On the pad at Tellus · 2 crew", for the second line of a vessel row.</summary>
        public static string Describe(VesselRecord v, string bodyName)
        {
            int crew = 0;
            foreach (var p in v.parts) crew += p.crew?.Count ?? 0;
            string kind = v.kind == VesselKind.EVA ? "EVA" : crew > 0 ? $"{crew} crew" : "uncrewed";
            return $"{SituationText(v.situation)} {bodyName ?? v.bodyId} · {kind}";
        }
    }
}
