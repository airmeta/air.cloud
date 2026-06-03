using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Modules.Kafka.Contexts;
using Air.Cloud.Modules.Kafka.Utils;

using Confluent.Kafka;

namespace Air.Cloud.UnitTest.Modules.Kafka
{
    public class KafkaKeyGeneratorAndContextTests
    {
        [Fact]
        public void KafkaIntMessageQueueKeyGenerator_should_report_int_key_type()
        {
            var generator = new KafkaIntMessageQueueKeyGenerator();

            Assert.Equal(typeof(int), generator.GetKeyType());
        }

        [Fact]
        public void KafkaIntMessageQueueKeyGenerator_should_generate_int_inside_default_range()
        {
            var generator = new KafkaIntMessageQueueKeyGenerator();
            var context = new KafkaMessageQueueKeyGenerationContext()
            {
                TopicName = "unit-test-topic",
                MessageContent = new TestKafkaMessage { Content = "hello" },
                PublishConfig = new ProducerConfig(),
                KeyType = typeof(int)
            };

            var value = generator.Generate(context);

            Assert.InRange(value, 111111111, 999999998);
        }

        [Fact]
        public void Generic_key_generator_default_interface_methods_should_return_key_type_and_boxed_key()
        {
            IMessageQueueKeyGenerator generator = new TestStringKeyGenerator();
            var context = new KafkaMessageQueueKeyGenerationContext()
            {
                TopicName = "unit-test-topic",
                MessageContent = new TestKafkaMessage { Content = "hello" },
                PublishConfig = new ProducerConfig(),
                KeyType = typeof(string)
            };

            Assert.Equal(typeof(string), generator.GetKeyType());
            Assert.Equal("unit-test-topic-key", generator.Generate(context));
        }

        [Fact]
        public void KafkaMessageQueueKeyGenerationContext_should_expose_standard_and_kafka_specific_values()
        {
            var message = new TestKafkaMessage { Content = "payload" };
            var producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };
            var context = new KafkaMessageQueueKeyGenerationContext()
            {
                TopicName = "unit-test-topic",
                MessageContent = message,
                PublishConfig = producerConfig,
                KeyType = typeof(string)
            };

            Assert.Equal("unit-test-topic", context.TopicName);
            Assert.Same(message, context.MessageContent);
            Assert.Same(producerConfig, context.PublishConfig);
            Assert.Equal(typeof(TestKafkaMessage), context.MessageContentType);
            Assert.Equal(typeof(ProducerConfig), context.PublishConfigType);
            Assert.Equal(typeof(string), context.KeyType);
            Assert.Equal("Kafka", context.ProviderName);
            Assert.Same(producerConfig, context.ProducerConfig);
        }

        private sealed class TestKafkaMessage
        {
            public string Content { get; set; } = string.Empty;
        }

        private sealed class TestStringKeyGenerator : IMessageQueueKeyGenerator<string>
        {
            public string Generate(IMessageQueueKeyGenerationContext context)
            {
                return $"{context.TopicName}-key";
            }
        }
    }
}
