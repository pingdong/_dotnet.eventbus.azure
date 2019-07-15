using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PingDong.EventBus.Azure
{
    public class TopicSubscriber : IEventBusSubscriber, IDisposable
    {
        private const string IntegrationEventSuffix = "IntegrationEvent";

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubscriptionsManager _subscriptions;

        private readonly SubscriptionClient _subscriptionClient;

        #region ctor

        public TopicSubscriber(ISubscriptionsManager subscriptions
                            , ILoggerProvider loggerProvider
                            , IServiceProvider serviceProvider
                            , IConfiguration config)
        {
            _logger = loggerProvider.CreateLogger("AzureServiceBus");
            _subscriptions = subscriptions;
            _serviceProvider = serviceProvider;

            var connection = new ServiceBusConnectionStringBuilder(config["EventBus:ConnectionString"]);

            _subscriptionClient = new SubscriptionClient(connection, config["EventBus:ClientName"]);

            RemoveDefaultRule();
            RegisterSubscriptionClientMessageHandler();
        }

        private void RemoveDefaultRule()
        {
            try
            {
                _subscriptionClient.RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogInformation($"The messaging entity { RuleDescription.DefaultRuleName } Could not be found.");
            }
        }

        private void RegisterSubscriptionClientMessageHandler()
        {
            _subscriptionClient.RegisterMessageHandler(
                async (message, token) => await ProcessEvent(message, token).ConfigureAwait(false),
                new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 10, AutoComplete = false }
            );
        }

        private async Task ProcessAsync(Message message)
        {
            if (message == null)
                return;

            var data = Encoding.UTF8.GetString(message.Body);
            if (string.IsNullOrWhiteSpace(data))
                return;

            var eventName = $"{message.Label}{IntegrationEventSuffix}";
            if (!_subscriptions.HasSubscribers(eventName))
                return;

            using (var scope = _serviceProvider.CreateScope())
            {
                var subscribers = _subscriptions.GetSubscribers(eventName);

                foreach (var subscriber in subscribers)
                {
                    if (subscriber.IsDynamic)
                    {
                        if (scope.ServiceProvider.GetService(subscriber.HandlerType) is IDynamicIntegrationEventHandler handler)
                        {
                            dynamic eventData = JObject.Parse(data);
                            await handler.Handle(eventData).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        var eventType = _subscriptions.GetEventType(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(data, eventType);

                        var handler = scope.ServiceProvider.GetService(subscriber.HandlerType);
                        if (handler != null)
                        {
                            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                            await (Task) concreteType.GetMethod("Handle")
                                .Invoke(handler, new object[] {integrationEvent});
                        }
                    }
                }
            }
                    
            // Complete the message so that it is not received again.
            await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        #endregion

        #region IEventBusSubscriber

        public async Task SubscribeDynamicAsync<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            await SubscribeAsync(eventName);

            _subscriptions.AddSubscriber<TH>(eventName);
        }

        public async Task UnsubscribeDynamicAsync<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            await UnsubscribeAsync(eventName);

            _subscriptions.RemoveSubscriber<TH>(eventName);
        }

        public async Task SubscribeAsync<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName =  typeof(T).Name.Replace(IntegrationEventSuffix, "");

            await SubscribeAsync(eventName);

            _subscriptions.AddSubscriber<T, TH>();
        }

        public async Task UnsubscribeAsync<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name.Replace(IntegrationEventSuffix, "");

            await UnsubscribeAsync(eventName);

            _subscriptions.RemoveSubscriber<T, TH>();
        }

        #endregion

        #region Private Methods

        private async Task UnsubscribeAsync(string eventName)
        {
            if (!_subscriptions.HasSubscribers(eventName))
                return;
            
            try
            {
                await _subscriptionClient.RemoveRuleAsync(eventName);
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger.LogInformation($"The messaging entity {eventName} Could not be found.");
            }
        }

        private async Task SubscribeAsync(string eventName)
        {
            if (_subscriptions.HasSubscribers(eventName))
                return;

            try
            {
                var rule = new RuleDescription
                {
                    Filter = new CorrelationFilter {Label = eventName},
                    Name = eventName
                };

                await _subscriptionClient.AddRuleAsync(rule);
            }
            catch(ServiceBusException)
            {
                _logger.LogInformation($"The messaging entity {eventName} already exists.");
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var sb = new StringBuilder();
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            sb.AppendLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            sb.AppendLine($"- Endpoint: {context.Endpoint}");
            sb.AppendLine($"- Entity Path: {context.EntityPath}");
            sb.AppendLine($"- Executing Action: {context.Action}");

            _logger.LogError(sb.ToString());

            return Task.CompletedTask;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _subscriptions.Clear();
        }

        #endregion
    }
}
