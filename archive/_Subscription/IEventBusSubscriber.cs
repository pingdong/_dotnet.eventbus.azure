
using System.Threading.Tasks;

namespace PingDong.EventBus
{
    public interface IEventBusSubscriber
    {
        Task ProcessAsync(string label, string message);

        Task SubscribeAsync<T, TH>() 
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
        Task SubscribeDynamicAsync<TH>(string eventName) 
            where TH : IDynamicIntegrationEventHandler;

        Task UnsubscribeDynamicAsync<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;

        Task UnsubscribeAsync<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent;
    }
}
