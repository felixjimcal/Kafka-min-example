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
                collection.AddHostedService<KafkaCosnumerHostedService>();
                collection.AddHostedService<KafkaProducerHostedService>();
            });

        public class KafkaCosnumerHostedService : IHostedService
        {
            private readonly ILogger<KafkaCosnumerHostedService> _logger;
            private ClusterClient _cluster;

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
                _cluster.ConsumeFromLatest(Topics.topicProducts);
                _cluster.ConsumeFromLatest(Topics.topicCategories);

                return Task.CompletedTask;
            }

            private void ProcessMessage(dynamic record)
            {
                try
                {
                    User message = JsonSerializer.Deserialize<User>(Encoding.UTF8.GetString(record.Value as byte[]));
                    if (message != null)
                    {
                        var processor = MessageProcessorFactory.GetProcessor(message.Topic);
                        processor.ProcessMessage(message);
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
                    var myObject = new User
                    {
                        Id = i,
                        Name = Faker.StarWars.Character(),
                        Topic = i % 2 == 0 ? Topics.topicProducts : Topics.topicCategories
                    };

                    string jsonMessage = JsonSerializer.Serialize(myObject);
                    _logger.LogInformation("Producer sending: {jsonMessage}", jsonMessage);

                    await _producer.ProduceAsync(myObject.Topic, new Message<Null, string> { Value = jsonMessage }, cancellationToken);
                }

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
            void ProcessMessage(User message);
        }

        public class ProductMessageProcessor : ITypeMessageProcessor
        {
            public void ProcessMessage(User user)
            {
                Console.WriteLine($"Consuming mensaje de topic: {user.Topic}, {user.Name} con Id: {user.Id}");
            }
        }

        public class CategoryMessageProcessor : ITypeMessageProcessor
        {
            public void ProcessMessage(User user)
            {
                Console.WriteLine($"Consuming mensaje de topic: {user.Topic}, {user.Name} con Id: {user.Id}");
            }
        }

        public class MessageProcessorFactory
        {
            public static ITypeMessageProcessor GetProcessor(string topic)
            {
                return topic switch
                {
                    Topics.topicProducts => new ProductMessageProcessor(),
                    Topics.topicCategories => new CategoryMessageProcessor(),
                    _ => throw new ArgumentException("No hay un procesador definido para este topic", topic),
                };
            }
        }

        public class User
        {
            public string? Name { get; set; }
            public string? Topic { get; set; }
            public int Id { get; set; }
        }

        public static class Topics
        {
            public const string topicProducts = "Products";
            public const string topicCategories = "Categories";
        }
    }
}