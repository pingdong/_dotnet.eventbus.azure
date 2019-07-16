using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PingDong.EventBus.Azure
{
    public class QueuePublisher : IEventBusPublisher
    {
        private const string IntegrationEventSuffix = "IntegrationEvent";

        private readonly QueueClient _client;

        #region ctor

        public QueuePublisher(IConfiguration config, ReceiveMode mode)
        {
            var connection = new ServiceBusConnectionStringBuilder(config["EventBus:ConnectionString"]);

            _client = new QueueClient(connection, mode, RetryPolicy.Default);
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
            
            await _client.SendAsync(message);
        }

        #endregion
    }
}
