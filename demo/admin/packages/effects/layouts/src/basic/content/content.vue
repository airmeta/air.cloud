<script lang="ts" setup>
import type { RouteLocationNormalizedLoadedGeneric } from 'vue-router';

import { unref } from 'vue';
import { RouterView } from 'vue-router';

import { usePreferences } from '@vben/preferences';
import { getTabKey, storeToRefs, useTabbarStore } from '@vben/stores';

import { transformComponent, useLayoutHook } from '../../hooks';
import { IFrameRouterView } from '../../iframe';
import { RouteCachedPage, RouteCachedView } from '../../route-cached';

defineOptions({ name: 'LayoutContent' });

const tabbarStore = useTabbarStore();
const { keepAlive } = usePreferences();

const { getCachedTabs, getExcludeCachedTabs, renderRouteView } =
  storeToRefs(tabbarStore);

const { getEnabledTransition, getTransitionName } = useLayoutHook();

/**
 * 是否显示component
 * @param route
 */
const showComponent = (route: RouteLocationNormalizedLoadedGeneric) => {
  return !route.meta.domCached && unref(renderRouteView);
};

const shouldKeepAlive = (route: RouteLocationNormalizedLoadedGeneric) => {
  return keepAlive.value && route.meta.keepAlive !== false;
};

const showNormalComponent = (
  route: RouteLocationNormalizedLoadedGeneric,
) => {
  return showComponent(route) && !shouldKeepAlive(route);
};

const showKeepAliveComponent = (
  route: RouteLocationNormalizedLoadedGeneric,
) => {
  return showComponent(route) && shouldKeepAlive(route);
};
</script>

<template>
  <div class="relative h-full">
    <IFrameRouterView />
    <RouteCachedView />
    <RouterView v-slot="{ Component, route }">
      <RouteCachedPage
        :component="Component"
        :route="route"
        v-if="route.meta.domCached"
      />
      <component
        v-if="showNormalComponent(route)"
        :is="Component"
        :key="getTabKey(route)"
      />
      <Transition
        v-else-if="getEnabledTransition"
        :name="getTransitionName(route)"
        appear
        mode="out-in"
      >
        <KeepAlive
          v-if="shouldKeepAlive(route)"
          :exclude="getExcludeCachedTabs"
          :include="getCachedTabs"
        >
          <component
            :is="transformComponent(Component, route)"
            v-if="showKeepAliveComponent(route)"
            v-show="!route.meta.iframeSrc"
            :key="getTabKey(route)"
          />
        </KeepAlive>
      </Transition>
      <template v-else>
        <KeepAlive
          v-if="shouldKeepAlive(route)"
          :exclude="getExcludeCachedTabs"
          :include="getCachedTabs"
        >
          <component
            :is="transformComponent(Component, route)"
            v-if="showKeepAliveComponent(route)"
            v-show="!route.meta.iframeSrc"
            :key="getTabKey(route)"
          />
        </KeepAlive>
      </template>
    </RouterView>
  </div>
</template>
