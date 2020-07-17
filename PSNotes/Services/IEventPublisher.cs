using PSNotes.Models;

namespace PSNotes.Services
{
    public interface IEventPublisher
    {
        void PublishEvent(Event eventData);
    }
}