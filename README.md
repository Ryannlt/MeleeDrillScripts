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

**Runtime Config**

```
rc get xvxDistance
rc get Players all
rc set xvxDistance 5
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
3. **Add bots** by loading CarbonPlayer AI next to MDS. See **[Bots](#bots)**.

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
  ```

---

## Bots

Bots live in their own mod, CarbonPlayer AI. Load it next to MDS to spawn and train against them. MDS sees its bots as ordinary players, so `xvx`, `groupfight` and `openmelee` move them like anyone else.

The `rc` commands kept their names and moved with the bots: `bot`, `summonLine`, `summonLineAt`, `spawnLine` and `meleelog`, plus the `rc set` and `rc get` keys `botDefaultAi`, `botDefaultDeathPolicy`, `botKickDelay`, `botReplaceDelay`, `globalAI`, `lineBotCount` and `lineSpacing`.

Their config variables moved to the `CarbonPlayerAI:` prefix. For each of these, swap `MDS:` for `CarbonPlayerAI:` and keep the rest of the line: `EnableMeleeLog`, `SetBotDefaultAi`, `SetBotDefaultDeathPolicy`, `SetBotKickDelay`, `SetBotReplaceDelay`, `SetGlobalAi`, `SpawnBot`, `SetLineBotCount`, `SetLineSpacing` and `SpawnLine`. MDS logs `Unknown config command key` for any old line it still gets.

---

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

---

## Mod Config Variables

Use **global** `mod_variable` or **per‑map** `mod_variable_local` to set MDS options in rotation configs.

**Format:** `MDS:<ConfigVariable>:<Argument(s)>`

### General

* **EnableDebugLogging**: `true | false`
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
```

---

## Future Features

* Automatic repeating drills with user customization
* Modded UI
* Multi-arena support

---

## Support

Feedback & bugs: open a GitHub issue or DM **@ryanlt** on Discord.
