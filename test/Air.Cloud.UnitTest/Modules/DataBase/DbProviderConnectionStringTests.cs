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

using Air.Cloud.Core.App;
using Air.Cloud.EntityFrameWork.Core.Contexts.Attributes;
using Air.Cloud.EntityFrameWork.Core.Internal;
using Air.Cloud.EntityFrameWork.Core.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:验证数据库连接字符串按 Air.Cloud 统一节点解析，同时保留历史来源兼容。</para>
/// <para>en-us:Verifies connection strings resolve from the unified Air.Cloud node while preserving legacy source compatibility.</para>
/// </summary>
public sealed class DbProviderConnectionStringTests
{
    /// <summary>
    /// <para>zh-cn:验证正式节点优先级、官方节点兼容、完整路径兼容以及原始连接字符串透传。</para>
    /// <para>en-us:Verifies canonical-node precedence, official-node compatibility, full-path compatibility, and raw connection-string pass-through.</para>
    /// </summary>
    [Fact]
    public void Should_resolve_connection_strings_from_the_unified_air_cloud_node()
    {
        var originalConfiguration = AppConfigurationLoader.Configurations;
        try
        {
            var configuration = new ConfigurationManager();
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataSourceSettings:ConnectionStrings:default"] = "Server=air-cloud;Database=canonical;",
                ["ConnectionStrings:default"] = "Server=official;Database=legacy;",
                ["ConnectionStrings:official"] = "Server=official-only;Database=legacy;",
                ["LegacyDatabase:ConnectionString"] = "Server=path;Database=legacy;",
                ["DataSourceSettings:ConnectionStrings:attribute"] = "Server=attribute;Database=canonical;"
            });
            AppConfigurationLoader.Configurations = configuration;

            var options = configuration.GetSection("DataSourceSettings").Get<DataSourceOptions>();
            Assert.NotNull(options);
            Assert.Equal(
                "Server=air-cloud;Database=canonical;",
                options!.GetConnectionString("default"));
            Assert.Equal(
                "Server=override;Database=memory;",
                options.SetConnectionString("override", "Server=override;Database=memory;")
            );
            Assert.Equal(
                "Server=override;Database=memory;",
                options.GetConnectionString("override"));

            Assert.Equal(
                "Server=air-cloud;Database=canonical;",
                DbProvider.GetConnectionString<TestDbContext>("default"));
            Assert.Equal(
                "Server=official-only;Database=legacy;",
                DbProvider.GetConnectionString<TestDbContext>("official"));
            Assert.Equal(
                "Server=path;Database=legacy;",
                DbProvider.GetConnectionString<TestDbContext>("LegacyDatabase:ConnectionString"));
            Assert.Equal(
                "Server=raw;Database=direct;",
                DbProvider.GetConnectionString<TestDbContext>("Server=raw;Database=direct;"));
            Assert.Equal(
                "Server=attribute;Database=canonical;",
                DbProvider.GetConnectionString<AttributeDbContext>());
            Assert.Equal(
                "missing",
                DbProvider.GetConnectionString<TestDbContext>("missing"));
        }
        finally
        {
            AppConfigurationLoader.Configurations = originalConfiguration;
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 AppConfiguration.Configuration 默认可以读取 InnerConfiguration 的配置。</para>
    /// <para>en-us:Verifies that AppConfiguration.Configuration reads values from InnerConfiguration by default.</para>
    /// </summary>
    [Fact]
    public void App_configuration_should_include_inner_configuration()
    {
        var originalConfiguration = AppConfigurationLoader.Configurations;
        var originalInnerConfiguration = AppConfigurationLoader.InnerConfiguration;
        try
        {
            var innerConfiguration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DataSourceSettings:ConnectionStrings:inner"] = "Server=inner;Database=configuration;"
                })
                .Build();

            AppConfigurationLoader.Configurations = new ConfigurationManager();
            AppConfigurationLoader.InnerConfiguration = innerConfiguration;

            Assert.Equal(
                "Server=inner;Database=configuration;",
                AppConfigurationLoader.InnerConfiguration["DataSourceSettings:ConnectionStrings:inner"]);
            Assert.Equal(
                "Server=inner;Database=configuration;",
                AppConfiguration.Configuration["DataSourceSettings:ConnectionStrings:inner"]);
        }
        finally
        {
            AppConfigurationLoader.InnerConfiguration = originalInnerConfiguration;
            AppConfigurationLoader.Configurations = originalConfiguration;
        }
    }

    private sealed class TestDbContext : DbContext
    {
    }

    [AppDbContext("attribute")]
    private sealed class AttributeDbContext : DbContext
    {
    }
}
