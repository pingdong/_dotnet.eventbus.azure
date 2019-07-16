using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PingDong.EventBus.Azure
{
    public class TopicPublisher : IEventBusPublisher
    {
        private const string IntegrationEventSuffix = "IntegrationEvent";

        private readonly TopicClient _topicClient;

        #region ctor

        public TopicPublisher(IConfiguration config)
        {
            var connection = config["EventBus:ConnectionString"];
            var topic = config["EventBus:Topic"];

            _topicClient = new TopicClient(connection, topic, RetryPolicy.Default);
        }

        #endregion

        #region IEventBus

        public async Task PublishAsync(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name.Replace(IntegrationEventSuffix, "");
            var jsonMessage = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = @event.CorrelationId,
                Label = eventName,
                Body = body,
            };
            
            await _topicClient.SendAsync(message);
        }

        #endregion
    }
}
