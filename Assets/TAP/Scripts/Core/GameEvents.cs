using System;
using System.Collections.Generic;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>What can happen in a game that something else may want to know about (FND-14).</summary>
    public enum GameEventKind
    {
        /// <summary>A vessel left the pad (its launch time was set).</summary>
        Launched,
        /// <summary>A stage was fired. Detail: the stage number.</summary>
        Staged,
        /// <summary>A vessel climbed above its body's atmosphere (70 km where there is none).</summary>
        ReachedSpace,
        /// <summary>A vessel went into orbit.</summary>
        Orbit,
        /// <summary>A vessel crossed into another body's sphere of influence. Detail: the body it left.</summary>
        SoiChanged,
        Landed,
        Splashed,
        /// <summary>A crew member stepped out. Crew: the name.</summary>
        EvaStarted,
        /// <summary>A crew member boarded a vessel. Crew: the name.</summary>
        EvaEnded,
        FlagPlanted,
        Docked,
        /// <summary>A vessel with crew was recovered.</summary>
        CrewRecovered,
        /// <summary>Any vessel was recovered.</summary>
        VesselRecovered,
        /// <summary>Science came in. Raised by the science tasks (SCI); nothing raises it yet.</summary>
        ScienceCollected,
        /// <summary>A part broke up or burned. Detail: the part's title, then why.</summary>
        PartDestroyed,
        /// <summary>A crew member died. Crew: the name. Detail: why.</summary>
        CrewLost,
        /// <summary>A crewed vessel came out of a fast, hot fall through the air.</summary>
        ReentrySurvived,
    }

    public sealed class GameEvent
    {
        public GameEventKind Kind;
        /// <summary>Universal time it happened.</summary>
        public double UT;
        public string VesselId, VesselName;
        /// <summary>The body (its id) the vessel was at, where that means something.</summary>
        public string BodyId;
        public string Crew, Detail;

        public override string ToString() => $"{Kind} {VesselName} {BodyId} {Detail}".Trim();
    }

    /// <summary>
    /// The game's event bus (FND-14): the simulation and the scenes raise events as things happen; the objectives, and later
    /// contracts, achievements, progression and tutorials, listen. Static, so nothing has to be handed around; listeners remove
    /// themselves when their scene ends.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GameEvent> Raised;

        /// <summary>The last events, oldest first (for the developer tools and for tests).</summary>
        public static readonly List<GameEvent> Recent = new List<GameEvent>();
        private const int KeepRecent = 100;

        public static void Raise(GameEvent e)
        {
            Recent.Add(e);
            if (Recent.Count > KeepRecent) Recent.RemoveAt(0);
            var handlers = Raised;
            if (handlers == null) return;
            // One listener's failure must not keep the others from hearing it.
            foreach (Action<GameEvent> h in handlers.GetInvocationList())
            {
                try { h(e); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        public static void Raise(GameEventKind kind, double ut, string vesselId = null, string vesselName = null, string bodyId = null, string crew = null, string detail = null)
            => Raise(new GameEvent { Kind = kind, UT = ut, VesselId = vesselId, VesselName = vesselName, BodyId = bodyId, Crew = crew, Detail = detail });

        public static void Clear() => Recent.Clear();
    }
}
