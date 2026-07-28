<script lang="ts" setup>
import type { SystemApi } from '#/api';
import type { FormInstanceFunctions, FormRules, PrimaryTableCol } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref, watch } from 'vue';

import {
  listDictionaryItemsApi,
  listDictionaryTypesApi,
  listPromptTemplatesApi,
  savePromptTemplateApi,
} from '#/api';
import {
  requiredRule,
  validateForm,
} from '#/views/_shared/form-rules';
import MarkdownEditor from '#/views/sprint/_shared/markdown-editor.vue';
import RowAction from '#/views/system/_shared/row-action.vue';
import { getCellRow } from '#/views/system/_shared/table-cell';
import SystemPage from '#/views/system/_shared/system-page.vue';
import {
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Space as TSpace,
  TabPanel as TTabPanel,
  Tabs as TTabs,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'GlobalConfigPromptTemplates' });

const AI_PLATFORM_DICTIONARY_CODE = 'ai_platform_support';
const CODEX_PLATFORM_CODE = 'codex';

const fallbackPlatformItems: SystemApi.DictionaryItem[] = [
  {
    code: 'codex',
    dictionaryTypeId: AI_PLATFORM_DICTIONARY_CODE,
    id: 'fallback-ai-platform-codex',
    name: 'Codex',
    sort: 10,
    status: 1,
  },
  {
    code: 'claude_code',
    dictionaryTypeId: AI_PLATFORM_DICTIONARY_CODE,
    id: 'fallback-ai-platform-claude-code',
    name: 'ClaudeCode',
    sort: 20,
    status: 1,
  },
  {
    code: 'work_buddy',
    dictionaryTypeId: AI_PLATFORM_DICTIONARY_CODE,
    id: 'fallback-ai-platform-work-buddy',
    name: 'WorkBuddy',
    sort: 30,
    status: 1,
  },
  {
    code: 'open_claw',
    dictionaryTypeId: AI_PLATFORM_DICTIONARY_CODE,
    id: 'fallback-ai-platform-open-claw',
    name: 'OpenClaw',
    sort: 40,
    status: 1,
  },
];

const activePromptTab = ref(CODEX_PLATFORM_CODE);
const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const platformItems = ref<SystemApi.DictionaryItem[]>([]);
const templates = ref<SystemApi.PromptTemplate[]>([]);

const form = reactive<Partial<SystemApi.PromptTemplate>>({
  agentEnvironment: CODEX_PLATFORM_CODE,
  code: '',
  content: '',
  description: '',
  id: undefined,
  name: '',
  sort: 10,
  status: 1,
});
const rules: FormRules<typeof form> = {
  code: requiredRule('请输入模板编码'),
  content: requiredRule('请输入提示词内容'),
  name: requiredRule('请输入模板名称'),
};
const templateVariables = [
  '{{agent_token}}',
  '{{mcp_endpoint}}',
  '{{task_id}}',
];
const columns: PrimaryTableCol[] = [
  { colKey: 'code', title: '模板编码', width: 180 },
  { colKey: 'name', title: '模板名称', width: 180 },
  {
    cell: (...args: any[]) => getCellRow(args[0], args[1])?.description || '-',
    colKey: 'description',
    title: '说明',
  },
  { cell: 'actions', colKey: 'actions', title: '操作', width: 100 },
];

const promptTabs = computed(() => {
  const source = platformItems.value.length > 0 ? platformItems.value : fallbackPlatformItems;
  return [...source]
    .filter((item) => item.status === 1)
    .sort((left, right) => left.sort - right.sort || left.code.localeCompare(right.code))
    .map((item) => ({ label: item.name, value: item.code }));
});
const currentPlatformLabel = computed(() => {
  return promptTabs.value.find((item) => item.value === form.agentEnvironment)?.label || form.agentEnvironment || '-';
});

watch(
  promptTabs,
  (tabs) => {
    if (tabs.length === 0) return;
    if (!tabs.some((item) => item.value === activePromptTab.value)) {
      activePromptTab.value = tabs.some((item) => item.value === CODEX_PLATFORM_CODE)
        ? CODEX_PLATFORM_CODE
        : tabs[0]!.value;
    }
  },
  { immediate: true },
);

watch(
  activePromptTab,
  async (platform) => {
    if (platform.toLowerCase() === CODEX_PLATFORM_CODE) {
      await loadTemplates();
    }
  },
);

function resetForm(row: SystemApi.PromptTemplate) {
  Object.assign(form, {
    agentEnvironment: row.agentEnvironment || activePromptTab.value,
    code: row.code || '',
    content: row.content || '',
    description: row.description || '',
    id: row.id,
    name: row.name || '',
    sort: row.sort ?? 10,
    status: row.status ?? 1,
  });
}

function openTemplate(row: SystemApi.PromptTemplate) {
  resetForm(row);
  visible.value = true;
}

async function loadPlatformItems() {
  const dictionaryTypes = await listDictionaryTypesApi();
  const aiPlatformType = dictionaryTypes.find((item) =>
    item.status === 1 &&
    (item.code === AI_PLATFORM_DICTIONARY_CODE || item.name === 'AI平台支持' || item.name === 'AI 平台支持'),
  );
  platformItems.value = aiPlatformType ? await listDictionaryItemsApi(aiPlatformType.id) : fallbackPlatformItems;
}

async function loadTemplates() {
  loading.value = true;
  try {
    templates.value = await listPromptTemplatesApi(activePromptTab.value);
  } finally {
    loading.value = false;
  }
}

async function saveTemplate() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
  if (!form.id) {
    MessagePlugin.warning('请选择要编辑的提示词模板');
    return;
  }

  saving.value = true;
  try {
    await savePromptTemplateApi({
      ...form,
      agentEnvironment: form.agentEnvironment,
      code: form.code,
      sort: Number(form.sort || 0),
    });
    MessagePlugin.success('提示词模板已保存');
    visible.value = false;
    await loadTemplates();
  } finally {
    saving.value = false;
  }
}

onMounted(async () => {
  await loadPlatformItems();
  await loadTemplates();
});
</script>

<template>
  <div class="prompt-page">
    <TTabs v-model="activePromptTab" class="prompt-tabs" :destroy-on-hide="false">
      <TTabPanel v-for="tab in promptTabs" :key="tab.value" :value="tab.value" :label="tab.label">
        <SystemPage
          v-if="tab.value.toLowerCase() === CODEX_PLATFORM_CODE"
          title="提示词设置"
          description="维护 Codex 可复用的提示词模板，模板可使用 {{...}} 占位符注入令牌、端点和任务上下文。"
          :addable="false"
          :columns="columns"
          :data="templates"
          :loading="loading"
          :show-filter-form="false"
          @refresh="loadTemplates"
        >
          <template #toolbar>
            <TTag theme="primary" variant="light">变量</TTag>
            <TTag v-for="variable in templateVariables" :key="variable" variant="light">
              {{ variable }}
            </TTag>
          </template>
          <template #actions="{ row }">
            <TSpace>
              <RowAction icon="lucide:pencil" label="编辑" @click="openTemplate(row)" />
            </TSpace>
          </template>
        </SystemPage>

        <section v-else class="developing-panel">
          <TTag theme="warning" variant="light">正在开发</TTag>
        </section>
      </TTabPanel>
    </TTabs>

    <TDialog
      v-model:visible="visible"
      header="提示词模板"
      width="960px"
      :confirm-btn="{ content: '保存', loading: saving }"
      @confirm="saveTemplate"
    >
      <TForm ref="formRef" :data="form" :rules="rules" label-width="112px">
        <TFormItem label="AI 平台">
          <TInput :model-value="currentPlatformLabel" disabled />
        </TFormItem>
        <TFormItem label="模板编码" name="code">
          <TInput v-model="form.code" disabled />
        </TFormItem>
        <TFormItem label="模板名称" name="name">
          <TInput v-model="form.name" placeholder="任务执行提示词" />
        </TFormItem>
        <TFormItem label="提示词内容" name="content">
          <MarkdownEditor v-model="form.content" :height="520" placeholder="请输入提示词内容" />
        </TFormItem>
        <TFormItem label="说明">
          <TTextarea v-model="form.description" :autosize="{ minRows: 2, maxRows: 4 }" />
        </TFormItem>
      </TForm>
    </TDialog>
  </div>
</template>

<style scoped>
.prompt-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.prompt-tabs {
  background: var(--td-bg-color-container);
  border-radius: 6px;
}

.prompt-tabs :deep(.t-tabs__content) {
  padding-top: 12px;
}

.developing-panel {
  display: grid;
  min-height: 260px;
  place-items: center;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}
</style>
