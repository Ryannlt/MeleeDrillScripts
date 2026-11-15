using System;
using System.Collections.Generic;
using Holdfast.Modding;
using MDS.Features.Arenas;
using MDS.Features.Drills;
using MDS.Features.ShootingTraining;
using MDS.Features.Status;
using MDS.Shared.Commands;
using MDS.Shared.Config;
using MDS.Shared.Game;
using MDS.Shared.Logging;
using MDS.Shared.Players;
using MDS.Shared.Rounds;
using MDS.Shared.Runtime;
using MDS.Shared.Settings;

namespace MDS
{
    // Nothing outlives the mod instance, so nothing leaks across a map change.
    // What needs the engine comes from the runtime, so the unit tests can build this whole context with their own.
    public sealed class ModContext : IDisposable
    {
        private const string ConfigPrefix = "MDS";

        private readonly List<IDisposable> _features = new();

        public ModContext(ModHandles handles, IRuntime runtime, GameSide side)
        {
            Runtime = runtime;
            Log = new ModLog("MDS", runtime.CreateLogSink());
            Clock = runtime.CreateClock();
            IsServer = side == GameSide.Server;
            Console = new GameConsole(handles.Console, Log, Clock);
            Replies = new Replies(Console, handles.Networking, Log);
            Routines = new Routines(Log, Clock);
            Queries = new PlayerQueries(handles.ServerQueries);
            Round = new RoundTracker();
            Players = new PlayerRegistry(Round, Queries, Log);
            Admins = new AdminRegistry(Players, Log);
            Settings = new SettingsRegistry(Log);
            Commands = new CommandRouter(Replies, Admins, Log);
            Config = new ConfigRouter(ConfigPrefix, Log);

            BuildFeatures();
            LogSide(side);
        }

        public bool IsServer { get; }

        public IRuntime Runtime { get; }

        public ModLog Log { get; }

        public IClock Clock { get; }

        public GameConsole Console { get; }

        public Replies Replies { get; }

        public ClientMessageRouter ClientMessages { get; } = new();

        public Routines Routines { get; }

        public PlayerQueries Queries { get; }

        public RoundTracker Round { get; }

        public PlayerRegistry Players { get; }

        public AdminRegistry Admins { get; }

        public SettingsRegistry Settings { get; }

        public CommandRouter Commands { get; }

        public ConfigRouter Config { get; }

        public void Dispose()
        {
            Routines.StopAll();
            for (int i = _features.Count - 1; i >= 0; i--) _features[i].Dispose();
            _features.Clear();
            Admins.Dispose();
            Players.Dispose();
        }

        // Drills read the arena, so the arena feature is built first.
        private void BuildFeatures()
        {
            Commands.Register(new GetCommand(Settings));
            Commands.Register(new SetCommand(Settings));
            Settings.Register(new DebugLoggingSetting(Log, Replies));
            Config.Register(new DebugLoggingVariable(Log));

            var arena = new ArenaFeature(this);
            _features.Add(arena);
            _features.Add(new DrillsFeature(this, arena.Store));
            _features.Add(new ShootingTrainingFeature(this));
            _features.Add(new StatusFeature(this, arena.Store));
        }

        private void LogSide(GameSide side)
        {
            if (side == GameSide.Unknown) Log.Error("The game did not say which side this is, so MDS stays client side.");
            else Log.Info(IsServer ? "Running on the server." : "Running on a client.");
        }
    }
}
