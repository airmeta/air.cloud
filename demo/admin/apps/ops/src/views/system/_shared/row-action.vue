<script lang="ts" setup>
import { computed } from 'vue';

import { IconifyIcon } from '@vben/icons';

import { Link as TLink } from 'tdesign-vue-next';

const props = withDefaults(
  defineProps<{
    icon?: string;
    label: string;
    theme?: 'danger' | 'default' | 'primary' | 'success' | 'warning';
  }>(),
  {
    theme: 'primary',
  },
);

const iconName = computed(() => {
  if (props.icon) {
    return props.icon;
  }

  const normalized = props.label.toLowerCase();
  if (['delete', 'revoke', '删除', '撤销'].some((item) => normalized.includes(item.toLowerCase()))) {
    return 'lucide:trash-2';
  }
  if (['edit', '编辑', '授权', '维护'].some((item) => normalized.includes(item.toLowerCase()))) {
    return 'lucide:pencil';
  }
  if (['select', '选择'].some((item) => normalized.includes(item.toLowerCase()))) {
    return 'lucide:check';
  }
  if (['service', '服务'].some((item) => normalized.includes(item.toLowerCase()))) {
    return 'lucide:server-cog';
  }
  if (['permission', '权限'].some((item) => normalized.includes(item.toLowerCase()))) {
    return 'lucide:key-round';
  }
  return 'lucide:circle-dot';
});
</script>

<template>
  <TLink :theme="theme" class="system-row-action">
    <IconifyIcon :icon="iconName" class="system-row-action__icon" />
    <span>{{ label }}</span>
  </TLink>
</template>

<style scoped>
.system-row-action {
  display: inline-flex;
  align-items: center;
  gap: 4px;
}

.system-row-action__icon {
  width: 14px;
  height: 14px;
}
</style>
