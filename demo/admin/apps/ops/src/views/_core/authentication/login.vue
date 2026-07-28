<script lang="ts" setup>
import type { VbenFormSchema } from '@vben/common-ui';
import type { Recordable } from '@vben/types';

import { computed, markRaw, nextTick, useTemplateRef } from 'vue';

import { AuthenticationLogin, z } from '@vben/common-ui';
import { $t } from '@vben/locales';

import { useAuthStore } from '#/store';

import BackendSliderCaptcha from './backend-slider-captcha.vue';

defineOptions({ name: 'Login' });

const authStore = useAuthStore();
const loginRef =
  useTemplateRef<InstanceType<typeof AuthenticationLogin>>('loginRef');

const formSchema = computed((): VbenFormSchema[] => {
  return [
    {
      component: 'VbenInput',
      componentProps: {
        placeholder: $t('authentication.usernameTip'),
      },
      fieldName: 'username',
      label: $t('authentication.username'),
      rules: z.string().min(1, { message: $t('authentication.usernameTip') }),
    },
    {
      component: 'VbenInputPassword',
      componentProps: {
        placeholder: $t('authentication.password'),
      },
      fieldName: 'password',
      label: $t('authentication.password'),
      rules: z.string().min(1, { message: $t('authentication.passwordTip') }),
    },
    {
      component: markRaw(BackendSliderCaptcha),
      fieldName: 'captcha',
      modelPropName: 'modelValue',
      rules: z
        .any()
        .refine((value) => Boolean(value?.id) && Number.isFinite(value?.x), {
          message: $t('authentication.verifyRequiredTip'),
        }),
    },
  ];
});

async function resetCaptcha() {
  const formApi = loginRef.value?.getFormApi();
  const captchaRef = formApi
    ?.getFieldComponentRef<InstanceType<typeof BackendSliderCaptcha>>(
      'captcha',
    );

  await captchaRef?.resume();
  await formApi?.setFieldValue('captcha', false, false);
  await nextTick();
  await formApi?.resetValidate();
  window.setTimeout(() => {
    void formApi?.resetValidate();
  });
}

async function handleLogin(params: Recordable<any>) {
  await authStore.authLogin(params).catch(() => {
    return resetCaptcha();
  });
}
</script>

<template>
  <AuthenticationLogin
    ref="loginRef"
    :form-schema="formSchema"
    :loading="authStore.loginLoading"
    @submit="handleLogin"
  />
</template>
