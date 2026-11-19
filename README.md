# MDS (Melee Drill Scripts)

*Commands and scripts to run and automate melee drills in **Holdfast: Nations At War***

---

## Command Quick Reference

Run every command as a logged-in server admin, prefixed with `rc`.

```
rc arena corner1
rc arena corner2
rc arena 2 corner1
rc arena show
rc xvx 3 3
rc xvx 3 3 arena=2
rc xvx 3 3 eastwest spacing=1
rc groupfight
rc openmelee
rc openmelee spread
rc popcorn
rc popcorn 2v2 4v4 distance=12
rc popcorn tight
rc popcorn late
rc customdrill
rc customdrill flank
rc customdrill flank turn=random
rc customdrill flank create next
rc customdrill flank add attacker (-5,0)
rc customdrill flank add attacker here
rc preview openmelee groups
rc preview customdrill flank
rc program
rc program pits create next balance
rc program pits add xvx 2 1 (4,5,6,7) rounds 3
rc program pits run
rc program stop
rc shootingTraining
rc mds
```

---

## Quick Start

1. Set the arena. Stand in one corner and run `rc arena corner1`, then stand in the opposite corner and run `rc arena corner2`. Or put `mod_variable_local MDS:Arena:(x,z),(x,z)` in the map's rotation.
2. For more arenas, put the arena number first: `rc arena 2 corner1` and `rc arena 2 corner2`, or `MDS:Arena:2:(x,z),(x,z)`.
3. Run a drill: `rc xvx 3 3`, `rc groupfight` or `rc openmelee`. Add `arena=2` to run it in arena 2.
4. For bots, load CarbonPlayer AI next to MDS. See [Bots](#bots).

Commands and keys are not case sensitive.

---

## Commands

| Command | What it does |
| --- | --- |
| `rc mds` | Shows the round, the arenas and how many players are on each side |
| `rc mds round` | Shows the round, the map, the mode and both factions |
| `rc mds players [all\|attacking\|defending\|spectator] [count]` | Lists the players in a group in your F2 log. With `count`, replies with the number only |
| `rc mds player <id>` | Shows one player: name, admin, faction, alive, position |
| `rc arena` | Lists every arena that is set |
| `rc arena <n>` | Shows arena n |
| `rc arena [n] corner1 [x z]` | Sets the first corner of arena n, or arena 1 without a number. Without coordinates it uses where you stand |
| `rc arena [n] corner2 [x z]` | Sets the opposite corner |
| `rc arena show [on\|off]` | Paints every arena on the ground: a thin border that fades inward and the arena number in the middle, one color per arena. Only you see it. With no argument it switches to the other state |
| `rc xvx <attackers> <defenders> [options]` | Places two lines of picked players facing each other across the arena center. Anyone else standing where the lines go is sent back to their spawn |
| `rc groupfight [options]` | Places each whole team in a line, facing the other |
| `rc openmelee [options]` | Counts down, then places everyone on the field across the arena with a [spawn strategy](#open-melee-strategies) |
| `rc popcorn [options]` | Counts down, then splits both teams into small fights of mixed sizes across the arena. See [Popcorn](#popcorn) |
| `rc customdrill` | Lists the custom drills by number. See [Custom Drills](#custom-drills) |
| `rc customdrill <name\|number> [options]` | Runs a custom drill: picked players on its static spots, then everyone else in its formation. Anyone else standing there is sent back to their spawn |
| `rc customdrill <name> create [settings]` | Makes a custom drill. It lasts until the next map |
| `rc customdrill <name> set <settings>` | Changes its settings: `[strategy] [turn] [countdown on\|off] [godmode on\|off] [godModeSeconds]` |
| `rc customdrill <name> add <part>` | Adds a spot or a formation. See [Custom Drills](#custom-drills) |
| `rc customdrill <name> edit <n> <part>` | Replaces part n |
| `rc customdrill <name> remove <n>` | Removes part n |
| `rc customdrill <name> show` | Shows the drill as the lines that make it |
| `rc customdrill <name> delete` | Deletes the drill until the next map |
| `rc preview <drill> [options]` | Shows where a drill would put players, without moving anyone: yellow rings for attackers, cyan for defenders, and a white border round the area `openmelee` draws from. Only you see it. Open melee and a random facing draw again when the drill runs, so this is one sample. It stays until the next preview |
| `rc preview off` | Clears the preview |
| `rc shootingTraining [on\|off]` | Turns infinite firearm ammo and drawn bullet paths on or off for the current map. With no argument it switches to the other state |
| `rc program` | Lists the programs, or says how the running one is doing. See [Programs](#programs) |
| `rc program <name>` | Shows a program as the lines that make it |
| `rc program <name> create [settings]` | Makes a program. It lasts until the next map |
| `rc program <name> set <settings>` | Changes the settings every drill shares. `name=<new>` renames it |
| `rc program <name> add <drill>` | Adds a drill at the end |
| `rc program <name> edit <n> <drill>` | Replaces drill n |
| `rc program <name> move <n> <to>` | Moves drill n to place `to` |
| `rc program <name> remove <n>` | Removes drill n |
| `rc program <name> try <n>` | Runs drill n once, for one round |
| `rc program <name> run [loop]` | Runs the program, and with `loop` starts it over after the last drill |
| `rc program <name> delete` | Deletes the program until the next map |
| `rc program stop` | Stops the running program. Fights already running carry on |
| `rc get <key>` / `rc set <key> <value>` | Reads or changes a [setting](#settings) |

### Leftover Players

When `rc xvx` places its players, anyone else still alive in the box around the new lines, 3 m past the outermost players, goes back to the spawn they came from, if their side may still use it. Players elsewhere in the arena are left alone, so a spawn inside a big arena keeps working. If a spawn is inside that box, they go to the nearest spawn of their side outside it. They land at random spots around it, at least 1.2 m from each other and from anyone already standing there, so players cleared again and again never land inside each other. The game only tells a mod where a spawn is, not its box or nodes, so the spots spread over a circle that grows with the group. `groupfight` and `openmelee` place everyone, so they skip this.

## Drill Options

Options can be given in order, by name, or both. A strategy or facing word can go anywhere. A bare number fills the next option that takes a number, in the order below, facing included. So `rc openmelee teams 90` sets spacing to 90, and an angle goes by name: `rc openmelee teams facing=90`.

```
rc xvx 3 3 next 4 1 northsouth
rc xvx 3 3 eastwest spacing=1
rc xvx 5 2 any distance=12
rc groupfight repeat spacing=1.5
rc groupfight 25
rc openmelee spacing=2 offset=3
rc openmelee groups 4
rc openmelee teams eastwest
rc xvx 2 2 arena=3
```

| Drill | Options in order |
| --- | --- |
| `xvx` | attackers, defenders, strategy, distance, spacing, facing, mirror |
| `groupfight` | strategy, distance, spacing, facing |
| `openmelee` | strategy, spacing, offset, facing, teleport |
| `popcorn` | strategy, min, max, distance, lineDistance, spacing, teleport |
| `customdrill` | name, strategy, mirror, teleport. `turn` goes by name only |

Every drill also takes `arena=<n>`. Without it the drill runs in arena 1. A whole number after every other option also sets the arena, so `rc xvx 3 3 random 20 2 northsouth 2` runs in arena 2, but `rc xvx 3 3 2` sets the distance.

| Option | Value | Meaning |
| --- | --- | --- |
| `attackers`, `defenders` | whole number | Players on each line |
| `strategy` | see below | How players are picked. For `openmelee`, how they are spread over the arena. For `popcorn`, how the fights are |
| `distance` | meters | Distance between the two lines. For `popcorn`, the least distance between players of different fights |
| `spacing` | meters | Distance between neighbors on a line. For `openmelee`, the least distance between any two players, at least 0.5. With `groups`, the least distance between players of different groups |
| `facing` | `NorthSouth`, `EastWest`, `SouthNorth`, `WestEast`, `Random` (or `-1`) or 0 to 359 | Compass direction from the attackers' line to the defenders'. For `openmelee teams`, from the attackers' half to the defenders' |
| `offset` | meters, 0 or more | How far in from the arena edges `openmelee` keeps everyone. 0 uses the arena edge to edge |
| `arena` | an arena number | The arena the drill runs in |
| `min`, `max` | attackers v defenders, such as `2v1` | The smallest and largest `popcorn` fight. Only the total counts |
| `lineDistance` | meters | Distance between a `popcorn` fight's two lines |
| `name` | a custom drill's name or number | The custom drill to run |
| `turn` | 0 to 359, or `Random`, by name only: `turn=90` | Turns a whole custom drill around the arena center for this run |
| `teleport` | `early` or `late` | When a drill with a countdown moves everyone. `early` moves them as the countdown starts and holds them still until it ends, so they can find their fight. `late` moves them as it ends. Popcorn is `early` without it, open melee and custom drills `late` |
| `mirror` | the word `mirror` | Swaps which team takes which side: `rc xvx 2 1 mirror` puts 1 attacker against 2 defenders, and a custom drill gives its attacker spots and line to the defenders and the other way round |

Any option left out uses its setting.

`xvx` and `groupfight` keep both lines inside the arena. If `distance` would put someone outside it, the lines move closer, down to 0.5 m. If the lines are still too long for the arena, `spacing` shrinks as well. The drill tells you what it changed, such as `Shrunk to fit arena 3: distance 6 instead of 10.`, and `rc preview` shows the same. An arena too small even at 0.5 m is refused.

| Strategy | Drills | Picks |
| --- | --- | --- |
| `Random` | xvx, groupfight, customdrill | Each side at random from its own team |
| `Any` | xvx, customdrill | At random from everyone on the field, whatever their team |
| `Next` | xvx, customdrill | The next players in each team's order, carrying on from the last drill |
| `Repeat` | xvx, groupfight, customdrill | The same players as that drill's last run this round |

`Random` and `Any` remember where each player stood in the last line this round and try a few shuffles to put everyone somewhere new. A player can still land in the same place, just much less often.

### Open Melee Strategies

| Strategy | Places |
| --- | --- |
| `Scatter` | Everyone in uneven pockets grown from one random spot, so fights start at once. The default |
| `Spread` | Everyone evenly over the whole arena, as far apart as the player count allows, for regroup practice. Spacing is only the least gap |
| `Groups` | Both teams shuffled into groups of 2 to 4 that stand close together, with the groups spread over the arena. Each group needs a 4 m box, so a narrow arena runs out of room for the boxes before the spacing matters |
| `Teams` | Each team scattered over its own half, split across the facing, attackers on the half the facing points away from. Each half stays half the spacing back from the middle line |
| `Random` | One of the four above, drawn again each run. The log and `rc preview` name the one drawn |

A bare `random` after `rc openmelee` picks the Random strategy. For a random facing, write `facing=random`.

`Scatter` and `Teams` refuse a spacing too fine for a big arena. `Spread` and `Groups` take any spacing of 0.5 m or more.

Open melee and popcorn count down before anyone moves. If players join during the countdown and the arena has no room for all of them, the ones without room stay where they are and whoever ran the drill is told.

### Custom Drills

A custom drill has static spots and one formation. Each static spot holds one picked player. The formation takes everyone else on the field. Make a drill with config lines or with `rc customdrill <name> create` and `add`, then run it with `rc customdrill <name>` or its number from `rc customdrill`.

```text
mod_variable_local MDS:CustomDrill:flank:attacker,(-5,-1),(-5,1)
mod_variable_local MDS:CustomDrill:flank:defender,(5,0)
mod_variable_local MDS:CustomDrill:flank:spread,spacing=2
mod_variable_local MDS:CustomDrill:flank:strategy,next
mod_variable_local MDS:CustomDrill:wall:line,attacker,(0,-8)
mod_variable_local MDS:CustomDrill:wall:line,defender,(0,8),spacing=1.5
mod_variable_local MDS:CustomDrill:wall:countdown,on
mod_variable_local MDS:CustomDrill:wall:godmode,on
```

Each line is one of these, its parts separated by commas. A line may also start with `add,`. Positions are `(x,z)`, meters from the arena center on the same axes as the arena corners. Lines with the same name build one drill, numbered in the order the names first appear. In rc the same parts go after `add`, with spaces: `rc customdrill flank add line defender (0,8) 1.5`.

| Line | What it does |
| --- | --- |
| `attacker,(x,z)` or `defender,(x,z)` | Static spots for that side, one player each. More positions can follow: `attacker,(-5,-1),(-5,1)` |
| `scatter`, `spread`, `groups`, `teams` or `random` | An open melee formation: everyone not on a static spot, spread over the arena. It takes the same options as `rc openmelee`, in order or by name: `spread,2,3` or `spread,spacing=2,offset=3` |
| `line,<attacker\|defender>,(x,z)` | A line formation for one team: every player of that team not on a static spot, in a line centered on the position. Spacing and facing follow, in order or by name: `line,defender,(0,8),1.5,eastwest`. Without spacing it uses `groupfightSpacing`. Without a facing the line faces the middle of the other team's spots and line |
| `strategy,<random\|any\|next\|repeat>` | How the static spots are picked when `rc customdrill` gives no strategy. Random without it |
| `turn,<degrees\|random>` | How far the drill turns when `rc customdrill` gives no turn. 0 without it |
| `countdown,<on\|off>` | Counts down before anyone moves. Off without it |
| `godmode,<on\|off>` | God mode while the players land. Off without it |
| `godModeSeconds,<seconds>` | How long god mode lasts. 2 without it |

- The static spots fill first, attackers in the order they were written and defenders likewise. Each drill remembers its own picks, so `next` and `repeat` carry on from that drill's last run.
- A drill has one formation. A later formation part replaces the earlier one and takes its place in the list, except that a line for the other team joins a line. Without a formation, only the static spots are filled.
- `turn=90` turns the whole drill a quarter turn around the arena center, the same way facing goes from 0 to 90. `turn=random` draws a new turn every run. A turn goes by name only, so a bare number after the name picks the arena: `rc customdrill flank 2` runs in arena 2.
- A drill whose static spots or lines would leave the arena shrinks toward the arena center until everyone fits, and says so: `Shrunk to fit arena 2: 64% of its size.` An arena too small for its lines even at 0.5 m spacing refuses the drill. An open melee formation stays inside the arena on its own.
- How close the drill puts its players is up to the drill. Spots and lines can share ground.
- A name can't be a number or a pick word (`random`, `any`, `next`, `repeat`), so `rc customdrill next flank` and `rc customdrill flank next` both work.
- In rc, `here` in place of a position is where you stand, measured from the center of the arena you stand in. Inside nested arenas the smallest one counts: `rc customdrill flank add attacker here`.
- Settings go in order without names, and `-` keeps one as it is: `rc customdrill flank set next - on` sets the strategy and the countdown. Each also works by name: `godmode=on`.
- A drill made in rc can be tried with `rc preview customdrill flank` before it goes in the config. Drills made this way last until the next map.
- A bad config line is skipped, and the server log says why: `Invalid value 'flank:attacker' for MDS:CustomDrill: attacker needs a position, like attacker,(-5,0).`

### Programs

A program runs drills one after another. It is also the way to run a drill in several arenas at once, such as as many 2v1s as the players allow in small arenas drawn inside a big one.

```text
mod_variable_local MDS:Arena:4:(-40.99,5.38),(-22.17,24.04)
mod_variable_local MDS:Arena:5:(-22.17,5.38),(-3.35,24.04)
mod_variable_local MDS:Arena:6:(-40.99,24.04),(-22.17,42.69)
mod_variable_local MDS:Arena:7:(-22.17,24.04),(-3.35,42.69)
mod_variable_local MDS:Program:pits:set,next,balance
mod_variable_local MDS:Program:pits:add,xvx,2,1,(4,5,6,7),rolling,minutes,5
mod_variable_local MDS:Program:pits:add,groupfight,(1),rounds,2
```

A config line is the rc words with commas: `set,<settings>` or `add,<drill>`. Lines with the same name build one program, in the order they are written. Values go in order without names, and `-` keeps one at its default. Each value also works by name, such as `pause=3`.

**Settings** apply to every drill that doesn't set its own:

| Setting | What it does |
| --- | --- |
| `random`, `any`, `next` or `repeat` | How drills pick their players. `next`, a round robin, without it |
| `fill` or `balance` | `fill` runs every fight as written: in a 2v1 the attackers are always the 2. `balance` chooses each fight's sides to play the most people, so a 2v1 can run as a 1v2. `fill` without it |
| `together` or `rolling` | `together` starts a new round once every fight of the last round is over. `rolling` starts an arena's next fight as soon as its last one is over. `together` without it |
| pause | The first number. Seconds of countdown before each drill. 5 without it |
| fight time | The second number. How long a fight can last, 0 for no limit. No limit without it |
| `loop` or `once` | Whether the program starts over after its last drill. `once` without it |

**A drill** is the drill and its options as `rc` takes them, then its arenas and its own timing: `xvx 2 1 8 (4,5,6,7) rolling each 2`.

| Part | What it does |
| --- | --- |
| `(4,5,6,7)` | Runs it in each of those arenas, as players allow. `(1)` or no arenas runs it in one |
| `with` | Runs it at the same time as the drill above, on the players that drill leaves free. It takes that drill's timing |
| `random`, `any`, `next` or `repeat` | Its own pick. Not for drills that take everyone |
| `together` or `rolling` | Its own way of starting fights. Not for drills that take everyone |
| `round` | Ends after one fight in every arena. The default, except with `rolling` |
| `rounds <n>` | Ends after n rounds. A round is one fight in every arena. Not with `rolling` |
| `each <n>` | Ends once every player on the field has fought n times. The default with `rolling` is `each 1`. Not for drills that take everyone |
| `minutes <n>` | No new fight starts after n minutes |
| pause, fight time | The two numbers after the end, as in the settings: `rounds 3 2 60` |

- Each drill uses as many of its arenas as the free players allow, so the same program works with 3 players a side or 20. With 3 attackers and 5 defenders, 2v1s in 4 arenas are 1 fight with `fill` and 2 fights with `balance`. The rest wait their turn, and `next` brings whoever has waited longest in first.
- Nobody is in two fights at once. A drill that takes everyone, such as `groupfight`, takes everyone not already in another fight, and runs in one arena.
- A fight is over when one side has all died since it started, or when its time limit, if it has one, is up.
- A drill ends when its last fight is over, and the next one is announced on screen.
- Only one program runs at a time. A new round stops it.
- `rc program <name> run` checks every drill in every arena first, and says which would fail before anything starts.
- `rc program pits try 2` runs drill 2 once, for one round, to try it out.
- Programs made in rc last until the next map.
- A bad config line is skipped, and the server log says why.

### Popcorn

`rc popcorn` counts down, then deals both teams into fights and places every fight as two short lines facing each other, each turned a random way. Everyone is moved as the countdown starts and held still until it ends, so they can find their fight, and god mode stays on until a second after it ends. With `late` they are moved as it ends, under a second of god mode. Only one countdown drill runs at a time, so `rc openmelee` and `rc popcorn` wait for each other.

1. Each fight draws a size between `min` and `max` and splits it as evenly as it can: 2v1, 2v2, 3v2 and so on. Small fights are common and each player more makes a fight rarer, and about two fights in three are uneven. With 2v1 to 4v4 on a 10v10 field, about 43% are 2v1, 32% 2v2, 16% 3v2, 5% 3v3, 4% 4v3 and 1% 4v4. The odd player goes to the team with more players left.
2. Fights are drawn until no fight fits the players left. They join an even fight or one where their side is behind, even past `max`, so everyone on the field fights.
3. Set `max` to suit the server: 4v4 suits 10v10 and under, and a bigger field can take 5v5. The panel offers 1v1 to 6v6.
4. Fights stand at least `distance` apart and wholly inside the arena. Each fight takes room for its own size, so one big fight doesn't spread the rest out. If the arena has no room for them all, popcorn uses the largest `distance` that fits and tells you. If they don't fit even at 0.5 m, the drill is refused and says so.

| Strategy | Places the fights |
| --- | --- |
| `Scatter` | In uneven clumps grown from one random spot |
| `Spread` | Evenly over the whole arena, as far apart as the count allows. The default |
| `Tight` | Packed together around a random spot, as close as `distance` allows |
| `Random` | One of the three above, drawn again each run |

`rc preview popcorn` shows one draw and names the strategy and the fight sizes.

## Settings

Each setting is changed with `rc set`, read with `rc get`, and can be set from the server config under the same name with the `MDS:` prefix.

| Key | Default | Value |
| --- | --- | --- |
| `xvxDistance` | 15 | meters |
| `xvxSpacing` | 1.5 | meters |
| `xvxStrategy` | Next | Random, Any, Next, Repeat |
| `groupfightDistance` | 25 | meters |
| `groupfightSpacing` | 1.5 | meters |
| `groupfightStrategy` | Random | Random, Repeat |
| `openMeleeSpacing` | 1.5 | meters |
| `openMeleeOffset` | 2 | meters, 0 or more |
| `openMeleeStrategy` | Scatter | Scatter, Spread, Groups, Teams, Random |
| `popcornMin` | 2v1 | attackers v defenders |
| `popcornMax` | 4v4 | attackers v defenders |
| `popcornDistance` | 6 | meters |
| `popcornLineDistance` | 6 | meters |
| `popcornSpacing` | 1.5 | meters |
| `popcornStrategy` | Spread | Scatter, Spread, Tight, Random |
| `DrillOrientation` | NorthSouth | a facing |
| `countdownSeconds` | 3 | whole seconds, 1 to 30. How long open melee, popcorn and custom drills count down |
| `MDSDebugLogging` | false | true, false |

The defaults are tuned for Palisade Arena A1.

## Config Variables

Use `mod_variable` for every map or `mod_variable_local` inside a map rotation. Format: `MDS:<Key>:<Value>`.

| Key | Value |
| --- | --- |
| `EnableDebugLogging` | `true` or `false` |
| `Arena` | `(x,z),(x,z)` for arena 1, or `<n>:(x,z),(x,z)` for arena n |
| `ArenaCorner1`, `ArenaCorner2` | `x,z` for arena 1, or `<n>:x,z` for arena n |
| `CustomDrill` | `<name>:<line>`, one line per spot, formation or setting. See [Custom Drills](#custom-drills) |
| `Program` | `<name>:set,<settings>` or `<name>:add,<drill>`, one line each. See [Programs](#programs) |
| any key from [Settings](#settings) except `MDSDebugLogging` | as in the table |

```text
mod_variable MDS:EnableDebugLogging:true

# Map rotation
mod_variable_local MDS:Arena:(-40.99,42.69),(-3.35,5.38)
mod_variable_local MDS:Arena:2:(-3.35,42.69),(33.80,5.38)
mod_variable_local MDS:XvXDistance:20
mod_variable_local MDS:XvXSpacing:2
mod_variable_local MDS:XvXStrategy:Random
mod_variable_local MDS:GroupfightDistance:25
mod_variable_local MDS:GroupfightSpacing:2
mod_variable_local MDS:GroupfightStrategy:Random
mod_variable_local MDS:OpenMeleeSpacing:1.5
mod_variable_local MDS:OpenMeleeOffset:2
mod_variable_local MDS:OpenMeleeStrategy:Scatter
mod_variable_local MDS:PopcornMin:2v1
mod_variable_local MDS:PopcornMax:4v4
mod_variable_local MDS:PopcornDistance:6
mod_variable_local MDS:PopcornLineDistance:6
mod_variable_local MDS:PopcornSpacing:2
mod_variable_local MDS:PopcornStrategy:Spread
mod_variable_local MDS:DrillOrientation:NorthSouth
mod_variable_local MDS:CustomDrill:flank:attacker,(-5,-1),(-5,1)
mod_variable_local MDS:CustomDrill:flank:defender,(5,0)
mod_variable_local MDS:CustomDrill:flank:spread
```

---

## Bots

Bots live in their own mod, CarbonPlayer AI. Load it next to MDS to train against them. MDS moves its bots like any other player.

CarbonPlayer AI owns `bot`, `summonLine`, `summonLineAt`, `spawnLine` and `meleelog`, the keys `botDefaultAi`, `botDefaultDeathPolicy`, `botKickDelay`, `botReplaceDelay`, `globalAI`, `lineBotCount` and `lineSpacing`, and every config variable with the `CarbonPlayerAI:` prefix.

---

## Changes From Earlier Versions

| Before | Now |
| --- | --- |
| `rc 3v3` | `rc xvx 3 3` |
| `rc set ArenaCorner1 [x z]`, `rc set ArenaCorner2 [x z]` | `rc arena corner1 [x z]`, `rc arena corner2 [x z]` |
| `rc get Round`, `rc get Player <id>`, `rc get Players ...` | `rc mds round`, `rc mds player <id>`, `rc mds players ...` |
| `rc set Orientation` | `rc set DrillOrientation` |
| `rc set DebugLogging` | `rc set MDSDebugLogging` |
| `rc test` | removed |
| `rc set AdminOnly`, `MDS:EnableAdminOnly` | removed. Every command is for admins |
| `MDS:SetArena`, `MDS:SetXvXDistance` and the other `MDS:Set...` keys | `MDS:Arena`, `MDS:XvXDistance` and the rest without `Set` |
| `openMeleeOffset` counted twice | counted once. Set it to 4 to keep the old spread |
| `MDS:AddArena` | `MDS:Arena:2:(x,z),(x,z)` |
| `programPause`, `programFightSeconds` | the pause and fight time in each program's settings |
| `MDS:Program:<name>:step,...` and drill lines | `set,<settings>` and `add,<drill>` lines. Each drill has its own timing |
| `rc program run <name>`, `add`, `show`, `undo`, `remove` | `rc program <name> run`, `add`, a name alone, `remove <n>`, `delete` |
| `rc customdrill add <name> <line>`, `show`, `remove` | `rc customdrill <name> add <part>`, `show`, `delete` |

MDS logs `Unknown config variable` for any old key it still gets.

---

## Support

Feedback and bugs: open a GitHub issue or DM **@ryanlt** on Discord.
