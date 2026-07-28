import type { RouteRecordRaw } from 'vue-router';

import { coreRoutes, fallbackNotFoundRoute } from './core';

const routes: RouteRecordRaw[] = [...coreRoutes, fallbackNotFoundRoute];

function collectRouteNames(sourceRoutes: RouteRecordRaw[]) {
  const names: Array<RouteRecordRaw['name']> = [];
  sourceRoutes.forEach((route) => {
    if (route.name) {
      names.push(route.name);
    }
    if (route.children?.length) {
      names.push(...collectRouteNames(route.children));
    }
  });
  return names;
}

const coreRouteNames = collectRouteNames(coreRoutes);

const accessRoutes: RouteRecordRaw[] = [];

export { accessRoutes, coreRouteNames, routes };
