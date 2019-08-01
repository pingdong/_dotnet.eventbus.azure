using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PingDong.CleanArchitect.Core;
using PingDong.EventBus.Core;
using PingDong.Linq;

namespace PingDong.EventBus.Azure
{
    public class ServiceBusMessageDispatcher : IMessageDispatcher<Message>
    {
        private readonly ISubscriptionsManager _subscriptions;
        private readonly IServiceProvider _services;
        private readonly ILogger<ServiceBusMessageDispatcher> _logger;

        public ServiceBusMessageDispatcher(
            IServiceProvider services
            , ILogger<ServiceBusMessageDispatcher> logger
            , ISubscriptionsManager subscriptions)
        {
            _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task DispatchAsync(Message message)
        {
            if (message == null)
                return;

            if (!_subscriptions.HasSubscribers(message.Label))
                return;

            if (_subscriptions.IsDynamic(message.Label))
                await HandleDynamicTypeIntegrationEvent(message);
            else
                await HandleFixedTypeIntegrationEvent(message);
        }

        private async Task HandleDynamicTypeIntegrationEvent(Message message)
        {
            var subscribers = _subscriptions.GetSubscribers(message.Label);
            if (subscribers.IsNullOrEmpty())
                return;

            var data = Encoding.UTF8.GetString(message.Body);
            if (string.IsNullOrWhiteSpace(data))
                return;

            dynamic integrationEvent = JsonConvert.DeserializeObject(data);
                
            foreach (var subscriber in subscribers)
            {
                // Find handler for the message type
                var handler = ActivatorUtilities.GetServiceOrCreateInstance(_services, subscriber.HandlerType);
                if (handler == null)
                    continue;

                if (handler is IDynamicIntegrationEventHandler dynamicHandler)
                {
                    // Process handler
                    await dynamicHandler.Handle(integrationEvent);
                }
            }
        }

        private async Task HandleFixedTypeIntegrationEvent(Message message)
        {
            // Fixed Type

            // Extract the input message.body and convert to type
            var eventType = _subscriptions.GetEventType(message.Label);
            if (eventType == null)
                return;

            var data = Encoding.UTF8.GetString(message.Body);
            if (string.IsNullOrWhiteSpace(data))
                return;

            var integrationEvent = JsonConvert.DeserializeObject(data, eventType);
            if (integrationEvent is IntegrationEvent @event)
            {
                if (string.IsNullOrWhiteSpace(message.MessageId))
                {
                    _logger.LogError("Missing requestId or is invalid");
                    return;
                }

                if (string.IsNullOrWhiteSpace(message.PartitionKey))
                {
                    _logger.LogError("Missing tenantId or is invalid");
                    return;
                }

                @event.TenantId = message.PartitionKey;
                @event.CorrelationId = message.CorrelationId;
                @event.RequestId = message.MessageId;
            }

            var fixedTypeSubscribers = _subscriptions.GetSubscribers(message.Label);

            foreach (var subscriber in fixedTypeSubscribers)
            {
                // Find handler for the message type
                var handler = ActivatorUtilities.GetServiceOrCreateInstance(_services, subscriber.HandlerType);
                if (handler == null)
                    continue;
                
                var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                // Process handler
                await (Task) concreteType.GetMethod("Handle").Invoke(handler, new[] {integrationEvent});
            }
        }
    }
}
