<script lang="ts" setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';

import { useAuthStore } from '#/store';
import { confirmLogout } from '#/utils/sso/confirm-logout';
import { getAccountInfo } from '#/utils/sso/session';

defineOptions({ name: 'SsoBasicLayout' });

const route = useRoute();
const authStore = useAuthStore();

const accountName = computed(() => {
  const account = getAccountInfo();
  return account?.Account || account?.UserName || '已登录用户';
});

const pageTitle = computed(() => {
  return (route.meta?.title as string | undefined) || '应用中心';
});

function handleLogout() {
  confirmLogout({
    onConfirm: async () => {
      await authStore.logout(false);
    },
  });
}
</script>

<template>
  <div class="sso-shell">
    <header class="sso-header">
      <div class="sso-brand">
        <span class="sso-brand__logo" aria-hidden="true"></span>
        <span class="sso-brand__name">Air.Cloud 单点登录</span>
      </div>

      <div class="sso-header__right">
        <span class="sso-header__page">{{ pageTitle }}</span>
        <span class="sso-header__account">{{ accountName }}</span>
        <button
          aria-label="退出登录"
          class="sso-header__logout"
          title="退出登录"
          type="button"
          @click="handleLogout"
        >
          <svg
            aria-hidden="true"
            class="sso-header__logout-icon"
            fill="none"
            viewBox="0 0 24 24"
          >
            <path
              d="M15.75 9V5.75A2.75 2.75 0 0 0 13 3H6.75A2.75 2.75 0 0 0 4 5.75v12.5A2.75 2.75 0 0 0 6.75 21H13a2.75 2.75 0 0 0 2.75-2.75V15"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="1.8"
            />
            <path
              d="M10 12h9m0 0-3-3m3 3-3 3"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="1.8"
            />
          </svg>
        </button>
      </div>
    </header>

    <main class="sso-content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.sso-shell {
  min-height: 100vh;
  color: hsl(var(--foreground));
  background:
    radial-gradient(circle at 20% 0%, hsl(var(--primary) / 10%), transparent 28%),
    hsl(var(--background-deep, var(--background)));
}

.sso-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 56px;
  padding: 0 24px;
  background: hsl(var(--background));
  border-bottom: 1px solid hsl(var(--border));
}

.sso-brand,
.sso-header__right {
  display: flex;
  align-items: center;
}

.sso-brand {
  gap: 10px;
}

.sso-brand__logo {
  position: relative;
  width: 28px;
  height: 28px;
  background: linear-gradient(135deg, #8b5cf6 0%, #06b6d4 100%);
  border-radius: 7px;
  transform: skew(-10deg);
}

.sso-brand__logo::after {
  position: absolute;
  right: -8px;
  bottom: 2px;
  width: 17px;
  height: 17px;
  content: '';
  background: #22d3ee;
  border-radius: 5px;
}

.sso-brand__name {
  font-size: 17px;
  font-weight: 700;
}

.sso-header__right {
  gap: 14px;
  color: hsl(var(--muted-foreground));
  font-size: 13px;
}

.sso-header__page {
  color: hsl(var(--primary));
  font-weight: 700;
}

.sso-header__account {
  max-width: 220px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sso-header__logout {
  display: inline-flex;
  height: 32px;
  width: 32px;
  align-items: center;
  justify-content: center;
  padding: 0;
  color: hsl(var(--foreground));
  cursor: pointer;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
}

.sso-header__logout:hover {
  color: hsl(var(--primary));
  border-color: hsl(var(--primary));
}

.sso-header__logout-icon {
  width: 18px;
  height: 18px;
}

.sso-content {
  min-height: calc(100vh - 56px);
}
</style>
