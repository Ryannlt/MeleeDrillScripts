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
| `rc openmelee [options]` | Counts down 3, 2, 1, then scatters everyone on the field across the arena |
| `rc shootingTraining [on\|off]` | Turns infinite firearm ammo and drawn bullet paths on or off for the current map. With no argument it switches to the other state |
| `rc get <key>` / `rc set <key> <value>` | Reads or changes a [setting](#settings) |

### Leftover Players

When `rc xvx` places its players, anyone else still alive in the box around the new lines, 3 m past the outermost players, goes back to the spawn they came from, if their side may still use it. Players elsewhere in the arena are left alone, so a spawn inside a big arena keeps working. If a spawn is inside that box, they go to the nearest spawn of their side outside it. Players sent to the same spawn stand in a small ring around it. `groupfight` and `openmelee` place everyone, so they skip this.

## Drill Options

Options can be given in order, by name, or both. A strategy or facing word can go anywhere.

```
rc xvx 3 3 next 4 1 northsouth
rc xvx 3 3 eastwest spacing=1
rc xvx 5 2 any distance=12
rc groupfight repeat spacing=1.5
rc groupfight 25
rc openmelee spacing=2 offset=3
rc xvx 2 2 arena=3
```

| Drill | Options in order |
| --- | --- |
| `xvx` | attackers, defenders, strategy, distance, spacing, facing |
| `groupfight` | strategy, distance, spacing, facing |
| `openmelee` | spacing, offset |

Every drill also takes `arena=<n>`, by name only. Without it the drill runs in arena 1.

| Option | Value | Meaning |
| --- | --- | --- |
| `attackers`, `defenders` | whole number | Players on each line |
| `strategy` | see below | How players are picked |
| `distance` | meters | Distance between the two lines |
| `spacing` | meters | Distance between neighbors on a line. For `openmelee`, the least distance between any two players, at least 0.5 |
| `facing` | `NorthSouth`, `EastWest`, `SouthNorth`, `WestEast`, `Random` (or `-1`) or 0 to 359 | Compass direction from the attackers' line to the defenders' |
| `offset` | meters, 0 or more | How far in from the arena edges `openmelee` keeps everyone. 0 uses the arena edge to edge |
| `arena` | an arena number | The arena the drill runs in |

Any option left out uses its setting.

| Strategy | Drills | Picks |
| --- | --- | --- |
| `Random` | xvx, groupfight | Each side at random from its own team |
| `Any` | xvx | At random from everyone on the field, whatever their team |
| `Next` | xvx | The next players in each team's order, carrying on from the last drill |
| `Repeat` | xvx, groupfight | The same players as that drill's last run this round |

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
mod_variable_local MDS:DrillOrientation:NorthSouth
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
