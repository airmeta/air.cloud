using Air.Cloud.Core.Standard.Taxin.Attributes;
using System.Reflection;

namespace Air.Cloud.UnitTest.Modules.Taxin
{
    /// <summary>
    /// <para>zh-cn:Taxin 服务绑定元数据测试集合。</para>
    /// <para>en-us:Test suite for Taxin service binding metadata.</para>
    /// </summary>
    public class TaxinServiceBindingTests
    {
        /// <summary>
        /// <para>zh-cn:验证 ClientB 方法声明了预期的 TaxinService 特性与服务名称。</para>
        /// <para>en-us:Verifies that ClientB declares the expected TaxinService attribute and service name.</para>
        /// </summary>
        [Fact]
        public void ClientB_should_declare_expected_taxin_service_attribute()
        {
            var method = GetClientBMethod();
            var attribute = method.GetCustomAttribute<TaxinServiceAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal("taxin.service.test", attribute.ServiceName);
        }

        /// <summary>
        /// <para>zh-cn:验证 ClientB 方法的 TaxinService 特性默认版本调用信息与全局配置一致。</para>
        /// <para>en-us:Verifies that the default version call information on ClientB matches the global Taxin configuration.</para>
        /// </summary>
        [Fact]
        public void ClientB_should_use_default_version_calls_when_not_specified()
        {
            var method = GetClientBMethod();
            var attribute = method.GetCustomAttribute<TaxinServiceAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal(Air.Cloud.Core.App.AppCore.Settings.VersionSerialize, attribute.VersionCalls);
        }

        /// <summary>
        /// <para>zh-cn:验证 ClientB 方法接收的参数类型与原始 Taxin 响应模型一致。</para>
        /// <para>en-us:Verifies that the ClientB method parameter type matches the original Taxin response model.</para>
        /// </summary>
        [Fact]
        public void ClientB_should_accept_taxin_response_model_parameter()
        {
            var method = GetClientBMethod();
            var parameters = method.GetParameters();

            Assert.Single(parameters);
            Assert.Equal(typeof(unit.webapp.service.services.TaxinModuleTest.TaxinResponseModel), parameters[0].ParameterType);
        }

        /// <summary>
        /// <para>zh-cn:获取 TaxinConnectService 中的 ClientB 方法元数据。</para>
        /// <para>en-us:Gets the ClientB method metadata from TaxinConnectService.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:返回 ClientB 方法信息。</para>
        /// <para>en-us:Returns the ClientB method information.</para>
        /// </returns>
        private static MethodInfo GetClientBMethod()
        {
            var method = typeof(unit.webapp.service.services.TaxinModuleTest.TaxinConnectService)
                .GetMethod("ClientB", BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(method);
            return method;
        }
    }
}
