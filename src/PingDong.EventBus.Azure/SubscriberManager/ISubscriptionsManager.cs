using System;
using System.Collections.Generic;

namespace PingDong.EventBus
{
    public interface ISubscriptionsManager
    {
        Type GetEventType(string eventName);
        
        bool HasSubscribers<T>() where T : IntegrationEvent;
        bool HasSubscribers(string eventName);

        void AddSubscriber(Type eventType, Type eventHandler);
        void AddSubscriber<T, THandler>() where T : IntegrationEvent where THandler : IIntegrationEventHandler<T>;
        void AddSubscriber<THandler>(string eventName) where THandler : IDynamicIntegrationEventHandler;

        void RemoveSubscriber<T, THandler>() where T : IntegrationEvent where THandler : IIntegrationEventHandler<T>;
        void RemoveSubscriber<THandler>(string eventName) where THandler : IDynamicIntegrationEventHandler;

        IList<SubscriptionsManager.Subscriber> GetSubscribers<T>() where T : IntegrationEvent;
        IList<SubscriptionsManager.Subscriber> GetSubscribers(string eventName);

        void Clear();
    }
}