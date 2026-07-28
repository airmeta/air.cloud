<script lang="ts" setup>
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import { useAccessStore, useUserStore } from '@vben/stores';
import { Button as TButton, Card as TCard } from 'tdesign-vue-next';

import {
  clearAuthorityAccountCache,
  getAllMenusApi,
  getAuthorityAccountApi,
  getFirstAccessibleMenuPath,
  hasAccessibleMenuPath,
} from '#/api';
import { requestClient } from '#/api/request';

interface AutoLoginResult {
  account?: {
    ticket?: string;
  };
  app?: {
    appId?: string;
    appName?: string;
  };
}

const TICKET_STORAGE_KEY = 'vea:auth:ticket';
const CHOOSE_APP_STORAGE_KEY = 'vea:auth:choose_app';
const APP_CONFIG_STORAGE_KEY = 'app_config';
const SSO_LOGOUT_URL = import.meta.env.VITE_SSO_LOGOUT_URL || '';

const route = useRoute();
const router = useRouter();
const accessStore = useAccessStore();
const errorMessage = ref('');
const userStore = useUserStore();

function getAuthorizationKey() {
  const queryKey = route.query.key;
  return typeof queryKey === 'string' && queryKey ? queryKey : null;
}

async function enterApplication() {
  errorMessage.value = '';
  const key = getAuthorizationKey();
  if (!key) {
    errorMessage.value = '授权凭据缺失。';
    return;
  }

  try {
    // A callback must replace, rather than reuse, the prior app session.
    accessStore.setAccessToken('');
    accessStore.setIsAccessChecked(false);
    userStore.setUserInfo(null);
    localStorage.removeItem(TICKET_STORAGE_KEY);
    clearAuthorityAccountCache();

    const result = await requestClient.get<AutoLoginResult>(
      `/v1/identity/autologin/${key}`,
    );
    const ticket = result.account?.ticket;
    if (!ticket) {
      throw new Error('自动登录未返回有效票据。');
    }

    localStorage.setItem(TICKET_STORAGE_KEY, ticket);
    if (result.app?.appId) {
      sessionStorage.setItem(CHOOSE_APP_STORAGE_KEY, result.app.appId);
      sessionStorage.setItem(
        APP_CONFIG_STORAGE_KEY,
        JSON.stringify({
          appId: result.app.appId,
          appName: result.app.appName,
          ssoLogoutUrl: SSO_LOGOUT_URL,
        }),
      );
    }

    accessStore.setAccessToken(ticket);
    await getAuthorityAccountApi();
    const menuRoutes = await getAllMenusApi();

    const pathParam = route.params.path;
    const callbackPath = typeof pathParam === 'string' ? `/${pathParam}` : '';
    const targetPath =
      callbackPath && hasAccessibleMenuPath(menuRoutes, callbackPath)
        ? callbackPath
        : getFirstAccessibleMenuPath(menuRoutes) || '/403';
    const query = { ...route.query };
    delete query.key;
    await router.replace({ path: targetPath, query });
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : '进入应用失败，请重试。';
  }
}

onMounted(() => {
  void enterApplication();
});
</script>

<template>
  <main class="oauth-page">
    <TCard class="oauth-card" :bordered="false">
      <h1>正在进入应用</h1>
      <p>{{ errorMessage || '正在读取授权凭据，请稍候。' }}</p>
      <TButton v-if="errorMessage" theme="primary" @click="enterApplication">
        重新进入
      </TButton>
    </TCard>
  </main>
</template>

<style scoped>
.oauth-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100dvh;
  padding: 24px;
  background: var(--td-bg-color-page);
}

.oauth-card {
  width: min(480px, 100%);
  text-align: center;
}

.oauth-card h1 {
  margin: 0 0 12px;
  font-size: 20px;
}

.oauth-card p {
  margin: 0 0 20px;
  color: var(--td-text-color-secondary);
}
</style>
