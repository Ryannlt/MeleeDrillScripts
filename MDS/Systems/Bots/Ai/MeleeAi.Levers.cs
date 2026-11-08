using System.Collections.Generic;
using MDS.ConfigVariables;

// The lever half of MeleeAi: tunable state, per-preset defaults, and the IConfigurableAi plumbing.

namespace MDS.Systems
{
    public partial class MeleeAi
    {
        // Tuning levers (IConfigurableAi). Defaults per preset in DefaultLeversFor.
        private float _offensiveBase, _offensiveVar;   // close spacing to press and attack from (base plus jitter)
        private float _defensiveBase, _defensiveVar;   // further spacing to guard and read from (base plus jitter)
        private float _attackRange;                    // within this distance a stab can land, so we throw
        private float _attackReadBeat;                 // extra randomized beat on the attack cooldown
        private float _riposteReactionMin, _riposteReactionMax; // reaction beat between a block landing and the counter
        private float _riposteWindow;                  // how long the post-block counter stays available
        private float _blockReactionMin, _blockReactionMax;     // reaction beat between reading an attack and raising the guard (0 = instant)

        // The block reaction beat used in every posture except fighting: waiting, backing off, withdrawing.
        private float _passiveBlockReaction;

        // Capability toggles (levers).
        private bool _press;      // throw the first blow when the enemy isn't threatening
        private bool _riposte;    // counter after our guard absorbs an attack
        private bool _move;       // drive melee spacing (false = stand our ground)
        private bool _pursue;     // advance toward a target that's too far (false = hold ground, let it walk away)
        private bool _engageOnAttack; // start passive (block only), only fight a player who attacks us, and only until they die

        // Targeting levers (see ResolveTarget).
        private float _targetRange;   // only acquire candidates within this range (<= 0 = unlimited)
        private bool _ignoreTeam;     // target any player regardless of faction (else enemies only)
        private bool _ignoreBots;     // target only human players (skip bots)
        private bool _stickyTarget;   // keep the current target while valid (else re-pick the closest each tick)
        private float _passiveRange;  // engageOnAttack: hold distance while waiting (engaged uses defensiveRange)

        // Guard levers: an escort holds station by its ward and only fights what threatens them.
        private bool _guard;              // act as an escort for the guard target, rather than ignoring it
        private float _guardRange;        // an enemy this close to the guarded player pulls the bot into the fight
        private float _guardFollowRange;  // distance the bot holds from the guarded player while nothing is happening
        private float _separationRange;   // push apart from other bots within this, 0 to disable

        // Squad levers. When enabled and another bot from the same spawn batch is present, SquadCoordinator hands
        // this bot a slot on the arc around the enemy and says whether its swing line is clear of squadmates.
        private bool _squad;

        // Imperfection levers: each is the WORST the bot may be, rolled from zero every time.
        private float _slotError;         // furthest it may stand from its place on the ring, metres
        private float _formationLag;      // longest it may work from a stale slot, seconds
        private float _stabSeparation;    // smallest gap between opposite stabs in one formation, seconds

        // Mid-swing safety. Two radii because the gate costs stabs while the clamp costs only tracking.
        private float _gateRadius;        // half-width used to refuse a stab outright, metres
        private float _clampRadius;       // half-width used to stop turning mid-stab, metres
        private float _bladeMargin;       // degrees of slack kept outside a mate's band rather than sitting on it
        private float _mateConeFloor;     // narrowest the danger cone may ever get, degrees, whatever the geometry says
        private float _mateCrowdRatio;    // a mate closer than this many spacings is in the way at any bearing, 0 = off
        private bool _gateOnMate;         // hold fire while a mate stands in the blade's band
        private bool _abortOnMate;        // block to cancel our own stab when the aim cannot be pulled clear

        // Chance the bot takes the direction the line assigned rather than picking its own, 0 to 1.
        private float _coordinate;

        // Vertical aim in the engine's own pitch scale, 0 level. Drives BladeBearing and BladeReach.
        private float _aimPitch;

        // Longest a chambered stab may be kept back waiting for the enemy to move. 0 = throw on the fixed beat.
        private float _holdMax;
        private float _holdMin;           // shortest a hold lasts once begun, so a guard has time to come up
        private float _feintCount;        // cancels before the stab is thrown for real. 0 = never feint
        private float _feintDepth;        // seconds into the chamber before it is cancelled
        private float _feintDwell;        // how long the cancelling block is held before it is released

        private float _squadSpacing;      // closest the line ever stands, the floor its breathing works up from
        private float _squadSpacingVar;   // how much wider than that it may drift during a fight, 0 = a fixed gap
        private float _laneHalfWidth;     // how close a squadmate may be to the swing line before it is blocked
        private float _squadStandoff;     // range the formation's point holds from the enemy

        // Station levers. Independent of squad: one bot can hold a post and return to it without ever forming up
        // with anybody, which is what makes these useful on a plain duellist.
        private bool _post;               // wait at the post until provoked, and return to it afterwards
        private bool _breakoff;           // once provoked, re-establish range before throwing anything
        private float _breakoffRange;     // furthest the group gives ground when breaking off, from where it was provoked
        private float _engageDelay;       // seconds after the first provocation before the group may swing
        private float _resetRange;        // how far the target may get from the post before disengaging (0 = no limit)
        private int _minMembers;          // fewest members it will fight with; below this it breaks off and stays shut
        private bool _holdReplacement;    // a dead member's replacement waits for the bout to end before spawning
        private float _returnDelay;       // seconds it lingers where the bout ended before walking back to the post

        private int? _guardTargetId;      // the friendly being escorted, from the summon or the guardTarget lever

        // Built-in lever defaults per preset. GlobalAiConfigurable seeds its globals from this.
        public static (string name, string value)[] DefaultLeversFor(BotAiEnum aiType)
        {
            var levers = new List<(string, string)>
            {
                ("offensiveRange", "0.7"), ("offensiveRangeVariance", "0.1"),
                ("defensiveRange", "2.0"), ("defensiveRangeVariance", "0.4"),
                ("attackRange", "2.0"), ("riposteWindow", "0.6"),
                ("ignoreTeam", "true"),  // target anyone by default; no need to be on the opposing faction to use a bot
                ("ignoreBots", "true"),  // only respond to human players by default
                ("passiveRange", "0.6"), // engageOnAttack waiting-mode hold distance
                ("guard", "false"),      // summons hand every bot a guard target; only escorts act on it
                ("guardTarget", "0"),    // no one to escort unless a summon or the lever names someone
                ("guardRange", "10"), ("guardFollowRange", "3"),
                ("separationRange", "0"), // off unless a preset wants bots to keep clear of each other
                ("squad", "false"),       // fight as a formation with the rest of the spawn batch
                ("coordinate", "0.5"),    // neutral: each bot picks its own swing, so updowns happen only by luck
                ("slotError", "0"), ("formationLag", "0"), // perfect placement and perfect tracking by default
                ("stabSeparation", "0"),  // no floor: a pair may throw opposite stabs on the same tick
                // Effective envelopes rather than shoulder widths, fitted to observed kills at one spacing.
                ("gateRadius", "0.3"), ("clampRadius", "0.4"), ("bladeMargin", "5"),

                // Floor under the danger cone, because asin(radius/dist) collapses as the line widens. Clamp only.
                ("mateConeFloor", "28"),

                // A mate closer than this many spacings is in the way at any bearing ahead. Clamp only, 0 = off.
                ("mateCrowdRatio", "1"),

                // Hold fire while a mate stands in the blade's band, rather than stabbing and hoping.
                ("gateOnMate", "true"),

                // Still off. Blocking does not cancel reliably, so it stays a last resort behind its own lever.
                ("abortOnMate", "false"),
                // The line breathes between squadSpacing and squadSpacing+Variance; 0.85 is what it can hold moving.
                ("aimPitch", "0"),   // level, and the only value known to mean the same to the command and the engine
                ("holdMax", "0"),    // no holding: chamber and throw, which is what every preset but Dueling does
                ("holdMin", "0.8"),  // inert while holdMax is 0, so turning holding on alone still behaves
                ("feintCount", "0"), // no feinting: chamber and throw, which is what every preset but Feinting does
                ("feintDepth", "0.2"), // seconds into the flying stab before the block kills it
                ("feintDwell", "0.1"),   // block held, then released; also the dial for slowing a bot down
                ("squadSpacing", "0.85"), ("squadSpacingVariance", "0.7"), ("laneHalfWidth", "0.5"),
                // The formation's point holds this range from the enemy, so a circling enemy leaves it alone while
                // one that closes or withdraws tows it along.
                ("squadStandoff", "1.5"),
                ("passiveBlockReaction", "0"), // waiting bots block instantly, so a walk-up stab can't end the drill early
                ("post", "false"),          // only drill stations wait to be provoked and return afterwards
                ("breakoff", "false"),      // and only some of those reset the distance before fighting
                // resetRange 0 = no distance limit: a bout ends when it is won or lost, not when someone steps
                // away from it. Tidying the arena afterwards is returnDelay's job, not this one's.
                ("breakoffRange", "2"), ("resetRange", "0"),
                // Off outside the drill stations. The Group tiers set it below.
                ("engageDelay", "0"),
                ("minMembers", "0"),          // 0 = fight on however few are left
                ("holdReplacement", "false"), // only a drill with a group size worth preserving holds one back
                ("returnDelay", "30"),        // hold where the bout ended long enough to be used again straight away
            };
            switch (aiType)
            {
                // Guardian: escorts whoever summoned it, and fights only what comes within guardRange of them.
                case BotAiEnum.Guardian:
                    levers.Add(("press", "true"));  levers.Add(("riposte", "true"));  levers.Add(("move", "true")); levers.Add(("pursue", "true"));
                    levers.Add(("targetRange", "0")); levers.Add(("stickyTarget", "false")); levers.Add(("engageOnAttack", "false"));
                    levers.Add(("ignoreTeam", "false"));
                    levers.Add(("guard", "true"));
                    levers.Add(("separationRange", "1.5"));
                    // Deliberately a step below SparringEasy: a guard buys its ward a moment, it does not win the duel.
                    levers.Add(("blockReactionMin", "0.5")); levers.Add(("blockReactionMax", "0.8"));
                    levers.Add(("riposteReactionMin", "0.4")); levers.Add(("riposteReactionMax", "1.1"));
                    levers.Add(("attackReadBeat", "1.2")); break;

                // Sparring and Dueling: passive until a player's attack is blocked, then fight that attacker to
                // the death. Sparring is the practice ladder; Dueling is the same reads with offence on top.
                case BotAiEnum.SparringEasy:
                case BotAiEnum.SparringNormal:
                case BotAiEnum.Sparring:
                case BotAiEnum.Dueling:
                case BotAiEnum.Feinting:
                case BotAiEnum.GroupEasy:
                case BotAiEnum.GroupNormal:
                case BotAiEnum.Group:
                case BotAiEnum.GroupHard:
                case BotAiEnum.Test:
                    levers.Add(("press", "true"));  levers.Add(("riposte", "true"));  levers.Add(("move", "true")); levers.Add(("pursue", "true"));
                    levers.Add(("targetRange", "3")); levers.Add(("stickyTarget", "false")); levers.Add(("engageOnAttack", "true"));

                    // Every preset here holds formation. Separation sits just under squadSpacing so it only resists overlap.
                    levers.Add(("squad", "true")); levers.Add(("separationRange", "0.8"));

                    // And a post: wait on the mark it was set up on, and walk back to it after a bout.
                    levers.Add(("post", "true"));

                    // Duel bots keep the neutral 0.5. Deliberate updowns are a Group trait, set per tier below.
                    if (aiType == BotAiEnum.Test) levers.Add(("coordinate", "1"));

                    // Group family: a drill station. Wakes as one, gives ground, fights, returns to post.
                    if (aiType == BotAiEnum.GroupEasy || aiType == BotAiEnum.GroupNormal
                        || aiType == BotAiEnum.Group || aiType == BotAiEnum.GroupHard)
                    {
                        levers.Add(("breakoff", "true"));

                        // Give ground once, then stand and defend for a moment before either side commits.
                        levers.Add(("engageDelay", "1.5"));
                        // Stop one short of a duel: a 3v1 plays on as a 2v1 and resets before it would become a 1v1.
                        levers.Add(("minMembers", "2"));

                        // A shorthanded bout stays shorthanded until it is over, which is what makes minMembers mean anything.
                        levers.Add(("holdReplacement", "true"));

                        // The updown axis. For a pair P(opposite) is c^2+(1-c)^2, so the useful values crowd against 1.
                        levers.Add(("coordinate",
                            aiType == BotAiEnum.GroupEasy ? "0.97" :
                            aiType == BotAiEnum.GroupNormal ? "0.98" : "1"));

                        // Floor under an updown: opposite stabs closer together than this cannot be blocked at all.
                        levers.Add(("stabSeparation",
                            aiType == BotAiEnum.GroupEasy ? "0.3" :
                            aiType == BotAiEnum.GroupNormal ? "0.25" :
                            aiType == BotAiEnum.GroupHard ? "0.15" : "0"));

                        // How much room the line gives itself to breathe. A narrower band is the harder one to fight.
                        levers.Add(("squadSpacingVariance",
                            aiType == BotAiEnum.GroupEasy ? "0.5" :
                            aiType == BotAiEnum.GroupNormal ? "0.3" :
                            aiType == BotAiEnum.GroupHard ? "0.1" : "0"));
                    }

                    switch (aiType)
                    {
                        // Difficulty is reaction speed plus how well a line is held. The imperfection levers are ceilings.
                        case BotAiEnum.SparringEasy:   // sluggish, and holds a line badly
                            levers.Add(("blockReactionMin", "0.3")); levers.Add(("blockReactionMax", "0.5"));
                            levers.Add(("riposteReactionMin", "0.2")); levers.Add(("riposteReactionMax", "0.8"));
                            levers.Add(("attackReadBeat", "0.9"));
                            // Barely holds a line: wide enough to walk between, and late enough answering that
                            // the gap stays open long enough to use.
                            levers.Add(("slotError", "0.9")); levers.Add(("formationLag", "1.2")); break;

                        case BotAiEnum.SparringNormal: // human reactions, and human sloppiness
                            levers.Add(("blockReactionMin", "0.1")); levers.Add(("blockReactionMax", "0.2"));
                            levers.Add(("riposteReactionMin", "0")); levers.Add(("riposteReactionMax", "0.5"));
                            levers.Add(("attackReadBeat", "0.6"));
                            levers.Add(("slotError", "0.5")); levers.Add(("formationLag", "0.6")); break;

                        // GroupEasy is what GroupNormal was when it measured well in play, moved down a tier whole.
                        case BotAiEnum.GroupEasy:
                            levers.Add(("blockReactionMin", "0.1")); levers.Add(("blockReactionMax", "0.2"));
                            levers.Add(("riposteReactionMin", "0")); levers.Add(("riposteReactionMax", "0.5"));
                            // Enough of a beat to break the metronome. MissedStabDuration already holds the bot for 1.5s.
                            levers.Add(("attackReadBeat", "0.1"));
                            levers.Add(("slotError", "0.5")); levers.Add(("formationLag", "0.2")); break;

                        case BotAiEnum.GroupNormal:
                            levers.Add(("blockReactionMin", "0.1")); levers.Add(("blockReactionMax", "0.2"));
                            levers.Add(("riposteReactionMin", "0")); levers.Add(("riposteReactionMax", "0.5"));
                            // Wider than Easy's, deliberately: the harder tier is the one whose rhythm you
                            // cannot settle into. Easy is more predictable here and slower everywhere else.
                            levers.Add(("attackReadBeat", "0.3"));
                            // Same misplacement as Easy, but answered instantly.
                            levers.Add(("slotError", "0.5")); levers.Add(("formationLag", "0")); break;

                        // GroupHard is Group with the edges off: same reads, blockable updowns, a fractionally late counter.
                        case BotAiEnum.GroupHard:
                            levers.Add(("blockReactionMin", "0")); levers.Add(("blockReactionMax", "0"));
                            levers.Add(("riposteReactionMin", "0")); levers.Add(("riposteReactionMax", "0.1"));
                            levers.Add(("attackReadBeat", "0.3"));
                            // Just off perfect, so the pair is not in identical relative positions every bout.
                            levers.Add(("slotError", "0.1")); levers.Add(("formationLag", "0")); break;

                        // Sparring's reads, plus offence: holds a stab against a raised guard and throws the
                        // moment it drops or they start their own, which needs no delay anywhere to be safe.
                        case BotAiEnum.Dueling:
                            levers.Add(("blockReactionMin", "0")); levers.Add(("blockReactionMax", "0"));
                            levers.Add(("riposteReactionMin", "0")); levers.Add(("riposteReactionMax", "0"));
                            levers.Add(("attackReadBeat", "0.3"));
                            levers.Add(("slotError", "0")); levers.Add(("formationLag", "0"));
                            levers.Add(("holdMax", "2.5")); break;

                        // Feint sandbox: Dueling with holding off, so feint timings are measured on their own.
                        case BotAiEnum.Feinting:
                            levers.Add(("blockReactionMin", "0")); levers.Add(("blockReactionMax", "0"));
                            levers.Add(("riposteReactionMin", "0")); levers.Add(("riposteReactionMax", "0"));
                            levers.Add(("attackReadBeat", "0.3"));
                            levers.Add(("slotError", "0")); levers.Add(("formationLag", "0"));
                            levers.Add(("feintCount", "1")); break;

                        default:                      // Sparring, Group and Test: how it is supposed to be done.
                            levers.Add(("blockReactionMin", "0")); levers.Add(("blockReactionMax", "0"));
                            levers.Add(("riposteReactionMin", "0")); levers.Add(("riposteReactionMax", "0"));
                            levers.Add(("attackReadBeat", "0.3"));
                            levers.Add(("slotError", "0")); levers.Add(("formationLag", "0")); break;
                    }
                    break;
                default: // RiposteDummy: stands its ground, blocks, counters only, engages the closest in range.
                    levers.Add(("press", "false")); levers.Add(("riposte", "true"));  levers.Add(("move", "false")); levers.Add(("pursue", "false"));
                    levers.Add(("targetRange", "3")); levers.Add(("stickyTarget", "false")); levers.Add(("engageOnAttack", "false"));
                    levers.Add(("blockReactionMin", "0.1")); levers.Add(("blockReactionMax", "0.2"));
                    levers.Add(("riposteReactionMin", "0")); levers.Add(("riposteReactionMax", "0.5"));
                    levers.Add(("attackReadBeat", "0.6")); break;
            }
            return levers.ToArray();
        }

        // Applies the preset's defaults, each overridden by a global default if one is set.
        private void SeedLevers(BotAiEnum aiType)
        {
            foreach (var (name, def) in DefaultLeversFor(aiType))
                if (!TrySet(name, GlobalAiConfigurable.Default(aiType.ToString(), name, def), out _))
                    TrySet(name, def, out _);
        }

        private static readonly string[] LeverNames =
        {
            "offensiveRange", "offensiveRangeVariance", "defensiveRange", "defensiveRangeVariance",
            "attackRange", "attackReadBeat", "riposteReactionMin", "riposteReactionMax", "riposteWindow",
            "blockReactionMin", "blockReactionMax", "press", "riposte", "move", "pursue",
            "targetRange", "ignoreTeam", "ignoreBots", "stickyTarget", "engageOnAttack",
            "passiveRange", "passiveBlockReaction",
            "guard", "guardTarget", "guardRange", "guardFollowRange", "separationRange",
            "squad", "coordinate", "slotError", "formationLag", "stabSeparation",
            "gateRadius", "clampRadius", "bladeMargin", "mateConeFloor", "mateCrowdRatio", "gateOnMate", "abortOnMate",
            "aimPitch", "holdMax", "holdMin", "feintCount", "feintDepth", "feintDwell", "squadSpacing", "squadSpacingVariance", "laneHalfWidth", "squadStandoff",
            "post", "breakoff", "breakoffRange", "engageDelay", "resetRange",
            "minMembers", "holdReplacement", "returnDelay"
        };

        // Levers that do nothing unless another lever is on. Setting a dormant one is allowed, not an error.
        private static readonly Dictionary<string, string> LeverGates = new()
        {
            { "offensiverange", "move" }, { "offensiverangevariance", "move" },
            { "defensiverange", "move" }, { "defensiverangevariance", "move" },
            { "pursue", "move" },
            { "attackrange", "press" },
            { "ripostereactionmin", "riposte" }, { "ripostereactionmax", "riposte" }, { "ripostewindow", "riposte" },
            // passiveBlockReaction is deliberately ungated: it applies in every posture except fighting, which a
            // bot can reach through engageOnAttack (waiting) or through post (backing off, withdrawing).
            { "passiverange", "engageonattack" },
            { "guardtarget", "guard" }, { "guardrange", "guard" }, { "guardfollowrange", "guard" },
            { "coordinate", "squad" }, { "sloterror", "squad" }, { "formationlag", "squad" },
            { "stabseparation", "squad" },
            // gateRadius, clampRadius and abortOnMate are ungated: the clamp runs on any bot with a live swing.
            { "squadspacing", "squad" }, { "squadspacingvariance", "squad" },
            { "holdmax", "melee" }, { "holdmin", "melee" },
            { "lanehalfwidth", "squad" }, { "squadstandoff", "squad" },
            { "breakoff", "post" }, { "breakoffrange", "breakoff" }, { "resetrange", "post" },
            // engageDelay is deliberately NOT gated on breakoff. It is a separate layer on the same wake: a
            // station told to fight from where the blow landed can still be told to wait a beat before it does.
            { "engagedelay", "post" },
            { "minmembers", "post" }, { "holdreplacement", "post" }, { "returndelay", "post" },
        };

        // Only the levers that appear as gates above need answering here.
        private bool GateOpen(string gate)
        {
            switch (gate)
            {
                case "move":           return _move;
                case "press":          return _press;
                case "riposte":        return _riposte;
                case "guard":          return _guard;
                case "squad":          return _squad;
                case "post":           return _post;
                case "breakoff":       return _breakoff;
                case "engageonattack": return _engageOnAttack;
                default:               return true;
            }
        }

        // The nearest switched-off gate stopping this lever from doing anything, or null when it is live.
        private string BlockingGate(string lever)
        {
            string gate = LeverGates.TryGetValue(lever, out string g) ? g : null;

            while (gate != null)
            {
                if (!GateOpen(gate)) return gate;
                gate = LeverGates.TryGetValue(gate, out string next) ? next : null;
            }

            return null;
        }

        public bool TrySet(string name, string value, out string message)
        {
            if (!TrySetValue(name, value, out message)) return false;

            // Set it either way, but say so when it will sit dormant. The caller already names the lever, so this
            // only carries the reason.
            string blocked = BlockingGate(name.ToLowerInvariant());
            if (blocked != null)
                message = $"it does nothing while '{blocked}' is false.";

            return true;
        }

        private bool TrySetValue(string name, string value, out string error)
        {
            error = string.Empty;
            switch (name.ToLowerInvariant())
            {
                case "offensiverange":         return SetFloat(value, 0f, v => _offensiveBase = v, "offensiveRange", ref error);
                case "offensiverangevariance": return SetFloat(value, 0f, v => _offensiveVar = v, "offensiveRangeVariance", ref error);
                case "defensiverange":         return SetFloat(value, 0f, v => _defensiveBase = v, "defensiveRange", ref error);
                case "defensiverangevariance": return SetFloat(value, 0f, v => _defensiveVar = v, "defensiveRangeVariance", ref error);
                case "attackrange":            return SetFloat(value, 0f, v => _attackRange = v, "attackRange", ref error);
                case "attackreadbeat":         return SetFloat(value, 0f, v => _attackReadBeat = v, "attackReadBeat", ref error);
                case "ripostereactionmin":     return SetFloat(value, 0f, v => _riposteReactionMin = v, "riposteReactionMin", ref error);
                case "ripostereactionmax":     return SetFloat(value, 0f, v => _riposteReactionMax = v, "riposteReactionMax", ref error);
                case "ripostewindow":          return SetFloat(value, 0f, v => _riposteWindow = v, "riposteWindow", ref error);
                case "blockreactionmin":       return SetFloat(value, 0f, v => _blockReactionMin = v, "blockReactionMin", ref error);
                case "blockreactionmax":       return SetFloat(value, 0f, v => _blockReactionMax = v, "blockReactionMax", ref error);
                case "press":   return SetBool(value, v => _press = v, "press", ref error);
                case "riposte": return SetBool(value, v => _riposte = v, "riposte", ref error);
                case "move":    return SetBool(value, v => _move = v, "move", ref error);
                case "pursue":  return SetBool(value, v => _pursue = v, "pursue", ref error);
                case "targetrange":  return SetFloat(value, 0f, v => _targetRange = v, "targetRange", ref error); // 0 = unlimited
                case "ignoreteam":   return SetBool(value, v => _ignoreTeam = v, "ignoreTeam", ref error);
                case "ignorebots":   return SetBool(value, v => _ignoreBots = v, "ignoreBots", ref error);
                case "stickytarget": return SetBool(value, v => _stickyTarget = v, "stickyTarget", ref error);
                case "engageonattack": return SetBool(value, v => _engageOnAttack = v, "engageOnAttack", ref error);
                case "passiverange": return SetFloat(value, 0f, v => _passiveRange = v, "passiveRange", ref error);
                case "passiveblockreaction": return SetFloat(value, 0f, v => _passiveBlockReaction = v, "passiveBlockReaction", ref error);
                case "guard":            return SetBool(value, v => _guard = v, "guard", ref error);
                case "guardrange":       return SetFloat(value, 0f, v => _guardRange = v, "guardRange", ref error);
                case "guardfollowrange": return SetFloat(value, 0f, v => _guardFollowRange = v, "guardFollowRange", ref error);
                case "separationrange":  return SetFloat(value, 0f, v => _separationRange = v, "separationRange", ref error); // 0 = off
                case "squad":            return SetBool(value, v => _squad = v, "squad", ref error);
                case "coordinate":       return SetFraction(value, v => _coordinate = v, "coordinate", ref error);
                case "sloterror":        return SetFloat(value, 0f, v => _slotError = v, "slotError", ref error);
                case "formationlag":     return SetFloat(value, 0f, v => _formationLag = v, "formationLag", ref error);
                case "stabseparation":   return SetFloat(value, 0f, v => _stabSeparation = v, "stabSeparation", ref error);
                case "gateradius":       return SetFloat(value, 0f, v => _gateRadius = v, "gateRadius", ref error);
                case "clampradius":      return SetFloat(value, 0f, v => _clampRadius = v, "clampRadius", ref error);
                case "blademargin":      return SetFloat(value, 0f, v => _bladeMargin = v, "bladeMargin", ref error);
                case "mateconefloor":    return SetFloat(value, 0f, v => _mateConeFloor = v, "mateConeFloor", ref error);
                case "matecrowdratio":   return SetFloat(value, 0f, v => _mateCrowdRatio = v, "mateCrowdRatio", ref error);
                case "gateonmate":       return SetBool(value, v => _gateOnMate = v, "gateOnMate", ref error);
                case "abortonmate":      return SetBool(value, v => _abortOnMate = v, "abortOnMate", ref error);
                case "holdmax":          return SetFloat(value, 0f, v => _holdMax = v, "holdMax", ref error);
                case "holdmin":          return SetFloat(value, 0f, v => _holdMin = v, "holdMin", ref error);
                case "feintcount":       return SetFloat(value, 0f, v => _feintCount = v, "feintCount", ref error);
                case "feintdepth":       return SetFloat(value, 0f, v => _feintDepth = v, "feintDepth", ref error);
                case "feintdwell":       return SetFloat(value, 0f, v => _feintDwell = v, "feintDwell", ref error);
                case "aimpitch":         return SetFloat(value, float.MinValue, v => { _aimPitch = v; RefreshBladeGeometry(); }, "aimPitch", ref error);
                case "squadspacing":     return SetFloat(value, 0f, v => _squadSpacing = v, "squadSpacing", ref error);
                case "squadspacingvariance": return SetFloat(value, 0f, v => _squadSpacingVar = v, "squadSpacingVariance", ref error);
                case "lanehalfwidth":    return SetFloat(value, 0f, v => _laneHalfWidth = v, "laneHalfWidth", ref error);
                case "squadstandoff":    return SetFloat(value, 0f, v => _squadStandoff = v, "squadStandoff", ref error);
                case "post":             return SetBool(value, v => _post = v, "post", ref error);
                case "breakoff":         return SetBool(value, v => _breakoff = v, "breakoff", ref error);
                case "breakoffrange":    return SetFloat(value, 0f, v => _breakoffRange = v, "breakoffRange", ref error);
                case "engagedelay":      return SetFloat(value, 0f, v => _engageDelay = v, "engageDelay", ref error);
                case "resetrange":       return SetFloat(value, 0f, v => _resetRange = v, "resetRange", ref error); // 0 = no limit
                case "minmembers":       return SetInt(value, 0, v => _minMembers = v, "minMembers", ref error);
                case "holdreplacement":  return SetBool(value, v => _holdReplacement = v, "holdReplacement", ref error);
                case "returndelay":      return SetFloat(value, 0f, v => _returnDelay = v, "returnDelay", ref error);

                case "guardtarget":
                    // A player id to escort. 0 or less clears it, which is also how the presets say "nobody".
                    if (!int.TryParse(value, out int wardId))
                    {
                        error = "guardTarget must be a playerId, or 0 for none.";
                        return false;
                    }
                    _guardTargetId = wardId > 0 ? wardId : (int?)null;
                    return true;

                default:
                    error = $"Unknown lever '{name}'. MeleeAi levers: {string.Join(", ", LeverNames)}.";
                    return false;
            }
        }

        public List<(string name, string value, string inactive)> ListParams()
        {
            var raw = RawParams();
            var list = new List<(string name, string value, string inactive)>(raw.Count);
            foreach (var (name, value) in raw)
                list.Add((name, value, BlockingGate(name.ToLowerInvariant())));
            return list;
        }

        private List<(string name, string value)> RawParams()
        {
            var list = new List<(string name, string value)>();
            list.Add(("offensiveRange", _offensiveBase.ToString("0.##")));
            list.Add(("offensiveRangeVariance", _offensiveVar.ToString("0.##")));
            list.Add(("defensiveRange", _defensiveBase.ToString("0.##")));
            list.Add(("defensiveRangeVariance", _defensiveVar.ToString("0.##")));
            list.Add(("attackRange", _attackRange.ToString("0.##")));
            list.Add(("attackReadBeat", _attackReadBeat.ToString("0.##")));
            list.Add(("riposteReactionMin", _riposteReactionMin.ToString("0.##")));
            list.Add(("riposteReactionMax", _riposteReactionMax.ToString("0.##")));
            list.Add(("riposteWindow", _riposteWindow.ToString("0.##")));
            list.Add(("blockReactionMin", _blockReactionMin.ToString("0.##")));
            list.Add(("blockReactionMax", _blockReactionMax.ToString("0.##")));
            list.Add(("press", _press ? "true" : "false"));
            list.Add(("riposte", _riposte ? "true" : "false"));
            list.Add(("move", _move ? "true" : "false"));
            list.Add(("pursue", _pursue ? "true" : "false"));
            list.Add(("targetRange", _targetRange.ToString("0.##")));
            list.Add(("ignoreTeam", _ignoreTeam ? "true" : "false"));
            list.Add(("ignoreBots", _ignoreBots ? "true" : "false"));
            list.Add(("stickyTarget", _stickyTarget ? "true" : "false"));
            list.Add(("engageOnAttack", _engageOnAttack ? "true" : "false"));
            list.Add(("passiveRange", _passiveRange.ToString("0.##")));
            list.Add(("passiveBlockReaction", _passiveBlockReaction.ToString("0.##")));
            list.Add(("guard", _guard ? "true" : "false"));
            list.Add(("guardTarget", (_guardTargetId ?? 0).ToString()));
            list.Add(("guardRange", _guardRange.ToString("0.##")));
            list.Add(("guardFollowRange", _guardFollowRange.ToString("0.##")));
            list.Add(("separationRange", _separationRange.ToString("0.##")));
            list.Add(("squad", _squad ? "true" : "false"));
            list.Add(("coordinate", _coordinate.ToString("0.##")));
            list.Add(("slotError", _slotError.ToString("0.##")));
            list.Add(("formationLag", _formationLag.ToString("0.##")));
            list.Add(("stabSeparation", _stabSeparation.ToString("0.##")));
            list.Add(("gateRadius", _gateRadius.ToString("0.##")));
            list.Add(("clampRadius", _clampRadius.ToString("0.##")));
            list.Add(("bladeMargin", _bladeMargin.ToString("0.#")));
            list.Add(("mateConeFloor", _mateConeFloor.ToString("0.#")));
            list.Add(("mateCrowdRatio", _mateCrowdRatio.ToString("0.##")));
            list.Add(("gateOnMate", _gateOnMate ? "true" : "false"));
            list.Add(("abortOnMate", _abortOnMate ? "true" : "false"));
            list.Add(("aimPitch", _aimPitch.ToString("0.###")));
            list.Add(("holdMax", _holdMax.ToString("0.##")));
            list.Add(("holdMin", _holdMin.ToString("0.##")));
            list.Add(("feintCount", _feintCount.ToString("0.##")));
            list.Add(("feintDepth", _feintDepth.ToString("0.##")));
            list.Add(("feintDwell", _feintDwell.ToString("0.##")));
            list.Add(("squadSpacing", _squadSpacing.ToString("0.##")));
            list.Add(("squadSpacingVariance", _squadSpacingVar.ToString("0.##")));
            list.Add(("laneHalfWidth", _laneHalfWidth.ToString("0.##")));
            list.Add(("squadStandoff", _squadStandoff.ToString("0.##")));
            list.Add(("post", _post ? "true" : "false"));
            list.Add(("breakoff", _breakoff ? "true" : "false"));
            list.Add(("breakoffRange", _breakoffRange.ToString("0.##")));
            list.Add(("engageDelay", _engageDelay.ToString("0.##")));
            list.Add(("resetRange", _resetRange.ToString("0.##")));
            list.Add(("minMembers", _minMembers.ToString()));
            list.Add(("holdReplacement", _holdReplacement ? "true" : "false"));
            list.Add(("returnDelay", _returnDelay.ToString("0.##")));
            return list;
        }

        // Copies levers to a Replace replacement. Levers only: runtime state stays with the bot that earned it.
        private void CopyLeversFrom(MeleeAi p)
        {
            _offensiveBase = p._offensiveBase; _offensiveVar = p._offensiveVar;
            _defensiveBase = p._defensiveBase; _defensiveVar = p._defensiveVar;
            _attackRange = p._attackRange; _attackReadBeat = p._attackReadBeat;
            _riposteReactionMin = p._riposteReactionMin; _riposteReactionMax = p._riposteReactionMax;
            _riposteWindow = p._riposteWindow;
            _blockReactionMin = p._blockReactionMin; _blockReactionMax = p._blockReactionMax;
            _passiveBlockReaction = p._passiveBlockReaction;
            _press = p._press; _riposte = p._riposte; _move = p._move; _pursue = p._pursue;
            _targetRange = p._targetRange; _ignoreTeam = p._ignoreTeam; _ignoreBots = p._ignoreBots; _stickyTarget = p._stickyTarget;
            _passiveRange = p._passiveRange;
            _guard = p._guard; _guardRange = p._guardRange; _guardFollowRange = p._guardFollowRange; _separationRange = p._separationRange;
            _squad = p._squad; _coordinate = p._coordinate;
            _slotError = p._slotError; _formationLag = p._formationLag;
            _stabSeparation = p._stabSeparation;
            _gateRadius = p._gateRadius; _clampRadius = p._clampRadius;
            _bladeMargin = p._bladeMargin; _mateConeFloor = p._mateConeFloor; _mateCrowdRatio = p._mateCrowdRatio;
            _gateOnMate = p._gateOnMate; _abortOnMate = p._abortOnMate;
            _aimPitch = p._aimPitch; _holdMax = p._holdMax; _holdMin = p._holdMin;
            _feintCount = p._feintCount; _feintDepth = p._feintDepth; _feintDwell = p._feintDwell;
            RefreshBladeGeometry();
            _squadSpacing = p._squadSpacing; _squadSpacingVar = p._squadSpacingVar; _laneHalfWidth = p._laneHalfWidth;
            _squadStandoff = p._squadStandoff;
            _post = p._post; _breakoff = p._breakoff; _breakoffRange = p._breakoffRange; _engageDelay = p._engageDelay;
            _resetRange = p._resetRange;
            _minMembers = p._minMembers; _holdReplacement = p._holdReplacement; _returnDelay = p._returnDelay;
            _engageOnAttack = p._engageOnAttack;
            _guardTargetId = p._guardTargetId;   // a replacement guard keeps escorting the same player
        }

        private static bool SetFloat(string value, float min, System.Action<float> set, string name, ref string error)
        {
            if (!float.TryParse(value, out float v) || v < min)
            {
                error = $"{name} must be a number{(min > float.MinValue ? $" >= {min}" : "")}.";
                return false;
            }
            set(v);
            return true;
        }

        // A 0-to-1 chance. Rejected rather than clamped when out of range: silently turning 50 into 1 would read
        // as a working setting that does something quite different from what was asked for.
        private static bool SetFraction(string value, System.Action<float> set, string name, ref string error)
        {
            if (!float.TryParse(value, out float v) || v < 0f || v > 1f)
            {
                error = $"{name} must be a chance between 0 and 1.";
                return false;
            }
            set(v);
            return true;
        }

        private static bool SetInt(string value, int min, System.Action<int> set, string name, ref string error)
        {
            if (!int.TryParse(value, out int v) || v < min)
            {
                error = $"{name} must be a whole number >= {min}.";
                return false;
            }
            set(v);
            return true;
        }

        private static bool SetBool(string value, System.Action<bool> set, string name, ref string error)
        {
            switch (value.ToLowerInvariant())
            {
                case "true": case "on": case "1": case "yes": set(true); return true;
                case "false": case "off": case "0": case "no": set(false); return true;
                default: error = $"{name} must be true or false."; return false;
            }
        }
    }
}
