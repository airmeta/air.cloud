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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace air.gateway.Configuration;

/// <summary>
/// <para>zh-cn:集中判断 AirGateway 控制面路径和独立管理监听策略，避免业务插件或 Ocelot 把控制面请求当成普通业务流量处理。</para>
/// <para>en-us:Centralizes control-plane path and dedicated-management-listener checks so business plugins and Ocelot do not treat management requests as ordinary traffic.</para>
/// </summary>
internal static class ControlPlaneManagementPolicy
{
    public static bool IsControlPlanePath(HttpContext context, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("ControlPlaneAuth");
        if (!section.Exists())
        {
            return false;
        }

        var basePath = NormalizePath(section["BasePath"], "/_control-plane");
        var authPath = NormalizePath(section["AuthPath"], basePath + "/auth");
        var planePath = NormalizePath(section["PlanePath"], basePath);
        return context.Request.Path.StartsWithSegments(authPath)
            || context.Request.Path.StartsWithSegments(planePath);
    }

    public static bool IsManagementEnabled(IConfiguration configuration)
    {
        return configuration.GetSection("ControlPlaneAuth").GetValue("Enabled", true)
            && configuration.GetSection("ControlPlaneAuth:Management").GetValue("Enabled", true);
    }

    public static int GetManagementPort(IConfiguration configuration) =>
        configuration.GetSection("ControlPlaneAuth:Management").GetValue("Port", 0);

    public static bool IsRestrictedToPort(IConfiguration configuration) =>
        configuration.GetSection("ControlPlaneAuth:Management").GetValue("RestrictToPort", true);

    private static string NormalizePath(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        normalized = "/" + normalized.Trim('/');
        return normalized == "/" ? fallback : normalized;
    }
}
