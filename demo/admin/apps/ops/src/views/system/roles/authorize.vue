<script lang="ts" setup>
import { IconifyIcon } from '@vben/icons';
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import {
  listSystemMenusApi,
  listSystemPermissionsApi,
  listSystemRolesApi,
  saveSystemRoleApi,
  type SystemApi,
} from '#/api';
import {
  Button as TButton,
  Checkbox as TCheckbox,
  Descriptions as TDescriptions,
  DescriptionsItem as TDescriptionsItem,
  Empty as TEmpty,
  Loading as TLoading,
  MessagePlugin,
  Space as TSpace,
  TabPanel as TTabPanel,
  Tabs as TTabs,
  Tag as TTag,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemRoleAuthorize' });

const route = useRoute();
const router = useRouter();
const loading = ref(false);
const saving = ref(false);
const savingRules = ref(false);
const activeTab = ref('authorization');
const authTreeRef = ref<HTMLElement>();
const authTreeMaxHeight = ref(260);
const role = ref<SystemApi.Role>();
const menus = ref<SystemApi.Menu[]>([]);
const permissions = ref<SystemApi.Permission[]>([]);
const form = reactive<Partial<SystemApi.Role>>({
  code: '',
  description: '',
  id: undefined,
  menuIds: [],
  name: '',
  permissionIds: [],
  status: 1,
});

const roleId = computed(() => String(route.params.id || ''));
const statusText = computed(() => (role.value?.status === 1 ? '启用' : '停用'));
const grantStats = computed(() => ({
  behaviorRuleCount: 0,
  menuCount: form.menuIds?.length || 0,
  permissionCount: form.permissionIds?.length || 0,
}));
type MenuAuthNode = SystemApi.Menu & {
  children: MenuAuthNode[];
  level: number;
  permissions: SystemApi.Permission[];
};

const menuAuthTree = computed(() => buildMenuAuthTree(menus.value, permissions.value));
const menuAuthRows = computed(() => flattenMenuAuthTree(menuAuthTree.value));

function asArray<T>(value: T[] | null | undefined) {
  return Array.isArray(value) ? value : [];
}

function assignForm(value?: SystemApi.Role) {
  Object.assign(form, {
    code: value?.code || '',
    description: value?.description || '',
    id: value?.id,
    menuIds: [...asArray(value?.menuIds)],
    name: value?.name || '',
    permissionIds: [...asArray(value?.permissionIds)],
    status: value?.status ?? 1,
  });
}

function buildMenuAuthTree(sourceMenus: SystemApi.Menu[], sourcePermissions: SystemApi.Permission[]) {
  const permissionMap = new Map<string, SystemApi.Permission[]>();
  sourcePermissions.forEach((permission) => {
    if (!permission.menuId) return;
    const items = permissionMap.get(permission.menuId) || [];
    items.push(permission);
    permissionMap.set(permission.menuId, items);
  });

  const nodeMap = new Map<string, MenuAuthNode>();
  sourceMenus.forEach((menu) => {
    nodeMap.set(menu.id, {
      ...menu,
      children: [],
      level: 0,
      permissions: (permissionMap.get(menu.id) || []).sort((left, right) => left.code.localeCompare(right.code)),
    });
  });

  const roots: MenuAuthNode[] = [];
  nodeMap.forEach((node) => {
    if (node.parentId && nodeMap.has(node.parentId)) {
      nodeMap.get(node.parentId)?.children.push(node);
    } else {
      roots.push(node);
    }
  });

  function sortAndMarkLevel(nodes: MenuAuthNode[], level: number) {
    nodes.sort((left, right) => left.sort - right.sort || left.name.localeCompare(right.name));
    nodes.forEach((node) => {
      node.level = level;
      sortAndMarkLevel(node.children, level + 1);
    });
  }
  sortAndMarkLevel(roots, 0);
  return roots;
}

function flattenMenuAuthTree(nodes: MenuAuthNode[]) {
  const rows: MenuAuthNode[] = [];
  function walk(items: MenuAuthNode[]) {
    items.forEach((item) => {
      rows.push(item);
      walk(item.children);
    });
  }
  walk(nodes);
  return rows;
}

function isMenuChecked(menuId: string) {
  return asArray(form.menuIds).includes(menuId);
}

function isPermissionChecked(permissionId: string) {
  return asArray(form.permissionIds).includes(permissionId);
}

function setCheckedValue(currentIds: string[] | undefined, id: string, checked: boolean) {
  const nextIds = new Set(asArray(currentIds));
  if (checked) {
    nextIds.add(id);
  } else {
    nextIds.delete(id);
  }
  return [...nextIds];
}

function readChecked(value: boolean | { checked?: boolean }) {
  return typeof value === 'boolean' ? value : value.checked === true;
}

function toggleMenu(menuId: string, value: boolean | { checked?: boolean }) {
  form.menuIds = setCheckedValue(form.menuIds, menuId, readChecked(value));
}

function togglePermission(permissionId: string, value: boolean | { checked?: boolean }) {
  form.permissionIds = setCheckedValue(form.permissionIds, permissionId, readChecked(value));
}

async function loadDetail() {
  loading.value = true;
  try {
    const [roles, menuRows, permissionRows] = await Promise.all([
      listSystemRolesApi(),
      listSystemMenusApi(),
      listSystemPermissionsApi(),
    ]);
    role.value = roles.find((item) => item.id === roleId.value);
    assignForm(role.value);
    menus.value = menuRows;
    permissions.value = permissionRows;
  } finally {
    loading.value = false;
    updateAuthTreeHeight();
  }
}

async function saveAuthorize() {
  if (saving.value || !role.value) return;
  saving.value = true;
  try {
    const saved = await saveSystemRoleApi(form);
    role.value = saved;
    assignForm(saved);
    MessagePlugin.success('角色授权已保存');
  } finally {
    saving.value = false;
  }
}

async function saveBehaviorRules() {
  if (savingRules.value || !role.value) return;
  savingRules.value = true;
  try {
    MessagePlugin.success('行为规则已保存');
  } finally {
    savingRules.value = false;
  }
}

function updateAuthTreeHeight() {
  void nextTick(() => {
    const tree = authTreeRef.value;
    if (!tree) return;
    const rect = tree.getBoundingClientRect();
    authTreeMaxHeight.value = Math.max(180, window.innerHeight - rect.top - 24);
  });
}

onMounted(() => {
  void loadDetail();
  updateAuthTreeHeight();
  window.addEventListener('resize', updateAuthTreeHeight);
});

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateAuthTreeHeight);
});
</script>

<template>
  <div class="role-authorize-page">
    <section class="page-header">
      <div>
        <h2>{{ role?.name || '角色授权' }}</h2>
        <p>{{ role?.code || '加载角色信息后配置菜单与按钮权限' }}</p>
      </div>
      <TSpace>
        <TButton variant="outline" @click="router.push('/system/roles')">
          <template #icon>
            <IconifyIcon icon="lucide:arrow-left" />
          </template>
          返回
        </TButton>
        <TButton @click="loadDetail">
          <template #icon>
            <IconifyIcon icon="lucide:refresh-cw" />
          </template>
          刷新
        </TButton>
      </TSpace>
    </section>

    <TLoading :loading="loading" show-overlay>
      <TEmpty v-if="!loading && !role" description="角色不存在或已被删除" />

      <template v-else-if="role">
        <section class="panel">
          <h3>角色信息</h3>
          <TDescriptions bordered :column="2">
            <TDescriptionsItem label="角色编码">{{ role.code }}</TDescriptionsItem>
            <TDescriptionsItem label="角色名称">{{ role.name }}</TDescriptionsItem>
            <TDescriptionsItem label="状态">
              <TTag :theme="role.status === 1 ? 'success' : 'default'" variant="light">{{ statusText }}</TTag>
            </TDescriptionsItem>
            <TDescriptionsItem label="说明">{{ role.description || '未填写' }}</TDescriptionsItem>
          </TDescriptions>
        </section>

        <section class="stats">
          <div>
            <span>菜单授权</span>
            <strong>{{ grantStats.menuCount }}</strong>
          </div>
          <div>
            <span>按钮权限</span>
            <strong>{{ grantStats.permissionCount }}</strong>
          </div>
          <div>
            <span>行为规则</span>
            <strong>{{ grantStats.behaviorRuleCount }}</strong>
          </div>
        </section>

        <section class="panel tabs-panel">
          <div class="panel-header">
            <h3>授权范围</h3>
            <TButton
              v-if="activeTab === 'authorization'"
              theme="primary"
              :loading="saving"
              @click="saveAuthorize"
            >
              <template #icon>
                <IconifyIcon icon="lucide:save" />
              </template>
              保存授权
            </TButton>
            <TButton
              v-else-if="activeTab === 'behaviorRules'"
              theme="primary"
              :loading="savingRules"
              @click="saveBehaviorRules"
            >
              <template #icon>
                <IconifyIcon icon="lucide:save" />
              </template>
              保存规则
            </TButton>
          </div>
          <TTabs v-model="activeTab" :destroy-on-hide="false">
            <TTabPanel value="authorization" label="授权信息">
              <div class="tab-content">
                <div ref="authTreeRef" class="auth-tree" :style="{ maxHeight: `${authTreeMaxHeight}px` }">
                  <div v-for="menu in menuAuthRows" :key="menu.id" class="auth-menu-row">
                    <div class="auth-menu-main" :style="{ paddingLeft: `${menu.level * 22}px` }">
                      <TCheckbox :checked="isMenuChecked(menu.id)" @change="toggleMenu(menu.id, $event)" />
                      <div class="auth-menu-text">
                        <strong>{{ menu.name }}</strong>
                        <span>{{ menu.path }}</span>
                      </div>
                    </div>
                    <div class="auth-permission-line">
                      <TCheckbox
                        v-for="permission in menu.permissions"
                        :key="permission.id"
                        :checked="isPermissionChecked(permission.id)"
                        @change="togglePermission(permission.id, $event)"
                      >
                        {{ permission.name }}
                      </TCheckbox>
                      <span v-if="menu.permissions.length === 0" class="auth-permission-empty">暂无按钮权限</span>
                    </div>
                  </div>
                  <TEmpty v-if="menuAuthRows.length === 0" description="暂无菜单数据" />
                </div>
              </div>
            </TTabPanel>
            <TTabPanel value="behaviorRules" label="行为规则">
              <div class="tab-content">
                <TEmpty description="行为规则暂未配置" />
              </div>
            </TTabPanel>
          </TTabs>
        </section>
      </template>
    </TLoading>
  </div>
</template>

<style scoped>
.role-authorize-page {
  padding: 16px;
}

.page-header,
.panel,
.stats {
  margin-bottom: 16px;
  padding: 16px 20px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.page-header,
.panel-header {
  display: flex;
  gap: 16px;
  align-items: center;
  flex-wrap: wrap;
  justify-content: space-between;
}

.page-header h2,
.panel h3 {
  margin: 0;
}

.page-header p {
  margin: 6px 0 0;
  color: var(--td-text-color-secondary);
}

.panel h3 {
  margin-bottom: 12px;
}

.panel-header h3 {
  margin-bottom: 0;
}

.tab-content {
  padding-top: 16px;
}

.tabs-panel :deep(.t-tab-panel) {
  height: 100%;
}

.tabs-panel {
  position: relative;
}

.auth-tree {
  display: grid;
  gap: 8px;
  min-height: 180px;
  overflow-y: auto;
  padding-right: 6px;
}

.auth-menu-row {
  display: grid;
  grid-template-columns: minmax(240px, 320px) minmax(0, 1fr);
  gap: 16px;
  align-items: center;
  min-height: 52px;
  padding: 8px 12px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.auth-menu-main {
  display: flex;
  gap: 8px;
  align-items: center;
}

.auth-menu-text {
  display: grid;
  min-width: 0;
  gap: 2px;
}

.auth-menu-text strong,
.auth-menu-text span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.auth-menu-text span,
.auth-permission-empty {
  color: var(--td-text-color-secondary);
}

.auth-permission-line {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 16px;
  align-items: center;
}

.stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
}

.stats span {
  display: block;
  color: var(--td-text-color-secondary);
}

.stats strong {
  display: block;
  margin-top: 6px;
  font-size: 24px;
}

@media (max-width: 760px) {
  .auth-menu-row {
    grid-template-columns: 1fr;
  }

  .stats {
    grid-template-columns: 1fr;
  }
}
</style>
