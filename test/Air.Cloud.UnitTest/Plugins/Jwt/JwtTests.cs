using Air.Cloud.Core.Standard.Account;
using Air.Cloud.Plugins.Jwt;

namespace Air.Cloud.UnitTest.Plugins.Jwt
{
    /// <summary>
    /// <para>zh-cn:JWT 相关行为测试集合。</para>
    /// <para>en-us:Test suite for JWT related behaviors.</para>
    /// </summary>
    public class JwtTests
    {
        /// <summary>
        /// <para>zh-cn:测试 JWT 生成与校验闭环场景，确认按当前配置签发的令牌可被成功验证。</para>
        /// <para>en-us:Tests JWT issue-and-validate round-trip to ensure tokens issued with current settings can be validated.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造测试账号生成 token，执行 Validate 后校验有效性、Issuer 与 Audience 是否符合预期。</para>
        /// <para>en-us:Process: create a test account, issue token, validate it, then assert validity, issuer, and audience expectations.</para>
        /// </remarks>
        [Fact]
        public void Validate_should_accept_token_generated_from_current_settings()
        {
            var token = JWTEncryption.Create<TestAccount>(new TestAccount
            {
                Id = "test-user-id",
                Account = "unit-test",
                AccountName = "Unit Test User"
            });

            var (isValid, validatedToken, validationResult) = JWTEncryption.Validate(token);

            Assert.True(isValid);
            Assert.NotNull(validatedToken);
            Assert.True(validationResult?.IsValid);
            Assert.Equal("san shi soft", validatedToken.Issuer);
            Assert.Equal("client", validatedToken.Audiences.Single());
        }

        /// <summary>
        /// <para>zh-cn:用于生成测试 JWT 的账户模型。</para>
        /// <para>en-us:Account model used to generate test JWT tokens.</para>
        /// </summary>
        private sealed class TestAccount : AccountStandard
        {
        }
    }
}
