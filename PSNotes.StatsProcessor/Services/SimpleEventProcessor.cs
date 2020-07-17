using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;

namespace PSNotes.StatsProcessor.Services
{
    public class SimpleEventProcessor : IEventProcessor
    {
        private PerformanceCounter _totalOperations;
        private PerformanceCounter _operationsPerSecond;
        private PerformanceCounter _averageDuration;
        private PerformanceCounter _averageDurationBase;
        private const string PerfCounterCategoryName = "Pluralsight";

        public SimpleEventProcessor()
        {
            CreatePerformanceCountersCategory();
            CreatePerformanceCounters();
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine(
                $"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine(
                $"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine(
                $"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                Stopwatch watch = new Stopwatch();

                // Increment worker thread operations and operations / second.
                _totalOperations.Increment();
                _operationsPerSecond.Increment();

                // Start a stop watch on the worker thread work.
                watch.Start();

                if (eventData?.Body.Array != null)
                {
                    var data = Encoding.UTF8.GetString(
                        eventData.Body.Array,
                        eventData.Body.Offset,
                        eventData.Body.Count
                    );
                    Console.WriteLine(
                        $"Message received. Partition: '{context.PartitionId}', Data: '{data}'");
                }

                // Capture the stop point.
                watch.Stop();

                // Figure out average duration based on the stop watch,
                // then increment the base counter.
                _averageDuration.IncrementBy(watch.ElapsedTicks);
                _averageDurationBase.Increment();
            }

            return context.CheckpointAsync();
        }

        private void CreatePerformanceCountersCategory()
        {
            // Create a new category of perf counters
            try
            {
                if (!PerformanceCounterCategory.Exists(
                    PerfCounterCategoryName))
                {
                    var counters = new CounterCreationDataCollection();

                    // 1. counter for counting totals: PerformanceCounterType.NumberOfItems32
                    var totalOps = new CounterCreationData
                    {
                        CounterName = "# events processed",
                        CounterHelp = "Total number of events processed",
                        CounterType = PerformanceCounterType.NumberOfItems32
                    };
                    counters.Add(totalOps);

                    // 2. counter for counting operations per second:
                    //        PerformanceCounterType.Rate
                    var opsPerSecond = new CounterCreationData
                    {
                        CounterName = "# events processed per sec",
                        CounterHelp = "Number of events processed per second",
                        CounterType = PerformanceCounterType.RateOfCountsPerSecond32
                    };
                    counters.Add(opsPerSecond);

                    // 3. counter for counting average time per operation:
                    //                 PerformanceCounterType.AverageTimer32
                    var avgDuration = new CounterCreationData
                    {
                        CounterName = "average time per event processing",
                        CounterHelp = "Average duration per event processing",
                        CounterType = PerformanceCounterType.AverageTimer32
                    };
                    counters.Add(avgDuration);

                    // 4. base counter for counting average time
                    //         per operation: PerformanceCounterType.AverageBase
                    // NOTE: BASE counter MUST come after the counter for which it is the base!
                    var avgDurationBase = new CounterCreationData
                    {
                        CounterName = "average time perevent processing base",
                        CounterHelp = "Average duration per event processing base",
                        CounterType = PerformanceCounterType.AverageBase
                    };
                    counters.Add(avgDurationBase);


                    // create new category with the counters above
                    PerformanceCounterCategory.Create(PerfCounterCategoryName,
                        "Counters related to Azure Worker Thread",
                        PerformanceCounterCategoryType.SingleInstance,
                        counters);
                }
            }
            catch (Exception exp)
            {
                Trace.TraceError("Exception creating performance counters " + exp);
            }
        }

        private void CreatePerformanceCounters()
        {
            try
            {
                // create counters to work with
                _totalOperations = new PerformanceCounter
                {
                    CategoryName = PerfCounterCategoryName,
                    CounterName = "# events processed",
                    MachineName = ".",
                    ReadOnly = false
                };

                _operationsPerSecond = new PerformanceCounter
                {
                    CategoryName = PerfCounterCategoryName,
                    CounterName = "# events processed per sec",
                    MachineName = ".",
                    ReadOnly = false
                };

                _averageDuration = new PerformanceCounter
                {
                    CategoryName = PerfCounterCategoryName,
                    CounterName = "average time per event processing",
                    MachineName = ".",
                    ReadOnly = false
                };

                _averageDurationBase = new PerformanceCounter
                {
                    CategoryName = PerfCounterCategoryName,
                    CounterName = "average time perevent processing base",
                    MachineName = ".",
                    ReadOnly = false
                };
            }
            catch (Exception exp)
            {
                Trace.TraceError("Exception creating performance counters " + exp);
            }
        }
    }
}
