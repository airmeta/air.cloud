using Air.Cloud.Core.Plugins.Security.DES;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class DESCEncryptionTests
    {
        /// <summary>
        /// <para>zh-cn:测试 DES 加解密闭环场景，确认密文可被同一密钥正确还原。</para>
        /// <para>en-us:Tests DES round-trip behavior to ensure ciphertext can be restored with the same secret.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：使用固定 secret 对明文加密并解密，断言输出与输入完全一致。</para>
        /// <para>en-us:Process: encrypt and decrypt plaintext with fixed secret, then assert exact input/output match.</para>
        /// </remarks>
        [Fact]
        public void Encrypt_and_decrypt_should_roundtrip()
        {
            const string plain = "DES unit test payload";
            const string secret = "des-secret";

            var cipher = DESCEncryption.Encrypt(plain, secret);
            var restored = DESCEncryption.Decrypt(cipher, secret);

            Assert.Equal(plain, restored);
        }
    }
}
