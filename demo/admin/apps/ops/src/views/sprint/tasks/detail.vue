<script lang="ts" setup>
import type { SprintMvpApi } from '#/api/sprint/mvp';

import { CircleAlert, IconifyIcon, RotateCw } from '@vben/icons';

import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';

import {
  Button as TButton,
  Descriptions as TDescriptions,
  DescriptionsItem as TDescriptionsItem,
  Dialog as TDialog,
  Empty as TEmpty,
  Form as TForm,
  FormItem as TFormItem,
  MessagePlugin,
  Select as TSelect,
  Tag as TTag,
  Textarea as TTextarea,
  Tooltip as TTooltip,
} from 'tdesign-vue-next';

import { listAgentTokensApi, type SystemApi } from '#/api';
import {
  getDevelopmentTaskPromptApi,
  listDevelopmentTasksApi,
  listProjectsApi,
  listRequirementsApi,
} from '#/api/sprint/mvp';
import { formatDateTime } from '#/views/_shared/date-format';

import {
  buildAgentsprintMcpSetupPrompt,
  buildAgentsprintMcpSetupPromptWithToken,
} from '../_shared/mcpSetupPrompt';

const route = useRoute();
const loading = ref(false);
const promptLoading = ref(false);
const mcpCopyVisible = ref(false);
const promptResult = ref<SprintMvpApi.TaskPrompt>();
const mcpSetupContent = ref('');
const agentTokens = ref<SystemApi.AgentToken[]>([]);
const selectedAgentTokenId = ref('');
const project = ref<SprintMvpApi.Project>();
const requirement = ref<SprintMvpApi.Requirement>();
const task = ref<SprintMvpApi.DevelopmentTask>();

const statusText: Record<string, string> = {
  assigned: '已指派',
  completed: '已完成',
  in_progress: '推进中',
  pending_assign: '待指派',
};

const canGeneratePrompt = computed(() => Boolean(task.value?.assigneeId));
const validAgentTokens = computed(() =>
  agentTokens.value.filter(
    (token) =>
      token.status === 1 && !token.revokedAt && new Date(token.expiresAt).getTime() > Date.now(),
  ),
);
const agentTokenOptions = computed(() =>
  validAgentTokens.value.map((token) => ({
    label: `${token.name} (${token.maskedToken})`,
    value: token.id,
  })),
);
const displayedMcpSetupPrompt = computed(() =>
  buildAgentsprintMcpSetupPrompt(promptResult.value?.mcpSetupPrompt.content),
);

async function loadDetail() {
  loading.value = true;
  try {
    const taskId = String(route.params.id || '');
    const tasks = await listDevelopmentTasksApi();
    task.value = tasks.find((item) => item.id === taskId);
    if (!task.value) return;

    const [projects, requirements] = await Promise.all([
      listProjectsApi(),
      listRequirementsApi(task.value.projectId),
    ]);
    project.value = projects.find((item) => item.id === task.value?.projectId);
    requirement.value = requirements.find((item) => item.id === task.value?.requirementId);
    agentTokens.value = await listAgentTokensApi();
    await loadPrompt();
  } finally {
    loading.value = false;
  }
}

async function loadPrompt() {
  if (!task.value) return;
  promptLoading.value = true;
  try {
    const result = await getDevelopmentTaskPromptApi(task.value.id);
    promptResult.value = result;
  } catch {
    MessagePlugin.warning('仅任务负责人可以生成任务推进提示词');
  } finally {
    promptLoading.value = false;
  }
}

function fallbackCopyText(content: string) {
  const textarea = document.createElement('textarea');
  textarea.value = content;
  textarea.style.position = 'fixed';
  textarea.style.left = '-9999px';
  textarea.style.top = '0';
  textarea.setAttribute('readonly', 'readonly');
  document.body.append(textarea);
  textarea.select();
  textarea.setSelectionRange(0, textarea.value.length);
  try {
    return document.execCommand('copy');
  } finally {
    textarea.remove();
  }
}

async function copyPrompt(content: string) {
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(content);
    } else if (!fallbackCopyText(content)) {
      throw new Error('Clipboard fallback failed');
    }
    MessagePlugin.success('写入剪切板成功');
    return true;
  } catch {
    if (fallbackCopyText(content)) {
      MessagePlugin.success('写入剪切板成功');
      return true;
    }
    MessagePlugin.error('写入剪切板失败，请手动复制文本框内容');
    return false;
  }
}

async function openMcpCopy() {
  if (!task.value) return;
  selectedAgentTokenId.value = validAgentTokens.value[0]?.id || '';
  await refreshMcpSetupPrompt();
  mcpCopyVisible.value = true;
}

function selectedAgentToken() {
  return validAgentTokens.value.find((token) => token.id === selectedAgentTokenId.value);
}

async function refreshMcpSetupPrompt() {
  if (!task.value || !selectedAgentTokenId.value) {
    mcpSetupContent.value = '';
    return;
  }

  const result = await getDevelopmentTaskPromptApi(task.value.id);
  promptResult.value = result;
  const token = selectedAgentToken();
  const bearerToken = `Bearer ${token?.token || token?.maskedToken || ''}`;
  mcpSetupContent.value = buildAgentsprintMcpSetupPromptWithToken(
    result.mcpSetupPrompt.content,
    bearerToken,
  );
}

async function handleAgentTokenChange() {
  await refreshMcpSetupPrompt();
}

async function copyMcpSetupPrompt() {
  if (!selectedAgentTokenId.value) {
    MessagePlugin.warning('请选择一个有效令牌');
    return;
  }
  if (!mcpSetupContent.value) {
    await refreshMcpSetupPrompt();
  }
  const copied = await copyPrompt(mcpSetupContent.value);
  if (copied) {
    mcpCopyVisible.value = false;
  }
}

onMounted(loadDetail);
</script>

<template>
  <div class="detail-page">
    <section class="header">
      <div>
        <h2>{{ task?.title || '任务详情' }}</h2>
        <p>{{ project?.name || '未找到任务' }}</p>
      </div>
      <TButton @click="loadDetail">
        <template #icon>
          <IconifyIcon icon="lucide:refresh-cw" />
        </template>
        刷新
      </TButton>
    </section>

    <TEmpty v-if="!loading && !task" description="任务不存在或已被删除" />

    <template v-else-if="task">
      <section class="panel">
        <TDescriptions bordered :column="2">
          <TDescriptionsItem label="状态">
            <TTag variant="light">{{ statusText[task.status] || task.status }}</TTag>
          </TDescriptionsItem>
          <TDescriptionsItem label="优先级">{{ task.priority }}</TDescriptionsItem>
          <TDescriptionsItem label="项目">{{ project?.name || task.projectId }}</TDescriptionsItem>
          <TDescriptionsItem label="需求">
            {{ requirement?.title || task.requirementId }}
          </TDescriptionsItem>
          <TDescriptionsItem label="负责人">{{ task.assigneeId || '未指派' }}</TDescriptionsItem>
          <TDescriptionsItem label="指派人">{{ task.assignedBy || '-' }}</TDescriptionsItem>
          <TDescriptionsItem label="创建人">{{ task.createdBy }}</TDescriptionsItem>
          <TDescriptionsItem label="指派时间">{{ formatDateTime(task.assignedAt) }}</TDescriptionsItem>
          <TDescriptionsItem label="完成时间">{{ formatDateTime(task.completedAt) }}</TDescriptionsItem>
          <TDescriptionsItem label="创建时间">{{ formatDateTime(task.createTime) }}</TDescriptionsItem>
        </TDescriptions>
      </section>

      <section class="panel">
        <h3>任务说明</h3>
        <article>{{ task.description || '暂无任务说明' }}</article>
      </section>

      <section class="panel">
        <div class="prompt-header">
          <div>
            <h3>任务推进提示词</h3>
            <p v-if="promptResult">任务 ID：{{ promptResult.taskId }}</p>
          </div>
          <div>
            <TButton
              shape="circle"
              title="Refresh prompt"
              :disabled="!canGeneratePrompt"
              :loading="promptLoading"
              @click="loadPrompt"
            >
              <RotateCw class="prompt-refresh-icon" />
            </TButton>
          </div>
        </div>
        <section v-if="promptResult" class="prompt-blocks">
          <section class="prompt-block">
            <div class="prompt-block-header">
              <div>
                <h4>{{ promptResult.mcpSetupPrompt.title }}</h4>
              </div>
              <TButton theme="primary" @click="openMcpCopy">
                <template #icon>
                  <IconifyIcon icon="lucide:settings" />
                </template>
                初次接入配置
              </TButton>
            </div>
            <p class="prompt-usage">
              <strong>首次接入</strong>或本地 Codex 未发现
              <strong>AgentSprint MCP</strong> 时复制执行；默认只配置 MCP endpoint 和
              <strong>Authorization</strong>，不要默认写入
              <strong>X-AgentSprint-Api-Base-Url</strong>。
            </p>
            <div class="prompt-content">
              {{ displayedMcpSetupPrompt }}
            </div>
          </section>
          <section class="prompt-block">
            <div class="prompt-block-header">
              <div>
                <div class="prompt-title-line">
                  <h4>{{ promptResult.taskExecutionPrompt.title }}</h4>
                  <TTooltip placement="top" theme="light">
                    <template #content>
                      <ul class="prompt-note-list">
                        <li v-for="item in promptResult.taskExecutionPrompt.notes" :key="item">
                          {{ item }}
                        </li>
                      </ul>
                    </template>
                    <CircleAlert class="prompt-note-icon" />
                  </TTooltip>
                </div>
              </div>
              <TButton theme="primary" @click="copyPrompt(promptResult.taskExecutionPrompt.content)">
                <template #icon>
                  <IconifyIcon icon="lucide:copy" />
                </template>
                任务推进提示词
              </TButton>
            </div>
            <p class="prompt-usage">
              <strong>日常推进</strong>只复制此段，Codex 会按
              <strong>任务 ID</strong> 通过 <strong>MCP</strong> 拉取任务、需求和 Skill 上下文，并按
              <strong>next_work</strong> 仅作为状态参考，完成当前任务后停止接取新任务。
            </p>
            <div class="prompt-content">
              {{ promptResult.taskExecutionPrompt.content }}
            </div>
          </section>
        </section>
      </section>

      <TDialog
        v-model:visible="mcpCopyVisible"
        header="初次接入配置"
        width="min(960px, 92vw)"
        confirm-btn="写入剪切板"
        @confirm="copyMcpSetupPrompt"
      >
        <section class="mcp-copy-dialog">
          <p>
            选择一个有效令牌后会重新拉取初次接入提示词，并只替换 Authorization
            令牌；默认不写入 X-AgentSprint-Api-Base-Url。只有用户明确提供远程 MCP
            服务可访问的 AgentSprint API 地址时，才追加该可选覆盖配置，不要固定写入
            http://localhost:5000。
          </p>
          <TForm label-width="112px">
            <TFormItem label="令牌记录">
              <TSelect
                v-model="selectedAgentTokenId"
                filterable
                :options="agentTokenOptions"
                placeholder="请选择令牌"
                @change="handleAgentTokenChange"
              />
            </TFormItem>
          </TForm>
          <TTextarea
            class="mcp-copy-textarea"
            :model-value="mcpSetupContent"
            readonly
            :autosize="{ minRows: 20, maxRows: 20 }"
          />
        </section>
      </TDialog>
    </template>
  </div>
</template>

<style scoped>
.detail-page {
  padding: 16px;
}

.header,
.panel {
  margin-bottom: 16px;
  padding: 16px 20px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.header,
.prompt-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.header h2,
.panel h3 {
  margin: 0;
}

.header p {
  margin: 6px 0 0;
  color: var(--td-text-color-secondary);
}

.panel article {
  min-height: 80px;
  white-space: pre-wrap;
}

.prompt-header {
  margin-bottom: 12px;
}

.prompt-header > div {
  display: flex;
  gap: 8px;
}

.prompt-blocks {
  display: grid;
  gap: 12px;
}

.prompt-block {
  display: grid;
  gap: 12px;
  padding: 16px;
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.prompt-block-header {
  display: flex;
  gap: 16px;
  align-items: flex-start;
  justify-content: space-between;
}

.prompt-block-header h4 {
  margin: 0;
}

.prompt-title-line {
  display: flex;
  gap: 6px;
  align-items: center;
}

.prompt-note-icon {
  width: 16px;
  height: 16px;
  color: var(--td-warning-color);
  cursor: help;
}

.prompt-refresh-icon {
  width: 16px;
  height: 16px;
}

.prompt-note-list {
  max-width: 320px;
  padding-left: 18px;
  margin: 0;
  line-height: 1.7;
}

.prompt-note-list li + li {
  margin-top: 4px;
}

.prompt-usage {
  margin: 0;
  color: var(--td-text-color-secondary);
  line-height: 1.7;
}

.prompt-content {
  max-height: 360px;
  min-height: 240px;
  padding: 12px 14px;
  overflow: auto;
  font-size: 13px;
  font-family: ui-monospace, SFMono-Regular, Consolas, 'Liberation Mono', monospace;
  line-height: 1.7;
  white-space: pre-wrap;
  word-break: break-word;
  background: var(--td-bg-color-secondarycontainer);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.mcp-copy-dialog {
  display: grid;
  gap: 12px;
}

.mcp-copy-dialog p {
  margin: 0;
  color: var(--td-text-color-secondary);
  line-height: 1.7;
}

.mcp-copy-textarea :deep(textarea) {
  overflow-y: auto;
  font-family: ui-monospace, SFMono-Regular, Consolas, 'Liberation Mono', monospace;
  line-height: 1.7;
}
</style>
