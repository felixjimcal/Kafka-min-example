using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace AFDragonball
{
    public class FunctionDragonball
    {
        [FunctionName("FunctionDragonball")]
        public void ConsoleConsumer([KafkaTrigger("kafka:9092", "DragonBall", ConsumerGroup = "$Default", AuthenticationMode = BrokerAuthenticationMode.Plain)] KafkaEventData<string>[] kafkaEvents, ILogger logger)
        {
            foreach (var kafkaEvent in kafkaEvents)
                logger.LogInformation(kafkaEvent.Value.ToString());
        }
    }
}