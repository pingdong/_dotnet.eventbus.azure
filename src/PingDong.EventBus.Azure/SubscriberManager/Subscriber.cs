using System;

namespace PingDong.EventBus
{
    public partial class SubscriptionsManager
    {
        public class Subscriber
        {
            private Subscriber(bool isDynamic, Type handlerType)
            {
                IsDynamic = isDynamic;
                HandlerType = handlerType;
            }

            public bool IsDynamic { get; }

            public Type HandlerType { get; }

            public static Subscriber Dynamic(Type handlerType)
            {
                return new Subscriber(true, handlerType);
            }

            public static Subscriber Typed(Type handlerType)
            {
                return new Subscriber(false, handlerType);
            }
        }
    }
}
