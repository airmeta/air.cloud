<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';

import { IconifyIcon } from '@vben/icons';
import { downloadFileFromBlob } from '@vben/utils';
import { computed, onMounted, reactive, ref, watch } from 'vue';

import {
  Button as TButton,
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  Link as TLink,
  MessagePlugin,
  Pagination as TPagination,
  Select as TSelect,
  Table as TTable,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

import {
  createProjectMaterialFolderApi,
  deleteProjectMaterialApi,
  downloadProjectMaterialApi,
  listProjectMaterialsApi,
  moveProjectMaterialApi,
  updateProjectMaterialApi,
  uploadProjectMaterialApi,
} from '#/api/sprint/mvp';
import { formatDateTime } from '#/views/_shared/date-format';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';

const props = defineProps<{
  project: SprintMvpApi.Project;
  users: SprintUserApi.UserOption[];
}>();

const loading = ref(false);
const saving = ref(false);
const uploadVisible = ref(false);
const folderVisible = ref(false);
const editVisible = ref(false);
const moveVisible = ref(false);
const currentParentId = ref('');
const materials = ref<SprintMvpApi.ProjectMaterial[]>([]);
const total = ref(0);
const selectedMaterial = ref<SprintMvpApi.ProjectMaterial>();

const query = reactive({
  category: '',
  itemType: '',
  keyword: '',
  pageIndex: 1,
  pageSize: 10,
  uploadedBy: '',
});

const uploadForm = reactive({
  category: '',
  description: '',
  file: undefined as File | undefined,
  tags: '',
});

const folderForm = reactive({
  category: '',
  description: '',
  name: '',
  tags: '',
});

const editForm = reactive({
  category: '',
  description: '',
  name: '',
  tags: '',
});

const moveForm = reactive({
  parentId: '',
});

const userMap = computed(() => Object.fromEntries(props.users.map((item) => [item.id, item])));
const folderOptions = computed(() =>
  materials.value
    .filter((item) => item.itemType === 'folder')
    .map((item) => ({ label: item.name, value: item.id })),
);

const columns = [
  { colKey: 'serial-number', title: '序号', width: 70 },
  { colKey: 'name', title: '名称', minWidth: 220 },
  { colKey: 'itemType', title: '类型', width: 90 },
  { colKey: 'sizeBytes', title: '大小', width: 110 },
  { colKey: 'category', title: '分类', width: 120 },
  { colKey: 'uploadedBy', title: '上传人', width: 120 },
  { colKey: 'createTime', title: '上传时间', width: 170 },
  { colKey: 'extractStatus', title: '抽取状态', width: 110 },
  { colKey: 'actions', title: '操作', width: 300, fixed: 'right' as const },
];

function tagsToText(tags?: string[]) {
  return tags?.join(',') || '';
}

function parseTags(value: string) {
  return value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);
}

function resolveUserName(userId?: string) {
  return userId ? userMap.value[userId]?.displayName || userId : '未指定';
}

function formatFileSize(size: number) {
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / 1024 / 1024).toFixed(1)} MB`;
}

function materialTypeLabel(item: SprintMvpApi.ProjectMaterial) {
  return item.itemType === 'folder' ? '文件夹' : item.extension || '文件';
}

function extractStatusTheme(status: string) {
  if (status === 'completed') return 'success';
  if (status === 'failed') return 'danger';
  if (status === 'pending') return 'warning';
  return 'default';
}

async function loadMaterials() {
  loading.value = true;
  try {
    const result = await listProjectMaterialsApi(props.project.id, {
      category: query.category || undefined,
      itemType: query.itemType || undefined,
      keyword: query.keyword || undefined,
      pageIndex: query.pageIndex,
      pageSize: query.pageSize,
      parentId: currentParentId.value || undefined,
      uploadedBy: query.uploadedBy || undefined,
    });
    materials.value = result.items;
    total.value = result.total;
  } finally {
    loading.value = false;
  }
}

function resetQuery() {
  Object.assign(query, {
    category: '',
    itemType: '',
    keyword: '',
    pageIndex: 1,
    pageSize: 10,
    uploadedBy: '',
  });
  loadMaterials();
}

function openUpload() {
  Object.assign(uploadForm, {
    category: '',
    description: '',
    file: undefined,
    tags: '',
  });
  uploadVisible.value = true;
}

function openFolderCreate() {
  Object.assign(folderForm, {
    category: '',
    description: '',
    name: '',
    tags: '',
  });
  folderVisible.value = true;
}

function openEdit(row: SprintMvpApi.ProjectMaterial) {
  selectedMaterial.value = row;
  Object.assign(editForm, {
    category: row.category || '',
    description: row.description || '',
    name: row.name,
    tags: tagsToText(row.tags),
  });
  editVisible.value = true;
}

function openMove(row: SprintMvpApi.ProjectMaterial) {
  selectedMaterial.value = row;
  moveForm.parentId = row.parentId || '';
  moveVisible.value = true;
}

function openFolder(row: SprintMvpApi.ProjectMaterial) {
  currentParentId.value = row.id;
  query.pageIndex = 1;
  loadMaterials();
}

function goRoot() {
  currentParentId.value = '';
  query.pageIndex = 1;
  loadMaterials();
}

async function saveFolder() {
  if (!folderForm.name.trim()) {
    MessagePlugin.warning('请输入文件夹名称');
    return;
  }

  saving.value = true;
  try {
    await createProjectMaterialFolderApi(props.project.id, {
      category: folderForm.category || undefined,
      description: folderForm.description || undefined,
      name: folderForm.name.trim(),
      parentId: currentParentId.value || undefined,
      tags: parseTags(folderForm.tags),
    });
    folderVisible.value = false;
    MessagePlugin.success('文件夹已创建');
    await loadMaterials();
  } finally {
    saving.value = false;
  }
}

async function uploadMaterial() {
  if (!uploadForm.file) {
    MessagePlugin.warning('请选择上传文件');
    return;
  }

  const formData = new FormData();
  formData.append('file', uploadForm.file);
  if (currentParentId.value) formData.append('parentId', currentParentId.value);
  if (uploadForm.category) formData.append('category', uploadForm.category);
  if (uploadForm.description) formData.append('description', uploadForm.description);
  if (uploadForm.tags) formData.append('tags', uploadForm.tags);

  saving.value = true;
  try {
    await uploadProjectMaterialApi(props.project.id, formData);
    uploadVisible.value = false;
    MessagePlugin.success('材料已上传');
    await loadMaterials();
  } finally {
    saving.value = false;
  }
}

async function saveEdit() {
  if (!selectedMaterial.value || !editForm.name.trim()) {
    MessagePlugin.warning('请输入名称');
    return;
  }

  saving.value = true;
  try {
    await updateProjectMaterialApi(selectedMaterial.value.id, {
      category: editForm.category || undefined,
      description: editForm.description || undefined,
      name: editForm.name.trim(),
      tags: parseTags(editForm.tags),
    });
    editVisible.value = false;
    MessagePlugin.success('材料已保存');
    await loadMaterials();
  } finally {
    saving.value = false;
  }
}

async function saveMove() {
  if (!selectedMaterial.value) return;
  saving.value = true;
  try {
    await moveProjectMaterialApi(selectedMaterial.value.id, {
      parentId: moveForm.parentId || undefined,
    });
    moveVisible.value = false;
    MessagePlugin.success('材料已移动');
    await loadMaterials();
  } finally {
    saving.value = false;
  }
}

function deleteMaterial(row: SprintMvpApi.ProjectMaterial) {
  confirmAndClose({
    body:
      row.itemType === 'folder'
        ? `确认删除文件夹 ${row.name}？仅空文件夹允许删除。`
        : `确认删除材料 ${row.name}？删除后不会再出现在项目材料列表。`,
    confirmBtn: '删除',
    header: '删除项目材料',
    onConfirm: async () => {
      await deleteProjectMaterialApi(row.id);
      MessagePlugin.success('材料已删除');
      await loadMaterials();
    },
  });
}

async function downloadMaterial(row: SprintMvpApi.ProjectMaterial) {
  const blob = await downloadProjectMaterialApi(row.id);
  downloadFileFromBlob({
    fileName: row.originalFileName || row.name,
    source: blob,
  });
}

function handleFileChange(event: Event) {
  const target = event.target as HTMLInputElement;
  uploadForm.file = target.files?.[0];
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  query.pageIndex = pageInfo.current;
  query.pageSize = pageInfo.pageSize;
  loadMaterials();
}

watch(
  () => props.project.id,
  () => {
    currentParentId.value = '';
    query.pageIndex = 1;
    loadMaterials();
  },
);

onMounted(loadMaterials);
</script>

<template>
  <div class="materials-panel">
    <div class="materials-header">
      <div>
        <h3>项目材料</h3>
        <p>按项目归档需求调研、会议纪要、验收材料和方案文件。</p>
      </div>
      <div class="materials-actions">
        <TButton theme="primary" @click="openUpload">
          <template #icon><IconifyIcon icon="lucide:upload" /></template>
          上传
        </TButton>
        <TButton @click="openFolderCreate">
          <template #icon><IconifyIcon icon="lucide:folder-plus" /></template>
          新建文件夹
        </TButton>
        <TButton :disabled="loading" @click="loadMaterials">
          <template #icon><IconifyIcon icon="lucide:refresh-cw" /></template>
          刷新
        </TButton>
      </div>
    </div>

    <div class="materials-filter">
      <TInput v-model="query.keyword" clearable placeholder="搜索名称或说明" />
      <TSelect
        v-model="query.itemType"
        clearable
        :options="[
          { label: '文件', value: 'file' },
          { label: '文件夹', value: 'folder' },
        ]"
        placeholder="类型"
      />
      <TInput v-model="query.category" clearable placeholder="分类" />
      <TInput v-model="query.uploadedBy" clearable placeholder="上传人ID" />
      <TButton theme="primary" @click="loadMaterials">
        <template #icon><IconifyIcon icon="lucide:search" /></template>
        查询
      </TButton>
      <TButton @click="resetQuery">
        <template #icon><IconifyIcon icon="lucide:rotate-ccw" /></template>
        重置
      </TButton>
    </div>

    <div class="breadcrumb">
      <TLink theme="primary" @click="goRoot">
        <IconifyIcon icon="lucide:folder" />
        根目录
      </TLink>
      <span v-if="currentParentId">/ 当前目录</span>
    </div>

    <TTable
      row-key="id"
      :columns="columns"
      :data="materials"
      :loading="loading"
      hover
      stripe
    >
      <template #name="{ row }">
        <span class="material-name">
          <IconifyIcon :icon="row.itemType === 'folder' ? 'lucide:folder' : 'lucide:file'" />
          {{ row.name }}
        </span>
      </template>
      <template #itemType="{ row }">
        {{ materialTypeLabel(row) }}
      </template>
      <template #sizeBytes="{ row }">
        {{ row.itemType === 'folder' ? '-' : formatFileSize(row.sizeBytes) }}
      </template>
      <template #category="{ row }">
        {{ row.category || '-' }}
      </template>
      <template #uploadedBy="{ row }">
        {{ resolveUserName(row.uploadedBy) }}
      </template>
      <template #createTime="{ row }">
        {{ formatDateTime(row.createTime) }}
      </template>
      <template #extractStatus="{ row }">
        <TTag :theme="extractStatusTheme(row.extractStatus)" variant="light">
          {{ row.extractStatus }}
        </TTag>
      </template>
      <template #actions="{ row }">
        <div class="row-actions">
          <TLink v-if="row.itemType === 'folder'" theme="primary" @click="openFolder(row)">
            <IconifyIcon icon="lucide:folder-open" />
            打开
          </TLink>
          <TLink v-else theme="primary" @click="downloadMaterial(row)">
            <IconifyIcon icon="lucide:download" />
            下载
          </TLink>
          <TLink theme="primary" @click="openEdit(row)">
            <IconifyIcon icon="lucide:pencil" />
            重命名
          </TLink>
          <TLink theme="primary" @click="openMove(row)">
            <IconifyIcon icon="lucide:move-right" />
            移动
          </TLink>
          <TLink theme="danger" @click="deleteMaterial(row)">
            <IconifyIcon icon="lucide:trash-2" />
            删除
          </TLink>
        </div>
      </template>
    </TTable>

    <div class="pagination">
      <span>共计 {{ total }} 条数据</span>
      <TPagination
        :current="query.pageIndex"
        :page-size="query.pageSize"
        :total="total"
        @change="handlePageChange"
      />
    </div>

    <TDialog
      v-model:visible="uploadVisible"
      header="上传项目材料"
      width="560px"
      :confirm-btn="{ content: '上传', loading: saving }"
      @confirm="uploadMaterial"
    >
      <TForm label-width="90px">
        <TFormItem label="文件">
          <input type="file" @change="handleFileChange" />
        </TFormItem>
        <TFormItem label="分类">
          <TInput v-model="uploadForm.category" />
        </TFormItem>
        <TFormItem label="标签">
          <TInput v-model="uploadForm.tags" placeholder="多个标签用逗号分隔" />
        </TFormItem>
        <TFormItem label="说明">
          <TTextarea v-model="uploadForm.description" :autosize="{ minRows: 3, maxRows: 5 }" />
        </TFormItem>
      </TForm>
    </TDialog>

    <TDialog
      v-model:visible="folderVisible"
      header="新建文件夹"
      width="520px"
      :confirm-btn="{ content: '保存', loading: saving }"
      @confirm="saveFolder"
    >
      <TForm label-width="90px">
        <TFormItem label="名称">
          <TInput v-model="folderForm.name" />
        </TFormItem>
        <TFormItem label="分类">
          <TInput v-model="folderForm.category" />
        </TFormItem>
        <TFormItem label="标签">
          <TInput v-model="folderForm.tags" placeholder="多个标签用逗号分隔" />
        </TFormItem>
        <TFormItem label="说明">
          <TTextarea v-model="folderForm.description" :autosize="{ minRows: 3, maxRows: 5 }" />
        </TFormItem>
      </TForm>
    </TDialog>

    <TDialog
      v-model:visible="editVisible"
      header="材料信息"
      width="520px"
      :confirm-btn="{ content: '保存', loading: saving }"
      @confirm="saveEdit"
    >
      <TForm label-width="90px">
        <TFormItem label="名称">
          <TInput v-model="editForm.name" />
        </TFormItem>
        <TFormItem label="分类">
          <TInput v-model="editForm.category" />
        </TFormItem>
        <TFormItem label="标签">
          <TInput v-model="editForm.tags" placeholder="多个标签用逗号分隔" />
        </TFormItem>
        <TFormItem label="说明">
          <TTextarea v-model="editForm.description" :autosize="{ minRows: 3, maxRows: 5 }" />
        </TFormItem>
      </TForm>
    </TDialog>

    <TDialog
      v-model:visible="moveVisible"
      header="移动材料"
      width="520px"
      :confirm-btn="{ content: '保存', loading: saving }"
      @confirm="saveMove"
    >
      <TForm label-width="90px">
        <TFormItem label="目标目录">
          <TSelect
            v-model="moveForm.parentId"
            clearable
            :options="folderOptions"
            placeholder="根目录"
          />
        </TFormItem>
      </TForm>
    </TDialog>
  </div>
</template>

<style scoped>
.materials-panel {
  display: grid;
  gap: 14px;
}

.materials-header,
.materials-filter,
.pagination,
.row-actions,
.material-name,
.breadcrumb {
  display: flex;
  align-items: center;
}

.materials-header {
  justify-content: space-between;
  gap: 16px;
}

.materials-header h3,
.materials-header p {
  margin: 0;
}

.materials-header p {
  margin-top: 4px;
  color: var(--td-text-color-secondary);
}

.materials-actions,
.materials-filter {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.materials-filter :deep(.t-input),
.materials-filter :deep(.t-select) {
  width: 180px;
}

.breadcrumb {
  gap: 8px;
  min-height: 28px;
  color: var(--td-text-color-secondary);
}

.material-name,
.row-actions {
  gap: 8px;
}

.pagination {
  justify-content: space-between;
  gap: 16px;
}
</style>
