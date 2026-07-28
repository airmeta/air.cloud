<script lang="ts" setup>
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import { autoLoginApi, rejectAuthorizationApi } from '#/api';
import {
  setAccountInfo,
  setChosenApp,
  setClientAppConfig,
  setClientConfig,
  setTicket,
} from '#/utils/sso/session';

defineOptions({ name: 'OAuth' });

const route = useRoute();
const router = useRouter();
const loading = ref(false);
const errorMessage = ref('');

function extractKey() {
  const queryKey = route.query.key;
  return Array.isArray(queryKey) ? queryKey[0] : queryKey;
}

async function approveAuthorization() {
  const key = extractKey();
  if (!key) {
    errorMessage.value = '授权 key 缺失';
    return;
  }

  loading.value = true;
  errorMessage.value = '';
  try {
    const result = await autoLoginApi(key);
    const account = result?.account;
    const app = result?.app;

    if (app?.client) {
      setClientConfig(app.client);
    }
    if (app?.appId) {
      setClientAppConfig({
        appId: app.appId,
        appName: app.appName,
      });
      setChosenApp(app.appId);
    }
    if (account?.ticket) {
      setTicket(account.ticket);
    }
    if (account?.payload) {
      setAccountInfo(account.payload);
    }

    const pathParam = route.params.path as string | undefined;
    await router.replace(pathParam ? `/${pathParam}` : '/app-center');
  } catch (error: any) {
    errorMessage.value = error?.message || '自动授权失败';
  } finally {
    loading.value = false;
  }
}

async function denyAuthorization() {
  const key = extractKey();
  if (key) {
    await rejectAuthorizationApi(key).catch(() => undefined);
  }
  await router.replace('/auth/login');
}

onMounted(() => {
  void approveAuthorization();
});
</script>

<template>
  <section class="oauth-page">
    <div class="oauth-panel">
      <div v-if="loading" class="oauth-state">
        <span class="sso-spinner oauth-state__spinner"></span>
        <h1>正在完成应用授权</h1>
        <p>系统正在验证授权 key 并派生当前应用票据。</p>
      </div>

      <template v-else>
        <h1>正在完成应用授权</h1>
        <p v-if="!errorMessage">系统已完成授权准备，正在进入目标应用。</p>
        <div v-else class="error-box">
          {{ errorMessage }}
        </div>
        <div class="actions">
          <button class="sso-button" type="button" @click="approveAuthorization">
            重新授权
          </button>
          <button class="sso-button sso-button--outline" type="button" @click="denyAuthorization">
            拒绝并返回
          </button>
        </div>
      </template>
    </div>
  </section>
</template>

<style scoped>
.oauth-page {
  display: flex;
  min-height: 420px;
  align-items: center;
  justify-content: center;
  padding: 24px;
}

.oauth-panel {
  width: min(520px, 100%);
  padding: 28px;
  text-align: center;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 12px;
}

.oauth-panel h1 {
  margin: 0 0 12px;
  color: hsl(var(--foreground));
  font-size: 24px;
}

.oauth-panel p {
  margin: 0 0 20px;
  color: hsl(var(--muted-foreground));
}

.oauth-state {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.oauth-state__spinner {
  width: 30px;
  height: 30px;
  margin-bottom: 12px;
}

.error-box {
  margin: 0 0 20px;
  padding: 12px 14px;
  color: hsl(var(--destructive));
  background: hsl(var(--destructive) / 0.08);
  border: 1px solid hsl(var(--destructive) / 0.22);
  border-radius: 10px;
}

.actions {
  display: flex;
  justify-content: center;
  gap: 12px;
  margin-top: 20px;
}
</style>
