
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.ClairvoyanceStandard.Message;

using Microsoft.Extensions.Options;

using System.Text;

namespace unit.taxinclient.entry
{
    public class TestService : BackgroundService
    {
        protected override async  Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Factory.StartNew(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        //client check
                        await AppRealization.Clairvoyance.SendAsync(new Msg("123").Serialize());

                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }
                    catch (OperationCanceledException) { }
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
