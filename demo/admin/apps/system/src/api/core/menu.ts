import type { RouteRecordStringComponent } from '@vben/types';

import {
  getAuthorityAccountApi,
  type AuthorityItem,
} from './authority';

function normalizeComponent(authority: AuthorityItem) {
  if (authority.type === 0) {
    return 'BasicLayout';
  }

  return authority.component?.trim() || authority.path;
}

function sortRoutes(routes: RouteRecordStringComponent[]) {
  routes.sort((left, right) => {
    const leftOrder = left.meta?.order ?? Number.MAX_SAFE_INTEGER;
    const rightOrder = right.meta?.order ?? Number.MAX_SAFE_INTEGER;
    return leftOrder - rightOrder;
  });
  routes.forEach((route) => sortRoutes(route.children || []));
  return routes;
}

/**
 * zh-cn: 将后端 authority/account 返回的数据库权限记录转换为 Vben 后端路由结构。
 * 目录和菜单保留原始层级、路径和排序；按钮权限不生成路由，而是汇总到访问权限码。
 * en-us: Converts database-backed authority/account records into Vben backend route records.
 * Directories and menus retain their hierarchy, paths, and ordering; button authorities become
 * access codes instead of routes.
 */
export function convertAuthoritysToVbenRoutes(
  authoritys: AuthorityItem[] = [],
): RouteRecordStringComponent[] {
  const routeById = new Map<string, RouteRecordStringComponent>();
  const roots: RouteRecordStringComponent[] = [];

  authoritys
    .filter((authority) => authority.type === 0 || authority.type === 1 || (authority.type === 2 && !!authority.path))
    .forEach((authority) => {
      routeById.set(authority.id, {
        children: [],
        component: normalizeComponent(authority),
        meta: {
          hideInMenu: authority.hide === 1,
          icon: authority.icon,
          order: authority.sortNumber,
          title: authority.title,
        },
        name: `Authority${authority.id}`,
        path: authority.path,
      });
    });

  authoritys
    .filter((authority) => authority.type === 0 || authority.type === 1 || (authority.type === 2 && !!authority.path))
    .forEach((authority) => {
      const route = routeById.get(authority.id);
      if (!route) {
        return;
      }

      const parent = authority.parentId
        ? routeById.get(authority.parentId)
        : undefined;
      if (parent) {
        parent.children?.push(route);
      } else {
        roots.push(route);
      }
    });

  return sortRoutes(roots);
}

/**
 * zh-cn: 从 Vben 动态路由中选择第一个可见页面，用作 OAuth 成功后的默认目标。
 * en-us: Selects the first visible page from Vben dynamic routes as the default OAuth success target.
 */
export function getFirstAccessibleMenuPath(
  routes: RouteRecordStringComponent[],
): string | undefined {
  for (const route of routes) {
    if (route.meta?.hideInMenu) {
      continue;
    }

    const childPath = getFirstAccessibleMenuPath(route.children || []);
    if (childPath) {
      return childPath;
    }

    return route.path;
  }
}

/**
 * zh-cn: 判断 OAuth 回调携带的路径是否属于当前账户的 Vben 动态菜单。
 * en-us: Determines whether an OAuth callback path belongs to the current account's Vben dynamic menus.
 */
export function hasAccessibleMenuPath(
  routes: RouteRecordStringComponent[],
  path: string,
): boolean {
  return routes.some(
    (route) =>
      isSameOrDynamicMenuPath(route.path, path) ||
      hasAccessibleMenuPath(route.children || [], path),
  );
}

function isSameOrDynamicMenuPath(routePath: string, targetPath: string) {
  const normalizedRoutePath = routePath.replace(/\/+$/, '');
  const normalizedTargetPath = targetPath.replace(/\/+$/, '');

  if (normalizedRoutePath === normalizedTargetPath) {
    return true;
  }

  const routeSegments = normalizedRoutePath.split('/').filter(Boolean);
  const targetSegments = normalizedTargetPath.split('/').filter(Boolean);

  return (
    routeSegments.length === targetSegments.length &&
    routeSegments.every(
      (segment, index) =>
        segment.startsWith(':') || segment === targetSegments[index],
    )
  );
}

/**
 * zh-cn: 获取当前账户可访问的 Vben 动态路由。数据来自 authority/account 的数据库权限，
 * 不再请求不存在的 menu/all 接口。
 * en-us: Gets Vben dynamic routes available to the current account. Data comes from the
 * database-backed authority/account response and no longer requests the nonexistent menu/all API.
 */
export async function getAllMenusApi() {
  const account = await getAuthorityAccountApi();
  return convertAuthoritysToVbenRoutes(account.authoritys);
}

/**
 * zh-cn: 获取当前账户拥有的按钮权限码，供 Vben 的 v-access 和访问控制组件使用。
 * en-us: Gets current account button authority codes for Vben v-access and access-control components.
 */
export async function getAuthorityCodesApi() {
  const account = await getAuthorityAccountApi();
  return (account.authoritys || [])
    .filter((authority) => authority.type === 2 && authority.authority)
    .map((authority) => authority.authority as string);
}
