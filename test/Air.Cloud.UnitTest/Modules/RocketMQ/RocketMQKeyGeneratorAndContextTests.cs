using Air.Cloud.Modules.RocketMQ.Contexts;
using Air.Cloud.Modules.RocketMQ.Model;
using Air.Cloud.Modules.RocketMQ.Utils;

namespace Air.Cloud.UnitTest.Modules.RocketMQ
{
    public class RocketMQKeyGeneratorAndContextTests
    {
        [Fact]
        public void Default_string_key_generator_should_return_non_empty_unique_key()
        {
            var generator = new RocketMQStringMessageQueueKeyGenerator();
            var context = new RocketMQMessageQueueKeyGenerationContext
            {
                TopicName = "orders",
                MessageContent = new TestRocketMessage { Content = "created" },
                PublishConfig = new RocketMQProducerConfig(),
                KeyType = typeof(string)
            };

            var first = generator.Generate(context);
            var second = generator.Generate(context);

            Assert.False(string.IsNullOrWhiteSpace(first));
            Assert.False(string.IsNullOrWhiteSpace(second));
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Key_generation_context_should_expose_provider_and_runtime_types()
        {
            var content = new TestRocketMessage { Content = "created" };
            var config = new RocketMQProducerConfig { Endpoints = "127.0.0.1:8081" };

            var context = new RocketMQMessageQueueKeyGenerationContext
            {
                TopicName = "orders",
                MessageContent = content,
                PublishConfig = config,
                KeyType = typeof(string)
            };

            Assert.Equal("RocketMQ", context.ProviderName);
            Assert.Equal(typeof(TestRocketMessage), context.MessageContentType);
            Assert.Equal(typeof(RocketMQProducerConfig), context.PublishConfigType);
            Assert.Same(config, context.ProducerConfig);
        }

        private sealed class TestRocketMessage
        {
            public string Content { get; set; } = string.Empty;
        }
    }
}
