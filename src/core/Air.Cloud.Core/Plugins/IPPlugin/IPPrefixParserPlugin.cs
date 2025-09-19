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
using System;
using System.Net;
using System.Linq;
/// <summary>
/// <para>zh-cn: 前缀解析工具类</para>
/// <para>en-us: Prefix parsing utility class</para>
/// </summary>
public static class IPPrefixParserPlugin
{
    /// <summary>
    /// <para>zh-cn: 从HTTP前缀中解析出IP地址</para>
    /// <para>en-us: Parse IP address from HTTP prefix</para>
    /// </summary>
    /// <param name="prefix">
    /// <para>zh-cn: HTTP前缀字符串，如 "http://+:8080"</para>
    /// <para>en-us: HTTP prefix string, e.g. "http://+:8080"</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn: 解析后的IPAddress对象</para>
    /// <para>en-us: Parsed IPAddress object</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <para>zh-cn: 当参数为空或空白时抛出</para>
    /// <para>en-us: Thrown when parameter is null or empty</para>
    /// </exception>
    /// <exception cref="FormatException">
    /// <para>zh-cn: 当前缀格式无效时抛出</para>
    /// <para>en-us: Thrown when prefix format is invalid</para>
    /// </exception>
    public static IPAddress ParsePrefixToIPAddress(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));

        // 处理通配符情况
        if (prefix.Contains("://+") || prefix.Contains("://*"))
        {
            // 判断是IPv4还是IPv6通配符
            if (prefix.Contains("://[::]") || prefix.Contains("://[*]"))
                return IPAddress.IPv6Any;
            else
                return IPAddress.Any;
        }

        // 提取主机部分
        string hostPart = ExtractHostPartFromPrefix(prefix);

        // 处理特殊通配符
        if (hostPart == "+" || hostPart == "*")
            return IPAddress.Any;

        if (hostPart == "[::]" || hostPart == "[*]")
            return IPAddress.IPv6Any;

        // 解析IP地址
        try
        {
            return IPAddress.Parse(hostPart);
        }
        catch (FormatException ex)
        {
            throw new FormatException($"Invalid IP address in prefix: {hostPart}", ex);
        }
    }

    /// <summary>
    /// <para>zh-cn: 从前缀字符串中提取主机部分</para>
    /// <para>en-us: Extract host part from prefix string</para>
    /// </summary>
    /// <param name="prefix">
    /// <para>zh-cn: HTTP前缀字符串</para>
    /// <para>en-us: HTTP prefix string</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn: 主机名或IP地址部分</para>
    /// <para>en-us: Hostname or IP address part</para>
    /// </returns>
    /// <exception cref="FormatException">
    /// <para>zh-cn: 当格式无效时抛出</para>
    /// <para>en-us: Thrown when format is invalid</para>
    /// </exception>
    private static string ExtractHostPartFromPrefix(string prefix)
    {
        // 查找 :// 分隔符
        int protocolIndex = prefix.IndexOf("://");
        if (protocolIndex == -1)
            throw new FormatException("Invalid prefix format: missing '://'");

        string afterProtocol = prefix.Substring(protocolIndex + 3);

        // 处理IPv6地址（包含在方括号中）
        if (afterProtocol.StartsWith("["))
        {
            int closingBracketIndex = afterProtocol.IndexOf(']');
            if (closingBracketIndex == -1)
                throw new FormatException("Invalid IPv6 format: missing closing bracket");

            // 提取方括号内的内容
            return afterProtocol.Substring(1, closingBracketIndex - 1);
        }
        else
        {
            // 处理IPv4地址或主机名
            // 查找端口分隔符 :
            int portSeparatorIndex = afterProtocol.IndexOf(':');
            if (portSeparatorIndex == -1)
            {
                // 没有端口，整个就是主机部分
                return afterProtocol;
            }
            else
            {
                // 提取端口之前的部分
                return afterProtocol.Substring(0, portSeparatorIndex);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn: 从前缀中提取端口号</para>
    /// <para>en-us: Extract port number from prefix</para>
    /// </summary>
    /// <param name="prefix">
    /// <para>zh-cn: HTTP前缀字符串</para>
    /// <para>en-us: HTTP prefix string</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn: 端口号</para>
    /// <para>en-us: Port number</para>
    /// </returns>
    /// <exception cref="FormatException">
    /// <para>zh-cn: 当端口格式无效时抛出</para>
    /// <para>en-us: Thrown when port format is invalid</para>
    /// </exception>
    public static int GetPortFromPrefix(string prefix)
    {
        string afterProtocol = prefix.Contains("://")
            ? prefix.Split(new[] { "://" }, StringSplitOptions.None)[1]
            : prefix;

        // 查找最后一个冒号（处理IPv6情况）
        int lastColonIndex = afterProtocol.LastIndexOf(':');
        if (lastColonIndex == -1)
            throw new FormatException("No port specified in prefix");

        // 对于IPv6地址，需要跳过方括号内的内容
        if (afterProtocol.Contains('[') && afterProtocol.Contains(']'))
        {
            int bracketEnd = afterProtocol.IndexOf(']');
            if (bracketEnd != -1 && lastColonIndex < bracketEnd)
            {
                // 冒号在方括号内，不是端口分隔符
                throw new FormatException("Invalid port format for IPv6 address");
            }
        }

        string portString = afterProtocol.Substring(lastColonIndex + 1);
        if (int.TryParse(portString, out int port))
        {
            return port;
        }
        else
        {
            throw new FormatException($"Invalid port number: {portString}");
        }
    }

    /// <summary>
    /// <para>zh-cn: 解析主机名为IP地址（DNS解析）</para>
    /// <para>en-us: Resolve hostname to IP address (DNS resolution)</para>
    /// </summary>
    /// <param name="hostname">
    /// <para>zh-cn: 主机名</para>
    /// <para>en-us: Hostname</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn: 解析后的IP地址</para>
    /// <para>en-us: Resolved IP address</para>
    /// </returns>
    /// <exception cref="Exception">
    /// <para>zh-cn: 当DNS解析失败时抛出</para>
    /// <para>en-us: Thrown when DNS resolution fails</para>
    /// </exception>
    public static IPAddress ResolveHostname(string hostname)
    {
        try
        {
            var addresses = Dns.GetHostAddresses(hostname);
            return addresses.FirstOrDefault() ?? throw new Exception($"Could not resolve hostname: {hostname}");
        }
        catch (Exception ex)
        {
            throw new Exception($"DNS resolution failed for {hostname}", ex);
        }
    }

    /// <summary>
    /// <para>zh-cn: 增强版解析方法，支持主机名解析</para>
    /// <para>en-us: Enhanced parsing method with hostname resolution support</para>
    /// </summary>
    /// <param name="prefix">
    /// <para>zh-cn: HTTP前缀字符串</para>
    /// <para>en-us: HTTP prefix string</para>
    /// </param>
    /// <param name="resolveHostnames">
    /// <para>zh-cn: 是否解析主机名</para>
    /// <para>en-us: Whether to resolve hostnames</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn: 解析后的IPAddress对象</para>
    /// <para>en-us: Parsed IPAddress object</para>
    /// </returns>
    public static IPAddress ParsePrefixToIPAddressEnhanced(string prefix, bool resolveHostnames = false)
    {
        string hostPart = ExtractHostPartFromPrefix(prefix);

        // 处理通配符
        if (hostPart == "+" || hostPart == "*")
            return IPAddress.Any;

        if (hostPart == "[::]" || hostPart == "[*]")
            return IPAddress.IPv6Any;

        // 尝试直接解析为IP地址
        if (IPAddress.TryParse(hostPart, out IPAddress ipAddress))
        {
            return ipAddress;
        }
        else if (resolveHostnames)
        {
            // 如果不是IP地址且允许解析主机名，进行DNS解析
            return ResolveHostname(hostPart);
        }
        else
        {
            throw new FormatException($"Invalid IP address and hostname resolution is disabled: {hostPart}");
        }
    }
}