<script lang="ts" setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';

import { Dialog as TDialog } from 'tdesign-vue-next';

import type { SsoAuthApi } from '#/api';

import { createAppRedirectUrlApi, queryAccountAppsApi } from '#/api';
import { message } from '#/adapter/tdesign';
import { useAuthStore } from '#/store';
import { getAccountInfo, setChosenApp } from '#/utils/sso/session';

defineOptions({ name: 'AppCenter' });

const authStore = useAuthStore();
const apps = ref<SsoAuthApi.AccountApp[]>([]);
const loading = ref(false);
const noticeIndex = ref(0);
const selectedApp = ref<null | SsoAuthApi.AccountApp>(null);
const authorizeDialogVisible = ref(false);
const authorizing = ref(false);
let noticeTimer: number | undefined;

const notices = [
  '请选择已授权业务系统进入，平台会自动完成一次性授权票据派生。',
  '离开公共终端前请点击右上角退出图标，确保当前登录票据被清理。',
  '如目标应用无法进入，请联系管理员检查应用授权和回调地址配置。',
];

const currentNotice = computed(() => notices[noticeIndex.value] || '');

const accountInfo = computed(() => getAccountInfo() ?? {});

const displayName = computed(() => {
  const account = accountInfo.value;
  return (
    account.UserName ||
    account.userName ||
    account.RealName ||
    account.realName ||
    authStore.userInfo?.realName ||
    authStore.userInfo?.username ||
    account.Account ||
    account.account ||
    '当前登录用户'
  );
});

const authorizationGreeting = computed(() => {
  const appName = selectedApp.value?.appName || '目标应用';
  return `您好，${displayName.value}，您正在通过统一身份认证访问「${appName}」。请确认是否允许平台为本次访问完成单点登录授权。`;
});

const authorizationItems = computed(() => {
  const appName = selectedApp.value?.appName || '目标应用';
  return [
    {
      description: `用于在「${appName}」中建立用户身份映射，便于展示用户名称、关联业务权限和审计登录来源。`,
      title: '基础身份信息',
    },
    {
      description:
        '确认您已通过统一身份认证平台完成登录。平台不会向目标应用传递您的登录密码。',
      title: '登录状态校验',
    },
    {
      description:
        '生成仅用于本次跳转的一次性授权票据，目标应用将凭该票据完成登录。',
      title: '单点登录授权',
    },
  ];
});

function startNoticeRoller() {
  stopNoticeRoller();
  noticeTimer = window.setInterval(() => {
    noticeIndex.value = (noticeIndex.value + 1) % notices.length;
  }, 4000);
}

function stopNoticeRoller() {
  if (!noticeTimer) {
    return;
  }

  window.clearInterval(noticeTimer);
  noticeTimer = undefined;
}

async function loadApps() {
  loading.value = true;
  try {
    apps.value = await queryAccountAppsApi();
  } finally {
    loading.value = false;
  }
}

async function openApp(app: SsoAuthApi.AccountApp) {
  if (!app.appId) {
    return;
  }

  selectedApp.value = app;
  authorizeDialogVisible.value = true;
}

function closeAuthorizeDialog() {
  if (authorizing.value) {
    return;
  }

  authorizeDialogVisible.value = false;
  selectedApp.value = null;
}

async function confirmEnterApp() {
  const app = selectedApp.value;
  if (!app?.appId) {
    return;
  }

  const targetWindow = window.open('about:blank', '_blank');
  if (targetWindow) {
    targetWindow.opener = null;
  }

  authorizing.value = true;
  try {
    const url = await createAppRedirectUrlApi(app.appId);
    if (!url) {
      targetWindow?.close();
      message.warning('后端未返回应用跳转地址');
      return;
    }

    setChosenApp(app.appId);
    if (targetWindow) {
      targetWindow.location.href = url;
      authorizeDialogVisible.value = false;
      selectedApp.value = null;
      return;
    }

    const openedWindow = window.open(url, '_blank', 'noopener,noreferrer');
    if (!openedWindow) {
      message.warning('浏览器阻止了新窗口打开，请允许弹出窗口后重试');
    }
  } finally {
    authorizing.value = false;
  }
}

onMounted(async () => {
  await authStore.fetchUserInfo().catch(() => undefined);
  startNoticeRoller();
  await loadApps();
});

onBeforeUnmount(() => {
  stopNoticeRoller();
});
</script>

<template>
  <main class="app-center">
    <section class="hero">
      <div>
        <h1>应用中心</h1>
        <p>选择已授权的业务系统，平台会生成一次性授权 key 并完成票据派生。</p>
      </div>
    </section>

    <section class="notice-strip" aria-label="通知公告">
      <span class="notice-strip__label">通知公告</span>
      <Transition mode="out-in" name="notice-fade">
        <span :key="noticeIndex" class="notice-strip__content">
          {{ currentNotice }}
        </span>
      </Transition>
    </section>

    <section v-if="loading" class="state-panel">
      <span class="sso-spinner state-panel__spinner"></span>
      <span>正在加载应用列表...</span>
    </section>

    <section v-else-if="apps.length" class="app-grid">
      <article v-for="app in apps" :key="app.appId" class="app-card">
        <div class="logo-box">
          <img v-if="app.logoUrl" :src="app.logoUrl" :alt="app.appName" />
          <span v-else>{{ app.appName?.slice(0, 1) || 'A' }}</span>
        </div>
        <div class="app-info">
          <h2>{{ app.appName }}</h2>
          <p>{{ app.description || '已授权业务应用' }}</p>
        </div>
        <button class="sso-button sso-button--block" type="button" @click="openApp(app)">
          进入应用
        </button>
      </article>
    </section>

    <section v-else class="state-panel">
      <strong>暂无可访问应用</strong>
      <span>当前账号尚未被授予任何业务系统访问权限。</span>
    </section>

    <TDialog
      v-model:visible="authorizeDialogVisible"
      :close-on-esc-keydown="!authorizing"
      :close-on-overlay-click="!authorizing"
      :confirm-btn="{ content: '同意授权并进入', loading: authorizing, theme: 'primary' }"
      cancel-btn="取消"
      header="应用授权确认"
      placement="center"
      width="640px"
      @cancel="closeAuthorizeDialog"
      @close="closeAuthorizeDialog"
      @confirm="confirmEnterApp"
    >
      <section class="authorize-dialog">
        <div class="authorize-target">
          <div class="authorize-target__logo">
            <img
              v-if="selectedApp?.logoUrl"
              :src="selectedApp.logoUrl"
              :alt="selectedApp.appName"
            />
            <span v-else>{{ selectedApp?.appName?.slice(0, 1) || 'A' }}</span>
          </div>
          <div>
            <p>授权给</p>
            <strong>{{ selectedApp?.appName || '目标应用' }}</strong>
          </div>
        </div>

        <p class="authorize-greeting">
          {{ authorizationGreeting }}
        </p>

        <div class="authorize-section-title">授权获取</div>

        <ol class="authorize-list">
          <li v-for="item in authorizationItems" :key="item.title">
            <strong>{{ item.title }}</strong>
            <span>{{ item.description }}</span>
          </li>
        </ol>

        <p class="authorize-privacy">
          确认后将为本次访问生成临时登录凭证，仅用于进入当前应用。若您不认可上述授权范围，可取消并返回应用中心。
        </p>
      </section>
    </TDialog>
  </main>
</template>

<style scoped>
.app-center {
  width: 100%;
  padding: 24px;
}

.hero {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
  margin-bottom: 20px;
}

.hero h1 {
  margin: 0 0 8px;
  color: hsl(var(--foreground));
  font-size: 30px;
}

.hero p:last-child {
  margin: 0;
  color: hsl(var(--muted-foreground));
}

.notice-strip {
  display: flex;
  min-height: 52px;
  align-items: center;
  gap: 14px;
  overflow: hidden;
  padding: 0 16px;
  margin-bottom: 24px;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 12px;
}

.state-panel,
.app-card {
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 12px;
}

.notice-strip__label {
  flex: 0 0 auto;
  color: hsl(var(--primary));
  font-size: 13px;
  font-weight: 700;
}

.notice-strip__content {
  min-width: 0;
  overflow: hidden;
  color: hsl(var(--muted-foreground));
  font-size: 14px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.notice-fade-enter-active,
.notice-fade-leave-active {
  transition:
    opacity 0.25s ease,
    transform 0.25s ease;
}

.notice-fade-enter-from {
  opacity: 0;
  transform: translateY(8px);
}

.notice-fade-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}

.app-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 16px;
}

.app-card {
  display: flex;
  min-height: 260px;
  flex-direction: column;
  gap: 16px;
  padding: 20px;
}

.logo-box {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 56px;
  height: 56px;
  overflow: hidden;
  color: hsl(var(--primary));
  font-size: 24px;
  font-weight: 700;
  background: hsl(var(--primary) / 12%);
  border-radius: 8px;
}

.logo-box img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.app-info {
  flex: 1;
}

.app-info h2 {
  margin: 0 0 8px;
  color: hsl(var(--foreground));
  font-size: 18px;
}

.app-info p {
  margin: 0;
  color: hsl(var(--muted-foreground));
  line-height: 1.6;
}

.state-panel {
  display: flex;
  min-height: 180px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 12px;
  color: hsl(var(--muted-foreground));
}

.state-panel__spinner {
  width: 28px;
  height: 28px;
}

.authorize-dialog {
  color: hsl(var(--foreground));
}

.authorize-target {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  margin-bottom: 12px;
  background: hsl(var(--primary) / 8%);
  border: 1px solid hsl(var(--primary) / 18%);
  border-radius: 12px;
}

.authorize-target__logo {
  display: flex;
  width: 42px;
  height: 42px;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  color: hsl(var(--primary));
  font-size: 20px;
  font-weight: 700;
  background: hsl(var(--primary) / 12%);
  border-radius: 10px;
}

.authorize-target__logo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.authorize-target p {
  margin: 0 0 4px;
  color: hsl(var(--muted-foreground));
  font-size: 13px;
}

.authorize-target strong {
  color: hsl(var(--foreground));
  font-size: 17px;
}

.authorize-greeting {
  margin: 0 0 12px;
  color: hsl(var(--muted-foreground));
  font-size: 14px;
  line-height: 1.55;
}

.authorize-section-title {
  margin: 0 0 8px;
  color: hsl(var(--foreground));
  font-size: 15px;
  font-weight: 700;
}

.authorize-list {
  padding-left: 0;
  margin: 0 0 10px;
  color: hsl(var(--foreground));
  list-style: none;
}

.authorize-list li {
  position: relative;
  padding: 10px 12px 10px 40px;
  margin-bottom: 8px;
  background: hsl(var(--background-deep));
  border: 1px solid hsl(var(--border));
  border-radius: 10px;
}

.authorize-list li::before {
  position: absolute;
  top: 12px;
  left: 14px;
  display: flex;
  width: 18px;
  height: 18px;
  align-items: center;
  justify-content: center;
  color: #fff;
  content: counter(list-item);
  background: hsl(var(--primary));
  border-radius: 999px;
  font-size: 12px;
  font-weight: 700;
}

.authorize-list strong,
.authorize-list span {
  display: block;
}

.authorize-list strong {
  margin-bottom: 4px;
  color: hsl(var(--foreground));
  font-size: 14px;
}

.authorize-list span {
  color: hsl(var(--muted-foreground));
  font-size: 13px;
  line-height: 1.5;
}

.authorize-privacy {
  margin: 8px 0 0;
  color: hsl(var(--muted-foreground));
  font-size: 12px;
  line-height: 1.6;
}
</style>
