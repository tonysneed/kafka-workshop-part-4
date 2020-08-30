using EventStreamProcessing.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

// TODO: Uncomment alias
// using Result = Confluent.Kafka.DeliveryResult<Protos.Sink.v1.Key, Protos.Sink.v1.person>;

namespace Worker
{
    public class KafkaWorker : BackgroundService
    {
        // TODO: Uncomment after creating .proto files in ProtoLibrary/Protos
        // private readonly IEventProcessorWithResult<Result> eventProcessor;
        // private readonly ILogger logger;

        // public KafkaWorker(IEventProcessorWithResult<Result> eventProcessor, ILogger logger)
        // {
        //     this.eventProcessor = eventProcessor;
        //     this.logger = logger;
        // }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // TODO: Uncomment after uncommenting fields and constructor
                // logger.LogInformation($"Worker processing event at: {DateTimeOffset.Now}");

                // TODO: Process event stream and record delivery result
            }
        }
    }
}
