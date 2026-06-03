using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Model;
using Air.Cloud.Modules.Kafka.Options;

namespace Air.Cloud.UnitTest.Modules.Kafka
{
    public class KafkaConfigModelTests
    {
        [Fact]
        public void ProducerConfigModel_should_use_int_key_type_by_default()
        {
            var model = new ProducerConfigModel();

            Assert.Equal(typeof(int), model.KeyType);
        }

        [Fact]
        public void ProducerConfigModel_should_allow_string_key_type()
        {
            var model = new ProducerConfigModel
            {
                KeyType = typeof(string)
            };

            Assert.Equal(typeof(string), model.KeyType);
        }

        [Fact]
        public void ConsumerConfigModel_should_use_int_key_type_by_default()
        {
            var model = new ConsumerConfigModel();

            Assert.Equal(typeof(int), model.KeyType);
        }

        [Fact]
        public void ConsumerConfigModel_should_allow_string_key_type()
        {
            var model = new ConsumerConfigModel
            {
                KeyType = typeof(string)
            };

            Assert.Equal(typeof(string), model.KeyType);
        }

        [Fact]
        public void KafkaConsumerExecutionOptions_should_use_safe_defaults()
        {
            var options = new KafkaConsumerExecutionOptions();

            Assert.Equal(TimeSpan.FromSeconds(30), options.MessageHandlingTimeout);
            Assert.Equal(3, options.MaxRetryCount);
        }

        [Fact]
        public void KafkaConsumerExecutionOptions_should_allow_custom_values()
        {
            var options = new KafkaConsumerExecutionOptions
            {
                MessageHandlingTimeout = TimeSpan.FromSeconds(5),
                MaxRetryCount = 7
            };

            Assert.Equal(TimeSpan.FromSeconds(5), options.MessageHandlingTimeout);
            Assert.Equal(7, options.MaxRetryCount);
        }

        [Fact]
        public void KafkaSettingsOptions_should_create_default_producer_config_when_topic_is_missing()
        {
            var options = new KafkaSettingsOptions
            {
                ClusterAddress = "localhost:9092",
                ProducerConfigs = null
            };

            var model = options.GetProducerConfigModelByName("missing-topic");

            Assert.Equal("missing-topic", model.TopicName);
            Assert.Equal("localhost:9092", model.Config.BootstrapServers);
            Assert.Equal(typeof(int), model.KeyType);
        }

        [Fact]
        public void KafkaSettingsOptions_should_fill_existing_producer_config_bootstrap_servers_when_empty()
        {
            var options = new KafkaSettingsOptions
            {
                ClusterAddress = "localhost:9092",
                ProducerConfigs = new List<ProducerConfigModel>
                {
                    new()
                    {
                        TopicName = "configured-topic",
                        KeyType = typeof(string),
                        Config = new Confluent.Kafka.ProducerConfig()
                    }
                }
            };

            var model = options.GetProducerConfigModelByName("configured-topic");

            Assert.Equal("configured-topic", model.TopicName);
            Assert.Equal("localhost:9092", model.Config.BootstrapServers);
            Assert.Equal(typeof(string), model.KeyType);
        }

        [Fact]
        public void KafkaSettingsOptions_should_create_missing_existing_producer_config_without_resetting_key_type()
        {
            var options = new KafkaSettingsOptions
            {
                ClusterAddress = "localhost:9092",
                ProducerConfigs = new List<ProducerConfigModel>
                {
                    new()
                    {
                        TopicName = "configured-topic",
                        KeyType = typeof(string),
                        Config = null
                    }
                }
            };

            var model = options.GetProducerConfigModelByName("configured-topic");

            Assert.Equal("configured-topic", model.TopicName);
            Assert.Equal("localhost:9092", model.Config.BootstrapServers);
            Assert.Equal(typeof(string), model.KeyType);
        }

        [Fact]
        public void KafkaSettingsOptions_should_create_default_consumer_config_when_topic_is_missing()
        {
            var options = new KafkaSettingsOptions
            {
                ClusterAddress = "localhost:9092",
                ConsumerConfigs = null
            };

            var model = options.GetConsumerConfigModelByName("missing-topic");

            Assert.Equal("missing-topic", model.TopicName);
            Assert.Equal("localhost:9092", model.Config.BootstrapServers);
            Assert.Equal(typeof(int), model.KeyType);
        }

        [Fact]
        public void KafkaSettingsOptions_should_fill_existing_consumer_config_bootstrap_servers_when_empty()
        {
            var options = new KafkaSettingsOptions
            {
                ClusterAddress = "localhost:9092",
                ConsumerConfigs = new List<ConsumerConfigModel>
                {
                    new()
                    {
                        TopicName = "configured-topic",
                        KeyType = typeof(string),
                        Config = new Confluent.Kafka.ConsumerConfig()
                    }
                }
            };

            var model = options.GetConsumerConfigModelByName("configured-topic");

            Assert.Equal("configured-topic", model.TopicName);
            Assert.Equal("localhost:9092", model.Config.BootstrapServers);
            Assert.Equal(typeof(string), model.KeyType);
        }

        [Fact]
        public void KafkaSettingsOptions_should_create_missing_existing_consumer_config_without_resetting_key_type()
        {
            var options = new KafkaSettingsOptions
            {
                ClusterAddress = "localhost:9092",
                ConsumerConfigs = new List<ConsumerConfigModel>
                {
                    new()
                    {
                        TopicName = "configured-topic",
                        KeyType = typeof(string),
                        Config = null
                    }
                }
            };

            var model = options.GetConsumerConfigModelByName("configured-topic");

            Assert.Equal("configured-topic", model.TopicName);
            Assert.Equal("localhost:9092", model.Config.BootstrapServers);
            Assert.Equal(typeof(string), model.KeyType);
        }
    }
}
