using Xunit;

namespace PingDong.EventBus.Azure.UnitTests
{
    public class SubscriberTest
    {
        [Fact]
        public void Subscriber_Dynamic()
        {
            var sub = SubscriptionsManager.Subscriber.Dynamic(typeof(string));

            Assert.True(sub.IsDynamic);
            Assert.Equal(typeof(string), sub.HandlerType);
        }

        [Fact]
        public void Subscriber_Typed()
        {
            var sub = SubscriptionsManager.Subscriber.Typed(typeof(string));

            Assert.False(sub.IsDynamic);
            Assert.Equal(typeof(string), sub.HandlerType);
        }
    }
}
