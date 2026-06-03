using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.Assemblies;
using Air.Cloud.Core.Standard.Assemblies.Model;
using Air.Cloud.Core.Standard.DataBase.Options;
using Air.Cloud.Core.Standard.DistributedLock.Plugins;
using Air.Cloud.Core.Standard.Event.Extensions;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Standard.SchedulerStandard.Extensions;

namespace Air.Cloud.IntegrationTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:核心标准默认实现的集成测试，验证测试宿主中的真实配置、默认实现和标准入口可以协同工作。</para>
    /// <para>en-us:Integration tests for core standard defaults, verifying real test-host configuration, default implementations, and standard entry points work together.</para>
    /// </summary>
    public class CoreStandardIntegrationTests
    {
        /// <summary>
        /// <para>zh-cn:验证配置、压缩、JSON 与输出标准可以通过 AppRealization 入口完成一次完整协同调用。</para>
        /// <para>en-us:Verifies configuration, compression, JSON, and output standards can complete one coordinated call through AppRealization.</para>
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Module", "Standard")]
        public void AppRealization_defaults_should_coordinate_core_standard_behaviors()
        {
            var configuration = AppRealization.Configuration.LoadConfiguration(AppConst.DEFAULT_CONFIG_FILE, IsCommonConfiguration: false);
            var compressed = AppRealization.Compress.Compress("integration-standard-payload");
            var decompressed = AppRealization.Compress.Decompress(compressed);
            var json = AppRealization.JSON.Serialize(new IntegrationPayload { Name = "standard" });
            var payload = AppRealization.JSON.Deserialize<IntegrationPayload>(json);
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                AppRealization.Output.Print(new AppPrintInformation
                {
                    Title = "standard-integration",
                    Content = "output",
                    SourceAssembly = "Air.Cloud.IntegrationTest",
                    Level = AppPrintLevel.Information
                });
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Equal("Development", configuration["Environment"]);
            Assert.Equal("integration-standard-payload", decompressed);
            Assert.NotNull(payload);
            Assert.Equal("standard", payload.Name);
            Assert.Contains("standard-integration | output", writer.ToString());
        }

        /// <summary>
        /// <para>zh-cn:验证程序集扫描标准可以在真实 AppCore.Assemblies 入口上扫描集成测试程序集。</para>
        /// <para>en-us:Verifies the assembly scanning standard can scan the integration-test assembly through the real AppCore.Assemblies entry point.</para>
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Module", "Standard")]
        public void AssemblyScanning_should_scan_public_types_from_configured_appcore_assemblies()
        {
            var originalAssemblies = AppCore.Assemblies;
            var scannedTypes = new List<Type>();
            var finallyCalled = false;
            IAssemblyScanningStandard.Evensts.Clear();

            try
            {
                AppCore.Assemblies = new[] { typeof(CoreStandardIntegrationTests).Assembly.GetName() };
                AppRealization.AssemblyScanning.Add(new AssemblyScanningEvent
                {
                    Key = "integration-standard-scan",
                    Description = "standard integration assembly scan",
                    TargetType = typeof(IIntegrationScanningTarget),
                    Action = type => scannedTypes.Add(type),
                    Finally = () => finallyCalled = true
                });

                AppRealization.AssemblyScanning.Execute();

                Assert.Contains(typeof(IntegrationScanningTarget), scannedTypes);
                Assert.True(finallyCalled);
                Assert.Empty(IAssemblyScanningStandard.Evensts);
            }
            finally
            {
                AppCore.Assemblies = originalAssemblies;
                IAssemblyScanningStandard.Evensts.Clear();
            }
        }

        /// <summary>
        /// <para>zh-cn:验证公共配置与外部配置都会进入统一配置管理器，后加载配置可参与整体配置读取。</para>
        /// <para>en-us:Verifies public and external configurations are both added to the unified configuration manager and can be read together.</para>
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Module", "Standard")]
        public void Configuration_standard_should_merge_public_and_external_configuration_sources()
        {
            var publicFileName = $"air-cloud-public-{Guid.NewGuid():N}.json";
            var externalFileName = $"air-cloud-external-{Guid.NewGuid():N}.json";
            var publicFilePath = Path.Combine(AppConst.ApplicationPath, publicFileName);
            var externalFilePath = Path.Combine(AppConst.ApplicationPath, externalFileName);
            File.WriteAllText(publicFilePath, "{\"StandardIntegration\":{\"PublicValue\":\"public\"}}");
            File.WriteAllText(externalFilePath, "{\"StandardIntegration\":{\"ExternalValue\":\"external\"}}");

            try
            {
                AppRealization.Configuration.LoadConfiguration(publicFileName, IsCommonConfiguration: true);
                AppRealization.Configuration.LoadConfiguration(externalFileName, IsCommonConfiguration: false);

                Assert.Equal("public", AppConfigurationLoader.Configurations["StandardIntegration:PublicValue"]);
                Assert.Equal("external", AppConfigurationLoader.Configurations["StandardIntegration:ExternalValue"]);
            }
            finally
            {
                File.Delete(publicFilePath);
                File.Delete(externalFilePath);
            }
        }

        /// <summary>
        /// <para>zh-cn:验证多个平台无关标准辅助能力可以在同一宿主进程中组合使用。</para>
        /// <para>en-us:Verifies multiple provider-neutral standard helpers can be composed in the same host process.</para>
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Module", "Standard")]
        public void Provider_neutral_standard_helpers_should_work_together()
        {
            var dataBaseOption = new DataBaseOption { Key = "integration-db" };
            var lockKey = new DistributedLockKeyFactoryPlugin().GetKey(dataBaseOption.UID);
            var eventText = IntegrationEventKind.ConfigurationLoaded.ParseToString();
            var restoredEvent = eventText.ParseToEnum();

            Assert.Equal(32, dataBaseOption.UID.Length);
            Assert.Equal(32, lockKey.Length);
            Assert.Equal(IntegrationEventKind.ConfigurationLoaded, restoredEvent);
            Assert.True("not-a-real-cron".IsValidExpression());
        }

        private sealed class IntegrationPayload
        {
            public string Name { get; set; } = string.Empty;
        }

        private enum IntegrationEventKind
        {
            ConfigurationLoaded
        }
    }

    /// <summary>
    /// <para>zh-cn:集成测试程序集扫描使用的公开目标接口。</para>
    /// <para>en-us:Public target interface used by integration assembly scanning tests.</para>
    /// </summary>
    public interface IIntegrationScanningTarget
    {
    }

    /// <summary>
    /// <para>zh-cn:集成测试程序集扫描使用的公开实现类型。</para>
    /// <para>en-us:Public implementation type used by integration assembly scanning tests.</para>
    /// </summary>
    public class IntegrationScanningTarget : IIntegrationScanningTarget
    {
    }
}
