import type { RouteRecordRaw } from 'vue-router';

const BasicLayout = () => import('#/layouts/basic.vue');
const AuthPageLayout = () => import('#/layouts/auth.vue');
const LOGIN_PATH = '/auth/login';
const DEFAULT_HOME_PATH = '/app-center';

const fallbackNotFoundRoute: RouteRecordRaw = {
  component: () => import('#/views/_core/fallback/not-found.vue'),
  meta: {
    hideInBreadcrumb: true,
    hideInMenu: true,
    hideInTab: true,
    title: '404',
  },
  name: 'FallbackNotFound',
  path: '/:path(.*)*',
};

const coreRoutes: RouteRecordRaw[] = [
  {
    component: () => import('#/views/auth/logout.vue'),
    meta: {
      ignoreAccess: true,
      title: '退出登录',
    },
    name: 'SsoLogout',
    path: '/logout',
  },
  {
    children: [
      {
        component: () => import('#/views/app-center/index.vue'),
        meta: {
          affixTab: true,
          icon: 'lucide:grid-2x2',
          title: '应用中心',
        },
        name: 'AppCenter',
        path: 'app-center',
      },
      {
        component: () => import('#/views/oauth/index.vue'),
        meta: {
          hideInMenu: true,
          title: '应用授权',
        },
        name: 'OAuth',
        path: 'oauth/:path(.*)*',
      },
    ],
    component: BasicLayout,
    meta: {
      hideInBreadcrumb: true,
      title: 'Root',
    },
    name: 'Root',
    path: '/',
    redirect: DEFAULT_HOME_PATH,
  },
  {
    children: [
      {
        component: () => import('#/views/auth/login.vue'),
        meta: {
          ignoreAccess: true,
          title: '登录',
        },
        name: 'Login',
        path: 'login',
      },
      {
        component: () => import('#/views/inject/index.vue'),
        meta: {
          ignoreAccess: true,
          title: '初始化应用',
        },
        name: 'Inject',
        path: 'inject',
      },
    ],
    component: AuthPageLayout,
    meta: {
      hideInTab: true,
      ignoreAccess: true,
      title: '单点登录',
    },
    name: 'Authentication',
    path: '/auth',
    redirect: LOGIN_PATH,
  },
];

export { coreRoutes, fallbackNotFoundRoute };
