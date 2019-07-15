using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PingDong.EventBus.Azure.UnitTests
{
    public class SubscriptionManagerTest
    {
        #region Add Subscriber

        [Fact]
        public void AddTypedSubscriber()
        {
            var submgr = new SubscriptionsManager();
            submgr.AddSubscriber<TestEvent, TestEventHandler>();

            Assert.True(submgr.HasSubscribers<TestEvent>());
            Assert.Single(submgr.GetSubscribers<TestEvent>());
            Assert.Equal(typeof(TestEventHandler), submgr.GetSubscribers<TestEvent>().First().HandlerType);
        }

        [Fact]
        public void AddGenericTypedSubscriber()
        {
            var submgr = new SubscriptionsManager();
            submgr.AddSubscriber(typeof(TestEvent), typeof(TestEventHandler));

            Assert.True(submgr.HasSubscribers<TestEvent>());
            Assert.Single(submgr.GetSubscribers<TestEvent>());
            Assert.Equal(typeof(TestEventHandler), submgr.GetSubscribers<TestEvent>().First().HandlerType);
        }

        [Fact]
        public void AddGenericTypedSubscriber_Null()
        {
            var submgr = new SubscriptionsManager();
            Assert.Throws<ArgumentNullException>(() => submgr.AddSubscriber(null, typeof(TestEventHandler)));
            Assert.Throws<ArgumentNullException>(() => submgr.AddSubscriber(typeof(TestEvent), null));
        }

        [Fact]
        public void AddDynamicSubscriber()
        {
            var submgr = new SubscriptionsManager();
            submgr.AddSubscriber<TestDynamicEventHandler>("Test");

            Assert.True(submgr.HasSubscribers("Test"));
            Assert.Single(submgr.GetSubscribers("Test"));
            Assert.Equal(typeof(TestDynamicEventHandler), submgr.GetSubscribers("Test").First().HandlerType);
        }

        [Fact]
        public void AddDynamicSubscriber_Null()
        {
            var submgr = new SubscriptionsManager();
            Assert.Throws<ArgumentNullException>(() => submgr.AddSubscriber<TestDynamicEventHandler>(null));
        }

        #endregion

        #region Remove Subscriber
        
        [Fact]
        public void RemoveTypedSubscriber()
        {
            var submgr = new SubscriptionsManager();
            submgr.AddSubscriber<TestEvent, TestEventHandler>();
            submgr.RemoveSubscriber<TestEvent, TestEventHandler>();

            Assert.False(submgr.HasSubscribers<TestEvent>());
        }

        [Fact]
        public void RemoveDynamicSubscriber()
        {
            var submgr = new SubscriptionsManager();
            submgr.AddSubscriber<TestDynamicEventHandler>("Test");
            submgr.RemoveSubscriber<TestDynamicEventHandler>("Test");

            Assert.False(submgr.HasSubscribers("Test"));
        }

        [Fact]
        public void RemoveDynamicSubscriber_Null()
        {
            var submgr = new SubscriptionsManager();
            Assert.Throws<ArgumentNullException>(() => submgr.RemoveSubscriber<TestDynamicEventHandler>(null));
        }

        #endregion

        #region HasSubscriber

        [Fact]
        public void HasSubscriber_Null()
        {
            var submgr = new SubscriptionsManager();
            Assert.Throws<ArgumentNullException>(() => submgr.HasSubscribers(null));
        }

        #endregion

        #region GetEventType

        [Fact]
        public void GetEventType()
        {
            var submgr = new SubscriptionsManager();
            submgr.AddSubscriber<TestEvent, TestEventHandler>();

            Assert.Equal(typeof(TestEvent), submgr.GetEventType("TestEvent"));
            Assert.Equal(typeof(TestEvent), submgr.GetEventType<TestEvent>());
        }

        [Fact]
        public void GetEventType_Null()
        {
            var submgr = new SubscriptionsManager();
            Assert.Null(submgr.GetEventType(null));
            Assert.Null(submgr.GetEventType("NotExisted"));
        }

        #endregion

        #region Clear

        [Fact]
        public void Clear()
        {
            var submgr = new SubscriptionsManager();
            submgr.AddSubscriber<TestEvent, TestEventHandler>();
            submgr.AddSubscriber<TestDynamicEventHandler>("Test");

            Assert.True(submgr.HasSubscribers<TestEvent>());
            Assert.True(submgr.HasSubscribers("Test"));

            submgr.Clear();

            Assert.False(submgr.HasSubscribers<TestEvent>());
            Assert.False(submgr.HasSubscribers("Test"));
        }

        #endregion
    }

    #region Test Data

    public class TestEvent : IntegrationEvent
    {
        public TestEvent(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class TestEventHandler : IIntegrationEventHandler<TestEvent>
    {
        public Task Handle(TestEvent @event)
        {
            return Task.CompletedTask;
        }
    }

    public class TestDynamicEventHandler : IDynamicIntegrationEventHandler
    {
        public Task Handle(dynamic eventData)
        {
            return Task.CompletedTask;
        }
    }

    #endregion
}
