using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace FunctionApp1
{
    public class Function1
    {
        [FunctionName(nameof(ConsoleConsumer))]
        public void ConsoleConsumer(
        [KafkaTrigger(
            "localhost:9092",
            "StarWars",
            ConsumerGroup = "$Default",
            AuthenticationMode = BrokerAuthenticationMode.Plain)] KafkaEventData<string>[] kafkaEvents,
            ILogger logger)
        {
            foreach (var kafkaEvent in kafkaEvents)
                logger.LogInformation(kafkaEvent.Value.ToString());
        }
    }
}