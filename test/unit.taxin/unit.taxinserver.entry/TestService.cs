
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.ClairvoyanceStandard.Message;
using Air.Cloud.Modules.Clairvoyance;
using Air.Cloud.Modules.Clairvoyance.Dependencies;

using Microsoft.Extensions.Options;

using System.Net.Sockets;
using System.Text;

namespace unit.taxinclient.entry
{
    public class TestService : BackgroundService
    {
        protected override async  Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Factory.StartNew(async () =>
            {
                byte[] buff = new byte[16];
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("get data start");
                        var client = ClairvoyanceNettyServerDependency.Socket.Accept();
                        var D = await client.ReceiveAsync(buff,SocketFlags.OutOfBand);
                        if (D > 0)
                        {
                            Console.WriteLine("get data SUCCESS");
                        }
                        else
                        {
                            Console.WriteLine(Encoding.UTF8.GetString(buff));
                        }
                    }
                    catch (OperationCanceledException WC) {

                        throw;
                    
                    }
                }
            }, stoppingToken);
        }
    }

    public class Msg : IClairvoyanceMessage<String>, IClairvoyanceMessageEncoder<Msg>
    {
        public  string Id { get ; set ; }
        public String Body { get; set; }


        public Msg(String MSG)
        {
            Id = AppCore.Guid();
            Body = MSG;
        }

        public byte[] Serialize()
        {
            var Data = AppRealization.JSON.Serialize(this);
            return Encoding.UTF8.GetBytes(Data);
        }
        public Msg() { }

        public Msg DeSerialize(byte[] bytes)
        {
            string Content= Encoding.UTF8.GetString(bytes);
            Msg msg = AppRealization.JSON.Deserialize<Msg>(Content);
            return msg;

        }
    }
}
