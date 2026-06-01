using Air.Cloud.Modules.Quartz.Extensions;

using Microsoft.Extensions.DependencyInjection;

using Quartz;

namespace Air.Cloud.UnitTest.Modules.Quartz
{
    public class QuartzExtensionsTests
    {
        [Fact]
        public void ConfigureDefaultQuartz_should_allow_quartz_service_registration()
        {
            var services = new ServiceCollection();

            services.AddQuartz(QuartzExtensions.ConfigureDefaultQuartz);

            using var provider = services.BuildServiceProvider();
            var schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();

            Assert.NotNull(schedulerFactory);
        }
    }
}
