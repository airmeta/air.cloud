using Air.Cloud.Modules.RocketMQ.Config;
using Air.Cloud.Modules.RocketMQ.Model;

namespace Air.Cloud.UnitTest.Modules.RocketMQ
{
    public class RocketMQConfigModelTests
    {
        [Fact]
        public void GetProducerConfigModelByName_should_create_default_config_when_topic_not_configured()
        {
            var options = new RocketMQSettingsOptions
            {
                Endpoints = "127.0.0.1:8081",
                SslEnabled = true,
                RequestTimeout = TimeSpan.FromSeconds(5),
                AccessKey = "ak",
                AccessSecret = "sk"
            };

            var config = options.GetProducerConfigModelByName("orders");

            Assert.Equal("orders", config.TopicName);
            Assert.Equal(typeof(string), config.KeyType);
            Assert.Equal("127.0.0.1:8081", config.Config.Endpoints);
            Assert.True(config.Config.SslEnabled);
            Assert.Equal(TimeSpan.FromSeconds(5), config.Config.RequestTimeout);
            Assert.Equal("ak", config.Config.AccessKey);
            Assert.Equal("sk", config.Config.AccessSecret);
            Assert.Equal(3, config.Config.MaxAttempts);
        }

        [Fact]
        public void GetConsumerConfigModelByName_should_fill_missing_client_defaults_and_preserve_consumer_values()
        {
            var options = new RocketMQSettingsOptions
            {
                Endpoints = "127.0.0.1:8081",
                RequestTimeout = TimeSpan.FromSeconds(7),
                ConsumerConfigs = new List<RocketMQConsumerConfigModel>
                {
                    new()
                    {
                        TopicName = "payments",
                        Config = new RocketMQConsumerConfig
                        {
                            ConsumerGroup = "payments-group",
                            FilterExpression = "paid",
                            BatchSize = 8,
                            AwaitDuration = TimeSpan.FromSeconds(3)
                        }
                    }
                }
            };

            var config = options.GetConsumerConfigModelByName("payments");

            Assert.Equal("payments", config.TopicName);
            Assert.Equal(typeof(string), config.KeyType);
            Assert.Equal("127.0.0.1:8081", config.Config.Endpoints);
            Assert.Equal(TimeSpan.FromSeconds(7), config.Config.RequestTimeout);
            Assert.Equal("payments-group", config.Config.ConsumerGroup);
            Assert.Equal("paid", config.Config.FilterExpression);
            Assert.Equal(8, config.Config.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(3), config.Config.AwaitDuration);
        }
    }
}
