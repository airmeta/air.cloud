using Air.Cloud.Core.Standard.Account;
using Air.Cloud.Plugins.Jwt;

namespace Air.Cloud.UnitTest.Plugins.Jwt
{
    /// <summary>
    /// <para>zh-cn:JWT 功能相关测试集合。</para>
    /// <para>en-us:Test suite for JWT related behaviors.</para>
    /// </summary>
    public class JwtUnitTest
    {
        /// <summary>
        /// <para>zh-cn:验证使用当前配置生成的令牌可以被成功校验。</para>
        /// <para>en-us:Verifies that a token generated with the current settings can be validated successfully.</para>
        /// </summary>
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
