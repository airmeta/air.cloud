<script lang="ts" setup>
import { onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import { injectFirstAppApi } from '#/api';
import { message } from '#/adapter/tdesign';
import { useAuthStore } from '#/store';
import { getClientAppId } from '#/utils/sso/session';

defineOptions({ name: 'Inject' });

const router = useRouter();
const authStore = useAuthStore();
const loading = ref(false);
const form = reactive({
  appEncryptType: 0,
  appId: '',
  appName: 'Air.Cloud 单点登录',
  appRedirectUrl: '',
  defaultAccount: '',
  privateKey: '',
});

function validateForm() {
  if (!form.appName) return '请输入应用名称';
  if (!form.appId) return '请输入应用编号';
  if (!form.appRedirectUrl) return '请输入应用跳转地址';
  if (!form.defaultAccount) return '请输入默认管理员账号';
  if (!form.privateKey) return '请输入应用密钥';
  return '';
}

async function submitInject() {
  const error = validateForm();
  if (error) {
    message.warning(error);
    return;
  }

  loading.value = true;
  try {
    await injectFirstAppApi({
      appEncryptType: Number(form.appEncryptType ?? 0),
      appId: form.appId,
      appName: form.appName,
      appRedirectUrl: form.appRedirectUrl,
      defaultAccount: form.defaultAccount,
      defaultAppId: getClientAppId(),
      privateKey: form.privateKey,
    });
    message.success('初始化完成');
    await router.replace('/auth/login');
  } finally {
    loading.value = false;
  }
}

onMounted(async () => {
  const status = await authStore.initLoginPage().catch(() => undefined);
  if (status?.hasApp) {
    await router.replace('/auth/login');
  }
});
</script>

<template>
  <section class="inject-page">
    <form class="inject-panel" @submit.prevent="submitInject">
      <div class="panel-heading">
        <p class="eyebrow">First App</p>
        <h1>初始化认证应用</h1>
        <p>后端未检测到应用配置时，需要先创建默认应用和管理员账号。</p>
      </div>

      <label class="form-item">
        <span>应用名称</span>
        <input v-model.trim="form.appName" placeholder="请输入应用名称" type="text" />
      </label>

      <label class="form-item">
        <span>应用编号</span>
        <input v-model.trim="form.appId" placeholder="请输入应用编号" type="text" />
      </label>

      <label class="form-item">
        <span>跳转地址</span>
        <input
          v-model.trim="form.appRedirectUrl"
          placeholder="请输入应用跳转地址"
          type="text"
        />
      </label>

      <label class="form-item">
        <span>默认管理员账号</span>
        <input
          v-model.trim="form.defaultAccount"
          placeholder="请输入默认管理员账号"
          type="text"
        />
      </label>

      <label class="form-item">
        <span>加密方式</span>
        <select v-model.number="form.appEncryptType">
          <option :value="0">RSA</option>
          <option :value="1">SM2</option>
        </select>
      </label>

      <label class="form-item">
        <span>应用密钥</span>
        <textarea
          v-model.trim="form.privateKey"
          placeholder="请输入应用密钥"
          rows="5"
        ></textarea>
      </label>

      <button
        :disabled="loading"
        class="sso-button sso-button--block inject-submit"
        type="submit"
      >
        {{ loading ? '创建中...' : '创建默认应用' }}
      </button>
    </form>
  </section>
</template>

<style scoped>
.inject-page {
  display: flex;
  justify-content: center;
  width: 100%;
}

.inject-panel {
  width: min(520px, 100%);
  padding: 30px;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 16px;
}

.panel-heading {
  margin-bottom: 24px;
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
  font-size: 28px;
}

.panel-heading p:last-child {
  margin: 0;
  color: hsl(var(--muted-foreground));
}

.form-item {
  display: block;
  margin-bottom: 16px;
}

.form-item span {
  display: block;
  margin-bottom: 8px;
  font-weight: 600;
}

.form-item input,
.form-item select,
.form-item textarea {
  width: 100%;
  padding: 0 12px;
  color: hsl(var(--foreground));
  background: hsl(var(--background-deep));
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  outline: none;
}

.form-item input,
.form-item select {
  height: 40px;
}

.form-item textarea {
  min-height: 110px;
  padding-top: 10px;
  resize: vertical;
}

.form-item input:focus,
.form-item select:focus,
.form-item textarea:focus {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 1px hsl(var(--primary));
}

.inject-submit {
  margin-top: 8px;
}
</style>
