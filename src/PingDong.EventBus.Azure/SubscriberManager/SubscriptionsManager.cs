using System;
using System.Collections.Generic;
using System.Linq;

namespace PingDong.EventBus
{
    public partial class SubscriptionsManager : ISubscriptionsManager
    {
        #region Variables

        private readonly Dictionary<string, List<Subscriber>> _handlers;
        private readonly Dictionary<string, Type> _events;

        #endregion

        #region ctor

        public SubscriptionsManager()
        {
            _handlers = new Dictionary<string, List<Subscriber>>();
            _events = new Dictionary<string, Type>();
        }

        #endregion

        #region ISubscriptionsManager

        public void AddSubscriber(Type eventType, Type eventHandler)
        {
            var eventName = eventType.Name;

            _events.Add(eventName, eventType);

            AddSubscriber(eventHandler, eventName, false);
        }
        
        public void AddSubscriber<T, THandler>()
            where T : IntegrationEvent
            where THandler : IIntegrationEventHandler<T>
        {
            var eventName = GetEventName<T>();

            _events.Add(eventName, typeof(T));

            AddSubscriber(typeof(THandler), eventName, isDynamic: false);
        }

        public void AddSubscriber<THandler>(string eventName)
            where THandler : IDynamicIntegrationEventHandler
        {
            AddSubscriber(typeof(THandler), eventName, isDynamic: true);
        }

        public void RemoveSubscriber<T, THandler>()
            where THandler : IIntegrationEventHandler<T>
            where T : IntegrationEvent
        {
            var eventName = GetEventName<T>();

            var handlerToRemove = FindSubscriberToRemove<T, THandler>();
            RemoveSubscriber(eventName, handlerToRemove);
        }

        public void RemoveSubscriber<THandler>(string eventName)
            where THandler : IDynamicIntegrationEventHandler
        {
            var handlerToRemove = FindSubscriberToRemove<THandler>(eventName);
            RemoveSubscriber(eventName, handlerToRemove);
        }

        public IList<Subscriber> GetSubscribers<T>() where T : IntegrationEvent
        {
            var key = GetEventName<T>();

            return GetSubscribers(key);
        }

        public IList<Subscriber> GetSubscribers(string eventName)
        {
            return _handlers[eventName];
        }

        public bool HasSubscribers<T>() where T : IntegrationEvent
        {
            var key = GetEventName<T>();

            return HasSubscribers(key);
        }

        public bool HasSubscribers(string eventName)
        {
            return _handlers.ContainsKey(eventName);
        }

        public Type GetEventType(string eventName)
        {
            return !_events.ContainsKey(eventName) ? null : _events[eventName];
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        #endregion

        #region Private Methods

        private string GetEventName<T>()
        {
            return typeof(T).Name;
        }

        private Subscriber FindSubscriberToRemove<THandler>(string eventName)
            where THandler : IDynamicIntegrationEventHandler
        {
            return FindSubscriber<THandler>(eventName);
        }

        private Subscriber FindSubscriberToRemove<T, THandler>()
            where T : IntegrationEvent
            where THandler : IIntegrationEventHandler<T>
        {
            var eventName = GetEventName<T>();

            return FindSubscriber<THandler>(eventName);
        }

        private Subscriber FindSubscriber<THandler>(string eventName)
        {
            if (!HasSubscribers(eventName))
                return null;

            return _handlers[eventName].FirstOrDefault(s => s.HandlerType == typeof(THandler));
        }

        private void RemoveSubscriber(string eventName, Subscriber subscriber)
        {
            if (subscriber == null)
                return;

            // Remove SubscriptionInfo from List
            _handlers[eventName].Remove(subscriber);

            if (_handlers[eventName].Any())
                return;

            // If there is no handler for this event,
            //    remove the event from handler
            _handlers.Remove(eventName);
            _events.Remove(eventName);
        }

        private void AddSubscriber(Type handlerType, string eventName, bool isDynamic)
        {
            // If this is the first handler for this event,
            //    a new handler list needs to be created
            if (!HasSubscribers(eventName))
                _handlers.Add(eventName, new List<Subscriber>());
            
            // Can't have the same handler twice
            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
                throw new ArgumentException($"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));

            _handlers[eventName].Add(
                isDynamic 
                    ? Subscriber.Dynamic(handlerType) 
                    : Subscriber.Typed(handlerType)
            );
        }

        #endregion
    }
}
