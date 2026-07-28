import type { VNode } from 'vue';
import type {
  RouteLocationNormalizedLoaded,
  RouteLocationNormalizedLoadedGeneric,
} from 'vue-router';

import { computed } from 'vue';

import { preferences, usePreferences } from '@vben/preferences';
import { getTabCacheKey } from '@vben/stores';

/**
 * Align the rendered route component name with the tab route cache key.
 *
 * Several pages are implemented as `index.vue`, so relying on the original
 * component name can make KeepAlive treat different routes as the same page.
 */
export function transformComponent(
  component: VNode,
  route: RouteLocationNormalizedLoadedGeneric,
) {
  if (!component) {
    console.error(
      'Component view not found, please check the route configuration',
    );
    return undefined;
  }

  const routeKey = getTabCacheKey(route);
  if (!routeKey) {
    return component;
  }

  component.type ||= {};
  const componentType = component.type as any;
  if (componentType.name !== routeKey) {
    componentType.name = routeKey;
  }

  return component;
}

/**
 * Layout-related helpers.
 */
export function useLayoutHook() {
  const { keepAlive } = usePreferences();

  const getEnabledTransition = computed(() => {
    const { transition } = preferences;
    const transitionName = transition.name;
    return transitionName && transition.enable;
  });

  function getTransitionName(_route: RouteLocationNormalizedLoaded) {
    const { tabbar, transition } = preferences;
    const transitionName = transition.name;
    if (!transitionName || !transition.enable) {
      return;
    }

    if (!tabbar.enable || !keepAlive) {
      return transitionName;
    }

    return transitionName;
  }

  return {
    getEnabledTransition,
    getTransitionName,
  };
}
