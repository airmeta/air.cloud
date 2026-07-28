using Air.Cloud.Core.Standard.Taxin.Model;
using Air.Cloud.Modules.Taxin.Tools;

namespace Air.Cloud.UnitTest.Modules.Taxin
{
    /// <summary>
    /// <para>zh-cn:Taxin 路由参数绑定工具测试集合。</para>
    /// <para>en-us:Test suite for the Taxin route-parameter binder.</para>
    /// </summary>
    public class TaxinRouteParameterBinderTests
    {
        /// <summary>
        /// <para>zh-cn:验证匿名对象中的 Ticket 仅替换路由占位符，并进行路径 URI 编码。</para>
        /// <para>en-us:Verifies that Ticket from an anonymous object replaces only the route placeholder and is URI-escaped for a path.</para>
        /// </summary>
        [Fact]
        public void Bind_should_replace_ticket_placeholder_and_escape_path_value()
        {
            var route = CreateRoute("Ticket/payload/{Ticket}");

            var result = TaxinRouteParameterBinder.Bind(route, new { Ticket = "ticket/a b" });

            Assert.Equal("Ticket/payload/ticket%2Fa%20b", result);
        }

        /// <summary>
        /// <para>zh-cn:验证可选路由参数没有传入时会移除其完整路径段，同时保留已绑定的约束参数。</para>
        /// <para>en-us:Verifies that an omitted optional route parameter removes its complete path segment while a constrained parameter remains bound.</para>
        /// </summary>
        [Fact]
        public void Bind_should_omit_optional_segment_and_bind_constrained_parameter()
        {
            var route = CreateRoute("v1/orders/{Id:int}/{Slug?}");

            var result = TaxinRouteParameterBinder.Bind(route, new Dictionary<string, object> { ["Id"] = 42 });

            Assert.Equal("v1/orders/42", result);
        }

        /// <summary>
        /// <para>zh-cn:验证方法参数元数据声明为可选时，即使路由模板未使用问号也会省略完整路径段。</para>
        /// <para>en-us:Verifies that a complete path segment is omitted when method-parameter metadata marks it optional even if the route template has no question mark.</para>
        /// </summary>
        [Fact]
        public void Bind_should_omit_segment_when_route_parameter_metadata_is_optional()
        {
            var route = CreateRoute("v1/orders/{Slug}");
            route.Parameters.Add(new TaxinRouteParameter { Name = "Slug", IsOptional = true });

            var result = TaxinRouteParameterBinder.Bind(route, new { });

            Assert.Equal("v1/orders", result);
        }

        /// <summary>
        /// <para>zh-cn:验证缺少必填参数时不会发送包含原始占位符的地址，而是抛出明确异常。</para>
        /// <para>en-us:Verifies that a missing required parameter raises a clear exception instead of sending an address containing the raw placeholder.</para>
        /// </summary>
        [Fact]
        public void Bind_should_throw_when_required_parameter_is_missing()
        {
            var route = CreateRoute("Ticket/payload/{Ticket}");

            var exception = Assert.Throws<InvalidOperationException>(() => TaxinRouteParameterBinder.Bind(route, new { }));

            Assert.Contains("Ticket", exception.Message, StringComparison.Ordinal);
        }

        private static TaxinRouteInformation CreateRoute(string route)
        {
            return new TaxinRouteInformation
            {
                Route = route,
                Parameters = new List<TaxinRouteParameter>()
            };
        }
    }
}
