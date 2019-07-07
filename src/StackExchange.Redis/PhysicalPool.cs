using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StackExchange.Redis.ConnectionMultiplexer;

namespace StackExchange.Redis
{
    internal sealed class PhysicalPool : IDisposable
    {
        private bool isDisposed;
        private ConnectionMultiplexer Multiplexer { get; }
        private ServerEndPoint ServerEndPoint { get; }

        // Eventually use a pool of connections.
        public PhysicalBridge Interactive { get; private set; }
        public PhysicalBridge Subscription { get; private set; }

        public PhysicalPool(ConnectionMultiplexer multiplexer, ServerEndPoint serverEndPoint)
        {
            Multiplexer = multiplexer;
            ServerEndPoint = serverEndPoint;
        }

        public PhysicalBridge GetBridge(ConnectionType type, bool create = true, LogProxy log = null)
        {
            if (isDisposed) return null;
            switch (type)
            {
                case ConnectionType.Interactive:
                    return Interactive ?? (create ? Interactive = CreateBridge(ConnectionType.Interactive, log) : null);
                case ConnectionType.Subscription:
                    return Subscription ?? (create ? Subscription = CreateBridge(ConnectionType.Subscription, log) : null);
            }
            return null;
        }

        public PhysicalBridge GetBridge(RedisCommand command, bool create = true)
        {
            if (isDisposed) return null;
            switch (command)
            {
                case RedisCommand.SUBSCRIBE:
                case RedisCommand.UNSUBSCRIBE:
                case RedisCommand.PSUBSCRIBE:
                case RedisCommand.PUNSUBSCRIBE:
                    return Subscription ?? (create ? Subscription = CreateBridge(ConnectionType.Subscription, null) : null);
                default:
                    return Interactive ?? (create ? Interactive = CreateBridge(ConnectionType.Interactive, null) : null);
            }
        }

        public bool IsConnected => Interactive?.IsConnected == true;

        public bool IsConnecting => Interactive?.IsConnecting == true;

        public void ResetNonConnected()
        {
            Interactive?.ResetNonConnected();
            Subscription?.ResetNonConnected();
        }

        private PhysicalBridge CreateBridge(ConnectionType type, LogProxy log)
        {
            if (Multiplexer.IsDisposed) return null;
            Multiplexer.Trace(type.ToString());
            var bridge = new PhysicalBridge(ServerEndPoint, type, Multiplexer.TimeoutMilliseconds);
            bridge.TryConnect(log);
            return bridge;
        }

        public void Dispose()
        {
            // Cleanup all active connections...
            isDisposed = true;
            var tmp = Interactive;
            Interactive = null;
            tmp?.Dispose();

            tmp = Subscription;
            Subscription = null;
            tmp?.Dispose();
        }
    }
}
