using System;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using PingDong.EventBus.Azure.Json;

namespace PingDong.EventBus.Azure
{
    public class PublisherBase
    {
        private const string IntegrationEventSuffix = "IntegrationEvent";

        protected Message ToMessage(IntegrationEvent @event)
        {
            var jsonResolver = new PropertyRenameAndIgnoreSerializerContractResolver();
            jsonResolver.IgnoreProperty(typeof(IntegrationEvent), "RequestId");
            jsonResolver.IgnoreProperty(typeof(IntegrationEvent), "CorrelationId");
            jsonResolver.IgnoreProperty(typeof(IntegrationEvent), "CreationDate");

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = jsonResolver
            };

            var eventName = @event.GetType().Name.Replace(IntegrationEventSuffix, "");
            var jsonMessage = JsonConvert.SerializeObject(@event, serializerSettings);
            var body = Encoding.UTF8.GetBytes(jsonMessage);
            
            return new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                CorrelationId = @event.CorrelationId,
                Label = eventName,
                Body = body,
            };
        }
    }

}
