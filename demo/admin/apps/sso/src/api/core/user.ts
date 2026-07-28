import { requestClient } from '#/api/request';
import type { SsoUserInfo } from '#/types';

export interface SsoAuthorityItem {
  /**
   * zh-cn: 后端权限标识，通常用于按钮、菜单或接口级授权判断。
   * en-us: Backend authority code, usually used for button, menu, or API-level authorization.
   */
  authority?: string;
  /**
   * zh-cn: 子级权限节点，保留后端树形权限结构。
   * en-us: Child authority nodes, preserving the backend tree-shaped permission structure.
   */
  children?: SsoAuthorityItem[];
  /**
   * zh-cn: 后端菜单组件路径，SSO 当前不把它转换为动态路由。
   * en-us: Backend menu component path; SSO does not currently convert it into dynamic routes.
   */
  component?: string;
  /**
   * zh-cn: 后端隐藏标记，通常 1 表示隐藏菜单。
   * en-us: Backend hidden flag, commonly with 1 meaning the menu is hidden.
   */
  hide?: number;
  /**
   * zh-cn: 菜单图标名称。
   * en-us: Menu icon name.
   */
  icon?: string;
  /**
   * zh-cn: 权限节点唯一标识。
   * en-us: Unique authority node identifier.
   */
  id: string;
  /**
   * zh-cn: 后端扩展元数据，保留原始字符串。
   * en-us: Backend extension metadata, preserved as the original string.
   */
  meta?: string;
  /**
   * zh-cn: 父级权限节点标识。
   * en-us: Parent authority node identifier.
   */
  parentId?: string;
  /**
   * zh-cn: 权限或菜单路径。
   * en-us: Authority or menu path.
   */
  path: string;
  /**
   * zh-cn: 后端排序号。
   * en-us: Backend sort number.
   */
  sortNumber?: number;
  /**
   * zh-cn: 权限节点显示标题。
   * en-us: Display title of the authority node.
   */
  title: string;
  /**
   * zh-cn: 后端节点类型，具体含义由安全服务定义。
   * en-us: Backend node type, with semantics defined by the security service.
   */
  type?: number;
}

export interface SsoUserAccountInfo {
  /**
   * zh-cn: 登录账号名。
   * en-us: Login account name.
   */
  account: string;
  /**
   * zh-cn: 当前账号在目标应用下的权限集合。
   * en-us: Authority collection of the current account in the target application.
   */
  authoritys?: SsoAuthorityItem[];
  /**
   * zh-cn: 当前应用 AppId。
   * en-us: Current application AppId.
   */
  currentAppId?: string;
  /**
   * zh-cn: 当前应用名称。
   * en-us: Current application name.
   */
  currentAppName?: string;
  /**
   * zh-cn: 账号唯一标识。
   * en-us: Unique account identifier.
   */
  id: string;
  /**
   * zh-cn: 后端返回的用户扩展信息。
   * en-us: Extended user profile returned by the backend.
   */
  user?: Record<string, any>;
  /**
   * zh-cn: 用户所属角色或用户组编码。
   * en-us: Role or user-group codes assigned to the user.
   */
  userGroups?: string[];
}

/**
 * zh-cn: 查询当前 Ticket 对应的账号、用户和权限信息。
 * en-us: Queries account, user, and authority information for the current Ticket.
 */
export async function getAuthorityApi() {
  return requestClient.get<SsoUserAccountInfo>('/v1/identity/authority/account');
}

/**
 * zh-cn: 将后端账号权限信息映射为前端用户信息，认证状态仍由 Ticket 管理。
 * en-us: Maps backend account authority data to frontend user info while Ticket keeps authentication state.
 *
 * @returns
 * zh-cn: 兼容 Vben 用户仓储的用户信息，认证状态仍以 Ticket 为准。
 * en-us: User info compatible with the Vben user store while authentication state remains Ticket-based.
 */
export async function getUserInfoApi(): Promise<SsoUserInfo> {
  const account = await getAuthorityApi();
  const user = account.user ?? {};
  const userInfo: SsoUserInfo = {
    avatar: user.avatar || '',
    desc: account.currentAppName || '',
    homePath: '/app-center',
    realName: user.userName || account.account,
    roles: account.userGroups?.length ? account.userGroups : ['sso-user'],
    userId: account.id,
    username: account.account,
  };

  return userInfo;
}
