namespace MDS.Events
{
    public interface IEvent
    {
        EventEnum EventName { get; }
        bool Validate(object[] parameters, out string errorMessage);
        void Trigger(object[] parameters);
    }
}
