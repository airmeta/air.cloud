import { createAppSettings } from './base_settings';

/**
 * zh-cn: 当前 SSO 应用层设置，等价于原项目壳层 settings.ts 的职责，
 * 用于显式提供 appId 与登录加密公钥。
 * en-us: Current SSO app-level settings, equivalent to the shell-layer settings.ts
 * in the original project, explicitly providing appId and login encryption public key.
 */
export const appSettings = createAppSettings({
  appId: '5340de08f1844e8b88367c1f3ba3f77d',
  appName: '统一身份认证平台',
  appPublicKey:
    '-----BEGIN PUBLIC KEY-----\n' +
    'MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAmj0GJKPR1oO4jwb4arVd\n' +
    'VgqaG8PTvzaME9eTPkcBkKSbsLNPof3TfaQqhlnRUw2aOI1gvUYav5JY8UpasCIO\n' +
    'uva0d+2UH3ene3PiagWQWHKOFbuLfiZjfjENG8n9z31CB2NclGuv4RhrtoyuxnwO\n' +
    'MCtILN3GK5A6NoODNHryCO2y3DK6jzlBbI2tUqF9SzvLdLtigHe1ZjT/S2SL3f0r\n' +
    '+JqmyC2jUXjRgkNW/xPaI4E/Gs7Ql+P7Jm6zWO2oyh0Yc+PRl2B6au90YKxwgAIZ\n' +
    'WNMenHkHbHfOtBCTMYkaKbGO24m/94F1Q/ofb0oE80d0dl2frZSEnTotLQX9QcX5\n' +
    'MwIDAQAB\n' +
    '-----END PUBLIC KEY-----\n',
  title: '单点登录',
});
