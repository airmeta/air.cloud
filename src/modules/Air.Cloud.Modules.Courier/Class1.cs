using System.Net;
using System.Net.Sockets;
using System.Text;

public class SocketClient
{
    public static void LoaderOptimization(IPAddress ipAdress,int Port=8080)
    {
        Socket client_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //网络端点：为待请求连接的IP地址和端口号
        IPEndPoint ipEndpoint = new IPEndPoint(ipAdress, Port);
        //connect()向服务端发出连接请求。客户端不需要bind()绑定ip和端口号，
        //因为系统会自动生成一个随机的地址（具体应该为本机IP+随机端口号）
        client_socket.Connect(ipEndpoint);
        while (true)
        {
            string rl = Console.ReadLine();
            //发送消息到服务端
            client_socket.Send(Encoding.UTF8.GetBytes(rl.ToUpper()));
            byte[] buffer = new byte[1024 * 1024];
            //接收服务端消息
            int num = client_socket.Receive(buffer);
            string str = Encoding.UTF8.GetString(buffer, 0, num);
            Console.WriteLine("收到服务端数据 : " + str);
        }
    }
}