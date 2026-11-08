# MDS (Melee Drill Scripts)

*A utility mod with commands and scripts to assist, enhance, and automate melee drills in **Holdfast: Nations At War***

---

## Command Quick Reference

Prefix all commands with `rc` and run as a logged-in server admin.

**Arena**

```
rc set ArenaCorner1
rc set ArenaCorner2
```

With coordinates: `rc set ArenaCorner1 [x,z]`, `rc set ArenaCorner2 [x,z]`

**Drills**

```
rc 3v3
rc groupfight
rc openmelee 2 7
```

**Bots**

```
rc bot spawn 5 French ArmyLineInfantry
rc bot summon French ArmyLineInfantry None Replace
rc bot summonAt 42 Sparring
rc bot setBotAi all Sparring
rc bot cfg 42 stabInterval 2.5
rc bot move all seek me
rc bot remove all
rc summonLine 10 French ArmyLineInfantry
rc summonLineAt 42 10
rc spawnLine -20 30 90 10 French ArmyLineInfantry
```

**Runtime Config**

```
rc get xvxDistance
rc get Players all
rc set xvxDistance 5
rc set lineBotCount 10
rc set globalAI StabbingDummy stabInterval 2.5
rc get globalAI StabbingDummy stabInterval
```

---

## Overview

MDS tracks round and player state to run custom drills and utility commands. Use standard `rc` commands and **modded config variables** to tailor drills to your server's needs.

---

## Quick Start (In‑Game)

1. **Set the arena corners** (use your current position if no coords given):

   ```
   rc set ArenaCorner1
   rc set ArenaCorner2
   ```
2. **Run a drill** (examples):

   ```
   rc 3v3
   rc groupfight
   rc openmelee 2 7
   ```
3. **Spawn bots** (examples):

   ```
   rc bot spawn French ArmyLineInfantry
   rc summonLine 5
   ```

> Commands are **case‑insensitive**. Arguments in `<>` are required; `[ ]` are optional.

---

## Arena

All drills require a rectangular **arena** defined by two corner points *(x,z)*.

* **Config variable:**

  * `mod_variable MDS:SetArena:(x,z),(x,z)`
* **Runtime via `rc`:**

  * `rc set ArenaCorner1 [x,z]` *(uses player pos if none given)*
  * `rc set ArenaCorner2 [x,z]` *(uses player pos if none given)*

---

## Available Commands

All commands must be prefixed with `rc` and require admin.

### `xvx`

**Usage:** `rc xvx <attacking:int> <defending:int> [strategy] [distance:float] [spacing:float] [orientation]`

* Spawns an X‑v‑X match inside the arena using the selected strategy and parameters.
* Shorthand calls are supported using defaults. e.g. `rc 3v2`, `rc 20v1`.
* **Defaults (configurable):** `xvxStrategy`, `xvxDistance`, `xvxSpacing`
* **Examples:**

  ```
  rc xvx 3 3 next 4 1 northsouth
  rc 3v3
  ```

### `groupfight`

**Usage:** `rc groupfight [strategy] [distance:float] [spacing:float] [orientation]`

* Spawns both teams in lines for a groupfight inside the arena.
* **Defaults (configurable):** `groupfightStrategy`, `groupfightDistance`, `groupfightSpacing`
* **Examples:**

  ```
  rc groupfight random 25 2 180
  rc groupfight
  ```

### `openmelee`

**Usage:** `rc openmelee [spacing:float] [offset:float]`

* Spawns players randomly around the arena to simulate an open melee.
* **Defaults (configurable):** `openMeleeSpacing`, `openMeleeOffset`
* **Examples:**

  ```
  rc openmelee 2 7
  rc openmelee
  ```

### `shootingTraining`

**Usage:** `rc shootingTraining`

* Toggle. Turns infinite firearm ammo and firearm trajectory lines on, and off again on the next call.
* Takes no arguments. Replies with the new state.

### `meleelog` *(dev tool)*

**Usage:** `rc meleelog [on|off]` or `rc meleelog frames <on|off>`

* Records what the melee actually did in the server log. Off by default. Toggles if no state is given.
* Each melee hit writes one `hit` line: the attacker, friendly fire, weapon, damage, the distance and bearing to the victim, and whether a friendly stood nearer to the attacker than the nearest enemy.
* `src` says who named the attacker. `score` is the game's scorable action, `kill` is the kill callback, and `unknown` means neither arrived within a second.
* Blocks and clashes write one `block` or `clash` line each.
* Players under `rc bot probe` write one `probe` line per tick with position, facing, pitch, health and their nearest friend and enemy.
* `frames` keeps the last second of every player and prints it on each melee hit, about 20 lines per hit.
* `atk~`, `hold~`, `block~` and `strike~` are inferred from packets. `cmd` is the last action MDS ordered for a bot.


### `bot`

**Usage:** `rc bot <subcommand> [args]`

* Central command for spawning, summoning, and managing bots.
* Subcommands: `spawn`, `spawnRandom`, `summon`, `summonAt`, `setBotAi`, `setBotDeathPolicy`, `remove`, `list`, `move`, `cfg`
* Dev/probe tools: `probe`, `act`
* See **[Bot Subcommands](#bot-subcommands)** for full details and **[Bot AI Types](#bot-ai-types)** for the available AI behaviours.
* **Examples:**

  ```
  rc bot spawn 3 French ArmyLineInfantry
  rc bot remove all
  rc bot list
  ```

### `summonLine`

**Usage:** `rc summonLine [count] [faction] [class] [ai] [death] [name [regtag [uniformId]]]`

* Spawns a shoulder‑to‑shoulder line of bots **centred on your position**, facing your direction.
* `count` overrides `lineBotCount` for this call; faction/class default to yours if omitted.
* `faction` accepts `attacking` / `defending` (resolved to the round's factions) as well as a faction name.
* **Defaults (configurable):** `lineBotCount`, `lineSpacing`, `botDefaultAi`, `botDefaultDeathPolicy`
* **Examples:**

  ```
  rc summonLine
  rc summonLine 5
  rc summonLine 8 French ArmyLineInfantry None Replace
  ```

### `summonLineAt`

**Usage:** `rc summonLineAt <playerId> [count] [faction] [class] [ai] [death] [name [regtag [uniformId]]]`

* Same as `summonLine`, but the line is **centred on another player**, facing their direction.
* Faction and class default to **that player's**, not yours.
* Works while you're in free roam or spectating, where the server never learns your own position.
* Everything after the id follows the same grammar as `summonLine`. Replies and errors are sent to you, not to the target.
* **Examples:**

  ```
  rc summonLineAt 42
  rc summonLineAt 42 10
  rc summonLineAt 42 8 French ArmyLineInfantry None Replace
  ```
* **Bodyguards:** with the `Guardian` AI the whole line escorts the target, so this is the quickest way to put a detail around someone.

  ```
  rc summonLineAt 42 6 Guardian Replace
  ```

### `spawnLine`

**Usage:** `rc spawnLine <x> <z> <rotation> [count] [faction] [class] [ai] [death] [name [regtag [uniformId]]]`

* Spawns a line of bots at world position `(x, z)` facing `rotation` degrees from North.
* `count` overrides `lineBotCount` for this call; faction/class default to caller's if omitted.
* `faction` accepts `attacking` / `defending` (resolved to the round's factions) as well as a faction name.
* **Defaults (configurable):** `lineBotCount`, `lineSpacing`, `botDefaultAi`, `botDefaultDeathPolicy`
* **Examples:**

  ```
  rc spawnLine -20 30 90 10 French ArmyLineInfantry
  rc spawnLine 0 50 0
  ```

### `get`

**Usage:** `rc get <Configurable> [additional arguments]`

* Mirrors Holdfast's `rc get` to read **mod configurables** and **mod data** at runtime.
* Additional mod data shortcuts:

  ```
  Player <playerId>
  Players <faction> (Attacking|Defending|Spectator|All) [count]
  Round
  ```
* **Examples:**

  ```
  rc get xvxDistance
  rc get Players all
  ```

### `set`

**Usage:** `rc set <Configurable> <Value> [additional arguments]`

* Mirrors Holdfast's `rc set` to set **mod configurables** at runtime.
* **Example:**

  ```
  rc set xvxDistance 5
  rc set lineBotCount 10
  rc set lineSpacing 0.55
  ```

---

## Bot Subcommands

All bot subcommands are accessed via `rc bot <subcommand> [args]`.

### `spawn`

**Usage:** `rc bot spawn [count] [faction class] [ai] [death] [name [regtag [uniformId]]]`

* Spawns one or more specific bots at a random server spawn point.
* `faction` and `class` default to the caller's current faction/class if omitted. Providing `faction` without `class` uses the caller's class.
* `faction` accepts `attacking` / `defending` (resolved to the round's factions) as well as a faction name (e.g. `French`).
* `ai` and `death` default to `botDefaultAi` and `botDefaultDeathPolicy`.
* Arguments are **strictly positional**: omit from the right, not the middle.
* **Examples:**

  ```
  rc bot spawn
  rc bot spawn 5
  rc bot spawn Attacking ArmyLineInfantry
  rc bot spawn 3 French ArmyLineInfantry None Replace
  rc bot spawn 1 French ArmyLineInfantry None Replace Soldier 1stBattalion 14
  ```

### `spawnRandom`

**Usage:** `rc bot spawnRandom [count]`

* Spawns one or more bots with a **fully random** faction and class.
* AI and death policy default to `botDefaultAi` and `botDefaultDeathPolicy`.
* **Examples:**

  ```
  rc bot spawnRandom
  rc bot spawnRandom 5
  ```

### `summon`

**Usage:** `rc bot summon [faction] [class] [ai] [death] [name [regtag [uniformId]]]`

* Spawns a single bot **at your position**, facing your direction.
* Same faction/class/ai/death defaulting as `spawn`.
* **Examples:**

  ```
  rc bot summon
  rc bot summon French ArmyLineInfantry
  rc bot summon Defending ArmyLineInfantry None Replace
  ```

### `summonAt`

**Usage:** `rc bot summonAt <playerId> [faction] [class] [ai] [death] [name [regtag [uniformId]]]`

* Spawns a single bot **at another player's position**, facing their direction.
* Faction and class default to **that player's**, not yours, so dropping a bot on someone needs nothing but their id.
* Works while you're in free roam or spectating, where the server never learns your own position and plain `summon` has nothing to place the bot at.
* Everything after the id follows the same positional grammar as `summon`. Replies and errors are sent to you, not to the target.
* **Examples:**

  ```
  rc bot summonAt 42
  rc bot summonAt 42 French ArmyLineInfantry
  rc bot summonAt 42 Defending ArmyLineInfantry Sparring Replace
  ```
* **Bodyguards:** summoning the `Guardian` AI this way makes the target the player it guards, so repeating the command builds an escort around them.

  ```
  rc bot summonAt 42 Guardian Replace
  rc bot summonAt 42 Guardian Replace
  ```

### `setBotAi`

**Usage:** `rc bot setBotAi <target> <ai>`

* Sets the AI behaviour for one or more tracked bots immediately.
* **Target:** `all`, `attacking`, `defending`, `<faction>` (e.g. `French`), or `<playerId>`
* **AI types:** `None`, `Manual`, `StabbingDummy`, `RiposteDummy`, `Sparring`, `Dueling`, `Group`, `Guardian`, `Test`: see **[Bot AI Types](#bot-ai-types)**.
* **Examples:**

  ```
  rc bot setBotAi all Sparring
  rc bot setBotAi French RiposteDummy
  rc bot setBotAi 42 StabbingDummy
  ```

### `setBotDeathPolicy`

**Usage:** `rc bot setBotDeathPolicy <target> <policy>`

* Sets the death policy for one or more tracked bots.
* **Target:** `all`, `attacking`, `defending`, `<faction>`, or `<playerId>`
* **Policies:**
  * `None`: do nothing, defaulting to in game handling (They respawn at a random spawn as a random class)
  * `Kick`: kick the bot after `botKickDelay` seconds (lets the kill register)
  * `Replace`: kick then re-spawn with the same identity (name, regtag, uniform, faction, class) at death location
* **Examples:**

  ```
  rc bot setBotDeathPolicy all Kick
  rc bot setBotDeathPolicy French Replace
  rc bot setBotDeathPolicy 42 None
  ```

### `remove`

**Usage:** `rc bot remove <target>`

* Kicks (removes) one or more bots from the server immediately.
* **Target:** `all`, `attacking`, `defending`, `<faction>`, or `<playerId>`
* **Examples:**

  ```
  rc bot remove all
  rc bot remove French
  rc bot remove 42
  ```

### `list`

**Usage:** `rc bot list`

* Prints all currently tracked bots to your private messages.
* Shows player ID, faction/class, AI type, death policy, and spawn status.
* **Example:**

  ```
  rc bot list
  ```

### `move`

**Usage:** `rc bot move <target> <behavior> [args]`

* Drives bots that are on the **`Manual`** AI (set it first with `rc bot setBotAi <target> Manual`). Bots on other AIs are left untouched.
* **Target:** `all`, `attacking`, `defending`, `<faction>`, or `<playerId>`
* **Behaviors:**
  * `seek <dest>`: run toward a point/player
  * `arrive <dest>`: like seek, but decelerate to a smooth stop
  * `flee <dest>`: run directly away (`flee me facing me` = backpedal toward you)
  * `pursue <dest>`: lead a moving target to intercept it (predictive seek)
  * `evade <dest>`: flee from where a target is heading (predictive flee)
  * `wander`: roam continuously with gentle random turns
  * `facepoint <dest>`: rotate in place to face a point/player
  * `face <deg>`: rotate in place to a heading (degrees from North)
  * `stop`: halt movement
* **`<dest>`** = `x z` (two numbers), `<playerId>`, or `me`. A player or `me` destination is tracked live as they move.
* **Flags** (any combination, appended anywhere): `separate` (spread apart from other bots), `avoid` (steer around walls), `dodge` (steer around moving agents).
* **`facing <dest>`**: optional; decouples which way the bot faces from the way it travels.
* **Examples:**

  ```
  rc bot setBotAi all Manual
  rc bot move all seek me
  rc bot move all arrive 12 -4 facing me avoid
  rc bot move 42 flee me facing me
  rc bot move all wander separate
  ```

### `cfg`

**Usage:** `rc bot cfg <target> [<lever> <value>]`

* Sets or lists per-bot AI levers, overriding the global default for one bot or group.
* **Target:** `all`, `attacking`, `defending`, `<faction>`, or `<playerId>`
* With `<lever> <value>`: set it on the matching bots. Without: list their current levers.
* Levers held dormant by a switched-off lever are listed after `| inactive:`, tagged with what they wait on,
  e.g. `breakoffRange=6(needs breakoff)`. Setting a dormant lever is allowed and takes effect when its gate opens.
* Only affects bots whose AI is configurable (`StabbingDummy`, `RiposteDummy`, `Sparring*`, `Group*`).
* **Examples:**

  ```
  rc bot cfg 42 stabInterval 2.5
  rc bot cfg all stabDirection High
  rc bot cfg 42
  ```


### `probe` & `act` *(dev tools)*

Diagnostic helpers used while building and validating AI behaviour. They have no effect on normal drills.

* **`rc bot probe <playerId|me|all|attacking|defending|faction> [on|off]`**: logs a player's melee packet actions and hurt events to the server log (to learn the input tokens and timings). Toggles if `on|off` is omitted. Works on any player, human or bot.
  * Also emits a `MeleeSwing` line per tick while that bot's swing is live: `actual` facing, `desired` aim, the `clamped` aim it was given, how much was `held` back, and every squadmate's distance, bearing off the aim and required half-width. `actual` drifting from `clamped` means the bot is not turning as instructed, which is a different problem from the clamp's geometry being wrong.
* **`rc bot act <playerId> <actionToken> [argument]`**: fires a single raw `carbonPlayers` playerAction at a player/bot (e.g. `MeleeBlockHigh`, `MeleeStrikeHigh`, `ExecuteMeleeWeaponStrike`). No AI required.

---

## Bot AI Types

Assign with `rc bot setBotAi <target> <ai>`, inline when spawning (e.g. `rc bot summon <faction> <class> <ai>`), or via the `botDefaultAi` default.

| AI | Behaviour |
| --- | --- |
| `None` | Does nothing. Stands where it spawned. |
| `Manual` | Manually driven with `rc bot move`, a movement test harness. Issues no orders on its own. |
| `StabbingDummy` | Static training dummy. Stabs on a fixed cadence for block practice. Aim it by facing the way you want when you summon it. |
| `RiposteDummy` | Stands its ground, blocks, and only counters once provoked. Never throws first. |
| `Guardian` | Escorts whoever summoned it. Holds station beside them and fights only what comes within `guardRange`. |
| `Test` | Current development testing settings. |
| `SparringEasy` / `SparringNormal` / `Sparring` | Duelling practice. Passive until a player's attack is blocked, then locks onto that attacker and fights to the death. The tiers differ only in reaction speed. Several attacked by one player form up so they stop crowding each other. |
| `Dueling` | `Sparring`'s reads with offence on top. **Holds a stab against a raised guard** and throws the instant the guard drops or the opponent starts their own, so sitting on a block is punished rather than rewarded. Which chambers it holds, and which dropped guards it believes, come from a read on how you have played this duel: see **[Reading the opponent](#reading-the-opponent)**. Feints, spins and gambles still to come. |
| `Feinting` | Feint sandbox, not a difficulty. `Dueling` with holding switched off so feint timings can be measured on their own. Cancels its own windup with a block and queues the attack again, `feintCount` times, before throwing for real. See **[Feinting](#feinting)**. |
| `GroupEasy` / `GroupNormal` / `Group` / `GroupHard` | Drill station for 1vXs. Provoking any one wakes all of them; they give ground, re-form, fight as a line, and return to post. See **[Group drill stations](#group-drill-stations)**. `GroupHard` is `Group` with spaced updowns, so they can be blocked one at a time. |

`RiposteDummy` and the `Sparring` and `Group` tiers are presets of one configurable melee AI: the same behaviour with different capability toggles (`press`, `riposte`, `move`, `pursue`, `engageOnAttack`, `squad`, `post`) and tuning. `StabbingDummy` is a separate static-stabber AI. You can tweak any of them per bot with `rc bot cfg`.

### Group drill stations

A `Group` bot is for practising 2v1s and 3v1s on your own. Summon a batch and leave it:

```
rc summonLine 2 Defending ArmyLineInfantry Group Replace
```

**Batch vs formation.** Bots from one `summonLine`, `spawnLine` or `rc bot summon <count>` are a **batch**: shared
post, they wake together, and a `Replace` bot rejoins it. Any engaged batches on the **same player** merge into
one **formation**, then split back to their own posts. So walking separately summoned bots onto one player builds
a group that plays exactly like a summoned one.

**The cycle:**

| Phase | What happens |
| --- | --- |
| Waiting | Hold post, block, never attack. Bystanders can walk past. |
| Provoked | Attack any one and the whole group wakes onto whoever did it. A killing blow counts too. |
| Backing off | Give up to `breakoffRange` metres of ground and re-form. Skipped when `breakoff` is off. |
| Standing | For `engageDelay` seconds from the provocation they block but will not swing or counter. |
| Fighting | The line holds a point and rotates around it, shares guard, and throws opposite stabs. |
| Re-arming | Die or leave `resetRange` and they return to post and go quiet. |

`minMembers` with `holdReplacement` makes the group run shorthanded until it is no longer the drill, then reset.
Without them a killed member is replaced mid-bout, which makes an endurance grind instead.

A group that disengages while you are alive still blocks and counters the whole way home, and only goes quiet
once re-formed, you die or leave, or ten seconds pass. Waiting, backing off, standing and withdrawing all use
`passiveBlockReaction` rather than the tier's reaction beat.

**Stations without a group.** `post` is independent of `squad`, so a single bot of any melee preset can hold one:

```
rc bot summon Defending ArmyLineInfantry Sparring Replace
rc bot cfg <id> post true
rc bot cfg <id> breakoff true
```

That is a 1v1 sparring partner that waits on its mark, resets distance when attacked, and walks back afterwards.

### Reading the opponent

A `Dueling` bot keeps a read on the player it is fighting and uses it for two decisions:

- **Whether to hold this chamber at all**, or throw on the fixed beat like every other AI. It scores its own
  holds: one you break early, by dropping guard or attacking into it, paid off, and one that runs the full
  `holdMax` against your raised guard did not. Turtle against it and it stops holding on you.
- **Whether a dropped guard is an opening or a bait.** If your drops during a hold keep turning into a re-block,
  it stops taking them and waits you out instead.

- **Which way to re-chamber after a feint**, keeping the direction or switching it. It scores both against
  whether the stab that followed actually got through, so a player who has worked out that a feint always
  switches stops being handed that.
- **Whether a feint too long for the distance is shortened or aimed wide**, scored the same way.

Both are odds, not rules. A read that is certain still leaves the bot throwing against type now and then, so
there is no single line that beats it every time.

The read covers one duel. It is built from scratch when the bot picks a new target, and it is gone when the bot
dies. Nothing carries between rounds or between sessions.

`rc bot probe <id>` prints a `MeleeHold` line per released hold, carrying how long it was held, what ended it,
and both live rates. A `released=noHold` line is a chamber the read decided to throw straight.

### Feinting

A `Feinting` bot throws a real stab, cancels it in flight with a block before it can land, and attacks again.
The stab is genuine, so it has to be respected, and cancelling it this way costs none of the recovery a
completed stab does. It exists to measure the timings, so the three levers are the whole behaviour:

```
rc bot summon Defending ArmyLineInfantry Feinting Replace
rc bot cfg <id> feintCount 2
rc bot cfg <id> feintDepth 0.2
rc bot cfg <id> feintDwell 0.15
```

The cancel matches what a player is physically forced to do: hold the block for `feintDwell`, release it, then
attack again. A player cannot change attack direction without blocking first, and going straight from block to
attack interrupts the block pose before it plays, so the feint becomes invisible however long the block is held.

**Distance decides how long a feint can be.** A stab aimed at the player has to be pulled back before the blade
gets there, so the closer the bot stands the shorter its feints. When `feintDepth` asks for more than the
distance allows, the bot chooses: shorten the stab and keep it aimed, or aim it wide on purpose and hold it out
for the full length. A wide one cannot land at all, which is what buys the extra time.

If a feinted stab is cancelled too late and lands on your guard, the bot knows it has lost priority: it drops
the chain and gets its guard up for your riposte rather than carrying on.

`feintDepth` is the dial between the two kinds of feint: short pulls the stab back early, long leaves it out
until the last moment so you have to commit to a block. Too long and the stab lands instead of being cancelled,
which is the boundary worth finding for a given weapon.

The cancel is deliberately as fast as the tick allows: the re-chamber ends the block itself rather than lowering
it first, so no guard-lowering command goes out and nothing waits on the usual minimum guard hold. If you are
actually attacking when the cancel ends, the bot keeps the guard up for real and finishes the feint afterwards.

`rc bot probe <id>` prints a `MeleeFeint` line per cancel. Read it alongside the `MeleeProbe` action echo, which
timestamps what the engine actually received.

### Configurable AI levers

Some AIs expose named **levers** you can tune. A lever starts at its built-in default, is overridden by the global default if one is set, and is overridden again by a per-bot value.

* **Per-bot override**: `rc bot cfg <target> <lever> <value>` (that bot/group only). List with `rc bot cfg <target>`.
* **Global default**: `rc set globalAI <AiType> <lever> <value>`. New bots of that AI start from it. Read with `rc get globalAI <AiType> <lever>`, and persist it in a rotation config with the `SetGlobalAi` config variable.

Changing a global default affects **newly created** bots of that AI, not ones already spawned.

**`StabbingDummy` levers**

| Lever | Values | Default | Meaning |
| --- | --- | --- | --- |
| `stabInterval` | float > 0 (seconds) | `1.7` | Delay between stabs. |
| `stabDirection` | `Random` / `High` / `Low` / `Alternate` | `Random` | Which way each stab is thrown. |

*Example: a slow, high-only dummy.*

```
rc bot summon Defending ArmyLineInfantry
rc bot setBotAi <id> StabbingDummy
rc bot cfg <id> stabInterval 3
rc bot cfg <id> stabDirection High
```

**`RiposteDummy` / `Sparring*` levers**

`RiposteDummy` and the `Sparring` and `Group` difficulty tiers are presets of one melee AI and share the same levers. Within each family the tiers are identical except for their reaction speeds, and the `Group` tiers are the `Sparring` tiers plus `squad` and `post`. Booleans are `true`/`false` (`on`/`off` still accepted as input).

**Toggles & targeting** (the `Sparring` and `Group` tiers share one column):

| Setting | `RiposteDummy` | `Sparring*` / `Group*` | Meaning |
| --- | --- | --- | --- |
| `press` | `false` | `true` | Throw the first blow when the enemy isn't threatening. |
| `riposte` | `true` | `true` | Counter after the guard absorbs a hit. |
| `move` | `false` | `true` | Hold/adjust melee spacing vs. stand its ground. |
| `pursue` | `false` | `true` | Advance toward a target that is too far. `false` lets a player back away and disengage. |
| `stickyTarget` | `false` | `false` | Keep one target while valid, versus re-picking the closest each tick. |
| `targetRange` | `3` | `3` | Only engage players within this many metres, `0` = unlimited. For the `Sparring` tiers this is also the passive read range. |
| `engageOnAttack` | `false` | `true` | Start passive and engage only a player whose attack it blocks, fighting that target until it dies. |

**Difficulty** comes in two halves: reaction beats (how fast a bot answers you) and formation levers `coordinate`, `slotError` and `formationLag` (how well it holds its place beside another bot). The second half matters more in a group. Reactions alone give you a slow pair that still stands in a perfect line and throws perfectly opposite stabs.

Every formation lever is **the worst a bot may be, not how bad it is**. Each roll runs from zero up to the lever, so a bot can come out correct by chance and a lower tier does so less often. A pair that is *usually* too wide has to be read every bout instead of solved once. `squadSpacing` stays at `0.9` for every tier. Only the stray from it changes.

**Reaction beats**: the levers that separate the tiers (`seconds ≥ 0`).

The `Sparring` and `Group` families have separate ladders.

*Sparring family, plus the two standalone presets. `Dueling` reads exactly as `Sparring` does and has no column
of its own until it gains levers of its own:*

| Lever | `Guardian` | `RiposteDummy` | `SparringEasy` | `SparringNormal` | `Sparring` | Meaning |
| --- | --- | --- | --- | --- | --- | --- |
| `blockReactionMin` | `0.5` | `0.1` | `0.3` | `0.1` | `0` | Min delay between reading an attack and raising the guard. The main difficulty knob. |
| `blockReactionMax` | `0.8` | `0.2` | `0.5` | `0.2` | `0` | Max of that delay; each block rolls between min and max. |
| `riposteReactionMin` | `0.4` | `0` | `0.2` | `0` | `0` | Min delay between a block landing and the counter. |
| `riposteReactionMax` | `1.1` | `0.5` | `0.8` | `0.5` | `0` | Max of that delay. |
| `attackReadBeat` | `1.2` | `0.6` | `0.9` | `0.6` | `0.3` | Extra randomised beat on the attack cooldown. Lower presses faster. |

*Group family:*

| Lever | `GroupEasy` | `GroupNormal` | `GroupHard` | `Group` |
| --- | --- | --- | --- | --- |
| `blockReactionMin` / `Max` | `0.1` / `0.2` | `0.1` / `0.2` | `0` / `0` | `0` / `0` |
| `riposteReactionMin` / `Max` | `0` / `0.5` | `0` / `0.5` | `0` / `0.1` | `0` / `0` |
| `attackReadBeat` | `0.1` | `0.3` | `0.3` | `0.3` |
| `slotError` | `0.5` | `0.5` | `0.1` | `0` |
| `formationLag` | `0.2` | `0` | `0` | `0` |
| `coordinate` | `0.97` | `0.98` | `1` | `1` |
| `stabSeparation` | `0.3` | `0.25` | `0.15` | `0` |
| `squadSpacingVariance` | `0.5` | `0.3` | `0.1` | `0` |

**Shared tuning**: the rest of the lever set, grouped by what it does. Same defaults across all these
presets unless the Default column says otherwise (`seconds >= 0` or `metres`, floats).

**Ranges and timing**

| Lever | Default | Meaning |
| --- | --- | --- |
| `offensiveRange` | `0.7` | Close spacing it presses to (metres). |
| `offensiveRangeVariance` | `0.1` | Random jitter added on top of `offensiveRange`. |
| `defensiveRange` | `2.0` | Reading spacing it guards from (metres). |
| `defensiveRangeVariance` | `0.4` | Random jitter added on top of `defensiveRange`. |
| `attackRange` | `2.0` | How close a press attack commits a stab. A riposte ignores this. |
| `riposteWindow` | `0.6` | How long the post-block counter stays available (seconds). |
| `passiveRange` | `0.6` | `Sparring` tiers: hold distance while waiting. Uses `defensiveRange` once engaged. |
| `passiveBlockReaction` | `0` | Block reaction beat while waiting to be provoked, instead of `blockReactionMin`/`Max`. Not randomised. |

**Targeting**

The rest of the targeting set (`targetRange`, `stickyTarget`, `engageOnAttack`) varies by preset and is in the toggles table above. These two do not:

| Lever | Default | Meaning |
| --- | --- | --- |
| `ignoreTeam` | `true` | Target any player regardless of faction. `false` = enemies only. |
| `ignoreBots` | `true` | Target only human players, skipping bots. Defaults to `true` so bots focus the player and don't provoke each other. |

**Guard**

| Lever | Default | Meaning |
| --- | --- | --- |
| `guard` | `false` (`true` for `Guardian`) | Act as an escort for `guardTarget`. |
| `guardTarget` | `0` | The player id this bot escorts, `0` for none. Set automatically by the summon commands. |
| `guardRange` | `10` | An enemy this close to the guarded player pulls the bot into the fight. |
| `guardFollowRange` | `3` | `Guardian`: how far from the guarded player the bot holds station while nothing is happening. |
| `separationRange` | `0.8` for `Sparring*` / `Group*` / `Test`, `1.5` for `Guardian`, `0` otherwise | Push apart from other bots within this many metres. `0` disables. |

**Formation**

| Lever | Default | Meaning |
| --- | --- | --- |
| `squad` | `true` for `Sparring*` / `Group*` / `Test`, `false` otherwise | Stand in a formation with the rest of the spawn batch, and with any other engaged batch on the same player. Spacing and lane discipline only. |
| `coordinate` | `0`–`1`. `0.5` neutral, and the `Sparring*` default. `Group*`: `0.97` Easy, `0.98` Normal, `1` top | The updown axis, decided per swing. `1` always throws opposite to the neighbour, `0` always the same, `0.5` a free pick. Above `0.5` also shares the guard. Needs `squad`. |
| `slotError` | `0.9` SparringEasy, `0.5` SparringNormal / GroupEasy / GroupNormal, `0.1` GroupHard, `0` Sparring and Group | The furthest a bot may stand from its place on the ring, in metres. Re-rolled every few seconds. Needs `squad`. |
| `formationLag` | `1.2` SparringEasy, `0.6` SparringNormal, `0.2` GroupEasy, `0` GroupNormal and top tiers | The longest a bot may work from a stale slot, in seconds. `0` tracks perfectly. Needs `squad`. |
| `stabSeparation` | `0.35` GroupEasy, `0.25` GroupNormal, `0.15` GroupHard, `0` elsewhere | Smallest gap between two opposite stabs from one formation. `0` leaves them unblockable. Needs `squad`. |
| `aimPitch` | `0` | Vertical aim in the engine's pitch scale, `0` level and negative down. Blade geometry follows it automatically. |
| `holdMax` | `0`; `Dueling`: `2.5` | Longest a chambered stab is kept back waiting for the enemy to move, in seconds. The bot only holds against a **raised guard**, and throws the moment it drops or they start a windup, so the hold is a read rather than a delay. Whether a given chamber is held at all, and whether a dropped guard is taken or read as a bait, is decided per opponent from how they have been playing this duel: see [Reading the opponent](#reading-the-opponent). `0` = throw on the fixed beat, which is what every other preset does. |
| `holdMin` | `0.8` | Shortest a hold lasts once begun, in seconds. Without it the bot reads a player who is still raising their guard as unguarded and throws into the gap, so the hold never happens. A windup from the enemy still releases the stab immediately. A hold also never bails into a guard: it answers a new attack by releasing, not by blocking. A feint from the enemy still throws on the fixed beat, since that opening is the one worth taking now. Inert while `holdMax` is `0`. |
| `feintCount` | `0`; `Feinting`: `1` | How many thrown stabs are cancelled in flight before one is allowed to land. `0` = never feint, which is every preset but `Feinting`. |
| `feintDepth` | `0.2` | The longest a stab is left in flight before the block kills it, in seconds, measured from the moment it is thrown. Small values pull the stab back early; large ones leave it out until the last moment, so the defender has to respect it. This is a **maximum**: a stab aimed at the player is shortened to whatever the current distance allows, since the blade needs time to get there. Asking for more than that fits is what makes the bot consider a long feint. Inert while `feintCount` is `0`. |
| `feintDwell` | `0.1` | How long the cancelling block is held before it is released, in seconds. This is the block pose the opponent actually reads, so it is also the dial for making a bot slower and easier to train against: `0.15` to `0.2` are both good. Inert while `feintCount` is `0`. |
| `squadSpacing` | `0.85` | The tightest the line ever stands, and the floor its breathing works up from. |
| `squadSpacingVariance` | `0.7`; `Group*`: `0.5` Easy, `0.3` Normal, `0.1` Hard, `0` Group | How much wider than `squadSpacing` the line may drift mid-fight. Only while engaged; a posted line settles back to the floor. `0` = a fixed gap. Needs `squad`. |
| `laneHalfWidth` | `0.5` | How close a squadmate may be to the swing line before the shot counts as blocked. Keep it under `squadSpacing`. |
| `squadStandoff` | `1.5` | Range the whole line holds from the enemy, measured from its ends rather than its middle, so a wider formation stands its point that much closer. `1.5` is the practical ceiling. |
| | | Only as many as fit hold the line: 3 at the defaults, following `squadStandoff` and `squadSpacing`. The rest close to the same range from whichever side they are already on, with a wider and stronger push off their mates, so they spread into a **ring** around the enemy and can flank or take them from behind. No bearing is assigned; the ring is what the pushback settles into. The nearest hold the line, so running through a formation swaps who is in front. |

**Mate avoidance**

How a bot keeps its own bayonet off the squadmate beside it.

| Lever | Default | Meaning |
| --- | --- | --- |
| `gateRadius` | `0.3` | Half-width used to refuse a stab outright. The only mate-avoidance lever that costs aggression. |
| `clampRadius` | `0.4` | Half-width used to stop the bot turning mid-stab. Costs tracking only, never cancels a swing. |
| `bladeMargin` | `5` | Degrees of slack kept outside a mate's band rather than stopping on its boundary. |
| `mateCrowdRatio` | `1` | A mate closer than this many formation spacings blocks the stab at any bearing ahead. Clamp only, `0` disables. |
| `mateConeFloor` | `28` | The narrowest the danger cone around a squadmate may get, in degrees. Clamp only, never the release gate. |
| `gateOnMate` | `true` | Hold fire while a mate stands in the blade's band, instead of stabbing and hoping. |
| `abortOnMate` | `false` | Block to cancel the bot's own stab when a mate is already across the blade. Unreliable. |

**Stations**

| Lever | Default | Meaning |
| --- | --- | --- |
| `post` | `true` for `Sparring*` / `Group*` / `Test`, `false` otherwise | Make it a drill station: wait on the mark, and walk back afterwards. Independent of `squad`. |
| `breakoff` | `false` (`true` for `Group*`) | Once provoked, give ground and re-form before throwing anything. Needs `post`. |
| `breakoffRange` | `2` | Ground given once, in metres, measured from where the group was provoked rather than held from the player. Needs `breakoff`. |
| `engageDelay` | `0` (`1.5` for `Group*`) | Seconds from the first provocation before the group may swing or counter. It blocks throughout. Needs `post`. |
| `resetRange` | `0` | How far the target may get from the post before the group gives up. `0` = no limit. |
| `minMembers` | `0` (`2` for `Group*`) | Fewest members the batch will fight with; below it the bout ends and the group withdraws. Capped by the batch's own size. Needs `post`. |
| `holdReplacement` | `false` (`true` for `Group*`) | A dead member's replacement waits for the bout to finish. Only for a member killed by the group's own opponent. Needs `post`. |
| `returnDelay` | `30` | Seconds the group holds where the bout ended before walking back to the post. |
## Configurables

*(Defaults shown are tuned for Palisade Arena A1.)*

* **ArenaCorner1**: x,z coordinate of the 1st corner of the arena play area.

  * **args:** `x z` (floats) or none (uses player position)
  * **default:** Not set
* **ArenaCorner2**: x,z coordinate of the 2nd corner of the arena play area.

  * **args:** `x z` (floats) or none (uses player position)
  * **default:** Not set
* **xvxDistance**: Distance between attacking and defending faction lines for `xvx`.

  * **args:** `distance (float)`
  * **default:** `20`
* **xvxSpacing**: Space between each player on a line for `xvx`.

  * **args:** `distance (float)`
  * **default:** `2`
* **xvxStrategy**: Player selection strategy for `xvx`.

  * **args:** `Random | Next | Any | Repeat`
  * **default:** `Random`
* **groupfightDistance**: Distance between attacking and defending faction lines for `groupfight`.

  * **args:** `distance (float)`
  * **default:** `25`
* **groupfightSpacing**: Space between each player on a line for `groupfight`.

  * **args:** `distance (float)`
  * **default:** `2`
* **groupfightStrategy**: Player selection strategy for `groupfight`.

  * **args:** `Random | Repeat`
  * **default:** `Random`
* **openMeleeSpacing**: Minimum distance players can spawn from each other for `openmelee`.

  * **args:** `distance (float)`
  * **default:** `1.5`
* **openMeleeOffset**: Minimum spawn distance from the arena edges in `openmelee`.

  * **args:** `distance (float)`
  * **default:** `7`
* **Orientation**: Direction two lines spawn facing each other.

  * **args:** `degree (int)` or `NorthSouth | EastWest | SouthNorth | WestEast | Random`
  * **default:** `90` (NorthSouth)
* **botDefaultAi**: Default AI behaviour assigned to bots that do not specify one inline.

  * **args:** `None | Manual | StabbingDummy | RiposteDummy | SparringEasy | SparringNormal | Sparring | Dueling | GroupEasy | GroupNormal | Group | Guardian | Test` (see [Bot AI Types](#bot-ai-types))
  * **default:** `None`
* **botDefaultDeathPolicy**: Default death policy assigned to bots that do not specify one inline.

  * **args:** `None | Kick | Replace`
  * **default:** `Kick`
* **botKickDelay**: Seconds to wait after a bot dies before kicking it (allows the kill to register).

  * **args:** `seconds (float)`
  * **default:** `2`
* **botReplaceDelay**: Seconds to wait after kicking a bot before re‑spawning it (clears the slot).

  * **args:** `seconds (float)`
  * **default:** `0.5`
* **globalAI**: Global **default** value for a configurable AI lever (per-bot overrides are the separate `rc bot cfg` command). Reads/writes one lever at a time. See [Configurable AI levers](#configurable-ai-levers).

  * **set args:** `<AiType> <lever> <value>`: e.g. `rc set globalAI StabbingDummy stabInterval 2.5`
  * **get args:** `<AiType> <lever>`: e.g. `rc get globalAI StabbingDummy stabInterval`
  * **default:** each AI's built-in lever values (e.g. `StabbingDummy stabInterval` = `1.7`)
* **lineBotCount**: Default number of bots in a `summonLine` or `spawnLine` when count is not specified inline.

  * **args:** `count (int, > 0)`
  * **default:** `10`
* **lineSpacing**: Lateral spacing in metres between bots in a line. Set so bots will be shoulder to shoulder.

  * **args:** `metres (float, > 0)`
  * **default:** `0.55`

---

## Mod Config Variables

Use **global** `mod_variable` or **per‑map** `mod_variable_local` to set MDS options in rotation configs.

**Format:** `MDS:<ConfigVariable>:<Argument(s)>`

### General

* **EnableDebugLogging**: `true | false`
* **EnableMeleeLog**: `true | false | frames`
* **EnableAdminOnly**: `true | false`

### Arena

* **SetArena**: `(x,z),(x,z)`
* **AddArena**: `(x,z),(x,z)`
* **SetArenaCorner1**: `x,z`
* **SetArenaCorner2**: `x,z`

### Drill

* **SetXvXDistance**: `distance(float)`
* **SetXvXSpacing**: `distance(float)`
* **SetXvXStrategy**: `Random | Next | Any | Repeat`
* **SetGroupfightDistance**: `distance(float)`
* **SetGroupfightSpacing**: `distance(float)`
* **SetGroupfightStrategy**: `Random | Repeat`
* **SetOpenMeleeSpacing**: `distance(float)`
* **SetOpenMeleeOffset**: `distance(float)`
* **SetOrientation**: `degree(int)` **or** `NorthSouth | EastWest | SouthNorth | WestEast | Random`

### Bot

* **SetBotDefaultAi**: `None | Manual | StabbingDummy | RiposteDummy | SparringEasy | SparringNormal | Sparring | Dueling | GroupEasy | GroupNormal | Group | Guardian | Test`
* **SetBotDefaultDeathPolicy**: `None | Kick | Replace`
* **SetBotKickDelay**: `seconds(float)`
* **SetBotReplaceDelay**: `seconds(float)`
* **SetGlobalAi**: `<AiType>,<lever>,<value>`

  Sets a global default for one configurable AI lever at map load, the persistent form of `rc set globalAI`. Repeat it for each lever. Per-bot `rc bot cfg` still overrides. Spaces also work in place of commas.

  *Examples:*

  ```
  mod_variable_local MDS:SetGlobalAi:StabbingDummy,stabInterval,2.5
  mod_variable_local MDS:SetGlobalAi:StabbingDummy,stabDirection,High
  ```
* **SpawnBot**: `x,z,rotation[,faction][,class][,ai][,death][,name[,regtag[,uniformId]]]`

  Schedules a single bot to spawn at a world position when the round begins. Specify it multiple times for multiple bots. Same fields as `SpawnLine` but with no `count`; use `SpawnLine` when you want more than one bot.
  * `x,z,rotation`: required. World position and facing (degrees from North).
  * `faction`: optional. `attacking` (default), `defending`, or a faction name (e.g. `French`). `attacking`/`defending` resolve against the live round at spawn time, so the same config works across maps.
  * `class`: optional. Defaults to `ArmyLineInfantry`.
  * `ai,death`: optional. Default to `botDefaultAi` / `botDefaultDeathPolicy`.
  * `name,regtag,uniformId`: optional identity extras.

  *Examples:*

  ```
  mod_variable_local MDS:SpawnBot:-20,30,90
  mod_variable_local MDS:SpawnBot:12,-4,180,defending,ArmyLineInfantry,Sparring,Replace
  ```

### Line

* **SetLineBotCount**: `count(int)`
* **SetLineSpacing**: `metres(float)`
* **SpawnLine**: `x,z,rotation[,count][,faction][,class][,ai][,death][,name[,regtag[,uniformId]]]`

  Schedules a shoulder‑to‑shoulder bot line to spawn when the round begins. Specify it multiple times for multiple lines (e.g. two opposing lines). Mirrors the `rc spawnLine` grammar, with map‑load defaults instead of a caller:
  * `x,z,rotation`: required. World position and facing (degrees from North).
  * `count`: optional. Defaults to `lineBotCount`.
  * `faction`: optional. `attacking` (default), `defending`, or a faction name (e.g. `French`). `attacking`/`defending` resolve against the live round at spawn time, so the same config works across maps.
  * `class`: optional. Defaults to `ArmyLineInfantry`.
  * `ai,death`: optional. Default to `botDefaultAi` / `botDefaultDeathPolicy`.
  * `name,regtag,uniformId`: optional identity extras.

  *Examples:*

  ```
  mod_variable_local MDS:SpawnLine:-20,30,90
  mod_variable_local MDS:SpawnLine:-20,30,90,French
  mod_variable_local MDS:SpawnLine:20,30,270,10,defending,ArmyLineInfantry,None,Replace,Bot,None,1
  ```

---

## Example Config

```text
# Global
mod_variable MDS:EnableDebugLogging:true
mod_variable MDS:EnableAdminOnly:true

# Map Rotation (per‑map overrides)
mod_variable_local MDS:SetArena:(-40.99,42.69),(-3.35,5.38)
mod_variable_local MDS:SetXvXDistance:20
mod_variable_local MDS:SetXvXSpacing:2
mod_variable_local MDS:SetXvXStrategy:Random
mod_variable_local MDS:SetGroupfightDistance:25
mod_variable_local MDS:SetGroupfightSpacing:2
mod_variable_local MDS:SetGroupfightStrategy:Random
mod_variable_local MDS:SetOpenMeleeSpacing:1.5
mod_variable_local MDS:SetOpenMeleeOffset:7
mod_variable_local MDS:SetOrientation:NorthSouth

# Bots
mod_variable_local MDS:SetBotDefaultAi:Sparring
mod_variable_local MDS:SetBotDefaultDeathPolicy:Replace
mod_variable_local MDS:SetBotKickDelay:2
mod_variable_local MDS:SetBotReplaceDelay:0.5
mod_variable_local MDS:SetGlobalAi:StabbingDummy,stabInterval,2.5
mod_variable_local MDS:SpawnBot:0,0,90,defending,ArmyLineInfantry,Sparring,Replace
mod_variable_local MDS:SpawnLine:-20,30,90,10,attacking,ArmyLineInfantry
mod_variable_local MDS:SpawnLine:20,30,270,10,defending,ArmyLineInfantry,None,Replace,Bot,None,1
```

---

## Future Features

* Automatic repeating drills with user customization
* Modded UI
* Multi-arena support
* More melee AI depth (feints, spins, advanced movement) and difficulty scaling
* Saved preset bundles, config-at-summon, and a supervisor layer for target/session control

---

## Support

Feedback & bugs: open a GitHub issue or DM **@ryanlt** on Discord.
