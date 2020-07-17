using PSNotes.StatsProcessor.Models.Settings;
using PSNotes.StatsProcessor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System;
using System.IO;

namespace PSNotes.StatsProcessor
{
    public class Program
    {
        private readonly ILogger _logger;
        private readonly IEventConsumer _eventConsumer;

        public Program()
        {
            // Configuring configuration module
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Configuring logging
            var serviceCollection = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder
                        .AddConfiguration(configuration.GetSection("Logging"))
                        .AddConsole(logger => { logger.IncludeScopes = false; });
                });

            // Configuring dependency injection
            serviceCollection.Configure<EventHubsSettings>(configuration.GetSection("Events"));
            serviceCollection.AddSingleton<IEventConsumer, EventHubsEventConsumer>();

            // Build the service collection for dependency injection
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            // getting the logger using the class's name is conventional
            _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            // getting the event consumer
            _eventConsumer = serviceProvider.GetRequiredService<IEventConsumer>();
        }

        public static void Main(string[] args)
        {
            new Program().Execute();
        }

        public void Execute()
        {
            // Starting event consumer
            _eventConsumer.Start();
            
            Console.WriteLine("\r\nPress [enter] to stop the event consumer.\r\n\r\n");
            Console.ReadLine();
            
            // Stopping event consumer
            _eventConsumer.Stop();

            Console.WriteLine("\r\nPress [enter] to exit.\r\n\r\n");
            Console.ReadLine();
        }
    }
}