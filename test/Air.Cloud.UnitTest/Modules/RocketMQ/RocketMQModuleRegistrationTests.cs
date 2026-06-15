using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Modules.RocketMQ.Extensions;
using Air.Cloud.Modules.RocketMQ.Helper;
using Air.Cloud.Modules.RocketMQ.Options;
using Air.Cloud.Modules.RocketMQ.Standards;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.UnitTest.Modules.RocketMQ
{
    public class RocketMQModuleRegistrationTests
    {
        [Fact]
        public void AddRocketMQService_should_register_default_rocketmq_services()
        {
            var services = new ServiceCollection();

            InvokeWithRocketMQAssemblyScanOnly(() => services.AddRocketMQService());

            using var provider = services.BuildServiceProvider();

            Assert.IsType<RocketMQConsumerExecutionOptions>(provider.GetRequiredService<IMessageQueueConsumerExecutionOptions>());
            Assert.IsType<RocketMQConsumerRecoveryStandard>(provider.GetRequiredService<IMessageQueueConsumerRecoveryStandard>());
            Assert.IsType<RocketMQFailureCompensationStandard>(provider.GetRequiredService<IMessageQueueFailureCompensationStandard>());
            Assert.IsType<RocketMQMessageQueueDependency>(provider.GetRequiredService<IMessageQueueStandard>());
            Assert.NotNull(provider.GetRequiredService<IMessageQueueKeyGenerator<string>>());
            Assert.Contains(provider.GetServices<IMessageQueueKeyGenerator>(), generator => generator.GetKeyType() == typeof(string));
        }

        [Fact]
        public void AddRocketMQService_should_not_override_business_registered_defaults()
        {
            var services = new ServiceCollection();
            var customOptions = new CustomConsumerExecutionOptions();

            services.AddSingleton<IMessageQueueConsumerExecutionOptions>(customOptions);
            services.AddSingleton<IMessageQueueConsumerRecoveryStandard, CustomConsumerRecoveryStandard>();
            services.AddSingleton<IMessageQueueFailureCompensationStandard, CustomFailureCompensationStandard>();

            InvokeWithRocketMQAssemblyScanOnly(() => services.AddRocketMQService());

            using var provider = services.BuildServiceProvider();

            Assert.Same(customOptions, provider.GetRequiredService<IMessageQueueConsumerExecutionOptions>());
            Assert.IsType<CustomConsumerRecoveryStandard>(provider.GetRequiredService<IMessageQueueConsumerRecoveryStandard>());
            Assert.IsType<CustomFailureCompensationStandard>(provider.GetRequiredService<IMessageQueueFailureCompensationStandard>());
        }

        [Fact]
        public void AddRocketMQService_should_not_override_business_registered_string_key_generator()
        {
            var services = new ServiceCollection();
            var customGenerator = new CustomStringKeyGenerator();

            services.AddSingleton<IMessageQueueKeyGenerator<string>>(customGenerator);

            InvokeWithRocketMQAssemblyScanOnly(() => services.AddRocketMQService());

            using var provider = services.BuildServiceProvider();

            Assert.Same(customGenerator, provider.GetRequiredService<IMessageQueueKeyGenerator<string>>());
        }

        private static void InvokeWithRocketMQAssemblyScanOnly(Action action)
        {
            var originalAssemblies = AppCore.Assemblies;
            try
            {
                AppCore.Assemblies = new[] { typeof(RocketMQModuleExtensions).Assembly.GetName() };
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

        private sealed class CustomStringKeyGenerator : IMessageQueueKeyGenerator<string>
        {
            public string Generate(IMessageQueueKeyGenerationContext context)
            {
                return "business-key";
            }
        }
    }
}
