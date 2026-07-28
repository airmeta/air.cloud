import type { RouteRecordRaw } from 'vue-router';

import { preferences } from '@vben/preferences';

const BasicLayout = () => import('#/layouts/basic.vue');
const OauthPage = () => import('#/views/_core/oauth/index.vue');
const ForbiddenPage = () => import('@root/base/views/fallback/forbidden.vue');
const NotFoundPage = () => import('@root/base/views/fallback/not-found.vue');

const fallbackNotFoundRoute: RouteRecordRaw = {
  component: BasicLayout,
  children: [
    {
      component: NotFoundPage,
      meta: {
        hideInBreadcrumb: true,
        hideInMenu: true,
        hideInTab: true,
        title: '404',
      },
      name: 'FallbackNotFound',
      path: '',
    },
  ],
  meta: {
    hideInBreadcrumb: true,
    hideInMenu: true,
    hideInTab: true,
    title: '404',
  },
  name: 'FallbackNotFoundLayout',
  path: '/:path(.*)*',
};

const coreRoutes: RouteRecordRaw[] = [
  {
    component: BasicLayout,
    meta: {
      hideInBreadcrumb: true,
      title: 'Root',
    },
    name: 'Root',
    path: '/',
    redirect: preferences.app.defaultHomePath,
    children: [],
  },
  {
    component: OauthPage,
    meta: {
      hideInBreadcrumb: true,
      hideInMenu: true,
      hideInTab: true,
      ignoreAccess: true,
      title: 'OAuth',
    },
    name: 'OAuth',
    path: '/oauth/:path(.*)*',
  },
  {
    component: ForbiddenPage,
    meta: {
      hideInBreadcrumb: true,
      hideInMenu: true,
      hideInTab: true,
      ignoreAccess: true,
      title: '403',
    },
    name: 'Forbidden',
    path: '/403',
  },
];

export { coreRoutes, fallbackNotFoundRoute };
