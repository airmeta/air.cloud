using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:动态服务依赖标记与生命周期映射的单元测试。</para>
    /// <para>en-us:Unit tests for dynamic-service dependency markers and lifetime mapping.</para>
    /// </summary>
    public class DynamicServerStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证标准生命周期标记接口会映射到 Microsoft DI 对应生命周期。</para>
        /// <para>en-us:Verifies standard lifetime marker interfaces map to the matching Microsoft DI lifetimes.</para>
        /// </summary>
        [Theory]
        [InlineData(typeof(ITransient), ServiceLifetime.Transient)]
        [InlineData(typeof(IScoped), ServiceLifetime.Scoped)]
        [InlineData(typeof(ISingleton), ServiceLifetime.Singleton)]
        public void TryGetServiceLifetime_should_map_marker_interface_to_lifetime(Type markerType, ServiceLifetime expected)
        {
            var lifetime = DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(markerType);

            Assert.Equal(expected, lifetime);
        }

        /// <summary>
        /// <para>zh-cn:验证未知生命周期标记会抛出异常，避免错误接口被静默注册。</para>
        /// <para>en-us:Verifies unknown lifetime markers throw so invalid interfaces are not silently registered.</para>
        /// </summary>
        [Fact]
        public void TryGetServiceLifetime_should_throw_for_unknown_marker()
        {
            Assert.Throws<InvalidCastException>(() =>
                DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(typeof(IDisposable)));
        }
    }
}
