using Channels;
using Channels.Networking.Sockets;
using System.Threading.Tasks;

namespace RedisCore
{
    public class SocketConnectionClientChannelFactory : ClientChannelFactory
    {
        public override string ToString() => typeof(SocketConnection).FullName;
        public override async Task<IChannel> ConnectAsync(string location)
            => await SocketConnection.ConnectAsync(ParseIPEndPoint(location));
    }
}
