using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PingDong.EventBus.Core;

namespace PingDong.EventBus.Azure
{
    public class ServiceBusMessageDispatcher : IMessageDispatcher<Message>
    {
        private readonly string _integrationEventSuffix;
        private readonly ISubscriptionsManager _subscriptions;
        private readonly IServiceProvider _services;
        private readonly ILogger<ServiceBusMessageDispatcher> _logger;

        public ServiceBusMessageDispatcher(
            IServiceProvider services
            , ILogger<ServiceBusMessageDispatcher> logger
            , ISubscriptionsManager subscriptions
            , string eventTypeSuffix)
        {
            _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _integrationEventSuffix = eventTypeSuffix;
        }

        public async Task DispatchAsync(Message message)
        {
            if (message == null)
                return;

            // Extract the input message.body and convert to type
            var eventName = $"{message.Label}{_integrationEventSuffix}";
            var eventType = _subscriptions.GetEventType(eventName);
            if (eventType == null)
                return;

            var data = Encoding.UTF8.GetString(message.Body);
            if (string.IsNullOrWhiteSpace(data))
                return;

            var integrationEvent = JsonConvert.DeserializeObject(data, eventType);
            if (integrationEvent is IntegrationEvent @event)
            {
                if (!string.IsNullOrWhiteSpace(message.MessageId)
                    && Guid.TryParse(message.MessageId, out Guid requestId))
                {
                    @event.RequestId = requestId;
                }
                else
                {
                    _logger.LogError("Missing requestId or is invalid");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(message.CorrelationId)
                    && Guid.TryParse(message.CorrelationId, out Guid correlationId))
                {
                    @event.CorrelationId = correlationId;
                }
                else
                {
                    @event.CorrelationId = Guid.NewGuid();
                }

                if (!string.IsNullOrWhiteSpace(message.PartitionKey)
                    && Guid.TryParse(message.PartitionKey, out Guid tenantId))
                {
                    @event.TenantId = tenantId;
                }
                else
                {
                    _logger.LogError("Missing tenantId or is invalid");
                }
            }

            var subscribers = _subscriptions.GetSubscribers(eventName);

            foreach (var subscriber in subscribers)
            {
                // Find handler for the message type
                var handler = ActivatorUtilities.GetServiceOrCreateInstance(_services, subscriber.HandlerType);
                if (handler == null)
                    continue;

                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                // Process handler
                await (Task)concreteType.GetMethod("Handle").Invoke(handler, new [] { integrationEvent });
            }
        }
    }
}
