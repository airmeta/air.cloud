using Air.Cloud.Core.Plugins.Security.SM2;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class SM2SigningTests
    {
        /// <summary>
        /// <para>zh-cn:测试 SM2 签名验签场景，确认原文与签名组合可被公钥成功验证。</para>
        /// <para>en-us:Tests SM2 signing/verification to ensure original content and signature can be verified by public key.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：生成密钥后对内容签名并执行验签，断言验证结果为 true。</para>
        /// <para>en-us:Process: generate keys, sign content, verify signature, and assert verification is true.</para>
        /// </remarks>
        [Fact]
        public void Sign_and_verify_should_return_true_for_original_content()
        {
            var (publicKey, privateKey) = SM2Encryption.GenerateKeyPair();
            const string content = "SM2 sign payload";

            var sign = SM2Signing.Sign(content, privateKey);
            var verified = SM2Signing.VerifySign(content, sign, publicKey);

            Assert.True(verified);
        }
    }
}
