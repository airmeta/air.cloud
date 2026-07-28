using Air.Cloud.Core.App;
using Air.Cloud.Core.Modules.AppAspect;
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Core.Modules.AppAspect.Extensions;
using Air.Cloud.Core.Modules.AppAspect.Handler;
using Air.Cloud.Core.Modules.AppAspect.Model;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.IntegrationTest.Core.AppAspect;

/// <summary>
/// <para>zh-cn:验证 Aspect 扫描、排序和服务注册协作行为的集成测试。</para>
/// <para>en-us:Integration tests for Aspect scanning, ordering, and service registration working together.</para>
/// </summary>
public class AppAspectRegistrationIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 <c>AddAspect</c> 扫描带有多个 <see cref="AspectAttribute"/> 的方法时，会按配置的降序顺序保存元数据。</para>
    /// <para>en-us:Verifies <c>AddAspect</c> stores metadata in configured descending order when it scans a method with multiple <see cref="AspectAttribute"/> instances.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AppAspect")]
    public void AddAspect_should_preserve_configured_descending_order_in_metadata()
    {
        var originalAssemblies = AppCore.Assemblies;
        var originalMetadata = AppAspectOptionsBuilder.AspectMetadatas;
        var originalAspectTypes = AppAspectOptionsBuilder.UseAspectTypes;

        try
        {
            AppCore.Assemblies = new[] { typeof(AppAspectRegistrationIntegrationTests).Assembly.GetName() };
            var services = new ServiceCollection();

            services.AddAspect();

            var metadata = Assert.Single(AppAspectOptionsBuilder.AspectMetadatas.Values,
                item => item.AspectTargetType == typeof(OrderedAspectTarget));

            Assert.Collection(metadata.Aspects,
                aspect =>
                {
                    Assert.Equal(typeof(HigherPriorityAspect), aspect.AppAspectDependencies);
                    Assert.Equal(20, aspect.Order);
                },
                aspect =>
                {
                    Assert.Equal(typeof(LowerPriorityAspect), aspect.AppAspectDependencies);
                    Assert.Equal(5, aspect.Order);
                });
            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(HigherPriorityAspect));
            Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(LowerPriorityAspect));
        }
        finally
        {
            AppCore.Assemblies = originalAssemblies;
            AppAspectOptionsBuilder.AspectMetadatas = originalMetadata;
            AppAspectOptionsBuilder.UseAspectTypes = originalAspectTypes;
        }
    }

    /// <summary>
    /// <para>zh-cn:集成测试中用于承载多切面配置的目标方法。</para>
    /// <para>en-us:The target method that hosts multiple aspect configurations for the integration test.</para>
    /// </summary>
    public class OrderedAspectTarget
    {
        /// <summary>
        /// <para>zh-cn:声明顺序与执行优先级相反的测试方法，用于验证构建器排序。</para>
        /// <para>en-us:A test method whose declaration order is the reverse of its execution priority, used to verify builder ordering.</para>
        /// </summary>
        [Aspect(typeof(LowerPriorityAspect), 5)]
        [Aspect(typeof(HigherPriorityAspect), 20)]
        public void Execute()
        {
        }
    }

    /// <summary>
    /// <para>zh-cn:较低优先级的测试切面处理器。</para>
    /// <para>en-us:The lower-priority test aspect handler.</para>
    /// </summary>
    public class LowerPriorityAspect : IAspectExecuteHandler
    {
        /// <inheritdoc />
        public void Execute_Before(System.Reflection.MethodInfo methodInfo, object[] args)
        {
        }

        /// <inheritdoc />
        public void Execute_After(System.Reflection.MethodInfo methodInfo, object retValue)
        {
        }
    }

    /// <summary>
    /// <para>zh-cn:较高优先级的测试切面处理器。</para>
    /// <para>en-us:The higher-priority test aspect handler.</para>
    /// </summary>
    public class HigherPriorityAspect : IAspectExecuteHandler
    {
        /// <inheritdoc />
        public void Execute_Before(System.Reflection.MethodInfo methodInfo, object[] args)
        {
        }

        /// <inheritdoc />
        public void Execute_After(System.Reflection.MethodInfo methodInfo, object retValue)
        {
        }
    }
}
