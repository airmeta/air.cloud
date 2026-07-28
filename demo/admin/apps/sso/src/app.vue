<script lang="ts" setup>
import type { GlobalConfigProvider } from 'tdesign-vue-next';

import { ConfigProvider } from 'tdesign-vue-next';
import zhConfig from 'tdesign-vue-next/es/locale/zh_CN';

defineOptions({ name: 'App' });

document.documentElement.setAttribute('theme-mode', 'dark');

const globalConfig: GlobalConfigProvider = {
  ...(zhConfig as unknown as GlobalConfigProvider),
  calendar: {},
  pagination: {},
  table: {},
};
</script>

<template>
  <ConfigProvider :global-config="globalConfig">
    <RouterView />
  </ConfigProvider>
</template>

<style>
:root {
  --background: 222 16% 12%;
  --background-deep: 225 18% 8%;
  --border: 222 14% 20%;
  --destructive: 0 84% 60%;
  --foreground: 210 20% 96%;
  --muted-foreground: 220 10% 64%;
  --primary: 211 100% 50%;
}

* {
  box-sizing: border-box;
}

body {
  min-width: 320px;
  margin: 0;
  color: hsl(var(--foreground));
  background: hsl(var(--background-deep));
  font-family:
    Inter,
    ui-sans-serif,
    system-ui,
    -apple-system,
    BlinkMacSystemFont,
    'Segoe UI',
    sans-serif;
}

a {
  color: inherit;
  text-decoration: none;
}

button,
input,
select,
textarea {
  font: inherit;
}

.sso-button {
  display: inline-flex;
  height: 36px;
  align-items: center;
  justify-content: center;
  padding: 0 16px;
  color: #fff;
  cursor: pointer;
  background: hsl(var(--primary));
  border: 1px solid hsl(var(--primary));
  border-radius: 8px;
  font-weight: 600;
}

.sso-button:hover {
  filter: brightness(1.06);
}

.sso-button:disabled {
  cursor: not-allowed;
  opacity: 0.65;
}

.sso-button--outline {
  color: hsl(var(--foreground));
  background: hsl(var(--background));
  border-color: hsl(var(--border));
}

.sso-button--block {
  width: 100%;
}

.sso-spinner {
  width: 28px;
  height: 28px;
  border: 3px solid hsl(var(--primary) / 18%);
  border-top-color: hsl(var(--primary));
  border-radius: 999px;
  animation: sso-spin 0.8s linear infinite;
}

@keyframes sso-spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
