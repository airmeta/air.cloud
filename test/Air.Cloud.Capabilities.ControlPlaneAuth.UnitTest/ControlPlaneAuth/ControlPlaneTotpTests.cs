using Air.Cloud.Capabilities.ControlPlaneAuth.Security;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.UnitTest.ControlPlaneAuth;

/// <summary>
/// <para>zh-cn:验证 ControlPlaneAuth TOTP 实现符合 RFC 6238 标准向量。</para>
/// <para>en-us:Verifies the ControlPlaneAuth TOTP implementation against RFC 6238 standard vectors.</para>
/// </summary>
public sealed class ControlPlaneTotpTests
{
    /// <summary>
    /// <para>zh-cn:验证标准 SHA-1 TOTP 向量，确保 Microsoft Authenticator 和 Google Authenticator 使用相同协议时可以互操作。</para>
    /// <para>en-us:Verifies standard SHA-1 TOTP vectors to ensure interoperability with Microsoft Authenticator and Google Authenticator using the same protocol.</para>
    /// </summary>
    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    public void VerifyCode_should_match_rfc6238_sha1_vectors(long unixTimeSeconds, string expectedCode)
    {
        const string secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        var result = ControlPlaneTotp.VerifyCode(
            secret,
            expectedCode,
            DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds),
            null,
            out _);

        Assert.True(result);
    }
}
