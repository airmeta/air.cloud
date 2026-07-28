<script lang="ts" setup>
import type { EchartsUIType } from '@vben/plugins/echarts';

import { computed, onMounted, ref, watch } from 'vue';

import { IconifyIcon } from '@vben/icons';
import { useEcharts, EchartsUI } from '@vben/plugins/echarts';
import { useUserStore } from '@vben/stores';
import { useTransition } from '@vueuse/core';
import dayjs from 'dayjs';
import {
  Avatar as TAvatar,
  Card as TCard,
  Col as TCol,
  Link as TLink,
  RadioButton as TRadioButton,
  RadioGroup as TRadioGroup,
  Row as TRow,
  Space as TSpace,
  Tag as TTag,
} from 'tdesign-vue-next';

import { LogApi } from '#/api/open/log';

defineOptions({ name: 'Dashboard' });

interface VersionItem {
  content: string;
  date: string;
  id: string;
  link: string;
  tag?: string;
  title: string;
}

const userStore = useUserStore();
const chartRef = ref<EchartsUIType>();
const { renderEcharts } = useEcharts(chartRef);
const visitTrendDateRange = ref<7 | 30>(7);
const visitStatsLoading = ref(true);
const visitStats = ref<LogApi.VisitStatsVO>({
  pvGrowthRate: 0,
  todayPvCount: 0,
  todayUvCount: 0,
  totalPvCount: 0,
  totalUvCount: 0,
  uvGrowthRate: 0,
});

const displayName = computed(
  () =>
    userStore.userInfo?.realName || userStore.userInfo?.username || '系统用户',
);
const greetings = computed(() => {
  const hour = new Date().getHours();
  if (hour < 8) return '早上好，';
  if (hour < 12) return '上午好，';
  if (hour < 18) return '下午好，';
  return '晚上好，';
});
const transitionUvCount = useTransition(
  computed(() => visitStats.value.todayUvCount),
  { duration: 600 },
);
const transitionTotalUvCount = useTransition(
  computed(() => visitStats.value.totalUvCount),
  { duration: 700 },
);
const transitionPvCount = useTransition(
  computed(() => visitStats.value.todayPvCount),
  { duration: 600 },
);
const transitionTotalPvCount = useTransition(
  computed(() => visitStats.value.totalPvCount),
  { duration: 700 },
);

const versionList: VersionItem[] = [
  {
    content: '开放平台工作台已接入访问统计与接口映射视图。',
    date: '2025-06-06 00:00:00',
    id: '1',
    link: 'https://gitee.com/youlaiorg/vue3-element-admin/releases',
    tag: '里程碑',
    title: 'v3.0.0',
  },
  {
    content: '完成平台基础能力、权限和路由体系建设。',
    date: '2021-09-01 00:00:00',
    id: '2',
    link: 'https://gitee.com/youlaiorg/vue3-element-admin/releases',
    tag: '里程碑',
    title: 'v2.4.0',
  },
];

function formatGrowthRate(rate: number) {
  const prefix = rate > 0 ? '+' : '';
  return `${prefix}${Number(rate || 0).toFixed(2)}%`;
}

function growthTheme(rate: number) {
  if (rate > 0) return 'up';
  if (rate < 0) return 'down';
  return 'flat';
}

async function fetchVisitStats() {
  visitStatsLoading.value = true;
  try {
    visitStats.value = await LogApi.getVisitStats();
  } finally {
    visitStatsLoading.value = false;
  }
}

async function fetchVisitTrend() {
  const endDate = dayjs();
  const startDate = endDate.subtract(visitTrendDateRange.value - 1, 'day');
  const data = await LogApi.getVisitTrend({
    endDate: endDate.format('YYYY-MM-DD'),
    startDate: startDate.format('YYYY-MM-DD'),
  });

  renderEcharts({
    grid: { bottom: 40, containLabel: true, left: '1%', right: '4%', top: 24 },
    legend: { bottom: 4, data: ['浏览量（PV）', '访客数（UV）'] },
    series: [
      {
        areaStyle: { color: 'rgba(0, 82, 217, 0.1)' },
        data: data.pvList,
        itemStyle: { color: '#0052d9' },
        name: '浏览量（PV）',
        smooth: true,
        type: 'line',
      },
      {
        areaStyle: { color: 'rgba(0, 168, 112, 0.1)' },
        data: data.ipList,
        itemStyle: { color: '#00a870' },
        name: '访客数（UV）',
        smooth: true,
        type: 'line',
      },
    ],
    tooltip: { trigger: 'axis' },
    xAxis: { boundaryGap: false, data: data.dates, type: 'category' },
    yAxis: { splitLine: { lineStyle: { type: 'dashed' } }, type: 'value' },
  });
}

watch(visitTrendDateRange, () => void fetchVisitTrend());

onMounted(() => {
  void fetchVisitStats();
  void fetchVisitTrend();
});
</script>

<template>
  <div class="dashboard-container">
    <TCard class="welcome-card" bordered>
      <div class="welcome-content">
        <div class="profile-block">
          <TAvatar
            :image="userStore.userInfo?.avatar"
            class="profile-avatar"
            shape="circle"
            size="80px"
          />
          <div>
            <p class="greeting">{{ greetings }}{{ displayName }}</p>
            <p class="weather">
              今日天气晴朗，气温在 15C 至 25C 之间，东南风。
            </p>
          </div>
        </div>
        <div class="resource-groups">
          <div class="resource-group">
            <div class="resource-title repository">
              <IconifyIcon icon="lucide:folder" /> 仓库
            </div>
            <TSpace separator="|" size="small">
              <TLink
                href="https://gitee.com/youlaiorg/vue3-element-admin"
                target="_blank"
                >Gitee</TLink
              >
              <TLink
                href="https://github.com/youlaitech/vue3-element-admin"
                target="_blank"
                >GitHub</TLink
              >
              <TLink
                href="https://gitcode.com/youlai/vue3-element-admin"
                target="_blank"
                >GitCode</TLink
              >
            </TSpace>
          </div>
          <div class="resource-group">
            <div class="resource-title document">
              <IconifyIcon icon="lucide:file-text" /> 文档
            </div>
            <TSpace separator="|" size="small">
              <TLink
                href="https://juejin.cn/post/7228990409909108793"
                target="_blank"
                >掘金</TLink
              >
              <TLink
                href="https://youlai.blog.csdn.net/article/details/130191394"
                target="_blank"
                >CSDN</TLink
              >
              <TLink
                href="https://www.cnblogs.com/haoxianrui/p/17331952.html"
                target="_blank"
                >博客园</TLink
              >
            </TSpace>
          </div>
          <div class="resource-group video-group">
            <div class="resource-title video">
              <IconifyIcon icon="lucide:video" /> 视频
            </div>
            <TLink
              href="https://www.bilibili.com/video/BV1eFUuYyEFj"
              target="_blank"
              >Bilibili</TLink
            >
          </div>
        </div>
      </div>
    </TCard>

    <TRow :gutter="[12, 12]" class="metric-row">
      <TCol :lg="8" :md="8" :xs="24">
        <TCard class="metric-card" bordered>
          <div class="metric-header">
            <span>在线用户</span
            ><TTag size="small" theme="danger" variant="light">实时</TTag>
          </div>
          <div class="metric-main">
            <span class="metric-number">0</span
            ><span class="metric-status"
              ><IconifyIcon icon="lucide:circle-off" /> 未连接</span
            ><IconifyIcon class="metric-icon" icon="lucide:users" />
          </div>
          <div class="metric-footer">
            <span>更新时间</span><span>2025-01-01</span>
          </div>
        </TCard>
      </TCol>
      <TCol :lg="8" :md="8" :xs="24">
        <TCard class="metric-card" bordered :loading="visitStatsLoading">
          <div class="metric-header">
            <span>访客数（UV）</span
            ><TTag size="small" theme="success" variant="light">日</TTag>
          </div>
          <div class="metric-main">
            <span class="metric-number">{{
              Math.round(transitionUvCount)
            }}</span
            ><span class="growth" :class="growthTheme(visitStats.uvGrowthRate)"
              ><IconifyIcon
                :icon="
                  visitStats.uvGrowthRate >= 0
                    ? 'lucide:trending-up'
                    : 'lucide:trending-down'
                "
              />
              {{ formatGrowthRate(visitStats.uvGrowthRate) }}</span
            ><IconifyIcon class="metric-icon" icon="lucide:user-round-check" />
          </div>
          <div class="metric-footer">
            <span>总访客数</span
            ><span>{{ Math.round(transitionTotalUvCount) }}</span>
          </div>
        </TCard>
      </TCol>
      <TCol :lg="8" :md="8" :xs="24">
        <TCard class="metric-card" bordered :loading="visitStatsLoading">
          <div class="metric-header">
            <span>浏览量（PV）</span
            ><TTag size="small" theme="primary" variant="light">日</TTag>
          </div>
          <div class="metric-main">
            <span class="metric-number">{{
              Math.round(transitionPvCount)
            }}</span
            ><span class="growth" :class="growthTheme(visitStats.pvGrowthRate)"
              ><IconifyIcon
                :icon="
                  visitStats.pvGrowthRate >= 0
                    ? 'lucide:trending-up'
                    : 'lucide:trending-down'
                "
              />
              {{ formatGrowthRate(visitStats.pvGrowthRate) }}</span
            ><IconifyIcon class="metric-icon" icon="lucide:monitor-up" />
          </div>
          <div class="metric-footer">
            <span>总浏览量</span
            ><span>{{ Math.round(transitionTotalPvCount) }}</span>
          </div>
        </TCard>
      </TCol>
    </TRow>

    <TRow :gutter="[12, 12]" class="content-row">
      <TCol :lg="16" :md="16" :xs="24">
        <TCard class="trend-card" bordered>
          <template #header
            ><div class="card-header">
              <span>访问趋势</span
              ><TRadioGroup
                v-model="visitTrendDateRange"
                size="small"
                variant="default-filled"
                ><TRadioButton :value="7">近 7 天</TRadioButton
                ><TRadioButton :value="30">近 30 天</TRadioButton></TRadioGroup
              >
            </div></template
          >
          <EchartsUI ref="chartRef" class="trend-chart" />
        </TCard>
      </TCol>
      <TCol :lg="8" :md="8" :xs="24">
        <TCard class="activity-card" bordered>
          <template #header
            ><div class="card-header">
              <span>最新动态</span
              ><TLink
                href="https://gitee.com/youlaiorg/vue3-element-admin/releases"
                target="_blank"
                >完整记录 <IconifyIcon icon="lucide:arrow-up-right"
              /></TLink></div
          ></template>
          <div class="version-list">
            <article
              v-for="(item, index) in versionList"
              :key="item.id"
              class="version-item"
              :class="{ latest: index === 0 }"
            >
              <div class="version-line">
                <strong>{{ item.title }}</strong
                ><TTag
                  v-if="item.tag"
                  size="small"
                  :theme="index === 0 ? 'success' : 'default'"
                  variant="light"
                  >{{ item.tag }}</TTag
                >
              </div>
              <p>{{ item.content }}</p>
              <time>{{ item.date }}</time
              ><TLink :href="item.link" target="_blank"
                >详情 <IconifyIcon icon="lucide:arrow-up-right"
              /></TLink>
            </article>
          </div>
        </TCard>
      </TCol>
    </TRow>
  </div>
</template>

<style scoped>
.dashboard-container {
  display: flex;
  flex-direction: column;
  gap: 20px;
  padding: 24px;
}
.welcome-content,
.profile-block,
.resource-groups,
.metric-main,
.metric-header,
.metric-footer,
.card-header,
.version-line {
  display: flex;
  align-items: center;
}
.welcome-content {
  justify-content: space-between;
  gap: 24px;
}
.profile-block {
  gap: 20px;
  min-width: 0;
}
.profile-avatar {
  flex: 0 0 auto;
}
.greeting {
  margin: 0;
  font-size: 16px;
  color: var(--td-text-color-primary);
}
.weather {
  margin: 10px 0 0;
  color: var(--td-text-color-secondary);
  font-size: 13px;
}
.resource-groups {
  gap: 30px;
}
.resource-group {
  min-width: 120px;
}
.resource-title {
  display: flex;
  gap: 6px;
  align-items: center;
  margin-bottom: 12px;
  font-size: 13px;
  font-weight: 600;
}
.repository {
  color: #d97706;
}
.document {
  color: var(--td-brand-color);
}
.video {
  color: #d54941;
}
.metric-card {
  min-height: 158px;
}
.metric-header,
.metric-footer,
.card-header {
  justify-content: space-between;
}
.metric-header {
  color: var(--td-text-color-secondary);
}
.metric-main {
  gap: 10px;
  height: 76px;
}
.metric-number {
  font-size: 24px;
  color: var(--td-text-color-primary);
}
.metric-status,
.growth {
  display: inline-flex;
  gap: 4px;
  align-items: center;
  font-size: 12px;
}
.metric-status {
  color: var(--td-error-color);
}
.growth.up {
  color: var(--td-error-color);
}
.growth.down {
  color: var(--td-success-color);
}
.growth.flat {
  color: var(--td-text-color-secondary);
}
.metric-icon {
  margin-left: auto;
  font-size: 32px;
  color: var(--td-brand-color);
  opacity: 0.75;
}
.metric-footer {
  font-size: 13px;
  color: var(--td-text-color-secondary);
}
.trend-card,
.activity-card {
  height: 100%;
}
.trend-chart {
  height: 400px;
}
.version-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  max-height: 400px;
  overflow: auto;
}
.version-item {
  padding: 16px;
  background: var(--td-bg-color-secondarycontainer);
  border-radius: 8px;
  transition: transform 0.2s;
}
.version-item:hover {
  transform: translateX(5px);
}
.version-item.latest {
  border: 1px solid var(--td-brand-color-light);
  background: var(--td-brand-color-light);
}
.version-line {
  gap: 8px;
}
.version-item p {
  margin: 10px 0;
  color: var(--td-text-color-secondary);
  font-size: 13px;
  line-height: 1.5;
}
.version-item time {
  display: block;
  margin-bottom: 8px;
  color: var(--td-text-color-placeholder);
  font-size: 12px;
}
.card-header :deep(.t-link) {
  display: inline-flex;
  gap: 4px;
  align-items: center;
  font-size: 13px;
}
@media (max-width: 900px) {
  .welcome-content {
    align-items: flex-start;
    flex-direction: column;
  }
  .resource-groups {
    width: 100%;
    flex-wrap: wrap;
  }
  .video-group {
    min-width: auto;
  }
}
@media (max-width: 600px) {
  .dashboard-container {
    padding: 16px;
  }
  .resource-groups {
    gap: 16px;
  }
  .resource-group {
    min-width: auto;
  }
  .resource-group :deep(.t-space) {
    flex-wrap: wrap;
  }
}
</style>
