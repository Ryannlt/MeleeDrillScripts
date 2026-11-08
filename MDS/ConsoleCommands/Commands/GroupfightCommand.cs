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
    public class GroupfightCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.Groupfight;

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

            if (parameters.Length > 4)
            {
                errorMessage = "Too many arguments. Usage: rc groupfight [strategy] [distance:float] [spacing:float] [orientation:float|OrientationEnum]";
                return false;
            }

            var attackers = StateTracker.AttackingPlayers;
            var defenders = StateTracker.DefendingPlayers;

            if (attackers.Count == 0 || defenders.Count == 0)
            {
                errorMessage = "Both teams must have players for a groupfight.";
                return false;
            }

            if (parameters.Length >= 1)
            {
                string strategyInput = parameters[0];
                if (int.TryParse(strategyInput, out _))
                {
                    errorMessage = $"Invalid strategy '{strategyInput}'. Strategy must be a name like Random or Repeat.";
                    return false;
                }

                if (!EnumParser.TryParseEnumStrict(strategyInput, out SelectionStrategyType strategy))
                {
                    errorMessage = $"Invalid strategy '{strategyInput}'. Valid options: Random, Repeat.";
                    return false;
                }

                if (strategy != SelectionStrategyType.Random && strategy != SelectionStrategyType.Repeat)
                {
                    errorMessage = $"Unsupported strategy '{strategy}'. Only 'Random' and 'Repeat' are supported in groupfight.";
                    return false;
                }

                if (strategy == SelectionStrategyType.Repeat)
                {
                    if (_lastAttackers == null || _lastDefenders == null)
                    {
                        errorMessage = "Cannot use 'Repeat' strategy. No previous player selection stored.";
                        return false;
                    }
                }
            }

            if (parameters.Length >= 2)
            {
                if (!float.TryParse(parameters[1], out float distance) || distance <= 0f)
                {
                    errorMessage = $"Invalid line distance '{parameters[1]}'. Must be a positive number.";
                    return false;
                }
            }

            if (parameters.Length >= 3)
            {
                if (!float.TryParse(parameters[2], out float spacing) || spacing <= 0f)
                {
                    errorMessage = $"Invalid spacing '{parameters[2]}'. Must be a positive number.";
                    return false;
                }
            }

            if (parameters.Length == 4)
            {
                string orientationInput = parameters[3];
                bool validOrientation =
                    (EnumParser.TryParseEnumStrict(orientationInput, out OrientationEnum _)) ||
                    (float.TryParse(orientationInput, out float parsedAngle) && parsedAngle >= 0f && parsedAngle < 360f) ||
                    (orientationInput.Equals("Random", StringComparison.OrdinalIgnoreCase));

                if (!validOrientation)
                {
                    errorMessage = $"Invalid orientation '{orientationInput}'. Must be a valid enum (e.g., NorthSouth, EastWest, Random) or a degree between 0 and 359.";
                    return false;
                }
            }

            return true;
        }


        public void Execute(int playerId, string[] parameters)
        {
            var strategyConfig = (GroupfightStrategyConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.GroupfightStrategy);
            SelectionStrategyType strategy = strategyConfig.Strategy; // Default from configurable

            float lineDistance = ((GroupfightDistanceConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.GroupfightDistance)).GroupfightDistance;
            float intraSpacing = ((GroupfightSpacingConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.GroupfightSpacing)).GroupfightSpacing;
            float orientationDegrees = ((OrientationConfigurable)ConfigurableRegistry.Get(ConfigurableEnum.Orientation)).OrientationAngle;

            if (parameters.Length >= 1)
                Enum.TryParse(parameters[0], true, out strategy);
            if (parameters.Length >= 2)
                float.TryParse(parameters[1], out lineDistance);
            if (parameters.Length >= 3)
                float.TryParse(parameters[2], out intraSpacing);
            if (parameters.Length == 4)
            {
                if (Enum.TryParse(parameters[3], true, out OrientationEnum orientationEnum))
                {
                    orientationDegrees = (orientationEnum == OrientationEnum.Random) ? -1f : (float)orientationEnum;
                }
                else if (float.TryParse(parameters[3], out float parsedAngle))
                {
                    orientationDegrees = parsedAngle;
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
                        StateTracker.AttackingPlayers.Count,
                        StateTracker.DefendingPlayers.Count
                    );
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

            object[] eventParams = new object[] { attackersList, defendersList, center, lineDistance, intraSpacing, orientationDegrees };
            bool success = EventDispatcher.Trigger(EventEnum.XvX, eventParams, out string errorMessage);

            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                Logger.Log($"Failed to trigger GroupfightEvent: {errorMessage}", LogLevel.WARNING);
                Feedback.Reply(playerId, errorMessage);
            }
        }


        public static void Reset()
        {
            _lastAttackers = null;
            _lastDefenders = null;
        }
    }
}
