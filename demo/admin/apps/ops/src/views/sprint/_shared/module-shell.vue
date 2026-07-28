<script lang="ts" setup>
import { computed } from 'vue';
import { useRouter } from 'vue-router';

import { Page } from '@vben/common-ui';

import { Button, Card, Space, Tag } from 'tdesign-vue-next';

const props = withDefaults(
  defineProps<{
    description: string;
    detail?: boolean;
    detailFields?: Array<{ label: string; value: string }>;
    listItems?: Array<{ code: string; status: string; title: string }>;
    scope: string[];
    title: string;
  }>(),
  {
    detail: false,
    detailFields: () => [],
    listItems: () => [],
  },
);

const router = useRouter();

const currentItem = computed(() => props.listItems[0]);
const resolvedDetailFields = computed(() =>
  props.detailFields.length > 0
    ? props.detailFields
    : [
        { label: '状态', value: currentItem.value?.status ?? '待规划' },
        { label: '编号', value: currentItem.value?.code ?? '-' },
        { label: '承接范围', value: props.scope[0] ?? props.description },
      ],
);

function openProjectManagement() {
  router.push('/sprint/projects');
}
</script>

<template>
  <Page :description="description" :title="title">
    <div class="sprint-shell">
      <div class="sprint-shell__toolbar">
        <Space>
          <Tag theme="primary">{{ detail ? '详情路由' : '列表菜单' }}</Tag>
          <Tag theme="success">已接入后端菜单</Tag>
        </Space>
        <Button theme="primary" @click="openProjectManagement">
          返回项目管理
        </Button>
      </div>

      <div class="sprint-shell__layout">
        <Card :bordered="false" class="sprint-shell__list" title="列表">
          <div class="sprint-shell__list-head">
            <span>名称</span>
            <span>状态</span>
          </div>
          <button
            v-for="item in listItems"
            :key="item.code"
            class="sprint-shell__row"
            type="button"
          >
            <span>
              <strong>{{ item.title }}</strong>
              <small>{{ item.code }}</small>
            </span>
            <Tag theme="default">{{ item.status }}</Tag>
          </button>
        </Card>

        <Card :bordered="false" class="sprint-shell__detail" title="详情">
          <div class="sprint-shell__detail-title">
            {{ currentItem?.title ?? title }}
          </div>
          <dl>
            <template v-for="field in resolvedDetailFields" :key="field.label">
              <dt>{{ field.label }}</dt>
              <dd>{{ field.value }}</dd>
            </template>
          </dl>
          <div class="sprint-shell__scope">
            <div class="sprint-shell__scope-title">后续实现范围</div>
            <ul>
              <li v-for="item in scope" :key="item">{{ item }}</li>
            </ul>
          </div>
        </Card>
      </div>
    </div>
  </Page>
</template>

<style scoped>
.sprint-shell {
  min-height: 100%;
  padding: 16px;
  background: #f5f7fa;
}

.sprint-shell__toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.sprint-shell__layout {
  display: grid;
  grid-template-columns: minmax(320px, 420px) minmax(0, 1fr);
  gap: 16px;
}

.sprint-shell__list,
.sprint-shell__detail {
  min-height: 560px;
}

.sprint-shell__list-head,
.sprint-shell__row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 88px;
  align-items: center;
  gap: 12px;
}

.sprint-shell__list-head {
  padding: 0 10px 10px;
  color: #6b7280;
  font-size: 13px;
}

.sprint-shell__row {
  width: 100%;
  padding: 12px 10px;
  border: 0;
  border-top: 1px solid #edf0f5;
  background: transparent;
  color: inherit;
  text-align: left;
  cursor: pointer;
}

.sprint-shell__row:hover {
  background: #f8fafc;
}

.sprint-shell__row strong,
.sprint-shell__row small {
  display: block;
}

.sprint-shell__row strong {
  overflow: hidden;
  font-size: 14px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sprint-shell__row small {
  margin-top: 4px;
  color: #6b7280;
}

.sprint-shell__detail-title {
  margin-bottom: 18px;
  color: #111827;
  font-size: 18px;
  font-weight: 600;
}

.sprint-shell__detail dl {
  display: grid;
  grid-template-columns: 96px minmax(0, 1fr);
  gap: 12px 18px;
  margin: 0 0 24px;
}

.sprint-shell__detail dt {
  color: #6b7280;
}

.sprint-shell__detail dd {
  margin: 0;
  color: #1f2937;
}

.sprint-shell__scope {
  padding-top: 18px;
  border-top: 1px solid #edf0f5;
}

.sprint-shell__scope-title {
  margin-bottom: 10px;
  font-weight: 600;
}

.sprint-shell__scope ul {
  display: grid;
  gap: 8px;
  margin: 0;
  padding-left: 18px;
  color: #4b5563;
  line-height: 1.7;
}

@media (max-width: 960px) {
  .sprint-shell__layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 720px) {
  .sprint-shell__toolbar {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
