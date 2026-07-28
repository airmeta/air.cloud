<script lang="ts" setup>
import type { CodeReviewApi } from '#/api/code-review';
import type { PrimaryTableCol } from 'tdesign-vue-next';

import { computed, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import { IconifyIcon } from '@vben/icons';

import {
  Button as TButton,
  Descriptions as TDescriptions,
  DescriptionsItem as TDescriptionsItem,
  Empty as TEmpty,
  Loading as TLoading,
  TabPanel as TTabPanel,
  Tabs as TTabs,
  Table as TTable,
  Tag as TTag,
} from 'tdesign-vue-next';

import {
  getCodeAuditReleaseReportApi,
  getCodeAuditTaskApi,
} from '#/api/code-review';

defineOptions({ name: 'CodeReviewResultDetail' });

type JsonRecord = Record<string, unknown>;
type JsonItem = JsonRecord | string | number | boolean | null;
type IssueTableRow = {
  category: string;
  description: string;
  file: string;
  id: string;
  impact: string;
  level: string;
  location: string;
  rawJson: string;
  recommendation: string;
  sequence: number;
  severity: string;
  title: string;
  trigger: string;
};

const route = useRoute();
const router = useRouter();
const loading = ref(false);
const detail = ref<CodeReviewApi.CodeAuditTaskDetail>();
const releaseReport = ref<CodeReviewApi.ReleaseReport>();
const activeDetailTab = ref('issues');

const statusOptions = [
  { label: '通过', value: 'passed' },
  { label: '需修改', value: 'needs_changes' },
  { label: '阻断', value: 'blocked' },
  { label: '失败', value: 'failed' },
  { label: '已取消', value: 'cancelled' },
  { label: '待审计', value: 'pending' },
  { label: '审计中', value: 'running' },
];

const issueColumns: PrimaryTableCol[] = [
  { align: 'center', colKey: 'sequence', title: '序号', width: 72 },
  { colKey: 'title', ellipsis: true, minWidth: 420, title: '问题' },
  { cell: 'severity', colKey: 'severity', title: '严重程度', width: 130 },
  { cell: 'level', colKey: 'level', title: '级别', width: 110 },
  { colKey: 'category', title: '分类', width: 160 },
];

const annotationIssueColumns: PrimaryTableCol[] = [...issueColumns];

function statusTheme(status?: CodeReviewApi.TaskStatus) {
  if (status === 'passed') return 'success';
  if (status === 'running') return 'primary';
  if (status === 'pending' || status === 'needs_changes') return 'warning';
  if (status === 'blocked' || status === 'failed') return 'danger';
  return 'default';
}

function statusText(status?: CodeReviewApi.TaskStatus) {
  return statusOptions.find((item) => item.value === status)?.label || '-';
}

function parseJsonArray(value?: string): JsonItem[] {
  if (!value) {
    return [];
  }

  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function isRecord(item: JsonItem): item is JsonRecord {
  return typeof item === 'object' && item !== null && !Array.isArray(item);
}

function stringifyValue(value: unknown, fallback = '-') {
  if (value === undefined || value === null || value === '') {
    return fallback;
  }

  if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') {
    return String(value);
  }

  return JSON.stringify(value);
}

function pickText(item: JsonItem, keys: string[], fallback = '-') {
  if (!isRecord(item)) {
    return stringifyValue(item, fallback);
  }

  const value = keys
    .map((key) => item[key])
    .find((candidate) => candidate !== undefined && candidate !== null && candidate !== '');
  return stringifyValue(value, fallback);
}

function normalizeIssueRows(items: JsonItem[], prefix: string): IssueTableRow[] {
  return items.map((item, index) => ({
    category: pickText(item, ['category', 'type', 'rule', 'ruleId', 'code'], '-'),
    description: pickText(item, ['description', 'detail', 'reason', 'explanation'], ''),
    file: pickText(item, ['file', 'path', 'filePath', 'filename'], '-'),
    id: `${prefix}-${index}`,
    impact: pickText(item, ['impact'], ''),
    level: pickText(item, ['level'], '-'),
    location: pickText(item, ['location', 'position', 'range'], '-'),
    rawJson: stringifyValue(item, ''),
    recommendation: pickText(item, ['direction', 'fix', 'recommendation', 'suggestion', 'solution'], ''),
    sequence: index + 1,
    severity: pickText(item, ['severity'], 'unclassified'),
    title: pickText(item, ['problem', 'message', 'title', 'summary', 'issue'], '未命名问题'),
    trigger: pickText(item, ['trigger', 'condition'], ''),
  }));
}

function severityTheme(severity: string) {
  const normalized = severity.toLowerCase();
  if (['blocker', 'blocking', 'critical', 'high', '严重', '阻断', '高'].some((item) => normalized.includes(item))) return 'danger';
  if (['medium', 'major', 'warning', '中'].some((item) => normalized.includes(item))) return 'warning';
  if (['low', 'minor', 'info', '低'].some((item) => normalized.includes(item))) return 'primary';
  return 'default';
}

const issueItems = computed(() => parseJsonArray(detail.value?.result?.issuesJson));
const annotationIssueItems = computed(() => parseJsonArray(detail.value?.result?.annotationIssuesJson));
const manualCheckItems = computed(() => parseJsonArray(detail.value?.result?.manualCheckItemsJson));
const changedFiles = computed(() => parseJsonArray(detail.value?.result?.changedFilesJson));
const issueRows = computed(() => normalizeIssueRows(issueItems.value, 'issue'));
const annotationIssueRows = computed(() => normalizeIssueRows(annotationIssueItems.value, 'annotation'));

async function loadDetail() {
  const taskId = String(route.params.id || '');
  if (!taskId) return;

  loading.value = true;
  activeDetailTab.value = 'issues';
  releaseReport.value = undefined;
  try {
    const [taskDetail, report] = await Promise.all([
      getCodeAuditTaskApi(taskId),
      getCodeAuditReleaseReportApi(taskId),
    ]);
    detail.value = taskDetail;
    releaseReport.value = report;
  } finally {
    loading.value = false;
  }
}

function goBack() {
  router.push('/code-review/tasks');
}

loadDetail();
</script>

<template>
  <div class="result-detail-page">
    <section class="detail-header">
      <div>
        <h2>代码审计结果详情</h2>
        <p>{{ detail?.task.id || route.params.id }}</p>
      </div>
      <div class="header-actions">
        <TButton variant="outline" @click="goBack">
          <template #icon>
            <IconifyIcon icon="lucide:arrow-left" />
          </template>
          返回列表
        </TButton>
        <TButton :disabled="loading" @click="loadDetail">
          <template #icon>
            <IconifyIcon icon="lucide:refresh-cw" />
          </template>
          刷新
        </TButton>
      </div>
    </section>

    <TLoading :loading="loading" show-overlay>
      <TEmpty v-if="!loading && !detail" description="审计结果不存在或已被删除" />

      <template v-else-if="detail">
        <section class="detail-summary">
          <TDescriptions bordered :column="2">
            <TDescriptionsItem label="审计任务">{{ detail.task.id }}</TDescriptionsItem>
            <TDescriptionsItem label="结论">
              <TTag :theme="statusTheme(detail.task.status)" variant="light">{{ statusText(detail.task.status) }}</TTag>
            </TDescriptionsItem>
            <TDescriptionsItem label="项目">{{ detail.task.projectId }}</TDescriptionsItem>
            <TDescriptionsItem label="仓库">{{ detail.task.gitRepositoryId }}</TDescriptionsItem>
            <TDescriptionsItem label="分支">{{ detail.result?.branch || detail.task.branch }}</TDescriptionsItem>
            <TDescriptionsItem label="Git Commit">{{ detail.result?.gitCommitId || detail.task.headCommitId || '-' }}</TDescriptionsItem>
            <TDescriptionsItem label="Base Commit">{{ detail.task.baseCommitId || '-' }}</TDescriptionsItem>
            <TDescriptionsItem label="Head Commit">{{ detail.task.headCommitId || '-' }}</TDescriptionsItem>
            <TDescriptionsItem label="当前分支 Head">{{ detail.task.currentBranchHeadCommitId || '-' }}</TDescriptionsItem>
            <TDescriptionsItem label="运行记录">{{ detail.result?.workerRunId || '-' }}</TDescriptionsItem>
          </TDescriptions>

          <div v-if="detail.task.workspaceDirtyReason" class="detail-warning">
            {{ detail.task.workspaceDirtyReason }}
          </div>
        </section>

        <section class="detail-tabs">
          <TTabs v-model="activeDetailTab" class="result-tabs">
            <TTabPanel value="issues" label="问题列表">
              <div v-if="issueRows.length === 0" class="detail-empty">未记录明确问题。</div>
              <TTable
                v-else
                row-key="id"
                class="issue-table"
                size="small"
                :columns="issueColumns"
                :data="issueRows"
                hover
                stripe
              >
                <template #severity="{ row }">
                  <TTag :theme="severityTheme(row.severity)" variant="light">{{ row.severity }}</TTag>
                </template>
                <template #level="{ row }">
                  <TTag :theme="severityTheme(row.level)" variant="light">{{ row.level }}</TTag>
                </template>
                <template #expandedRow="{ row }">
                  <div class="issue-expanded">
                    <section v-if="row.file !== '-' || row.location !== '-'">
                      <h4>文件与位置</h4>
                      <p v-if="row.file !== '-'">文件：{{ row.file }}</p>
                      <p v-if="row.location !== '-'">位置：{{ row.location }}</p>
                    </section>
                    <section>
                      <h4>问题说明</h4>
                      <p>{{ row.description || row.title }}</p>
                    </section>
                    <section v-if="row.impact">
                      <h4>影响</h4>
                      <p>{{ row.impact }}</p>
                    </section>
                    <section v-if="row.trigger">
                      <h4>触发条件</h4>
                      <p>{{ row.trigger }}</p>
                    </section>
                    <section v-if="row.recommendation">
                      <h4>修复方向</h4>
                      <p>{{ row.recommendation }}</p>
                    </section>
                    <section>
                      <h4>原始数据</h4>
                      <pre>{{ row.rawJson }}</pre>
                    </section>
                  </div>
                </template>
              </TTable>
            </TTabPanel>

            <TTabPanel value="annotations" label="注释检查">
              <div v-if="annotationIssueRows.length === 0" class="detail-empty">注释检查未记录明确问题。</div>
              <TTable
                v-else
                row-key="id"
                class="issue-table"
                size="small"
                :columns="annotationIssueColumns"
                :data="annotationIssueRows"
                hover
                stripe
              >
                <template #severity="{ row }">
                  <TTag :theme="severityTheme(row.severity)" variant="light">{{ row.severity }}</TTag>
                </template>
                <template #level="{ row }">
                  <TTag :theme="severityTheme(row.level)" variant="light">{{ row.level }}</TTag>
                </template>
                <template #expandedRow="{ row }">
                  <div class="issue-expanded">
                    <section v-if="row.file !== '-' || row.location !== '-'">
                      <h4>文件与位置</h4>
                      <p v-if="row.file !== '-'">文件：{{ row.file }}</p>
                      <p v-if="row.location !== '-'">位置：{{ row.location }}</p>
                    </section>
                    <section>
                      <h4>问题说明</h4>
                      <p>{{ row.description || row.title }}</p>
                    </section>
                    <section v-if="row.impact">
                      <h4>影响</h4>
                      <p>{{ row.impact }}</p>
                    </section>
                    <section v-if="row.trigger">
                      <h4>触发条件</h4>
                      <p>{{ row.trigger }}</p>
                    </section>
                    <section v-if="row.recommendation">
                      <h4>修复方向</h4>
                      <p>{{ row.recommendation }}</p>
                    </section>
                    <section>
                      <h4>原始数据</h4>
                      <pre>{{ row.rawJson }}</pre>
                    </section>
                  </div>
                </template>
              </TTable>
            </TTabPanel>

            <TTabPanel value="manual" label="人工确认项">
              <div v-if="manualCheckItems.length === 0" class="detail-empty">暂无人工确认项。</div>
              <ul v-else class="manual-list">
                <li v-for="(item, index) in manualCheckItems" :key="index">{{ stringifyValue(item) }}</li>
              </ul>
            </TTabPanel>

            <TTabPanel value="release" label="发布报告">
              <div v-if="!releaseReport" class="detail-empty">暂无发布报告。</div>
              <div v-else class="report-panel">
                <div class="report-summary">
                  <TTag :theme="releaseReport.canRelease ? 'success' : 'danger'" variant="light">
                    {{ releaseReport.canRelease ? '可发布' : '不可发布' }}
                  </TTag>
                  <span>变更文件 {{ releaseReport.changedFileCount }}</span>
                  <span>问题 {{ releaseReport.issueCount }}</span>
                  <span>阻断 {{ releaseReport.blockingIssueCount }}</span>
                  <span>人工确认 {{ releaseReport.manualCheckCount }}</span>
                </div>
                <div class="report-block">
                  <h4>阻断摘要</h4>
                  <div v-if="releaseReport.blockingSummaries.length === 0" class="detail-empty">无阻断摘要。</div>
                  <ul v-else>
                    <li v-for="(item, index) in releaseReport.blockingSummaries" :key="index">{{ item }}</li>
                  </ul>
                </div>
                <div class="report-block">
                  <h4>人工确认</h4>
                  <div v-if="releaseReport.manualCheckItems.length === 0" class="detail-empty">无人工确认项。</div>
                  <ul v-else>
                    <li v-for="(item, index) in releaseReport.manualCheckItems" :key="index">{{ item }}</li>
                  </ul>
                </div>
              </div>
            </TTabPanel>

            <TTabPanel value="files" label="Changed files">
              <div v-if="changedFiles.length === 0" class="detail-empty">暂无 changed files 记录。</div>
              <template v-else>
                <div v-for="(item, index) in changedFiles" :key="index" class="file-row">
                  <TTag variant="light">{{ pickText(item, ['status'], 'changed') }}</TTag>
                  <span>{{ pickText(item, ['path', 'filePath']) }}</span>
                </div>
              </template>
            </TTabPanel>

            <TTabPanel value="raw" label="原始输出">
              <pre class="raw-output">{{ detail.result?.rawResult || '暂无原始输出。' }}</pre>
            </TTabPanel>
          </TTabs>
        </section>
      </template>
    </TLoading>
  </div>
</template>

<style scoped>
.result-detail-page {
  display: grid;
  gap: 16px;
  padding: 16px;
}

.detail-header,
.detail-summary {
  padding: 16px 20px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.detail-header {
  display: flex;
  gap: 16px;
  align-items: center;
  justify-content: space-between;
}

.detail-header h2 {
  margin: 0;
  font-size: 20px;
}

.detail-header p {
  margin: 6px 0 0;
  color: var(--td-text-color-secondary);
}

.header-actions,
.file-row,
.report-summary {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}

.detail-warning {
  padding: 10px 12px;
  margin-top: 16px;
  color: var(--td-error-color-7);
  background: var(--td-error-color-1);
  border: 1px solid var(--td-error-color-3);
  border-radius: 6px;
}

.detail-tabs {
  min-width: 0;
}

.result-tabs :deep(.t-tabs__nav) {
  margin-bottom: 14px;
}

.result-tabs :deep(.t-tabs__content) {
  padding-top: 0;
}

.detail-empty {
  padding: 18px 0;
  color: var(--td-text-color-secondary);
}

.file-row {
  padding: 8px 0;
  color: var(--td-text-color-secondary);
  border-bottom: 1px solid var(--td-border-level-1-color);
}

.issue-table {
  width: 100%;
}

.issue-table :deep(.t-table__expanded-row-inner) {
  padding: 0;
}

.issue-expanded {
  display: grid;
  gap: 12px;
  padding: 12px 16px;
  background: var(--td-bg-color-container-hover);
}

.issue-expanded h4 {
  margin: 0 0 6px;
  font-size: 13px;
}

.issue-expanded p {
  margin: 0;
  color: var(--td-text-color-secondary);
  white-space: pre-wrap;
}

.issue-expanded pre {
  max-height: 260px;
  padding: 10px;
  margin: 0;
  overflow: auto;
  white-space: pre-wrap;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.report-panel {
  display: grid;
  gap: 14px;
}

.report-block h4 {
  margin: 0 0 8px;
  font-size: 14px;
}

.manual-list {
  padding-left: 18px;
}

.raw-output {
  max-height: 620px;
  padding: 12px;
  overflow: auto;
  white-space: pre-wrap;
  background: var(--td-bg-color-container-hover);
  border-radius: 6px;
}

@media (max-width: 720px) {
  .detail-header {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
