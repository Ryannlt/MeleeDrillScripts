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
rc drill
rc drill flank
rc drill flank turn=random
rc drill add flank attacking 2 at -5,0
rc preview openmelee groups
rc preview drill flank
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
| `rc openmelee [options]` | Counts down 3, 2, 1, then places everyone on the field across the arena with a [spawn strategy](#open-melee-strategies) |
| `rc popcorn [options]` | Counts down 3, 2, 1, then splits both teams into small fights of mixed sizes across the arena. See [Popcorn](#popcorn) |
| `rc drill` | Lists the custom drills by number. See [Custom Drills](#custom-drills) |
| `rc drill <name\|number> [options]` | Runs a custom drill: its groups stand where the drill says, around the arena center. Anyone else standing there is sent back to their spawn |
| `rc drill add <name> <line>` | Adds a group, a strategy or a turn to a custom drill, and makes the drill if the name is new. It lasts until the next map |
| `rc drill show <name\|number>` | Shows a custom drill as the lines that make it |
| `rc drill remove <name\|number>` | Removes a custom drill until the next map |
| `rc preview <drill> [options]` | Shows where a drill would put players, without moving anyone: yellow rings for attackers, cyan for defenders, and a white border round the area `openmelee` draws from. Only you see it. Open melee and a random facing draw again when the drill runs, so this is one sample. It stays until the next preview |
| `rc preview off` | Clears the preview |
| `rc shootingTraining [on\|off]` | Turns infinite firearm ammo and drawn bullet paths on or off for the current map. With no argument it switches to the other state |
| `rc get <key>` / `rc set <key> <value>` | Reads or changes a [setting](#settings) |

### Leftover Players

When `rc xvx` places its players, anyone else still alive in the box around the new lines, 3 m past the outermost players, goes back to the spawn they came from, if their side may still use it. Players elsewhere in the arena are left alone, so a spawn inside a big arena keeps working. If a spawn is inside that box, they go to the nearest spawn of their side outside it. Players sent to the same spawn stand in a small ring around it. `groupfight` and `openmelee` place everyone, so they skip this.

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
| `xvx` | attackers, defenders, strategy, distance, spacing, facing |
| `groupfight` | strategy, distance, spacing, facing |
| `openmelee` | strategy, spacing, offset, facing |
| `popcorn` | strategy, min, max, distance, lineDistance, spacing |
| `drill` | name, strategy. `turn` goes by name only |

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

Any option left out uses its setting.

`xvx` and `groupfight` keep both lines inside the arena. If `distance` would put someone outside it, the lines move closer, down to 0.5 m. If the lines are still too long for the arena, `spacing` shrinks as well. The drill tells you what it changed, such as `Shrunk to fit arena 3: distance 6 instead of 10.`, and `rc preview` shows the same. An arena too small even at 0.5 m is refused.

| Strategy | Drills | Picks |
| --- | --- | --- |
| `Random` | xvx, groupfight, drill | Each side at random from its own team |
| `Any` | xvx, drill | At random from everyone on the field, whatever their team |
| `Next` | xvx, drill | The next players in each team's order, carrying on from the last drill |
| `Repeat` | xvx, groupfight, drill | The same players as that drill's last run this round |

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

A custom drill is a set of groups, each standing at its own spot around the arena center. Make one with config lines or with `rc drill add`, then run it with `rc drill <name>` or its number from `rc drill`.

```text
mod_variable_local MDS:Drill:flank:attacking 2 at -5,0
mod_variable_local MDS:Drill:flank:defending 1 at 5,0 facing eastwest
mod_variable_local MDS:Drill:flank:strategy next
mod_variable_local MDS:Drill:wedge:attacking 3 at 0,-6 spacing 1.5
mod_variable_local MDS:Drill:wedge:defending 4 at 0,6 scatter
mod_variable_local MDS:Drill:wedge:turn random
```

Each line is one of these. Lines with the same name build one drill, numbered in the order the names first appear.

| Line | What it does |
| --- | --- |
| `<attacking\|defending> <count> at <x>,<z>` | A group of that side, `x` and `z` meters from the arena center on the same axes as the arena corners |
| `... facing <direction>` | The way the group's line faces: `NorthSouth`, `EastWest`, `SouthNorth`, `WestEast`, 0 to 359 or `Random`. Without it the line faces the middle of the other side's groups, or the arena center when the drill has one side only |
| `... spacing <m>` | Distance between neighbors in the group, 2 m without it, at least 0.5 |
| `... line` or `... scatter` | A line across the facing, the default, or a loose pocket around the spot |
| `strategy <random\|any\|next\|repeat>` | How the drill picks its players when `rc drill` gives no strategy. Random without it |
| `turn <degrees\|random>` | How far the drill turns when `rc drill` gives no turn. 0 without it |

- Attackers fill the attacking groups in the order they were written, and defenders the defending ones. Each drill remembers its own picks, so `next` and `repeat` carry on from that drill's last run.
- `turn=90` turns the whole drill a quarter turn around the arena center, the same way facing goes from 0 to 90. `turn=random` draws a new turn every run. A turn goes by name only, so a bare number after the name picks the arena: `rc drill flank 2` runs in arena 2.
- A name can't be a number or a pick word (`random`, `any`, `next`, `repeat`), so `rc drill next flank` and `rc drill flank next` both work. Each side of a drill holds at most 100 players.
- A drill whose groups stand two players under 0.5 m apart is refused, whatever the arena.
- A drill wider than its arena shrinks toward the arena center until everyone fits, and says so: `Shrunk to fit arena 2: 64% of its size.` An arena too small even at 0.5 m spacing refuses the drill.
- `rc drill add flank attacking 2 at -5,0` takes the same lines as the config, so a drill can be built and tried with `rc preview drill flank` before it goes in the config. Drills added this way last until the next map.
- A bad config line is skipped, and the server log says why: `Invalid value 'flank:attacking two at 0,0' for MDS:Drill: 'two' isn't a count from 1 to 100.`

### Popcorn

`rc popcorn` counts down 3, 2, 1, then deals both teams into fights and places every fight as two short lines facing each other, each turned a random way. God mode is on for a second as the fights land. Only one countdown drill runs at a time, so `rc openmelee` and `rc popcorn` wait for each other.

1. Each fight draws a size between `min` and `max` and splits it as evenly as it can: 2v1, 2v2, 3v2 and so on. Small fights are common and each player more makes a fight rarer, and about two fights in three are uneven. With 2v1 to 4v4 on a 10v10 field, about 43% are 2v1, 32% 2v2, 16% 3v2, 5% 3v3, 4% 4v3 and 1% 4v4. The odd player goes to the team with more players left.
2. Fights are drawn until no fight fits the players left. They join an even fight or one where their side is behind, even past `max`, so everyone on the field fights.
3. Set `max` to suit the server: 4v4 suits 10v10 and under, and a bigger field can take 5v5.
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
| `xvxDistance` | 20 | meters |
| `xvxSpacing` | 2 | meters |
| `xvxStrategy` | Random | Random, Any, Next, Repeat |
| `groupfightDistance` | 25 | meters |
| `groupfightSpacing` | 2 | meters |
| `groupfightStrategy` | Random | Random, Repeat |
| `openMeleeSpacing` | 1.5 | meters |
| `openMeleeOffset` | 2 | meters, 0 or more |
| `openMeleeStrategy` | Scatter | Scatter, Spread, Groups, Teams, Random |
| `popcornMin` | 2v1 | attackers v defenders |
| `popcornMax` | 4v4 | attackers v defenders |
| `popcornDistance` | 6 | meters |
| `popcornLineDistance` | 6 | meters |
| `popcornSpacing` | 2 | meters |
| `popcornStrategy` | Spread | Scatter, Spread, Tight, Random |
| `DrillOrientation` | NorthSouth | a facing |
| `MDSDebugLogging` | false | true, false |

The defaults are tuned for Palisade Arena A1.

## Config Variables

Use `mod_variable` for every map or `mod_variable_local` inside a map rotation. Format: `MDS:<Key>:<Value>`.

| Key | Value |
| --- | --- |
| `EnableDebugLogging` | `true` or `false` |
| `Arena` | `(x,z),(x,z)` for arena 1, or `<n>:(x,z),(x,z)` for arena n |
| `ArenaCorner1`, `ArenaCorner2` | `x,z` for arena 1, or `<n>:x,z` for arena n |
| `Drill` | `<name>:<line>`, one line per group. See [Custom Drills](#custom-drills) |
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
mod_variable_local MDS:Drill:flank:attacking 2 at -5,0
mod_variable_local MDS:Drill:flank:defending 1 at 5,0
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

MDS logs `Unknown config variable` for any old key it still gets.

---

## Support

Feedback and bugs: open a GitHub issue or DM **@ryanlt** on Discord.
