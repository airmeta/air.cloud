using System.Security.Cryptography;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Security;

/// <summary>
/// <para>zh-cn:控制面随机 Ticket 生成器。</para>
/// <para>en-us:Random ticket generator for the control plane.</para>
/// </summary>
public static class ControlPlaneTicketGenerator
{
    /// <summary>
    /// <para>zh-cn:生成 URL 安全的随机 Ticket，默认使用 32 字节随机数。</para>
    /// <para>en-us:Generates a URL-safe random ticket using 32 random bytes by default.</para>
    /// </summary>
    /// <param name="byteLength">
    /// <para>zh-cn:随机字节长度。</para>
    /// <para>en-us:Random byte length.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:URL 安全的随机 Ticket。</para>
    /// <para>en-us:A URL-safe random ticket.</para>
    /// </returns>
    public static string Create(int byteLength = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(Math.Clamp(byteLength, 16, 128));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
