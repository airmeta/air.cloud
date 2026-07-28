import type { Router } from 'vue-router';

import { useAuthStore } from '#/store';
import { isLoggedIn } from '#/utils/sso/session';

const LOGIN_PATH = '/auth/login';
const DEFAULT_HOME_PATH = '/app-center';

/**
 * zh-cn: 注册通用路由守卫，用于页面加载标记和顶部进度条。
 * en-us: Registers common router guards for page-loaded markers and top progress display.
 *
 * @param router
 * zh-cn: Vue Router 实例。
 * en-us: Vue Router instance.
 */
function setupCommonGuard(router: Router) {
  const loadedPaths = new Set<string>();

  router.beforeEach((to) => {
    to.meta.loaded = loadedPaths.has(to.path);
    return true;
  });

  router.afterEach((to) => {
    loadedPaths.add(to.path);
  });
}

/**
 * zh-cn: 注册 SSO 访问守卫，使用 Ticket 判断登录态并补齐用户信息。
 * en-us: Registers the SSO access guard, using Ticket as login state and hydrating user information.
 *
 * @param router
 * zh-cn: Vue Router 实例。
 * en-us: Vue Router instance.
 */
function setupAccessGuard(router: Router) {
  router.beforeEach(async (to) => {
    const authStore = useAuthStore();
    const loggedIn = isLoggedIn();

    if (to.meta.ignoreAccess) {
      if (to.path === LOGIN_PATH && loggedIn) {
        return decodeURIComponent(
          (to.query?.redirect as string) || DEFAULT_HOME_PATH,
        );
      }
      return true;
    }

    if (!loggedIn) {
      return {
        path: LOGIN_PATH,
        query:
          to.fullPath === DEFAULT_HOME_PATH
            ? {}
            : { redirect: encodeURIComponent(to.fullPath) },
        replace: true,
      };
    }

    if (!authStore.userInfo) {
      await authStore.fetchUserInfo().catch(() => undefined);
    }

    if (!authStore.accessChecked) {
      await authStore.loadAccountAppsAsMenus();
    }

    return true;
  });
}

/**
 * zh-cn: 创建 SSO 路由守卫入口。
 * en-us: Creates the SSO router guard entry.
 *
 * @param router
 * zh-cn: Vue Router 实例。
 * en-us: Vue Router instance.
 */
function createRouterGuard(router: Router) {
  setupCommonGuard(router);
  setupAccessGuard(router);
}

export { createRouterGuard };
