using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Gateway;

namespace Air.Cloud.DynamicApp.UnitTest.Gateway;

/// <summary>
/// <para>zh-cn:动态路由模板、顺序和冲突校验测试。</para>
/// <para>en-us:Tests for dynamic route templates, ordering, and conflict validation.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteMatcherTests
{
    [Fact]
    public void Match_should_extract_ocelot_style_parameters_and_build_downstream_path()
    {
        var route = CreateRoute("/001/{everything}", "/mods/system/{everything}", order: 10);
        var match = new DynamicAppGatewayRouteMatcher().Match(new[] { route }, "POST", "/001/fcj_common_api/manage/init");

        Assert.NotNull(match);
        Assert.Equal("fcj_common_api/manage/init", match!.Parameters["everything"]);
        Assert.Equal("/mods/system/fcj_common_api/manage/init", match.BuildDownstreamPath());
    }

    [Fact]
    public void Match_should_follow_order_instead_of_auto_specificity()
    {
        var parent = CreateRoute("/api/{everything}", "/parent/{everything}", order: 10);
        var child = CreateRoute("/api/users/{id}", "/child/{id}", order: 20);
        parent.RouteId = "parent";
        child.RouteId = "child";

        var match = new DynamicAppGatewayRouteMatcher().Match(new[] { parent, child }, "GET", "/api/users/42");

        Assert.NotNull(match);
        Assert.Equal("parent", match!.Route.RouteId);
    }

    [Fact]
    public void Validate_should_reject_exact_duplicate_and_parent_before_child()
    {
        var parent = CreateRoute("/api/{everything}", "/parent/{everything}", order: 10);
        var child = CreateRoute("/api/users/{id}", "/child/{id}", order: 20);
        var duplicate = CreateRoute("/api/users/{id}", "/other/{id}", order: 30);

        var errors = DynamicAppGatewayRouteValidator.Validate(new[] { parent, child, duplicate });

        Assert.Contains(errors, error => error.Contains("exactly identical", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("lower Order", StringComparison.Ordinal));
    }

    [Fact]
    public void Match_should_filter_http_methods_and_keep_query_out_of_path_matching()
    {
        var route = CreateRoute("/api/{id}", "/mods/{id}", order: 1);
        route.UpstreamHttpMethods = new[] { "POST" };
        var matcher = new DynamicAppGatewayRouteMatcher();

        Assert.Null(matcher.Match(new[] { route }, "GET", "/api/42"));
        Assert.NotNull(matcher.Match(new[] { route }, "POST", "/api/42"));
        Assert.Throws<ArgumentException>(() => DynamicAppGatewayRouteTemplate.Parse("/api/{id}?x=1"));
    }

    private static DynamicAppGatewayRoute CreateRoute(string upstream, string downstream, int order)
        => new()
        {
            RouteId = Guid.NewGuid().ToString("N"),
            UpstreamPathTemplate = upstream,
            DownstreamPathTemplate = downstream,
            TargetDynamicAppId = "platform",
            TargetModId = "system",
            Order = order
        };
}
