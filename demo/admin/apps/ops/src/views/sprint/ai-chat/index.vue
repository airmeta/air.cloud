<script lang="ts" setup>
import type { SystemApi } from '#/api';
import type { SprintMvpApi, SprintTestApi } from '#/api/sprint/mvp';

import { computed, onMounted, reactive, ref, watch } from 'vue';

import {
  listBugsApi,
  listDevelopmentTasksApi,
  listProjectsApi,
  listRequirementsApi,
  listTestPlansApi,
} from '#/api/sprint/mvp';
import { listAiPlatformsApi } from '#/api/system/management';
import {
  startAiConversationApi,
} from '#/api';
import {
  Button as TButton,
  Card as TCard,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'SprintAiChat' });

const loading = ref(false);
const sending = ref(false);
const platforms = ref<SystemApi.AiPlatform[]>([]);
const projects = ref<SprintMvpApi.Project[]>([]);
const requirements = ref<SprintMvpApi.Requirement[]>([]);
const tasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const testPlans = ref<SprintTestApi.TestPlan[]>([]);
const bugs = ref<SprintMvpApi.Bug[]>([]);
const result = ref<SystemApi.AiConversation>();
const form = reactive<SystemApi.StartAiConversationRequest>({
  aiPlatformCode: 'openai',
  bugId: undefined,
  message: '',
  projectId: undefined,
  requirementId: undefined,
  taskId: undefined,
  testPlanId: undefined,
  title: '',
});
const activePlatforms = computed(() => platforms.value.filter((item) => item.status === 1));
const platformOptions = computed(() =>
  activePlatforms.value.map((item) => ({ label: `${item.name} / ${item.model}`, value: item.code })),
);
const projectOptions = computed(() => projects.value.map((item) => ({ label: item.name, value: item.id })));
const requirementOptions = computed(() =>
  requirements.value.map((item) => ({ label: item.title, value: item.id })),
);
const taskOptions = computed(() => tasks.value.map((item) => ({ label: item.title, value: item.id })));
const testPlanOptions = computed(() => testPlans.value.map((item) => ({ label: item.name, value: item.id })));
const bugOptions = computed(() => bugs.value.map((item) => ({ label: item.title, value: item.id })));
const contextSummary = computed(() => [
  form.requirementId && '需求',
  form.taskId && '任务',
  form.testPlanId && '测试计划',
  form.bugId && '缺陷',
].filter(Boolean));

watch(
  () => form.projectId,
  async () => {
    form.requirementId = undefined;
    form.taskId = undefined;
    form.testPlanId = undefined;
    form.bugId = undefined;
    await loadContextOptions();
  },
);

async function loadBase() {
  loading.value = true;
  try {
    const [platformRows, projectRows] = await Promise.all([
      listAiPlatformsApi({ status: 1 }),
      listProjectsApi(),
    ]);
    platforms.value = platformRows;
    projects.value = projectRows;
    form.aiPlatformCode = activePlatforms.value[0]?.code || 'openai';
    if (!form.projectId) {
      form.projectId = projectRows[0]?.id;
    }
    await loadContextOptions();
  } finally {
    loading.value = false;
  }
}

async function loadContextOptions() {
  const projectId = form.projectId;
  const [requirementRows, taskRows, testPlanRows, bugRows] = await Promise.all([
    listRequirementsApi(projectId),
    listDevelopmentTasksApi({ projectId }),
    listTestPlansApi(projectId),
    listBugsApi(projectId),
  ]);
  requirements.value = requirementRows;
  tasks.value = taskRows;
  testPlans.value = testPlanRows;
  bugs.value = bugRows;
}

async function send() {
  if (sending.value) return;
  if (!form.message?.trim()) {
    MessagePlugin.warning('请输入对话内容');
    return;
  }
  if (contextSummary.value.length === 0) {
    MessagePlugin.warning('请至少关联一个业务数据');
    return;
  }

  sending.value = true;
  try {
    result.value = await startAiConversationApi({
      ...form,
      message: form.message.trim(),
      title: form.title?.trim() || undefined,
    });
    if (result.value.status === 'completed') {
      MessagePlugin.success('AI对话已完成');
    } else {
      MessagePlugin.warning('AI对话调用失败，已保存记录');
    }
  } finally {
    sending.value = false;
  }
}

onMounted(loadBase);
</script>

<template>
  <div class="ai-chat-page">
    <header class="ai-chat-page__header">
      <div>
        <h2>AI对话</h2>
        <p>发起对话前选择需求、任务、测试计划或缺陷，平台会将选中的业务数据作为上下文提交给 AI。</p>
      </div>
      <TSpace>
        <TTag v-for="item in contextSummary" :key="item" theme="primary" variant="light">{{ item }}</TTag>
      </TSpace>
    </header>

    <section class="ai-chat-page__grid">
      <TCard title="上下文选择" :bordered="false">
        <TForm :data="form" label-width="100px">
          <TFormItem label="AI平台">
            <TSelect v-model="form.aiPlatformCode" :loading="loading" :options="platformOptions" placeholder="请选择AI平台" />
          </TFormItem>
          <TFormItem label="项目">
            <TSelect v-model="form.projectId" clearable :loading="loading" :options="projectOptions" placeholder="请选择项目" />
          </TFormItem>
          <TFormItem label="需求">
            <TSelect v-model="form.requirementId" clearable filterable :options="requirementOptions" placeholder="可选需求" />
          </TFormItem>
          <TFormItem label="任务">
            <TSelect v-model="form.taskId" clearable filterable :options="taskOptions" placeholder="可选任务" />
          </TFormItem>
          <TFormItem label="测试计划">
            <TSelect v-model="form.testPlanId" clearable filterable :options="testPlanOptions" placeholder="可选测试计划" />
          </TFormItem>
          <TFormItem label="缺陷">
            <TSelect v-model="form.bugId" clearable filterable :options="bugOptions" placeholder="可选缺陷" />
          </TFormItem>
          <TFormItem label="对话标题">
            <TInput v-model="form.title" clearable placeholder="可选标题" />
          </TFormItem>
        </TForm>
      </TCard>

      <TCard title="对话内容" :bordered="false">
        <TTextarea v-model="form.message" :autosize="{ minRows: 10, maxRows: 14 }" placeholder="请输入要咨询 AI 的问题" />
        <div class="ai-chat-page__actions">
          <TButton theme="primary" :loading="sending" @click="send">发送</TButton>
        </div>
        <div v-if="result" class="ai-chat-page__result">
          <TTag :theme="result.status === 'completed' ? 'success' : 'danger'" variant="light">
            {{ result.status === 'completed' ? '成功' : '失败' }}
          </TTag>
          <TTextarea
            readonly
            :model-value="result.assistantMessage || result.errorMessage || ''"
            :autosize="{ minRows: 8, maxRows: 16 }"
          />
        </div>
      </TCard>
    </section>
  </div>
</template>

<style scoped>
.ai-chat-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.ai-chat-page__header {
  align-items: flex-start;
  display: flex;
  justify-content: space-between;
  gap: 16px;
}

.ai-chat-page__header h2 {
  font-size: 20px;
  font-weight: 600;
  margin: 0;
}

.ai-chat-page__header p {
  color: var(--td-text-color-secondary);
  margin: 6px 0 0;
}

.ai-chat-page__grid {
  display: grid;
  gap: 16px;
  grid-template-columns: minmax(320px, 420px) minmax(0, 1fr);
}

.ai-chat-page__actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}

.ai-chat-page__result {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-top: 16px;
}

@media (max-width: 900px) {
  .ai-chat-page__grid,
  .ai-chat-page__header {
    grid-template-columns: 1fr;
    flex-direction: column;
  }
}
</style>
