using System.Collections.Generic;

// The EventDispatcher handles custom events by triggering predefined actions when a specific "event" is called.

namespace MDS.Events
{
    public static class EventDispatcher
    {
        private static readonly Dictionary<EventEnum, IEvent> eventRegistry = new();

        // Static constructor ensures events are registered at startup
        static EventDispatcher()
        {
            RegisterAllEvents();
        }

        // Registers all predefined events
        private static void RegisterAllEvents()
        {
            Register(EventEnum.TestEvent, new TestEvent());
            Register(EventEnum.OpenMelee, new OpenMeleeEvent());
            Register(EventEnum.XvX, new XvXEvent());
            Register(EventEnum.ShootingTraining, new ShootingTrainingEvent());

            Logger.Log($"Registered {eventRegistry.Count} predefined events.", LogLevel.INFO);
        }

        public static void Register(EventEnum eventName, IEvent handler)
        {
            if (eventRegistry.ContainsKey(eventName))
            {
                Logger.Log($"Event {eventName} is already registered.", LogLevel.DEBUG);
            }

            eventRegistry[eventName] = handler;
            Logger.Log($"Registered event: {eventName}", LogLevel.INFO);
        }

        public static bool Trigger(EventEnum eventName, object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!eventRegistry.TryGetValue(eventName, out var handler))
            {
                errorMessage = $"Attempted to trigger unregistered event: {eventName}";
                Logger.Log(errorMessage, LogLevel.WARNING);
                return false;
            }

            if (!handler.Validate(parameters, out errorMessage))
            {
                Logger.Log($"Validation failed for event '{eventName}': {errorMessage}", LogLevel.WARNING);
                return false;
            }

            handler.Trigger(parameters);
            return true;
        }
    }
}
