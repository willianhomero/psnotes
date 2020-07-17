using System;
using System.Text;
using PSNotes.Models;
using PSNotes.Models.Settings;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PSNotes.Services
{
    public class EventHubsEventPublisher : IEventPublisher
    {
        private readonly ILogger<EventHubsEventPublisher> _logger;
        private readonly EventHubClient _eventHubClient;

        public EventHubsEventPublisher(IOptions<EventHubsSettings> settings, ILogger<EventHubsEventPublisher> logger)
        {
            _logger = logger;

            try
            {
                // Creates an EventHubsConnectionStringBuilder object from a the connection string, and sets the EntityPath.
                // Typically the connection string should have the Entity Path in it, but for the sake of this simple scenario
                // we are using the connection string from the namespace.
                var connectionStringBuilder = new EventHubsConnectionStringBuilder(settings.Value.ConnectionString)
                {
                    EntityPath = settings.Value.EventHubName
                };

                _eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Couldn't create an instance of EventHubsEventPublisher");
                throw;
            }
            
        }

        public async void PublishEvent(Event eventData)
        {
            try
            {
                string message = JsonConvert.SerializeObject(eventData);

                await _eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(message)));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Couldn't publish an event to Event Hubs");
                throw;
            }
        }
    }
}
