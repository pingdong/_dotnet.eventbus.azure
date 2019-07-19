using System.Text;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using PingDong.CleanArchitect.Core;
using PingDong.EventBus.Azure.Json;

namespace PingDong.EventBus.Azure
{
    public class PublisherBase
    {
        private const string IntegrationEventSuffix = "IntegrationEvent";

        protected Message ToMessage(IntegrationEvent @event)
        {
            var resolver = new PropertyRenameAndIgnoreSerializerContractResolver();
            resolver.IgnoreProperty(typeof(IntegrationEvent), "RequestId");
            resolver.IgnoreProperty(typeof(IntegrationEvent), "TenantId");
            resolver.IgnoreProperty(typeof(IntegrationEvent), "CorrelationId");
            resolver.IgnoreProperty(typeof(IntegrationEvent), "CreationDate");

            var settings = new JsonSerializerSettings
            {
                ContractResolver = resolver
            };

            var eventName = @event.GetType().Name.Replace(IntegrationEventSuffix, "");
            var jsonMessage = JsonConvert.SerializeObject(@event, settings);
            var body = Encoding.UTF8.GetBytes(jsonMessage);
            
            var message = new Message
            {
                MessageId = @event.RequestId,
                ContentType = "application/json",
                Label = eventName,
                Body = body
            };

            if (@event.CorrelationId != default)
                message.CorrelationId = @event.CorrelationId;
            if (@event.TenantId != default)
                message.PartitionKey = @event.TenantId;

            return message;
        }
    }

}
