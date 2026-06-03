using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.Assemblies;
using Air.Cloud.Core.Standard.Assemblies.Model;

using System.Reflection;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:程序集扫描标准默认实现的单元测试。</para>
    /// <para>en-us:Unit tests for the default assembly scanning standard.</para>
    /// </summary>
    [Collection("AppCoreStartupTests")]
    public class AssemblyScanningStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证扫描事件可以命中当前程序集中的公开实现类型，并在结束后执行 Finally 与清理事件集合。</para>
        /// <para>en-us:Verifies scanning events can match public implementation types in the current assembly, run Finally, and clear the event collection.</para>
        /// </summary>
        [Fact]
        public void Execute_should_invoke_matching_event_and_finally_then_clear_events()
        {
            var originalAssemblies = AppCore.Assemblies;
            var matchedTypes = new List<Type>();
            var finallyCalled = false;
            IAssemblyScanningStandard.Evensts.Clear();

            try
            {
                AppCore.Assemblies = new[] { typeof(PublicScanningTarget).Assembly.GetName() };
                var standard = new DefaultAssemblyScanningDependency();
                standard.Add(new AssemblyScanningEvent
                {
                    Key = "scan-test",
                    Description = "standard scan test",
                    TargetType = typeof(IScanningTarget),
                    Action = type => matchedTypes.Add(type),
                    Finally = () => finallyCalled = true
                });

                standard.Execute();

                Assert.Contains(typeof(PublicScanningTarget), matchedTypes);
                Assert.DoesNotContain(typeof(InternalScanningTarget), matchedTypes);
                Assert.True(finallyCalled);
                Assert.Empty(IAssemblyScanningStandard.Evensts);
            }
            finally
            {
                AppCore.Assemblies = originalAssemblies;
                IAssemblyScanningStandard.Evensts.Clear();
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:程序集扫描测试使用的公开目标接口。</para>
    /// <para>en-us:Public target interface used by assembly scanning tests.</para>
    /// </summary>
    public interface IScanningTarget
    {
    }

    /// <summary>
    /// <para>zh-cn:程序集扫描测试使用的公开实现，默认扫描应能发现。</para>
    /// <para>en-us:Public implementation used by assembly scanning tests and expected to be discovered.</para>
    /// </summary>
    public class PublicScanningTarget : IScanningTarget
    {
    }

    internal class InternalScanningTarget : IScanningTarget
    {
    }
}
