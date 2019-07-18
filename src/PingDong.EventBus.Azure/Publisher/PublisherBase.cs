using System;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using PingDong.EventBus.Azure.Json;
using PingDong.EventBus.Core;

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
            
            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ContentType = "application/json",
                Label = eventName,
                Body = body
            };

            if (@event.CorrelationId != default)
                message.CorrelationId = @event.CorrelationId.ToString();
            if (@event.TenantId != default)
                message.PartitionKey = @event.TenantId.ToString();

            return message;
        }
    }

}
