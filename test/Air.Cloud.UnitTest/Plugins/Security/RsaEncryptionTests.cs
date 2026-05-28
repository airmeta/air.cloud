using Air.Cloud.Core.Plugins.Security.RSA;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class RsaEncryptionTests
    {
        /// <summary>
        /// <para>zh-cn:测试 RSA 动态密钥加解密闭环场景，确认生成密钥对可完成明文往返。</para>
        /// <para>en-us:Tests RSA round-trip with generated key pair to ensure plaintext can be encrypted and restored.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先生成公私钥，再执行加密与解密，断言解密结果与原始明文一致。</para>
        /// <para>en-us:Process: generate public/private keys, perform encryption and decryption, then assert restored text equals original plaintext.</para>
        /// </remarks>
        [Fact]
        public void Encrypt_and_decrypt_should_roundtrip_with_generated_keys()
        {
            var keys = RsaTools.CreateRSAKey();
            const string plain = "RSA unit test payload";

            var cipher = RsaEncryption.Encrypt(plain, keys.PublicKey, keys.PrivateKey);
            var restored = RsaEncryption.Decrypt(cipher, keys.PublicKey, keys.PrivateKey);

            Assert.Equal(plain, restored);
        }
    }
}
