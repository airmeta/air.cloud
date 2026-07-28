import { getDefaultSettings } from '#/base_settings';
import JSEncrypt from 'jsencrypt';

import type { SsoClientConfig } from './session';

/**
 * zh-cn: 根据后端客户端配置处理登录载荷加密，暂未匹配到加密方案时保持原文。
 * en-us: Encrypts the login payload according to backend client configuration, keeping plaintext when no scheme matches.
 *
 * @param payload
 * zh-cn: 登录请求原始 JSON 字符串。
 * en-us: Raw JSON string of the login request.
 * @param clientConfig
 * zh-cn: 后端初始化下发的客户端配置。
 * en-us: Client configuration returned by backend initialization.
 *
 * @returns
 * zh-cn: 可提交给后端的登录载荷内容。
 * en-us: Login payload content ready to submit to the backend.
 */
export async function encryptLoginPayload(
  payload: string,
  clientConfig: null | SsoClientConfig,
) {
  const appPublicKey = getDefaultSettings().appPublicKey;
  if (appPublicKey) {
    return encryptWithWebCryptoRsa(payload, appPublicKey);
  }

  if (clientConfig?.privateKey && clientConfig.appEntryptType === 0) {
    return encryptWithWebCryptoRsa(payload, clientConfig.privateKey);
  }

  return payload;
}

/**
 * zh-cn: 使用浏览器 Web Crypto RSA-OAEP 加密登录载荷，失败时回退为原文。
 * en-us: Encrypts login payload with browser Web Crypto RSA-OAEP and falls back to plaintext on failure.
 *
 * @param payload
 * zh-cn: 待加密登录载荷。
 * en-us: Login payload to encrypt.
 * @param publicKeyPem
 * zh-cn: PEM 格式公钥。
 * en-us: Public key in PEM format.
 */
async function encryptWithWebCryptoRsa(payload: string, publicKeyPem: string) {
  try {
    const encryptor = new JSEncrypt();
    encryptor.setPublicKey(publicKeyPem);
    return encryptor.encrypt(payload) || payload;
  } catch {
    return payload;
  }
}
