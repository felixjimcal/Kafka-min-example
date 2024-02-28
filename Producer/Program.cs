using Confluent.Kafka;
using FakerDotNet;
using Kafka.Public;
using Kafka.Public.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Example
{
    public class Example
    {
        private static void Main(string[] args)
        {
            CreateHostbuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostbuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureServices((context, collection) =>
            {
                // Adds hosted services for both consumer and producer
                collection.AddHostedService<KafkaCosnumerHostedService>();
                collection.AddHostedService<KafkaProducerHostedService>();
            });

        // Hosted service for consuming messages from Kafka
        public class KafkaCosnumerHostedService : IHostedService
        {
            private readonly ILogger<KafkaCosnumerHostedService> _logger;
            private readonly ClusterClient _cluster;

            public KafkaCosnumerHostedService(ILogger<KafkaCosnumerHostedService> logger)
            {
                _logger = logger;
                _cluster = new ClusterClient(new Configuration
                {
                    Seeds = "localhost:9092"
                }, new ConsoleLogger());
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                _cluster.MessageReceived += (record) =>
                {
                    try
                    {
                        ProcessMessage(record);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message");
                    }
                };

                // Subscribing on topics
                _cluster.ConsumeFromLatest(Topics.topicSW);
                _cluster.ConsumeFromLatest(Topics.topicDB);

                return Task.CompletedTask;
            }

            private void ProcessMessage(dynamic record)
            {
                try
                {
                    // Deserialize the message from JSON
                    string jsonMessage = Encoding.UTF8.GetString(record.Value as byte[]);
                    if (!string.IsNullOrWhiteSpace(jsonMessage))
                    {
                        // Gets the appropriate processor based on the message topic
                        var processor = MessageProcessorFactory.GetProcessor(record.Topic);
                        processor.ProcessMessage(jsonMessage);
                    }
                    else
                    {
                        _logger.LogWarning("Message deserialization returned null");
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing message");
                }
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                _cluster?.Dispose();
                return Task.CompletedTask;
            }
        }

        // Hosted service for producing messages to Kafka
        public class KafkaProducerHostedService : IHostedService
        {
            private readonly ILogger<KafkaProducerHostedService> _logger;
            private IProducer<Null, string> _producer;

            public KafkaProducerHostedService(ILogger<KafkaProducerHostedService> logger)
            {
                _logger = logger;
                var config = new ProducerConfig
                {
                    BootstrapServers = "localhost:9092"
                };
                _producer = new ProducerBuilder<Null, string>(config).Build();
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                for (var i = 0; i < 100; i++)
                {
                    dynamic message;
                    if (i % 2 == 0) // Creates messages alternating between two fictional characters
                    {
                        message = new SWCharacter
                        {
                            Id = i,
                            Name = Faker.StarWars.Character(),
                            Planet = Faker.StarWars.Planet(),
                            Topic = Topics.topicSW
                        };
                    }
                    else
                    {
                        message = new DBCharacter
                        {
                            Id = i,
                            Name = Faker.DragonBall.Character(),
                            Topic = Topics.topicDB
                        };
                    }

                    // Serializes the message to JSON and sends it to Kafka
                    string jsonMessage = JsonSerializer.Serialize(message);
                    _logger.LogInformation("Producer sending: {jsonMessage}", jsonMessage);

                    await _producer.ProduceAsync(message.Topic, new Message<Null, string> { Value = jsonMessage }, cancellationToken);
                }

                // Ensures all messages are sent before shutting down
                _producer.Flush(cancellationToken);
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                _producer?.Dispose();
                return Task.CompletedTask;
            }
        }

        public interface ITypeMessageProcessor
        {
            void ProcessMessage(dynamic message);
        }

        public class StarWarsMessageProcessor : ITypeMessageProcessor
        {
            public void ProcessMessage(dynamic message)
            {
                var item = JsonSerializer.Deserialize<SWCharacter>(message);
                Console.WriteLine($"Consuming mensaje de topic: {item.Topic}, {item.Name} con Id: {item.Id} del planeta {item.Planet}");
            }
        }

        public class DragonBallMessageProcessor : ITypeMessageProcessor
        {
            public void ProcessMessage(dynamic message)
            {
                var item = JsonSerializer.Deserialize<DBCharacter>(message);
                Console.WriteLine($"Consuming mensaje de topic: {item.Topic}, {item.Name} con Id: {item.Id}");
            }
        }

        public class MessageProcessorFactory
        {
            public static ITypeMessageProcessor GetProcessor(string topic)
            {
                return topic switch
                {
                    Topics.topicSW => new StarWarsMessageProcessor(),
                    Topics.topicDB => new DragonBallMessageProcessor(),
                    _ => throw new ArgumentException("No hay un procesador definido para este topic", topic),
                };
            }
        }

        public class SWCharacter
        {
            public string? Planet { get; set; }
            public string? Topic { get; set; }
            public string? Name { get; set; }
            public int Id { get; set; }
        }

        public class DBCharacter
        {
            public string? Topic { get; set; }
            public string? Name { get; set; }
            public int Id { get; set; }
        }

        public static class Topics
        {
            public const string topicSW = "StarWars";
            public const string topicDB = "DragonBall";
        }
    }
}