using Channels;
using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace RedisCore
{
    public abstract class ClientChannelFactory : IDisposable
    {
        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool disposing) { }
        protected IPEndPoint ParseIPEndPoint(string location)
        {
            // todo: lots of love
            int i = location.IndexOf(':');
            var ip = IPAddress.Parse(location.Substring(0, i));
            var port = int.Parse(location.Substring(i + 1), CultureInfo.InvariantCulture);
            return new IPEndPoint(ip, port);
        }
        public abstract Task<IChannel> ConnectAsync(string location);

        public virtual void Shutdown(IChannel channel)
        {
            channel?.Input?.CompleteReading();
            channel?.Output?.CompleteWriting();
        }
    }
}
