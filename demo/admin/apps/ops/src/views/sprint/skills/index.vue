<script lang="ts" setup>
import type { SprintMvpApi } from '#/api/sprint/mvp';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import { IconifyIcon } from '@vben/icons';

import {
  Button as TButton,
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import {
  createSkillApi,
  listSkillsApi,
  updateSkillApi,
} from '#/api/sprint/mvp';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import RowAction from '#/views/system/_shared/row-action.vue';

defineOptions({ name: 'GlobalConfigSkills' });

const loading = ref(false);
const saving = ref(false);
const importing = ref(false);
const visible = ref(false);
const importVisible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const importInputRef = ref<HTMLInputElement>();
const rows = ref<SprintMvpApi.Skill[]>([]);
const selected = ref<SprintMvpApi.Skill>();
const importFiles = ref<File[]>([]);

const filters = reactive({
  keyword: '',
  status: '',
  type: '',
});
const query = reactive({
  keyword: '',
  status: '',
  type: '',
});
const form = reactive({
  code: '',
  content: '',
  description: '',
  name: '',
  status: 'active',
  type: 'development',
});
const importForm = reactive({
  type: 'development',
});
const pagination = reactive({
  current: 1,
  pageSize: 30,
});

const skillTypeOptions = [
  { label: '开发', value: 'development' },
  { label: '调试', value: 'debugging' },
  { label: '运维', value: 'operations' },
  { label: '需求分析', value: 'requirement_analysis' },
  { label: '其他', value: 'other' },
];

function skillTypeLabel(type?: string) {
  return skillTypeOptions.find((item) => item.value === type)?.label || '其他';
}

const rules: FormRules<typeof form> = {
  content: requiredRule('请输入 Skill 内容'),
  name: requiredRule('请输入 Skill 名称'),
  type: requiredRule('请选择 Skill 类型'),
};

const columns = [
  { colKey: 'code', title: '编码', width: 160 },
  { colKey: 'name', title: '名称', width: 220 },
  { colKey: 'type', title: '类型', width: 120 },
  { colKey: 'description', ellipsis: true, title: '说明', width: 280 },
  { colKey: 'status', title: '状态', width: 100 },
  { colKey: 'actions', title: '操作', width: 150 },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: rows.value.length,
}));
const statusOptions = [
  { label: '启用', value: 'active' },
  { label: '停用', value: 'disabled' },
];

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await loadRows();
}

async function reset() {
  Object.assign(filters, { keyword: '', status: '', type: '' });
  await search();
}

function resetForm() {
  selected.value = undefined;
  Object.assign(form, {
    code: '',
    content: '',
    description: '',
    name: '',
    status: 'active',
    type: 'development',
  });
}

function openCreate() {
  resetForm();
  visible.value = true;
}

function openImport() {
  importForm.type = 'development';
  importFiles.value = [];
  if (importInputRef.value) {
    importInputRef.value.value = '';
  }
  importVisible.value = true;
}

function openEdit(row: SprintMvpApi.Skill) {
  selected.value = row;
  Object.assign(form, {
    code: row.code,
    content: row.content,
    description: row.description || '',
    name: row.name,
    status: row.status,
    type: row.type || 'development',
  });
  visible.value = true;
}

async function loadRows() {
  loading.value = true;
  try {
    rows.value = await listSkillsApi(false, {
      keyword: query.keyword || undefined,
      status: query.status || undefined,
      type: query.type || undefined,
    });
  } finally {
    loading.value = false;
  }
}

function triggerImportFileSelect() {
  importInputRef.value?.click();
}

function getSkillNameFromFile(file: File) {
  return file.name.replace(/\.(md|markdown)$/i, '').trim() || file.name;
}

function handleImportFileChange(event: Event) {
  const input = event.target as HTMLInputElement;
  const files = Array.from(input.files || []);
  const markdownFiles = files.filter((file) => /\.(md|markdown)$/i.test(file.name));
  importFiles.value = markdownFiles;
  if (files.length > 0 && markdownFiles.length !== files.length) {
    MessagePlugin.warning('仅支持导入 md 文件');
  }
}

async function importSkills() {
  if (importing.value) return;
  if (!importForm.type) {
    MessagePlugin.warning('请选择 Skill 类型');
    return;
  }
  if (importFiles.value.length === 0) {
    MessagePlugin.warning('请选择要导入的 md 文件');
    return;
  }

  importing.value = true;
  try {
    let importedCount = 0;
    for (const file of importFiles.value) {
      const content = (await file.text()).trim();
      if (!content) {
        MessagePlugin.warning(`${file.name} 内容为空，已跳过`);
        continue;
      }

      await createSkillApi({
        content,
        name: getSkillNameFromFile(file),
        type: importForm.type,
      });
      importedCount += 1;
    }

    if (importedCount === 0) {
      MessagePlugin.warning('没有可导入的 Skill 内容');
      return;
    }

    MessagePlugin.success(`已导入 ${importedCount} 个 Skill`);
    importVisible.value = false;
    await loadRows();
  } finally {
    importing.value = false;
  }
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
  if (!form.name.trim() || !form.content.trim()) {
    MessagePlugin.warning('Skill 名称和内容不能为空');
    return;
  }

  saving.value = true;
  try {
    if (selected.value) {
      await updateSkillApi(selected.value.id, {
        content: form.content.trim(),
        description: form.description.trim() || undefined,
        name: form.name.trim(),
        status: form.status,
        type: form.type,
      });
      MessagePlugin.success('Skill 已保存');
    } else {
      await createSkillApi({
        content: form.content.trim(),
        description: form.description.trim() || undefined,
        name: form.name.trim(),
        type: form.type,
      });
      MessagePlugin.success('Skill 已创建');
    }

    visible.value = false;
    await loadRows();
  } finally {
    saving.value = false;
  }
}

async function switchStatus(row: SprintMvpApi.Skill) {
  await updateSkillApi(row.id, {
    content: row.content,
    description: row.description,
    name: row.name,
    status: row.status === 'active' ? 'disabled' : 'active',
    type: row.type,
  });
  MessagePlugin.success(row.status === 'active' ? 'Skill 已停用' : 'Skill 已启用');
  await loadRows();
}

onMounted(async () => {
  await loadRows();
});
</script>

<template>
  <div class="skills-page">
    <AdminListPage
      title="Skill配置"
      description="维护项目构建和新建需求时可选择的 Skill 指令内容。"
      table-title="Skill列表"
      add-button-text="新增Skill"
      :columns="columns"
      :data="rows"
      :loading="loading"
      :pagination="tablePagination"
      @add="openCreate"
      @page-change="handlePageChange"
      @refresh="loadRows"
      @reset="reset"
      @search="search"
    >
      <template #toolbar>
        <TButton variant="outline" @click="openImport">
          <template #icon>
            <IconifyIcon icon="lucide:upload" />
          </template>
          批量导入
        </TButton>
      </template>

      <template #filters>
        <label class="filter-field">
          <span>Skill信息</span>
          <TInput v-model="filters.keyword" clearable placeholder="编码 / 名称 / 内容" />
        </label>
        <label class="filter-field">
          <span>类型</span>
          <TSelect v-model="filters.type" clearable placeholder="全部类型" :options="skillTypeOptions" />
        </label>
        <label class="filter-field">
          <span>状态</span>
          <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
        </label>
      </template>

      <template #type="{ row }">
        <TTag variant="light">{{ skillTypeLabel(row.type) }}</TTag>
      </template>
      <template #status="{ row }">
        <TTag :theme="row.status === 'active' ? 'success' : 'default'" variant="light">
          {{ row.status === 'active' ? '启用' : '停用' }}
        </TTag>
      </template>
      <template #actions="{ row }">
        <TSpace>
          <RowAction label="编辑" @click="openEdit(row)" />
          <RowAction
            :icon="row.status === 'active' ? 'lucide:ban' : 'lucide:check'"
            :label="row.status === 'active' ? '停用' : '启用'"
            :theme="row.status === 'active' ? 'danger' : 'primary'"
            @click="switchStatus(row)"
          />
        </TSpace>
      </template>
    </AdminListPage>

    <TDialog
      v-model:visible="visible"
      :header="selected ? '编辑Skill' : '新增Skill'"
      width="760px"
      :confirm-btn="{ content: '保存', loading: saving }"
      @confirm="save"
    >
      <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
        <TFormItem v-if="selected" label="编码" name="code">
          <TInput v-model="form.code" disabled />
        </TFormItem>
        <TFormItem label="名称" name="name">
          <TInput v-model="form.name" placeholder="Air.Cloud 交付规范" />
        </TFormItem>
        <TFormItem label="类型" name="type">
          <TSelect v-model="form.type" :options="skillTypeOptions" />
        </TFormItem>
        <TFormItem label="说明">
          <TInput v-model="form.description" placeholder="适用场景和约束摘要" />
        </TFormItem>
        <TFormItem v-if="selected" label="状态">
          <TSelect v-model="form.status" :options="statusOptions" />
        </TFormItem>
        <TFormItem label="内容" name="content">
          <TTextarea
            v-model="form.content"
            :autosize="{ minRows: 12, maxRows: 20 }"
            placeholder="填写 Skill 的使用规则、输入输出约束和验收要求"
          />
        </TFormItem>
      </TForm>
    </TDialog>

    <TDialog
      v-model:visible="importVisible"
      header="批量导入Skill"
      width="560px"
      :confirm-btn="{ content: '导入', loading: importing }"
      @confirm="importSkills"
    >
      <TForm :data="importForm" label-width="96px">
        <TFormItem label="类型" name="type">
          <TSelect v-model="importForm.type" :options="skillTypeOptions" />
        </TFormItem>
        <TFormItem label="文件">
          <div class="skill-import-field">
            <TButton variant="outline" @click="triggerImportFileSelect">
              <template #icon>
                <IconifyIcon icon="lucide:file-up" />
              </template>
              选择文件
            </TButton>
            <input
              ref="importInputRef"
              class="skill-import-input"
              type="file"
              multiple
              accept=".md,.markdown,text/markdown,text/plain"
              @change="handleImportFileChange"
            />
            <span v-if="importFiles.length" class="skill-import-count">
              已选择 {{ importFiles.length }} 个文件
            </span>
            <ul v-if="importFiles.length" class="skill-import-list">
              <li v-for="file in importFiles" :key="`${file.name}-${file.size}-${file.lastModified}`">
                {{ file.name }}
              </li>
            </ul>
          </div>
        </TFormItem>
      </TForm>
    </TDialog>
  </div>
</template>

<style scoped>
.filter-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}

.skill-import-field {
  display: flex;
  flex-direction: column;
  gap: 10px;
  width: 100%;
}

.skill-import-input {
  display: none;
}

.skill-import-count {
  color: var(--td-text-color-secondary);
  line-height: 20px;
}

.skill-import-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-height: 180px;
  padding: 8px 12px;
  margin: 0;
  overflow-y: auto;
  color: var(--td-text-color-primary);
  background: var(--td-bg-color-secondarycontainer);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
  list-style: none;
}

@media (max-width: 760px) {
  .filter-field {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
