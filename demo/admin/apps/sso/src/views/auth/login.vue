<script lang="ts" setup>
import { computed, onMounted, reactive } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import { MessagePlugin } from 'tdesign-vue-next';

import { getCaptchaApi } from '#/api';
import { useAuthStore } from '#/store';

import ImageCaptchaField from './image-captcha-field.vue';

defineOptions({ name: 'Login' });

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const form = reactive({
  code: '',
  password: '',
  username: '',
});

const captchaBase64 = computed(() => authStore.loginCaptchaBase64);
const captchaReady = computed(() => authStore.loginCaptchaReady);

async function refreshCaptcha() {
  if (!captchaReady.value) {
    return;
  }

  const captcha = await getCaptchaApi();
  authStore.setLoginCaptchaBase64(
    `data:image/png;base64,${captcha.captchaBase64}`,
  );
  form.code = '';
}

async function initPage() {
  authStore.setLoginCaptchaReady(false);
  authStore.setLoginCaptchaBase64('');

  const status = await authStore.initLoginPage();
  if (!status.hasApp) {
    await router.replace('/auth/inject');
    return;
  }

  if (!status.client?.ticket) {
    MessagePlugin.error('初始化票据获取失败');
    return;
  }

  authStore.setLoginCaptchaReady(true);
  await refreshCaptcha();
}

function validateForm() {
  if (!form.username) {
    MessagePlugin.warning('请输入账号');
    return false;
  }
  if (!form.password) {
    MessagePlugin.warning('请输入密码');
    return false;
  }
  if (!form.code) {
    MessagePlugin.warning('请输入验证码');
    return false;
  }
  return true;
}

async function handleLogin() {
  if (!captchaReady.value) {
    MessagePlugin.warning('验证码尚未初始化完成');
    return;
  }
  if (!validateForm()) {
    return;
  }

  await authStore
    .authLogin(form, async () => {
      const redirect = route.query.redirect as string | undefined;
      await router.push(redirect ? decodeURIComponent(redirect) : '/app-center');
    })
    .catch(async () => {
      await refreshCaptcha().catch(() => undefined);
    });
}

onMounted(() => {
  void initPage();
});
</script>

<template>
  <form class="login-panel" @submit.prevent="handleLogin">
    <div class="panel-heading">
      <p class="eyebrow">Air.Cloud SSO</p>
      <h1>单点登录</h1>
      <p>使用平台票据登录并进入可访问的业务应用。</p>
    </div>

    <label class="form-item">
      <span>账号</span>
      <input
        v-model.trim="form.username"
        autocomplete="username"
        placeholder="请输入账号"
        type="text"
      />
    </label>

    <label class="form-item">
      <span>密码</span>
      <input
        v-model="form.password"
        autocomplete="current-password"
        placeholder="请输入密码"
        type="password"
      />
    </label>

    <label class="form-item">
      <span>验证码</span>
      <ImageCaptchaField
        v-model="form.code"
        :disabled="!captchaReady"
        :image-url="captchaBase64"
        @refresh="refreshCaptcha"
      />
    </label>

    <button
      :disabled="authStore.loginLoading"
      class="sso-button sso-button--block login-submit"
      type="submit"
    >
      {{ authStore.loginLoading ? '登录中...' : '登录' }}
    </button>
  </form>
</template>

<style scoped>
.login-panel {
  width: min(100%, 440px);
  padding: 32px;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 16px;
  box-shadow: 0 18px 80px rgb(0 0 0 / 18%);
}

.panel-heading {
  margin-bottom: 28px;
  text-align: left;
}

.eyebrow {
  margin: 0 0 8px;
  color: hsl(var(--primary));
  font-size: 13px;
  font-weight: 700;
}

.panel-heading h1 {
  margin: 0 0 10px;
  color: hsl(var(--foreground));
  font-size: 30px;
  font-weight: 700;
}

.panel-heading p {
  margin: 0;
  color: hsl(var(--muted-foreground));
  line-height: 1.6;
}

.form-item {
  display: block;
  margin-bottom: 18px;
}

.form-item > span {
  display: block;
  margin-bottom: 8px;
  color: hsl(var(--foreground));
  font-size: 14px;
  font-weight: 600;
}

.form-item input {
  width: 100%;
  height: 42px;
  padding: 0 12px;
  color: hsl(var(--foreground));
  background: hsl(var(--background-deep));
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  outline: none;
}

.form-item input:focus {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 1px hsl(var(--primary));
}

.login-submit {
  height: 42px;
  margin-top: 4px;
}
</style>
