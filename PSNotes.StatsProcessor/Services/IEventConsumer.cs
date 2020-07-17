namespace PSNotes.StatsProcessor.Services
{
    public interface IEventConsumer
    {
        void Start();
        void Stop();
    }
}