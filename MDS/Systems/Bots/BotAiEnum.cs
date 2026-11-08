namespace MDS.Systems
{
    // Identifies a bot AI. Resolved to an IBotAi instance via BotAiFactory.
    public enum BotAiEnum
    {
        None,          // Does nothing - stands in place.
        Manual,        // Manually driven via 'rc bot move' - test harness for movement behaviors.
        StabbingDummy, // Static training dummy: stands facing one way and stabs on a cadence (practice target).
        RiposteDummy,  // Melee, reactive: stands its ground, blocks, and only counters once provoked.
        SparringEasy,   // Sparring difficulty: slow reactions - beatable.
        SparringNormal, // Sparring difficulty: human reactions.
        Sparring,       // Sparring difficulty: best reactions + fastest pacing (hardest). Base Sparring = passive until attacked, then fights the attacker to the death.
        Dueling,       // Sparring's reactions, and where the offensive mechanics go: feints, spins, held timings, gambles. Identical to Sparring until the first of those lands.
        Feinting,      // Feint sandbox: Dueling with holding off, so feint timings can be measured on their own. Not a difficulty.
        Guardian,      // Escorts the player it was summoned onto and fights whatever threatens them.
        GroupEasy,     // Group difficulty: slow reactions - beatable.
        GroupNormal,   // Group difficulty: human reactions.
        Group,         // Group difficulty: best reactions + fastest pacing. Throws perfect updowns, which cannot be blocked. Drill station: bots summoned together wait where they were set up, all wake when any one is stabbed, back off to re-form, then fight as a formation and return to the post afterwards.
        GroupHard,     // Group at full reactions, but opposite stabs are held at least stabSeparation apart, so an updown can always be blocked.
        Test           // Development sandbox: Sparring's levers plus whatever behaviour is being worked on.
        // Sparring*/Group*/RiposteDummy are MeleeAi presets (capability-toggle bundles); StabbingDummy is the MeleeDummy class. See MeleeAi.
    }
}
