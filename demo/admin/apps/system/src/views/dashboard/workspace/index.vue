<script lang="ts" setup>
import { computed } from 'vue';
import { useRouter } from 'vue-router';

import { preferences } from '@vben/preferences';
import { useUserStore } from '@vben/stores';
import { IconifyIcon } from '@vben/icons';

import {
  Avatar as TAvatar,
  Button as TButton,
  Card as TCard,
  Col as TCol,
  Row as TRow,
  Space as TSpace,
  Tag as TTag,
} from 'tdesign-vue-next';

const router = useRouter();
const userStore = useUserStore();

const displayName = computed(
  () => userStore.userInfo?.realName || userStore.userInfo?.username || '系统用户',
);

const quickEntries = [
  {
    description: '维护平台用户、账号状态与基础身份信息',
    icon: 'lucide:users',
    path: '/system/users',
    title: '用户管理',
  },
  {
    description: '维护角色并配置角色菜单与权限范围',
    icon: 'lucide:shield-check',
    path: '/system/roles',
    title: '角色管理',
  },
  {
    description: '维护系统菜单、路由与页面资源配置',
    icon: 'lucide:menu-square',
    path: '/system/menus',
    title: '菜单管理',
  },
  {
    description: '维护字典、参数与系统运行基础配置',
    icon: 'lucide:settings',
    path: '/system/configurations',
    title: '系统配置',
  },
];

function go(path: string) {
  router.push(path);
}
</script>

<template>
  <div class="system-dashboard">
    <section class="dashboard-hero">
      <div class="profile-block">
        <TAvatar
          class="profile-avatar"
          :image="userStore.userInfo?.avatar || preferences.app.defaultAvatar"
          size="64px"
        />
        <div>
          <h2>您好，{{ displayName }}</h2>
          <p>欢迎进入系统管理平台，请选择需要维护的系统能力。</p>
        </div>
      </div>
      <TTag theme="primary" variant="light">System Console</TTag>
    </section>

    <TRow :gutter="[16, 16]">
      <TCol v-for="entry in quickEntries" :key="entry.path" :lg="6" :md="6" :xs="12">
        <TCard class="entry-card" bordered hover-shadow @click="go(entry.path)">
          <div class="entry-icon">
            <IconifyIcon :icon="entry.icon" />
          </div>
          <h3>{{ entry.title }}</h3>
          <p>{{ entry.description }}</p>
          <TSpace>
            <TButton theme="primary" variant="text" @click.stop="go(entry.path)">
              进入维护
              <template #suffix>
                <IconifyIcon icon="lucide:arrow-right" />
              </template>
            </TButton>
          </TSpace>
        </TCard>
      </TCol>
    </TRow>
  </div>
</template>

<style scoped>
.system-dashboard {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.dashboard-hero {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 24px;
  background: linear-gradient(135deg, rgba(24, 144, 255, 0.12), rgba(22, 93, 255, 0.04));
  border: 1px solid var(--td-component-border);
  border-radius: 12px;
}

.profile-block {
  display: flex;
  gap: 16px;
  align-items: center;
}

.profile-avatar {
  background: var(--td-brand-color-light);
}

.profile-block h2 {
  margin: 0;
  font-size: 22px;
  font-weight: 600;
  color: var(--td-text-color-primary);
}

.profile-block p {
  margin: 8px 0 0;
  color: var(--td-text-color-secondary);
}

.entry-card {
  min-height: 180px;
  cursor: pointer;
}

.entry-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  margin-bottom: 14px;
  font-size: 24px;
  color: var(--td-brand-color);
  background: var(--td-brand-color-light);
  border-radius: 12px;
}

.entry-card h3 {
  margin: 0 0 8px;
  font-size: 17px;
  color: var(--td-text-color-primary);
}

.entry-card p {
  min-height: 44px;
  margin: 0 0 16px;
  line-height: 1.7;
  color: var(--td-text-color-secondary);
}
</style>
