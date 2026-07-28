using Air.Cloud.Core.Aspects;
using Air.Cloud.Core.Modules.AppAspect.Attributes;

namespace Air.Cloud.UnitTest.Core.AppAspect;

/// <summary>
/// <para>zh-cn:验证 <see cref="AspectAttribute"/> 配置值的单元测试。</para>
/// <para>en-us:Unit tests that verify <see cref="AspectAttribute"/> configuration values.</para>
/// </summary>
public class AspectAttributeTests
{
    /// <summary>
    /// <para>zh-cn:验证构造函数会保留调用方配置的切面执行顺序，而不是将其重置为默认值。</para>
    /// <para>en-us:Verifies the constructor preserves the caller-supplied execution order instead of resetting it to the default value.</para>
    /// </summary>
    /// <param name="order">
    /// <para>zh-cn:调用方配置的切面顺序。</para>
    /// <para>en-us:The caller-supplied aspect order.</para>
    /// </param>
    [Theory]
    [InlineData(-10)]
    [InlineData(1)]
    [InlineData(20)]
    public void Constructor_should_preserve_configured_order(int order)
    {
        var attribute = new AspectAttribute(typeof(ExecuteMethodPrinterAspectHandler), order);

        Assert.Equal(order, attribute.Order);
    }
}
