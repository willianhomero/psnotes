using System;
using PSNotes.StatsProcessor.Models.Settings;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PSNotes.StatsProcessor.Services
{
    public class EventHubsEventConsumer : IEventConsumer
    {
        private readonly ILogger<EventHubsEventConsumer> _logger;
        private readonly EventProcessorHost _processorHost;
        
        public EventHubsEventConsumer(IOptions<EventHubsSettings> settings, ILogger<EventHubsEventConsumer> logger)
        {
            _logger = logger;

            Console.WriteLine("Registering EventProcessor...");

            _processorHost = new EventProcessorHost(
                settings.Value.EventHubName,
                PartitionReceiver.DefaultConsumerGroupName,
                settings.Value.ConnectionString,
                settings.Value.StorageConnectionString,
                settings.Value.StorageContainerName);
        }

        public void Start()
        {
            _logger.LogInformation("Starting event consumer");

            try
            {
                // Registers the Event Processor Host and starts receiving messages
                _processorHost.RegisterEventProcessorAsync<SimpleEventProcessor>().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Couldn't start event processor");
                throw;
            }
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping event consumer");

            try
            {
                // Disposes of the Event Processor Host
                _processorHost.UnregisterEventProcessorAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Couldn't stop event processor");
                throw;
            }
        }
    }
}
