using Air.Cloud.Core.App;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:配置加载摘要单元测试，验证启动日志只提供来源定位和键数量，不泄露配置值。</para>
    /// <para>en-us:Configuration loading summary unit tests that verify startup output provides source metadata and key counts without exposing configuration values.</para>
    /// </summary>
    public class ConfigurationLoadSummaryTests
    {
        /// <summary>
        /// <para>zh-cn:验证摘要会统计有效配置项、保留配置来源定位，并移除 URI 中的用户凭据和全部配置值。</para>
        /// <para>en-us:Verifies the summary counts effective keys, preserves source metadata, and removes URI credentials and all configuration values.</para>
        /// </summary>
        [Fact]
        public void CreateContent_should_include_metadata_and_hide_configuration_values()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Feature:Enabled"] = "true",
                    ["Database:Password"] = "top-secret"
                })
                .Build();

            var content = ConfigurationLoadSummary.CreateContent(
                "Consul",
                "http://reader:top-secret@config.example:8500/",
                "order-service/appsettings.Production.json",
                configuration);

            Assert.Contains("来源:Consul", content);
            Assert.Contains("地址:http://config.example:8500/", content);
            Assert.Contains("配置定位:order-service/appsettings.Production.json", content);
            Assert.Contains("有效配置项:2个", content);
            Assert.DoesNotContain("reader", content);
            Assert.DoesNotContain("top-secret", content);
            Assert.DoesNotContain("true", content);
        }

        /// <summary>
        /// <para>zh-cn:验证空配置源的摘要会明确显示零个有效配置项。</para>
        /// <para>en-us:Verifies an empty configuration source is summarized with zero effective keys.</para>
        /// </summary>
        [Fact]
        public void CreateContent_should_report_zero_for_an_empty_configuration_source()
        {
            var configuration = new ConfigurationBuilder().Build();

            var content = ConfigurationLoadSummary.CreateContent("本地文件", @"C:\service", @"C:\service\appsettings.Production.json", configuration);

            Assert.Contains("未发现有效配置项", content);
            Assert.Contains("有效配置项:0个", content);
        }
    }
}
