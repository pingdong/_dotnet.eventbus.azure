using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PingDong.EventBus.Core;

namespace PingDong.EventBus.Azure
{
    public class ServiceBusMessageDispatcher : IEventBusMessageDispatcher<Message>
    {
        private readonly string _integrationEventSuffix;
        private readonly ISubscriptionsManager _subscriptions;
        private readonly IServiceProvider _services;

        public ServiceBusMessageDispatcher(
            IServiceProvider services
            , ISubscriptionsManager subscriptions
            , string eventTypeSuffix)
        {
            _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
            _services = services ?? throw new ArgumentNullException(nameof(services));

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
                if (!string.IsNullOrWhiteSpace(message.MessageId))
                    @event.RequestId = message.MessageId;

                if (!string.IsNullOrWhiteSpace(message.CorrelationId))
                    @event.CorrelationId = message.CorrelationId;
            }

            var subscribers = _subscriptions.GetSubscribers(eventName);

            foreach (var subscriber in subscribers)
            {
                // Find handler for the message type
                var handler = ActivatorUtilities.GetServiceOrCreateInstance(_services, subscriber.HandlerType);
                if (handler != null)
                {
                    var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                    // Process handler
                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new [] { integrationEvent });
                }
            }
        }
    }
}
