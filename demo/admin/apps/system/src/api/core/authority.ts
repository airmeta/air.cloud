import { requestClient } from '#/api/request';

const AUTHORITY_ACCOUNT_STORAGE_KEY = 'air-cloud-system:authority-account';
const APP_CONFIG_STORAGE_KEY = 'app_config';

export interface AuthorityItem {
  authority?: string;
  children?: AuthorityItem[];
  component?: string | null;
  hide?: number;
  icon?: string;
  id: string;
  parentId?: string | null;
  path: string;
  sortNumber?: number;
  title: string;
  type: number;
}

export interface AuthorityAccount {
  account: string;
  authoritys?: AuthorityItem[];
  currentAppId?: string;
  currentAppName?: string;
  id: string;
  user?: Record<string, any>;
  userGroups?: string[];
  userName?: string;
}

let authorityAccount: AuthorityAccount | undefined;
let authorityAccountPromise: Promise<AuthorityAccount> | undefined;
let authorityAccountVersion = 0;

function getCurrentAppId() {
  try {
    const appConfig = JSON.parse(
      sessionStorage.getItem(APP_CONFIG_STORAGE_KEY) || '{}',
    ) as { appId?: string };
    return appConfig.appId || sessionStorage.getItem('vea:auth:choose_app') || '';
  } catch {
    return sessionStorage.getItem('vea:auth:choose_app') || '';
  }
}

function hasLegacyRouteComponent(authoritys: AuthorityItem[] = []): boolean {
  return authoritys.some(
    (authority) =>
      authority.component?.startsWith('@room/views/') ||
      hasLegacyRouteComponent(authority.children),
  );
}

function readStoredAuthorityAccount() {
  try {
    const stored = JSON.parse(
      sessionStorage.getItem(AUTHORITY_ACCOUNT_STORAGE_KEY) || '{}',
    ) as { account?: AuthorityAccount; appId?: string };
    const account = stored.appId === getCurrentAppId() ? stored.account : undefined;
    if (account && hasLegacyRouteComponent(account.authoritys)) {
      sessionStorage.removeItem(AUTHORITY_ACCOUNT_STORAGE_KEY);
      return undefined;
    }
    return account;
  } catch {
    return undefined;
  }
}

function storeAuthorityAccount(account: AuthorityAccount) {
  sessionStorage.setItem(
    AUTHORITY_ACCOUNT_STORAGE_KEY,
    JSON.stringify({ account, appId: getCurrentAppId() }),
  );
}

/**
 * zh-cn: 获取当前 Ticket 和 AppId 对应的账户权限快照。同一轮页面初始化复用请求结果，
 * 避免用户信息和动态路由重复请求后端；登出时必须清除该快照。
 * en-us: Gets the account authority snapshot for the current Ticket and AppId. The result is
 * shared during one page initialization to prevent duplicate user-info and dynamic-route calls;
 * it must be cleared on logout.
 */
export async function getAuthorityAccountApi(): Promise<AuthorityAccount> {
  if (authorityAccount) {
    return authorityAccount;
  }

  const storedAccount = readStoredAuthorityAccount();
  if (storedAccount) {
    authorityAccount = storedAccount;
    return storedAccount;
  }

  const requestVersion = authorityAccountVersion;
  authorityAccountPromise ??= requestClient
    .get<AuthorityAccount>('/v1/identity/authority/account')
    .then((account) => {
      if (requestVersion === authorityAccountVersion) {
        authorityAccount = account;
        storeAuthorityAccount(account);
      }
      return account;
    })
    .finally(() => {
      authorityAccountPromise = undefined;
    });

  return authorityAccountPromise;
}

/**
 * zh-cn: 清除当前账户权限快照。仅在注销或认证身份明确切换时调用，确保下次初始化重新读取数据库权限。
 * en-us: Clears the current account authority snapshot. Call only on logout or an explicit
 * authentication-identity switch so the next initialization reloads database-backed authority.
 */
export function clearAuthorityAccountCache() {
  authorityAccountVersion += 1;
  authorityAccount = undefined;
  authorityAccountPromise = undefined;
  sessionStorage.removeItem(AUTHORITY_ACCOUNT_STORAGE_KEY);
}
