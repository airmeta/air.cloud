const SSO_STORAGE_PREFIX = 'air_sso_';
const CLIENT_CONFIG_KEY = `${SSO_STORAGE_PREFIX}client_config`;
const APP_CONFIG_KEY = `${SSO_STORAGE_PREFIX}app_config`;
const ACCOUNT_INFO_KEY = `${SSO_STORAGE_PREFIX}account_info`;
const CHOSEN_APP_KEY = `${SSO_STORAGE_PREFIX}chosen_app`;

export interface SsoClientConfig {
  /**
   * zh-cn: 应用加密类型，当前 0 表示尝试使用 RSA 加密登录载荷。
   * en-us: Application encryption type; currently 0 means trying RSA encryption for login payloads.
   */
  appEntryptType?: number;
  /**
   * zh-cn: 是否启用多因子认证，由后端初始化接口返回。
   * en-us: Whether MFA is enabled, returned by the backend initialization endpoint.
   */
  enableMFA?: number;
  /**
   * zh-cn: Ticket 过期秒数，用于前端展示或后续续期判断。
   * en-us: Ticket expiration in seconds, used for frontend display or later renewal checks.
   */
  expiredSeconds?: number;
  /**
   * zh-cn: 后端下发的登录加密公钥。
   * en-us: Login encryption public key issued by the backend.
   */
  privateKey?: string;
  /**
   * zh-cn: 当前 SSO 登录票据，是本应用唯一认证凭据。
   * en-us: Current SSO login Ticket, the only authentication credential used by this app.
   */
  ticket?: string;
}

export interface SsoAppConfig {
  /**
   * zh-cn: 当前客户端应用 AppId。
   * en-us: Current client application AppId.
   */
  appId: string;
  /**
   * zh-cn: 当前客户端应用名称。
   * en-us: Current client application name.
   */
  appName: string;
  /**
   * zh-cn: 后端指定的登录路径。
   * en-us: Login path specified by the backend.
   */
  loginPath?: string;
}

/**
 * zh-cn: 从 localStorage 读取 JSON 值，解析失败时返回 null。
 * en-us: Reads a JSON value from localStorage and returns null when parsing fails.
 *
 * @param key
 * zh-cn: localStorage 键名。
 * en-us: localStorage key.
 */
function readJson<T>(key: string): null | T {
  const value = localStorage.getItem(key);
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as T;
  } catch {
    return null;
  }
}

/**
 * zh-cn: 将对象序列化写入 localStorage，使同源 SSO 页面共享登录状态。
 * en-us: Serializes an object into localStorage so same-origin SSO pages share login state.
 *
 * @param key
 * zh-cn: localStorage 键名。
 * en-us: localStorage key.
 * @param value
 * zh-cn: 需要保存的结构化数据。
 * en-us: Structured data to persist.
 */
function writeJson<T>(key: string, value: T) {
  localStorage.setItem(key, JSON.stringify(value));
}

/**
 * zh-cn: 获取后端初始化下发的客户端配置。
 * en-us: Gets the client configuration returned by backend initialization.
 */
export function getClientConfig() {
  return readJson<SsoClientConfig>(CLIENT_CONFIG_KEY);
}

/**
 * zh-cn: 保存后端初始化下发的客户端配置。
 * en-us: Saves the client configuration returned by backend initialization.
 *
 * @param config
 * zh-cn: 客户端配置对象。
 * en-us: Client configuration object.
 */
export function setClientConfig(config: SsoClientConfig) {
  writeJson(CLIENT_CONFIG_KEY, config);
}

/**
 * zh-cn: 获取当前应用配置。
 * en-us: Gets current application configuration.
 */
export function getClientAppConfig() {
  return readJson<SsoAppConfig>(APP_CONFIG_KEY);
}

/**
 * zh-cn: 保存当前应用配置。
 * en-us: Saves current application configuration.
 *
 * @param config
 * zh-cn: 当前应用配置对象。
 * en-us: Current application configuration object.
 */
export function setClientAppConfig(config: SsoAppConfig) {
  writeJson(APP_CONFIG_KEY, config);
}

/**
 * zh-cn: 保存登录响应中的账号扩展载荷。
 * en-us: Saves the account extension payload from the login response.
 *
 * @param payload
 * zh-cn: 后端返回的账号扩展数据。
 * en-us: Account extension data returned by the backend.
 */
export function setAccountInfo(payload: Record<string, any>) {
  writeJson(ACCOUNT_INFO_KEY, payload);
}

/**
 * zh-cn: 获取登录响应中的账号扩展载荷。
 * en-us: Gets the account extension payload from the login response.
 */
export function getAccountInfo() {
  return readJson<Record<string, any>>(ACCOUNT_INFO_KEY);
}

/**
 * zh-cn: 判断当前浏览器是否已建立 SSO 登录态。初始化接口下发的临时 Ticket 仅用于登录前请求签名，不能作为登录态。
 * en-us: Determines whether the browser has an established SSO login session. The temporary Ticket issued by initialization is only for pre-login request signing and is not login state.
 */
export function isLoggedIn() {
  return getAccountInfo() !== null;
}

/**
 * zh-cn: 获取当前 SSO Ticket。
 * en-us: Gets the current SSO Ticket.
 */
export function getTicket() {
  return getClientConfig()?.ticket || '';
}

/**
 * zh-cn: 将登录成功后的 Ticket 写入客户端配置。
 * en-us: Writes the Ticket returned by login into the client configuration.
 *
 * @param ticket
 * zh-cn: 后端签发的 SSO Ticket。
 * en-us: SSO Ticket issued by the backend.
 */
export function setTicket(ticket: string) {
  const current = getClientConfig() ?? {};
  setClientConfig({
    ...current,
    ticket,
  });
}

/**
 * zh-cn: 获取当前客户端 AppId。
 * en-us: Gets the current client AppId.
 */
export function getClientAppId() {
  return getClientAppConfig()?.appId || '';
}

/**
 * zh-cn: 保存用户在应用中心选择的目标 AppId。
 * en-us: Saves the target AppId selected by the user in the app center.
 *
 * @param appId
 * zh-cn: 目标应用 AppId。
 * en-us: Target application AppId.
 */
export function setChosenApp(appId: string) {
  localStorage.setItem(CHOSEN_APP_KEY, appId);
}

/**
 * zh-cn: 获取用户在应用中心选择的目标 AppId。
 * en-us: Gets the target AppId selected by the user in the app center.
 */
export function getChosenApp() {
  return localStorage.getItem(CHOSEN_APP_KEY) || '';
}

/**
 * zh-cn: 清理全部 SSO localStorage 状态。
 * en-us: Clears all SSO localStorage state.
 */
/**
 * zh-cn: 清理浏览器中所有 SSO 前缀缓存，包括初始化票据、应用配置、账号信息、已选择应用以及后续新增的 SSO 缓存，确保重新进入登录页时重新触发 init。
 * en-us: Clears every browser cache entry with the SSO prefix, including initialization tickets, app config, account info, selected apps, and future SSO cache entries, so the login page triggers init again.
 */
export function clearSsoSession() {
  // Logout keeps no business cache in the current browser origin.
  localStorage.clear();
  sessionStorage.clear();
}
