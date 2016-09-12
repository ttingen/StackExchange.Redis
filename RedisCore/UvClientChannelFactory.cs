using Channels;
using Channels.Networking.Libuv;
using System;
using System.Threading.Tasks;

namespace RedisCore
{
    public class UvClientChannelFactory : ClientChannelFactory
    {
        UvThread thread;
        readonly bool ownsThread;
        public override string ToString() => typeof(UvTcpConnection).FullName;
        public UvClientChannelFactory() : this(null) { }
        public UvClientChannelFactory(UvThread thread)
        {
            if (thread == null)
            {
                thread = new UvThread();
                ownsThread = true;
            }
            this.thread = thread;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (ownsThread)
                {
                    thread?.Dispose();
                    thread = null;
                }
            }
        }
        public override async Task<IChannel> ConnectAsync(string location)
            => await new UvTcpClient(thread, ParseIPEndPoint(location)).ConnectAsync();
    }
}
