using Air.Cloud.Core.Plugins.Security.SM2;

namespace Air.Cloud.UnitTest.Plugins.Security
{
    public class SM2EncryptionTests
    {
        /// <summary>
        /// <para>zh-cn:测试 SM2 加解密闭环场景，确认随机生成密钥对可正确恢复明文。</para>
        /// <para>en-us:Tests SM2 encryption/decryption round-trip to ensure generated key pair restores plaintext correctly.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：生成密钥对后执行 Encrypt/Decrypt，断言解密结果与输入内容一致。</para>
        /// <para>en-us:Process: generate key pair, run Encrypt/Decrypt, and assert decrypted text matches original content.</para>
        /// </remarks>
        [Fact]
        public void Encrypt_and_decrypt_should_roundtrip_with_generated_keys()
        {
            var (publicKey, privateKey) = SM2Encryption.GenerateKeyPair();
            const string plain = "SM2 encryption payload";

            var cipher = SM2Encryption.Encrypt(plain, publicKey);
            var restored = SM2Encryption.Decrypt(cipher, privateKey);

            Assert.Equal(plain, restored);
        }
    }
}
