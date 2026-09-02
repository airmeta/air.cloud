using Microsoft.AspNetCore.DataProtection;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Security;

/// <summary>
/// <para>zh-cn:保护控制面 MFA 密钥和一次性绑定凭证。生产环境应使用持久化的 Data Protection 密钥目录，确保服务重启后仍可解密已绑定的 Authenticator 密钥。</para>
/// <para>en-us:Protects control-plane MFA secrets and one-time enrollment credentials. Production hosts should use a persistent Data Protection key directory so enrolled Authenticator secrets remain decryptable after restart.</para>
/// </summary>
public sealed class ControlPlaneMfaSecretProtector
{
    private readonly IDataProtector secretProtector;
    private readonly IDataProtector setupTokenProtector;

    /// <summary>
    /// <para>zh-cn:创建 MFA 密钥保护器。</para>
    /// <para>en-us:Creates the MFA secret protector.</para>
    /// </summary>
    /// <param name="provider"><para>zh-cn:数据保护提供程序。</para><para>en-us:Data Protection provider.</para></param>
    public ControlPlaneMfaSecretProtector(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        secretProtector = provider.CreateProtector("Air.Cloud.ControlPlaneAuth.MfaSecret.v1");
        setupTokenProtector = provider.CreateProtector("Air.Cloud.ControlPlaneAuth.MfaSetupToken.v1");
    }

    /// <summary>
    /// <para>zh-cn:加密保存 TOTP 密钥。</para>
    /// <para>en-us:Protects a TOTP secret for persistence.</para>
    /// </summary>
    /// <param name="secret"><para>zh-cn:TOTP 明文密钥。</para><para>en-us:Clear-text TOTP secret.</para></param>
    /// <returns><para>zh-cn:受保护的密钥。</para><para>en-us:Protected secret.</para></returns>
    public string ProtectSecret(string secret) => secretProtector.Protect(secret);

    /// <summary>
    /// <para>zh-cn:解密 TOTP 密钥。</para>
    /// <para>en-us:Unprotects a TOTP secret.</para>
    /// </summary>
    /// <param name="protectedSecret"><para>zh-cn:受保护的密钥。</para><para>en-us:Protected secret.</para></param>
    /// <returns><para>zh-cn:TOTP 明文密钥。</para><para>en-us:Clear-text TOTP secret.</para></returns>
    public string UnprotectSecret(string protectedSecret) => secretProtector.Unprotect(protectedSecret);

    /// <summary>
    /// <para>zh-cn:保护一次性 MFA 绑定凭证。</para>
    /// <para>en-us:Protects a one-time MFA enrollment credential.</para>
    /// </summary>
    /// <param name="payload"><para>zh-cn:绑定凭证载荷。</para><para>en-us:Enrollment-token payload.</para></param>
    /// <returns><para>zh-cn:受保护的绑定凭证。</para><para>en-us:Protected enrollment token.</para></returns>
    public string ProtectSetupToken(string payload) => setupTokenProtector.Protect(payload);

    /// <summary>
    /// <para>zh-cn:解开一次性 MFA 绑定凭证。</para>
    /// <para>en-us:Unprotects a one-time MFA enrollment credential.</para>
    /// </summary>
    /// <param name="token"><para>zh-cn:受保护的绑定凭证。</para><para>en-us:Protected enrollment token.</para></param>
    /// <returns><para>zh-cn:绑定凭证载荷。</para><para>en-us:Enrollment-token payload.</para></returns>
    public string UnprotectSetupToken(string token) => setupTokenProtector.Unprotect(token);
}
