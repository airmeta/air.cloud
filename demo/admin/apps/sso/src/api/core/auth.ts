import { getDefaultSettings } from '#/base_settings';
import { requestClient } from '#/api/request';
import { encryptLoginPayload } from '#/utils/sso/encrypt';
import { getClientConfig } from '#/utils/sso/session';

export namespace SsoAuthApi {
  export interface ClientConfig {
    /**
     * zh-cn: 应用加密类型，当前用于决定登录载荷是否尝试 RSA 加密。
     * en-us: Application encryption type, currently used to decide whether login payloads try RSA encryption.
     */
    appEntryptType?: number;
    /**
     * zh-cn: 是否启用多因子认证。
     * en-us: Whether multi-factor authentication is enabled.
     */
    enableMFA?: number;
    /**
     * zh-cn: Ticket 有效期秒数。
     * en-us: Ticket lifetime in seconds.
     */
    expiredSeconds?: number;
    /**
     * zh-cn: 登录载荷加密公钥。
     * en-us: Public key used to encrypt login payloads.
     */
    privateKey?: string;
    /**
     * zh-cn: 初始化阶段下发的客户端 Ticket。
     * en-us: Client Ticket returned during initialization.
     */
    ticket: string;
  }

  export interface AppStatus {
    /**
     * zh-cn: 当前应用 AppId。
     * en-us: Current application AppId.
     */
    appId: string;
    /**
     * zh-cn: 当前应用名称。
     * en-us: Current application name.
     */
    appName: string;
    /**
     * zh-cn: 后端下发的客户端配置。
     * en-us: Client configuration returned by the backend.
     */
    client: ClientConfig;
    /**
     * zh-cn: 系统是否已经存在应用配置。
     * en-us: Whether the system already has application configuration.
     */
    hasApp: boolean;
    /**
     * zh-cn: 后端指定的登录路径。
     * en-us: Login path specified by the backend.
     */
    loginPath?: string;
    /**
     * zh-cn: 应用扩展配置。
     * en-us: Application extension settings.
     */
    settings?: Record<string, string>;
  }

  export interface LoginParams {
    /**
     * zh-cn: 图形验证码。
     * en-us: Captcha code.
     */
    code: string;
    /**
     * zh-cn: 登录密码。
     * en-us: Login password.
     */
    password: string;
    /**
     * zh-cn: 登录账号。
     * en-us: Login account name.
     */
    username: string;
  }

  export interface LoginResult {
    /**
     * zh-cn: 后端账号状态码。
     * en-us: Backend account status code.
     */
    accountStatus: number;
    /**
     * zh-cn: 客户端随机标识。
     * en-us: Client random identifier.
     */
    clientUUID?: string;
    /**
     * zh-cn: 是否启用多因子认证。
     * en-us: Whether multi-factor authentication is enabled.
     */
    enableMFA: number;
    /**
     * zh-cn: Ticket 过期时间。
     * en-us: Ticket expiration time.
     */
    expiredTime: string;
    /**
     * zh-cn: 登录成功后的账号扩展载荷。
     * en-us: Account extension payload after successful login.
     */
    payload: Record<string, string>;
    /**
     * zh-cn: 后端签发的 SSO Ticket。
     * en-us: SSO Ticket issued by the backend.
     */
    ticket: string;
  }

  export interface CaptchaResult {
    /**
     * zh-cn: Base64 编码的验证码图片。
     * en-us: Base64-encoded captcha image.
     */
    captchaBase64: string;
  }

  export interface AccountApp {
    /**
     * zh-cn: 应用 AppId。
     * en-us: Application AppId.
     */
    appId: string;
    /**
     * zh-cn: 应用名称。
     * en-us: Application name.
     */
    appName: string;
    /**
     * zh-cn: 应用跳转随机键。
     * en-us: Random key for application redirection.
     */
    appRedirectKey?: string;
    /**
     * zh-cn: 应用跳转地址。
     * en-us: Application redirect URL.
     */
    appRedirectUrl?: string;
    /**
     * zh-cn: 应用创建时间。
     * en-us: Application creation time.
     */
    createTime?: string;
    /**
     * zh-cn: 应用描述。
     * en-us: Application description.
     */
    description?: string;
    /**
     * zh-cn: 应用 Logo 地址。
     * en-us: Application logo URL.
     */
    logoUrl?: string;
  }

  export interface InjectFirstAppParams {
    /**
     * zh-cn: 应用加密类型。
     * en-us: Application encryption type.
     */
    appEncryptType: number;
    /**
     * zh-cn: 首个应用 AppId。
     * en-us: First application AppId.
     */
    appId: string;
    /**
     * zh-cn: 首个应用名称。
     * en-us: First application name.
     */
    appName: string;
    /**
     * zh-cn: 首个应用回调地址。
     * en-us: First application callback URL.
     */
    appRedirectUrl: string;
    /**
     * zh-cn: 默认管理员账号。
     * en-us: Default administrator account.
     */
    defaultAccount: string;
    /**
     * zh-cn: 默认应用 AppId。
     * en-us: Default application AppId.
     */
    defaultAppId?: string;
    /**
     * zh-cn: 应用登录加密私钥或密钥配置。
     * en-us: Application login encryption private key or key configuration.
     */
    privateKey: string;
  }
}

/**
 * zh-cn: 初始化 SSO 应用状态，未传 AppId 时读取默认初始化状态。
 * en-us: Initializes SSO application status, reading default initialization state when AppId is absent.
 *
 * @param appId
 * zh-cn: 可选应用 AppId。
 * en-us: Optional application AppId.
 */
export async function initAppStatusApi(appId?: string) {
  const resolvedAppId = appId || getDefaultSettings().appId || '';
  return requestClient.get<SsoAuthApi.AppStatus>(`/v1/pub/init/${resolvedAppId}`);
}

/**
 * zh-cn: 获取登录验证码图片。
 * en-us: Gets the login captcha image.
 */
export async function getCaptchaApi() {
  return requestClient.get<SsoAuthApi.CaptchaResult>('/v1/pub/captcha');
}

/**
 * zh-cn: 使用账号、密码和验证码登录，登录载荷会按客户端配置进行加密适配。
 * en-us: Logs in with account, password, and captcha, adapting payload encryption from client configuration.
 *
 * @param data
 * zh-cn: 登录表单参数。
 * en-us: Login form parameters.
 */
export async function loginApi(data: SsoAuthApi.LoginParams) {
  const clientConfig = getClientConfig();
  return requestClient.post<SsoAuthApi.LoginResult>('/v1/identity/login', {
    content: await encryptLoginPayload(
      JSON.stringify({
        Account: data.username,
        Code: data.code,
        Password: data.password,
      }),
      clientConfig,
    ),
  });
}

/**
 * zh-cn: 退出当前 SSO 会话。跨应用退出时传入签发 Ticket 的业务应用 AppId，确保请求头和签名使用同一应用标识。
 * en-us: Logs out of the current SSO session. Cross-application logout accepts the business AppId that issued the Ticket so headers and signature use the same application identity.
 *
 * @param appId
 * zh-cn: 可选的业务应用 AppId；未传时使用 SSO 默认应用 AppId。
 * en-us: Optional business AppId; the SSO default AppId is used when omitted.
 */
export async function logoutApi(appId?: string) {
  return requestClient.get('/v1/identity/logout', { appId });
}

/**
 * zh-cn: 注入首个系统应用，供空系统初始化使用。
 * en-us: Injects the first system application for empty-system initialization.
 *
 * @param data
 * zh-cn: 首个应用初始化参数。
 * en-us: First application initialization parameters.
 */
export async function injectFirstAppApi(data: SsoAuthApi.InjectFirstAppParams) {
  return requestClient.post<boolean>('/v1/security/system/app/inject', data);
}

/**
 * zh-cn: 查询当前账号可进入的应用列表。
 * en-us: Queries applications available to the current account.
 */
export async function queryAccountAppsApi() {
  return requestClient.get<SsoAuthApi.AccountApp[]>('/v1/identity/account/apps');
}

/**
 * zh-cn: 创建进入目标应用的跳转地址。
 * en-us: Creates the redirect URL for entering a target application.
 *
 * @param appId
 * zh-cn: 目标应用 AppId。
 * en-us: Target application AppId.
 */
export async function createAppRedirectUrlApi(appId: string) {
  return requestClient.get<string>(`/v1/identity/authority/go/${appId}`);
}

/**
 * zh-cn: 根据随机键自动登录授权来源应用。
 * en-us: Automatically logs in to the authorization source application by random key.
 *
 * @param key
 * zh-cn: 后端签发的自动登录随机键。
 * en-us: Auto-login random key issued by the backend.
 */
export async function autoLoginApi(key: string) {
  return requestClient.get<Record<string, any>>(`/v1/identity/autologin/${key}`);
}

/**
 * zh-cn: 拒绝本次应用授权并通知后端清理随机键。
 * en-us: Rejects the current application authorization and asks the backend to clear the random key.
 *
 * @param key
 * zh-cn: 本次授权随机键。
 * en-us: Random key of the current authorization.
 */
export async function rejectAuthorizationApi(key: string) {
  return requestClient.get<boolean>(`/v1/identity/authority/reject/${key}`);
}
