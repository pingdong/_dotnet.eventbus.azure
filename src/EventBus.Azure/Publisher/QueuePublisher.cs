using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PingDong.CleanArchitect.Core;
using PingDong.EventBus.Core;

namespace PingDong.EventBus.Azure
{
    public class QueuePublisher : PublisherBase, IEventBusPublisher
    {
        private readonly QueueClient _client;

        #region ctor

        public QueuePublisher(IConfiguration config, ReceiveMode mode)
        {
            var connection = new ServiceBusConnectionStringBuilder(config["EventBus:ConnectionString"]);

            _client = new QueueClient(connection, mode, RetryPolicy.Default);
        }

        #endregion

        #region IEventBus

        public async Task PublishAsync(IntegrationEvent @event)
        {
            await _client.SendAsync(ToMessage(@event));
        }

        #endregion
    }
}
