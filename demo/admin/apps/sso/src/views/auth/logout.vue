<script lang="ts" setup>
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';

import { useAuthStore } from '#/store';
import { confirmLogout } from '#/utils/sso/confirm-logout';
import { setTicket } from '#/utils/sso/session';

defineOptions({ name: 'SsoLogout' });

const authStore = useAuthStore();
const route = useRoute();
const statusText = ref('\u8bf7\u786e\u8ba4\u662f\u5426\u9000\u51fa\u767b\u5f55');

function getLogoutTicket() {
  const ticket = route.query.tickit;
  return typeof ticket === 'string' ? ticket : '';
}

function getLogoutAppId() {
  const appId = route.query.appId;
  return typeof appId === 'string' ? appId : '';
}

async function logout() {
  const ticket = getLogoutTicket();
  if (ticket) {
    setTicket(ticket);
  }
  statusText.value = '\u6b63\u5728\u9000\u51fa\u767b\u5f55...';
  await authStore.logout(false, getLogoutAppId());
}

onMounted(async () => {
  if (getLogoutTicket()) {
    await logout();
    return;
  }

  confirmLogout({
    onCancel: () => {
      statusText.value = '\u5df2\u53d6\u6d88\u9000\u51fa\u767b\u5f55';
    },
    onConfirm: logout,
  });
});
</script>

<template>
  <main class="sso-logout-page">{{ statusText }}</main>
</template>

<style scoped>
.sso-logout-page {
  display: grid;
  min-height: 100dvh;
  place-items: center;
  color: var(--td-text-color-secondary);
}
</style>
