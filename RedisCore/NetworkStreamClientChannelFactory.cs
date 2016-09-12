using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Channels;

namespace RedisCore
{
    public class NetworkStreamClientChannelFactory : ClientChannelFactory
    {
        public override string ToString() => typeof(NetworkStream).FullName;
        private ChannelFactory channelFactory = new ChannelFactory();
        protected override void Dispose(bool disposing)
        {
            if (disposing) channelFactory?.Dispose();
        }
        class NetworkStreamClientChannel : IChannel
        {
            private Socket socket;
            private NetworkStream ns;
            private IReadableChannel input;
            private IWritableChannel output;
            public NetworkStreamClientChannel(ChannelFactory channelFactory, Socket socket)
            {
                this.socket = socket;
                this.ns = new NetworkStream(socket);
                this.input = channelFactory.MakeReadableChannel(ns);
                this.output = channelFactory.MakeWriteableChannel(ns);
            }

            IReadableChannel IChannel.Input => input;

            IWritableChannel IChannel.Output => output;

            void IDisposable.Dispose()
            {
                input?.CompleteReading();
                input = null;
                output?.CompleteWriting();
                output = null;
                ns?.Dispose();
                ns = null;
                socket?.Dispose();
                socket = null;
            }
        }
        class ConnectState
        {
            public readonly TaskCompletionSource<IChannel> TaskSource;
            public readonly ChannelFactory ChannelFactory;
            public ConnectState(TaskCompletionSource<IChannel> taskSource, ChannelFactory channelFactory)
            {
                this.TaskSource = taskSource;
                this.ChannelFactory = channelFactory;
            }
        }
        static readonly EventHandler<SocketAsyncEventArgs> onConnect = (sender, args) =>
        {
            var state = (ConnectState)args.UserToken;
            var tcs = state.TaskSource;
            try
            {
                if (args.SocketError != SocketError.Success)
                {
                    tcs.TrySetException(new SocketException((int)args.SocketError));
                    return;
                }
                var socket = args.ConnectSocket;
                socket.NoDelay = true;
                var channel = new NetworkStreamClientChannel(state.ChannelFactory, socket);
                tcs.SetResult(channel);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        };
        public override Task<IChannel> ConnectAsync(string location)
        {
            var tcs = new TaskCompletionSource<IChannel>();
            var ep = ParseIPEndPoint(location);
            var args = new SocketAsyncEventArgs();
            args.UserToken = new ConnectState(tcs, channelFactory);
            args.RemoteEndPoint = ep;
            args.Completed += onConnect;
            if (!Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, args)) onConnect(typeof(Socket), args);
            return tcs.Task;
        }
    }
}
