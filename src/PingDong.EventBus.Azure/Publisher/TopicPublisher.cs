using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PingDong.EventBus.Core;

namespace PingDong.EventBus.Azure
{
    public class TopicPublisher : PublisherBase, IEventBusPublisher
    {
        private readonly TopicClient _topicClient;

        #region ctor

        public TopicPublisher(IConfiguration config)
        {
            var connection = config["EventBus:ConnectionString"];
            var topic = config["EventBus:Topic"];

            _topicClient = new TopicClient(connection, topic, RetryPolicy.Default);
        }

        #endregion

        #region IEventBus

        public async Task PublishAsync(IntegrationEvent @event)
        {
            await _topicClient.SendAsync(ToMessage(@event));
        }

        #endregion
    }
}
