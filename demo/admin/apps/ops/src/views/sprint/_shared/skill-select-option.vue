<script lang="ts" setup>
import type { SprintMvpApi } from '#/api/sprint/mvp';

import { Tag as TTag } from 'tdesign-vue-next';

defineProps<{
  skill: SprintMvpApi.Skill;
}>();

const skillTypeLabels: Record<string, string> = {
  debugging: '调试',
  development: '开发',
  operations: '运维',
  other: '其他',
  requirement_analysis: '需求分析',
};

function skillTypeLabel(type?: string) {
  return type ? skillTypeLabels[type] || '其他' : '其他';
}
</script>

<template>
  <span class="skill-select-option">
    <span class="skill-select-option__name">{{ skill.code }} - {{ skill.name }}</span>
    <TTag size="small" variant="light">{{ skillTypeLabel(skill.type) }}</TTag>
  </span>
</template>

<style scoped>
.skill-select-option {
  display: flex;
  width: 100%;
  min-width: 0;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.skill-select-option__name {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
