<script lang="ts" setup>
import type { ToolbarNames } from 'md-editor-v3';

import { computed } from 'vue';

import { MdEditor, MdPreview } from 'md-editor-v3';

import 'md-editor-v3/lib/style.css';

const props = withDefaults(
  defineProps<{
    height?: number | string;
    placeholder?: string;
    preview?: boolean;
    previewOnly?: boolean;
    readOnly?: boolean;
    toolbarsExclude?: ToolbarNames[];
  }>(),
  {
    height: 360,
    placeholder: '使用 Markdown 编写内容。',
    preview: false,
    previewOnly: false,
    readOnly: false,
    toolbarsExclude: () =>
      ['github', 'save', 'catalog', 'preview', 'previewOnly', 'htmlPreview'] as ToolbarNames[],
  },
);

const modelValue = defineModel<string>({ default: '' });
const editorHeight = computed(() =>
  typeof props.height === 'number' ? `${props.height}px` : props.height,
);
</script>

<template>
  <MdPreview
    v-if="previewOnly"
    :model-value="modelValue"
    class="sprint-markdown-editor sprint-markdown-preview-only"
    language="zh-CN"
    :preview-theme="'default'"
    :style="{ height: editorHeight }"
  />
  <MdEditor
    v-else
    v-model="modelValue"
    class="sprint-markdown-editor"
    language="zh-CN"
    :preview="preview"
    :placeholder="placeholder"
    :preview-theme="'default'"
    :read-only="readOnly"
    :style="{ height: editorHeight }"
    :toolbars-exclude="toolbarsExclude"
  />
</template>

<style scoped>
.sprint-markdown-editor {
  border-color: var(--td-component-border);
  border-radius: 6px;
}

.sprint-markdown-editor :deep(.md-editor-toolbar-wrapper) {
  border-bottom-color: var(--td-component-border);
}

.sprint-markdown-preview-only {
  overflow: auto;
}
</style>
