using Air.Cloud.Core;
using Air.Cloud.Core.Standard.ClairvoyanceStandard;
using Air.Cloud.Core.Standard.ClairvoyanceStandard.Options;
using Air.Cloud.Modules.Clairvoyance.Options;

using Microsoft.Extensions.Options;

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Air.Cloud.Modules.Clairvoyance.Dependencies
{
    public class ClairvoyanceNettyClientDependency :
        IClairvoyanceClientStandard,
        IClairvoyanceTarget
    {
        private static Socket Socket;
        private static NetworkStream sendStream;
        private static ClairvoyanceClientOptions ClairvoyanceClientOptions;
        public void Send(object message)
        {
            SendAsync(message).GetAwaiter().GetResult();
        }

        public async Task SendAsync(object message)
        {
            if (sendStream != null)
            {
                string Content = AppRealization.JSON.Serialize(message);
                var sendBytes = Encoding.UTF8.GetBytes(Content);
                sendStream.Write(sendBytes, 0, sendBytes.Length);
                sendStream.Flush();
                Console.WriteLine("send data.....");
            }
           
        }

        public async Task UnderLineAsync()
        {
            Socket.Close();
            Socket.Shutdown(SocketShutdown.Both);
        }
        public async Task OnLineAsync()
        {
            Socket = new Socket(SocketType.Stream,ProtocolType.Tcp);
            Socket.Bind(new IPEndPoint(ClairvoyanceClientOptions.IPAddress, ClairvoyanceClientOptions.Port));
            Socket.Connect(ClairvoyanceClientOptions.ServerIP, ClairvoyanceClientOptions.ServerPort);
            sendStream = new NetworkStream(Socket);
        }

        public T Create<T>(IClairvoyanceOptions clairvoyanceOptions) where T : class
        {
            ClairvoyanceClientOptions = clairvoyanceOptions as ClairvoyanceClientOptions;
            if(ClairvoyanceClientOptions==null) AppRealization.Output.Error(new Exception("ClairvoyanceServerOptions配置为空"));
            return Socket as T;
        }
    }
}
