using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Modules.Kafka.Extensions;
using Air.Cloud.Modules.Kafka.Helper;
using Air.Cloud.Modules.Kafka.Options;
using Air.Cloud.Modules.Kafka.Standards;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Air.Cloud.UnitTest.Modules.Kafka
{
    public class KafkaModuleRegistrationTests
    {
        [Fact]
        public void AddKafkaService_should_register_default_kafka_services()
        {
            var services = new ServiceCollection();

            InvokeWithKafkaAssemblyScanOnly(() => services.AddKafkaService());

            using var provider = services.BuildServiceProvider();

            Assert.IsType<KafkaConsumerExecutionOptions>(provider.GetRequiredService<IMessageQueueConsumerExecutionOptions>());
            Assert.IsType<KafkaConsumerRecoveryStandard>(provider.GetRequiredService<IMessageQueueConsumerRecoveryStandard>());
            Assert.IsType<KafkaFailureCompensationStandard>(provider.GetRequiredService<IMessageQueueFailureCompensationStandard>());
            Assert.IsType<KafkaMessageQueueDependency>(provider.GetRequiredService<IMessageQueueStandard>());
            Assert.NotNull(provider.GetRequiredService<IMessageQueueKeyGenerator<int>>());
            Assert.Contains(provider.GetServices<IMessageQueueKeyGenerator>(), generator => generator.GetKeyType() == typeof(int));
        }

        [Fact]
        public void AddKafkaService_should_not_override_business_registered_defaults()
        {
            var services = new ServiceCollection();
            var customOptions = new CustomConsumerExecutionOptions();

            services.AddSingleton<IMessageQueueConsumerExecutionOptions>(customOptions);
            services.AddSingleton<IMessageQueueConsumerRecoveryStandard, CustomConsumerRecoveryStandard>();
            services.AddSingleton<IMessageQueueFailureCompensationStandard, CustomFailureCompensationStandard>();

            InvokeWithKafkaAssemblyScanOnly(() => services.AddKafkaService());

            using var provider = services.BuildServiceProvider();

            Assert.Same(customOptions, provider.GetRequiredService<IMessageQueueConsumerExecutionOptions>());
            Assert.IsType<CustomConsumerRecoveryStandard>(provider.GetRequiredService<IMessageQueueConsumerRecoveryStandard>());
            Assert.IsType<CustomFailureCompensationStandard>(provider.GetRequiredService<IMessageQueueFailureCompensationStandard>());
        }

        [Fact]
        public void AddKafkaService_should_not_override_business_registered_int_key_generator()
        {
            var services = new ServiceCollection();
            var customGenerator = new CustomIntKeyGenerator();

            services.AddSingleton<IMessageQueueKeyGenerator<int>>(customGenerator);

            InvokeWithKafkaAssemblyScanOnly(() => services.AddKafkaService());

            using var provider = services.BuildServiceProvider();

            Assert.Same(customGenerator, provider.GetRequiredService<IMessageQueueKeyGenerator<int>>());
        }

        private static void InvokeWithKafkaAssemblyScanOnly(Action action)
        {
            var originalAssemblies = AppCore.Assemblies;
            try
            {
                AppCore.Assemblies = new[] { typeof(KafkaModuleExtensions).Assembly.GetName() };
                action.Invoke();
            }
            finally
            {
                AppCore.Assemblies = originalAssemblies;
            }
        }

        private sealed class CustomConsumerExecutionOptions : IMessageQueueConsumerExecutionOptions
        {
            public TimeSpan? MessageHandlingTimeout => TimeSpan.FromSeconds(2);

            public int MaxRetryCount => 9;
        }

        private sealed class CustomConsumerRecoveryStandard : IMessageQueueConsumerRecoveryStandard
        {
            public Task OnConsumerInterruptedAsync(IMessageQueueConsumerContext context)
            {
                return Task.CompletedTask;
            }

            public Task OnConsumerRecoveredAsync(IMessageQueueConsumerContext context)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class CustomFailureCompensationStandard : IMessageQueueFailureCompensationStandard
        {
            public Task OnFailedAsync(IMessageQueueFailureContext context)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class CustomIntKeyGenerator : IMessageQueueKeyGenerator<int>
        {
            public int Generate(IMessageQueueKeyGenerationContext context)
            {
                return 42;
            }
        }
    }
}
