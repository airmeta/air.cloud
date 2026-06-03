using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.Configuration.Defaults;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:配置加载标准默认实现的单元测试。</para>
    /// <para>en-us:Unit tests for the default configuration loading standard.</para>
    /// </summary>
    public class ConfigurationStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证存在的 JSON 配置文件可以从应用目录加载并读取配置值。</para>
        /// <para>en-us:Verifies an existing JSON configuration file can be loaded from the application directory and read.</para>
        /// </summary>
        [Fact]
        public void LoadConfiguration_should_load_existing_json_file()
        {
            var fileName = $"air-cloud-config-test-{Guid.NewGuid():N}.json";
            var filePath = Path.Combine(AppConst.ApplicationPath, fileName);
            File.WriteAllText(filePath, "{\"StandardTest\":{\"Name\":\"configuration\"}}");

            try
            {
                var standard = new DefaultAppConfigurationDependency();

                var configuration = standard.LoadConfiguration(fileName, IsCommonConfiguration: false);

                Assert.Equal("configuration", configuration["StandardTest:Name"]);
                Assert.NotNull(AppConfigurationLoader.ExternalConfigChangeToken);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// <para>zh-cn:验证缺失配置文件按可选配置处理，不应因为业务配置暂未提供而中断启动流程。</para>
        /// <para>en-us:Verifies missing configuration files are optional and should not interrupt startup when business configuration is not provided yet.</para>
        /// </summary>
        [Fact]
        public void LoadConfiguration_should_return_empty_configuration_when_file_is_missing()
        {
            var standard = new DefaultAppConfigurationDependency();
            var fileName = $"missing-{Guid.NewGuid():N}.json";

            var configuration = standard.LoadConfiguration(fileName, IsCommonConfiguration: true);

            Assert.NotNull(configuration);
            Assert.Null(configuration["Any:Missing:Key"]);
            Assert.NotNull(AppConfigurationLoader.CommonConfigChangeToken);
        }
    }
}
