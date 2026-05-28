using Air.Cloud.Core.Standard.Taxin.Attributes;
using System.Reflection;

using Air.Cloud.UnitTest.Compatibility.Services;

namespace Air.Cloud.UnitTest.Modules.Taxin
{
    /// <summary>
    /// <para>zh-cn:Taxin 服务绑定元数据测试集合。</para>
    /// <para>en-us:Test suite for Taxin service binding metadata.</para>
    /// </summary>
    public class TaxinServiceBindingTests
    {
        /// <summary>
        /// <para>zh-cn:测试 TaxinService 特性绑定场景，确认 ClientB 声明了正确的服务名。</para>
        /// <para>en-us:Tests TaxinService attribute binding to ensure ClientB declares the expected service name.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：通过反射获取 ClientB 方法与特性，断言特性存在且 ServiceName 等于 taxin.service.test。</para>
        /// <para>en-us:Process: reflect ClientB method and attribute, then assert attribute exists and ServiceName equals taxin.service.test.</para>
        /// </remarks>
        [Fact]
        public void ClientB_should_declare_expected_taxin_service_attribute()
        {
            var method = GetClientBMethod();
            var attribute = method.GetCustomAttribute<TaxinServiceAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal("taxin.service.test", attribute.ServiceName);
        }

        /// <summary>
        /// <para>zh-cn:测试默认版本调用策略，确认未显式配置时使用全局 VersionSerialize 设定。</para>
        /// <para>en-us:Tests default version-call strategy to ensure global VersionSerialize is used when not explicitly set.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：反射读取 ClientB 的 TaxinService 特性，断言 VersionCalls 与 AppCore.Settings.VersionSerialize 一致。</para>
        /// <para>en-us:Process: read TaxinService attribute on ClientB via reflection and assert VersionCalls matches AppCore.Settings.VersionSerialize.</para>
        /// </remarks>
        [Fact]
        public void ClientB_should_use_default_version_calls_when_not_specified()
        {
            var method = GetClientBMethod();
            var attribute = method.GetCustomAttribute<TaxinServiceAttribute>();

            Assert.NotNull(attribute);
            Assert.Equal(Air.Cloud.Core.App.AppCore.Settings.VersionSerialize, attribute.VersionCalls);
        }

        /// <summary>
        /// <para>zh-cn:测试方法签名契约，确认 ClientB 仅接收一个 TaxinResponseModel 参数。</para>
        /// <para>en-us:Tests method signature contract to ensure ClientB accepts exactly one TaxinResponseModel parameter.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：反射获取 ClientB 参数列表，断言参数数量为 1 且类型为 TaxinResponseModel。</para>
        /// <para>en-us:Process: reflect ClientB parameter list and assert exactly one parameter of TaxinResponseModel type.</para>
        /// </remarks>
        [Fact]
        public void ClientB_should_accept_taxin_response_model_parameter()
        {
            var method = GetClientBMethod();
            var parameters = method.GetParameters();

            Assert.Single(parameters);
            Assert.Equal(typeof(TaxinResponseModel), parameters[0].ParameterType);
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
            var method = typeof(TaxinConnectService)
                .GetMethod("ClientB", BindingFlags.Public | BindingFlags.Instance);

            Assert.NotNull(method);
            return method;
        }
    }
}
