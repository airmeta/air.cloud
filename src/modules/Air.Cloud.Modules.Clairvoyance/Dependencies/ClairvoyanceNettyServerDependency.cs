using Air.Cloud.Core.Standard.ClairvoyanceStandard;
using Air.Cloud.Core.Standard.ClairvoyanceStandard.Options;

using System.Net.Sockets;
using System.Net;
using Air.Cloud.Modules.Clairvoyance.Options;
using Air.Cloud.Core;

namespace Air.Cloud.Modules.Clairvoyance.Dependencies
{
    public class ClairvoyanceNettyServerDependency :
        IClairvoyanceServerStandard,
        IClairvoyanceTarget
    {
        public  static Socket Socket = null;
        private static ClairvoyanceServerOptions? options;
        public T Create<T>(IClairvoyanceOptions clairvoyanceOptions) where T : class
        {
            options = clairvoyanceOptions as ClairvoyanceServerOptions;
            if (options == null) AppRealization.Output.Error(new Exception("ClairvoyanceServerOptions配置为空"));
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(new IPEndPoint(options.IPAddress, options.Port));
            Socket.ReceiveTimeout = options.ReceiveTimeout;
            Socket.SendTimeout = options.ReceiveTimeout;
            return Socket as T;
        }
        public async Task OnLineAsync()
        {
            Socket.Listen(10);
        }

        public async Task UnderLineAsync()
        {
            Socket.Shutdown(SocketShutdown.Both);
        }
    }
}

