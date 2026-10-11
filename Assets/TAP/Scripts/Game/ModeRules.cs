using TAP.Persistence;

namespace TAP.Game
{
    /// <summary>
    /// What a game mode allows (FND-06): the one place that answers whether the player may use a part, afford a cost,
    /// use a facility level, see contracts or hire crew. Sandbox allows everything. Science and Career restrict, and
    /// until the state they read exists (the tech tree, funds, facility levels) a restricted answer is no: a mode never
    /// opens up by accident. No screen starts a game in Science or Career yet.
    /// </summary>
    public sealed class ModeRules
    {
        public static readonly ModeRules Sandbox = new ModeRules(GameMode.Sandbox);
        public static readonly ModeRules Science = new ModeRules(GameMode.Science);
        public static readonly ModeRules Career = new ModeRules(GameMode.Career);

        public readonly GameMode Mode;

        private ModeRules(GameMode mode) { Mode = mode; }

        public static ModeRules For(GameMode mode) => mode == GameMode.Career ? Career : mode == GameMode.Science ? Science : Sandbox;

        /// <summary>The rules of the game being played (sandbox when none is loaded).</summary>
        public static ModeRules Current => For(GameSession.Save?.mode ?? GameMode.Sandbox);

        /// <summary>Science and Career: researched parts only. The tech state (TEC-02) will answer; nothing is researched yet.</summary>
        public bool PartAvailable(string partId) => Mode == GameMode.Sandbox;

        /// <summary>Career: costs funds. The funds (CAR-01) will answer; until then only what is free.</summary>
        public bool CanAfford(double cost) => Mode != GameMode.Career || cost <= 0;

        /// <summary>Career: facilities have levels. Their levels (CAR-03) will answer; until then level 1 only.</summary>
        public bool FacilityAllows(string facility, int level) => Mode != GameMode.Career || level <= 1;

        /// <summary>Whether contracts are offered: Career only.</summary>
        public bool ContractsOn => Mode == GameMode.Career;

        /// <summary>Career: hiring costs funds, so it waits for them (CAR-01).</summary>
        public bool CanHireCrew => Mode != GameMode.Career;
    }
}
