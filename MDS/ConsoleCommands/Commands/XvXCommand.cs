using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MDS.ConfigVariables;
using MDS.Systems;
using MDS.Core;
using MDS.Events;

namespace MDS.ConsoleCommands
{
    public class XvXCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.XvX;

        private static int _attackingIterator = 0;
        private static int _defendingIterator = 0;

        private static List<IPlayer> _lastAttackers;
        private static List<IPlayer> _lastDefenders;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            var arena = ArenaManager.Arena;
            if (arena == null || arena.Min == arena.Max)
            {
                errorMessage = "Arena is not properly defined. Set arena bounds.";
                return false;
            }

            if (parameters.Length < 2)
            {
                errorMessage = "Missing arguments. Usage: rc xvx <attackers:int> <defenders:int> [strategy] [distance:float] [spacing:float] [orientation:float|OrientationEnum]";
                return false;
            }

            if (!int.TryParse(parameters[0], out int attackers) || attackers <= 0 ||
                !int.TryParse(parameters[1], out int defenders) || defenders <= 0)
            {
                errorMessage = "Attacker and defender counts must be positive integers.";
                return false;
            }

            // Parse and validate strategy
            SelectionStrategyType strategy = ((XvXStrategyConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.XvXStrategy)).Strategy;
            if (parameters.Length >= 3)
            {
                string strategyInput = parameters[2];
                if (int.TryParse(strategyInput, out _))
                {
                    errorMessage = $"Invalid strategy '{strategyInput}'. Must be a name like Random, Next, Any, Repeat.";
                    return false;
                }

                if (!Enum.TryParse(strategyInput, true, out strategy))
                {
                    errorMessage = $"Invalid strategy '{strategyInput}'. Valid options: Random, Any, Next, Repeat.";
                    return false;
                }
            }

            // Validate strategy usage rules
            var attackersList = StateTracker.AttackingPlayers;
            var defendersList = StateTracker.DefendingPlayers;
            var allPlayers = StateTracker.AllPlayers;

            if (strategy == SelectionStrategyType.Repeat)
            {
                if (_lastAttackers == null || _lastDefenders == null)
                {
                    errorMessage = "Cannot use 'Repeat' strategy. No previous player selection stored.";
                    return false;
                }
                if (_lastAttackers.Count != attackers || _lastDefenders.Count != defenders)
                {
                    errorMessage = "Repeat selection mismatch: player counts don't match last XvX.";
                    return false;
                }
            }
            else if (strategy == SelectionStrategyType.Any)
            {
                if (allPlayers.Count < attackers + defenders)
                {
                    errorMessage = $"Not enough players available. Needed {attackers + defenders}, but only {allPlayers.Count} players.";
                    return false;
                }
            }
            else
            {
                if (attackersList.Count < attackers)
                {
                    errorMessage = $"Not enough attackers. Needed {attackers}, but only {attackersList.Count} available.";
                    return false;
                }
                if (defendersList.Count < defenders)
                {
                    errorMessage = $"Not enough defenders. Needed {defenders}, but only {defendersList.Count} available.";
                    return false;
                }
            }

            // Optional float validation
            if (parameters.Length >= 4 && (!float.TryParse(parameters[3], out float distance) || distance <= 0f))
            {
                errorMessage = $"Invalid line distance '{parameters[3]}'. Must be a positive number.";
                return false;
            }

            if (parameters.Length >= 5 && (!float.TryParse(parameters[4], out float spacing) || spacing <= 0f))
            {
                errorMessage = $"Invalid inter spacing '{parameters[4]}'. Must be a positive number.";
                return false;
            }

            if (parameters.Length >= 6)
            {
                string orientationInput = parameters[5];
                bool validOrientation =
                    (Enum.TryParse(orientationInput, true, out OrientationEnum _)) ||
                    (float.TryParse(orientationInput, out float parsedAngle) && (parsedAngle >= 0f && parsedAngle < 360f)) ||
                    orientationInput.Equals("Random", StringComparison.OrdinalIgnoreCase);

                if (!validOrientation)
                {
                    errorMessage = $"Invalid orientation '{orientationInput}'. Must be an OrientationEnum or degree between 0 and 359.";
                    return false;
                }
            }

            return true;
        }


        public void Execute(int playerId, string[] parameters)
        {
            int attackers = int.Parse(parameters[0]);
            int defenders = int.Parse(parameters[1]);

            var strategyConfig = (XvXStrategyConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.XvXStrategy);
            SelectionStrategyType strategy = strategyConfig.Strategy; // Use configured default

            float lineDistance = ((XvXDistanceConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.XvXDistance)).XvXDistance;
            float intraSpacing = ((XvXSpacingConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.XvXSpacing)).XvXSpacing;
            float orientation = ((OrientationConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.Orientation)).OrientationAngle;

            if (parameters.Length >= 3)
                Enum.TryParse(parameters[2], true, out strategy);
            if (parameters.Length >= 4)
                float.TryParse(parameters[3], out lineDistance);
            if (parameters.Length >= 5)
                float.TryParse(parameters[4], out intraSpacing);
            if (parameters.Length >= 6)
            {
                if (Enum.TryParse(parameters[5], true, out OrientationEnum parsedEnum))
                {
                    orientation = parsedEnum == OrientationEnum.Random ? -1f : (float)parsedEnum;
                }
                else if (float.TryParse(parameters[5], out float parsedAngle))
                {
                    orientation = parsedAngle;
                }
            }

            List<IPlayer> attackersList = new();
            List<IPlayer> defendersList = new();

            switch (strategy)
            {
                case SelectionStrategyType.Random:
                    (attackersList, defendersList) = SelectionStrategy.Random(
                        StateTracker.AttackingPlayers.ToList(),
                        StateTracker.DefendingPlayers.ToList(),
                        attackers,
                        defenders
                    );
                    break;

                case SelectionStrategyType.Any:
                    (attackersList, defendersList) = SelectionStrategy.Any(
                        StateTracker.AllPlayers.ToList(),
                        attackers,
                        defenders
                    );
                    break;

                case SelectionStrategyType.Next:
                    (attackersList, defendersList) = SelectionStrategy.Next(
                        StateTracker.AttackingPlayers.ToList(),
                        StateTracker.DefendingPlayers.ToList(),
                        attackers,
                        defenders,
                        ref _attackingIterator,
                        ref _defendingIterator
                    );

                    // Shuffle to avoid repeated placements over extended calls
                    attackersList = attackersList.OrderBy(_ => Guid.NewGuid()).ToList();
                    defendersList = defendersList.OrderBy(_ => Guid.NewGuid()).ToList();
                    break;


                case SelectionStrategyType.Repeat:
                    attackersList = _lastAttackers;
                    defendersList = _lastDefenders;
                    break;
            }

            _lastAttackers = attackersList;
            _lastDefenders = defendersList;

            var min = ArenaManager.Arena.Min;
            var max = ArenaManager.Arena.Max;
            Vector2 center = new Vector2((min.x + max.x) / 2f, (min.y + max.y) / 2f);

            object[] eventParams = new object[] { attackersList, defendersList, center, lineDistance, intraSpacing, orientation };
            bool success = EventDispatcher.Trigger(EventEnum.XvX, eventParams, out string errorMessage);

            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                Logger.Log($"Failed to trigger XvXEvent: {errorMessage}", LogLevel.WARNING);
                Feedback.Reply(playerId, errorMessage);
            }
        }

        public static void Reset()
        {
            _attackingIterator = 0;
            _defendingIterator = 0;
            _lastAttackers = null;
            _lastDefenders = null;
        }
    }
}
