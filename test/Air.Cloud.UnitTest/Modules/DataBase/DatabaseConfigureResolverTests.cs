/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */

using Air.Cloud.Core.Standard.DataBase.Locators;
using Air.Cloud.EntityFrameWork.Core.Configure;
using Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;
using Air.Cloud.EntityFrameWork.Core.ContextPools;
using Air.Cloud.EntityFrameWork.Core.Extensions;
using Air.Cloud.EntityFrameWork.Core.Extensions.DatabaseProvider;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.UnitTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:数据库 Provider 配置器全局解析和自动注册测试。</para>
/// <para>en-us:Tests for global database Provider configurator resolution and automatic registration.</para>
/// </summary>
public sealed class DatabaseConfigureResolverTests
{
    /// <summary>
    /// <para>zh-cn:验证多个 Provider 可以根据 Locator 的 Provider 标识选择对应配置器。</para>
    /// <para>en-us:Verifies that multiple Providers resolve the matching configurator from the locator Provider key.</para>
    /// </summary>
    [Fact]
    public void Resolve_should_select_configure_by_locator_provider_key()
    {
        var oracle = new OracleConfigure();
        var mysql = new MySqlConfigure();
        var resolver = new DatabaseConfigureResolver(
            new IDatabaseConfigure[] { oracle, mysql },
            new IDatabaseConfigureDescriptor[]
            {
                new DatabaseConfigureDescriptor(typeof(OracleProvider), typeof(OracleConfigure)),
                new DatabaseConfigureDescriptor(typeof(MySqlProvider), typeof(MySqlConfigure))
            },
            new IDatabaseProvider[] { new OracleProvider(), new MySqlProvider() });

        Assert.Same(oracle, resolver.Resolve(typeof(TestDbContext), typeof(OracleLocator)));
        Assert.Same(mysql, resolver.Resolve(typeof(TestDbContext), typeof(MySqlLocator)));
    }

    /// <summary>
    /// <para>zh-cn:验证只注册一个 Provider 时仍保持默认配置器兼容行为。</para>
    /// <para>en-us:Verifies the compatibility fallback when only one Provider is registered.</para>
    /// </summary>
    [Fact]
    public void Resolve_should_fallback_when_only_one_provider_is_registered()
    {
        var configure = new OracleConfigure();
        var resolver = new DatabaseConfigureResolver(
            new IDatabaseConfigure[] { configure },
            new IDatabaseConfigureDescriptor[]
            {
                new DatabaseConfigureDescriptor("Oracle", typeof(OracleConfigure))
            });

        Assert.Same(configure, resolver.Resolve(typeof(TestDbContext), typeof(UnmarkedLocator)));
    }

    /// <summary>
    /// <para>zh-cn:验证多个 Provider 共存但 Locator 未声明 Provider 时不会依赖注册顺序。</para>
    /// <para>en-us:Verifies that an unmarked locator fails deterministically when multiple Providers coexist instead of depending on registration order.</para>
    /// </summary>
    [Fact]
    public void Resolve_should_reject_ambiguous_unmarked_locator()
    {
        var resolver = new DatabaseConfigureResolver(
            new IDatabaseConfigure[] { new OracleConfigure(), new MySqlConfigure() },
            new IDatabaseConfigureDescriptor[]
            {
                new DatabaseConfigureDescriptor("Oracle", typeof(OracleConfigure)),
                new DatabaseConfigureDescriptor("MySQL", typeof(MySqlConfigure))
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.Resolve(typeof(TestDbContext), typeof(UnmarkedLocator)));

        Assert.Contains("does not declare a Provider key", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证 Provider 包的注册扩展会自动注册配置器集合和全局解析器。</para>
    /// <para>en-us:Verifies that the Provider registration extension automatically registers configurators and the global resolver.</para>
    /// </summary>
    [Fact]
    public void AddDatabaseConfigure_should_register_provider_configurators_automatically()
    {
        var services = new ServiceCollection()
            .AddDatabaseConfigure<OracleConfigure, OracleProvider>()
            .AddDatabaseConfigure<MySqlConfigure, MySqlProvider>();

        using var provider = services.BuildServiceProvider();
        var resolver = provider.GetRequiredService<IDatabaseConfigureResolver>();

        Assert.IsType<OracleConfigure>(resolver.Resolve(typeof(TestDbContext), typeof(OracleLocator)));
        Assert.IsType<MySqlConfigure>(resolver.Resolve(typeof(TestDbContext), typeof(MySqlLocator)));
    }

    /// <summary>
    /// <para>zh-cn:验证标准 AddDb 注册在解析 DbContext 时使用全局配置器，而不是直接解析最后注册的实现。</para>
    /// <para>en-us:Verifies that standard AddDb registration uses the global configurator when resolving a DbContext instead of the last registered implementation.</para>
    /// </summary>
    [Fact]
    public void AddDb_should_resolve_configurator_for_the_locator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddDatabaseConfigure<OracleConfigure, OracleProvider>();
        services.AddDatabaseAccessor(options =>
        {
            options.AddDb<TestDbContext, OracleLocator>(connectionMetadata: "test");
        });

        using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<TestDbContext>();

        Assert.Contains("InMemory", context.Database.ProviderName, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证 Locator 不能静默绑定多个不同数据库上下文。</para>
    /// <para>en-us:Verifies that a locator cannot be silently rebound to multiple different DbContext types.</para>
    /// </summary>
    [Fact]
    public void RegisterDbContext_should_reject_conflicting_locator_binding()
    {
        var services = new ServiceCollection();
        services.RegisterDbContext<TestDbContext, ConflictLocator>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.RegisterDbContext<OtherDbContext, ConflictLocator>());

        Assert.Contains("cannot be rebound", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证公开事务管理器与默认上下文池解析为同一个作用域实例。</para>
    /// <para>en-us:Verifies that the public transaction manager resolves to the same scoped instance as the default context pool.</para>
    /// </summary>
    [Fact]
    public void AddDatabaseAccessor_should_expose_the_scoped_local_transaction_manager()
    {
        var services = new ServiceCollection();
        services.AddDatabaseAccessor();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var pool = scope.ServiceProvider.GetRequiredService<IDbContextPool>();
        var manager = scope.ServiceProvider.GetRequiredService<ILocalTransactionManager>();

        Assert.Same(pool, manager);
        Assert.False(manager.IsActive);
        Assert.Equal(0, manager.Depth);
    }

    [DatabaseProvider(typeof(OracleProvider))]
    private sealed class OracleLocator : IDbContextLocator
    {
    }

    [DatabaseProvider(typeof(MySqlProvider))]
    private sealed class MySqlLocator : IDbContextLocator
    {
    }

    private sealed class OracleProvider : IDatabaseProvider
    {
        public string ProviderKey => "Oracle";
    }

    private sealed class MySqlProvider : IDatabaseProvider
    {
        public string ProviderKey => "MySQL";
    }

    private sealed class UnmarkedLocator : IDbContextLocator
    {
    }

    private sealed class ConflictLocator : IDbContextLocator
    {
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }
    }

    private sealed class OtherDbContext : DbContext
    {
    }

    private sealed class OracleConfigure : IDatabaseConfigure
    {
        public DbContextOptionsBuilder Configure<TDbContext>(DbContextOptionsBuilder builder, string connectionMetadata)
            where TDbContext : DbContext => builder.UseInMemoryDatabase("database-configure-resolver-test");
    }

    private sealed class MySqlConfigure : IDatabaseConfigure
    {
        public DbContextOptionsBuilder Configure<TDbContext>(DbContextOptionsBuilder builder, string connectionMetadata)
            where TDbContext : DbContext => builder.UseInMemoryDatabase("database-configure-resolver-test-mysql");
    }
}
