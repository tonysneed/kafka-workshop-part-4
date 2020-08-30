using Confluent.Kafka;
using EventStreamProcessing.Abstractions;
using EventStreamProcessing.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

// TODO: Uncomment aliases after creating .proto files in ProtoLibrary/Protos
// using SourcePerson = Protos.Source.v1.person;
// using SinkKey = Protos.Sink.v1.Key;
// using SinkPerson = Protos.Sink.v1.person;
// using Result = Confluent.Kafka.DeliveryResult<Protos.Sink.v1.Key, Protos.Sink.v1.person>;

namespace Worker
{
    class Program
    {
        private static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Add options
                    var brokerOptions = hostContext.Configuration
                        .GetSection(nameof(BrokerOptions))
                        .Get<BrokerOptions>();
                    var consumerOptions = hostContext.Configuration
                        .GetSection(nameof(ConsumerOptions))
                        .Get<ConsumerOptions>();
                    var producerOptions = hostContext.Configuration
                        .GetSection(nameof(ProducerOptions))
                        .Get<ProducerOptions>();
                    services.AddSingleton(brokerOptions);
                    services.AddSingleton(consumerOptions);
                    services.AddSingleton(producerOptions);

                    // Add logger
                    services.AddSingleton<ILogger>(sp =>
                    {
                        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<KafkaWorker>();
                        logger.LogInformation($"Hosting Environment: {hostContext.HostingEnvironment.EnvironmentName}");
                        logger.LogInformation($"Consumer Brokers: {brokerOptions.Brokers}");
                        logger.LogInformation($"Consumer Brokers: {brokerOptions.Brokers}");
                        return logger;
                    });

                    // TODO: Add event processor

                    // Add worker
                    services.AddHostedService<KafkaWorker>();
                });
            return builder;
        }
    }
}