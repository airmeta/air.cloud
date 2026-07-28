<script lang="ts" setup>
import type { EchartsUIType } from '@vben/plugins/echarts';

import { onMounted, ref } from 'vue';

import { useEcharts, EchartsUI } from '@vben/plugins/echarts';

defineOptions({ name: 'InterfaceMappingSankey' });

const chartRef = ref<EchartsUIType>();
const { renderEcharts } = useEcharts(chartRef);

const categoryColors = ['#0052d9', '#00a870', '#d97706', '#d54941'];

const nodes: any[] = [
  ['系统主入口接口', 0, 0, '入口层'],
  ['动作映射_用户模块', 1, 1, '动作/外部映射层'],
  ['动作映射_订单模块', 1, 1, '动作/外部映射层'],
  ['动作映射_商品模块', 1, 1, '动作/外部映射层'],
  ['动作映射_支付模块', 1, 1, '动作/外部映射层'],
  ['动作映射_物流模块', 1, 1, '动作/外部映射层'],
  ['外部接口_用户查询', 2, 2, '外部接口层'],
  ['外部接口_用户新增', 2, 2, '外部接口层'],
  ['外部接口_订单创建', 2, 2, '外部接口层'],
  ['外部接口_订单查询', 2, 2, '外部接口层'],
  ['外部接口_商品列表', 2, 2, '外部接口层'],
  ['外部接口_商品详情', 2, 2, '外部接口层'],
  ['外部接口_支付发起', 2, 2, '外部接口层'],
  ['外部接口_支付回调', 2, 2, '外部接口层'],
  ['外部接口_物流轨迹', 2, 2, '外部接口层'],
  ['外部接口_物流签收', 2, 2, '外部接口层'],
  ['外部接口_通用查询', 2, 2, '外部接口层'],
  ['外部接口_通用同步', 2, 2, '外部接口层'],
  ['内部核心接口_数据查询', 3, 3, '内部接口层'],
  ['内部核心接口_数据写入', 3, 3, '内部接口层'],
  ['内部核心接口_状态同步', 3, 3, '内部接口层'],
  ['内部核心接口_结果回调', 3, 3, '内部接口层'],
].map(([name, level, category, categoryName]) => ({
  category,
  categoryName,
  itemStyle: { color: categoryColors[Number(category)] },
  level,
  name,
}));

const links: any[] = [
  ['系统主入口接口', '动作映射_用户模块'],
  ['系统主入口接口', '动作映射_订单模块'],
  ['系统主入口接口', '动作映射_商品模块'],
  ['系统主入口接口', '动作映射_支付模块'],
  ['系统主入口接口', '动作映射_物流模块'],
  ['动作映射_用户模块', '外部接口_用户查询'],
  ['动作映射_用户模块', '外部接口_用户新增'],
  ['动作映射_订单模块', '外部接口_订单创建'],
  ['动作映射_订单模块', '外部接口_订单查询'],
  ['动作映射_商品模块', '外部接口_商品列表'],
  ['动作映射_商品模块', '外部接口_商品详情'],
  ['动作映射_支付模块', '外部接口_支付发起'],
  ['动作映射_支付模块', '外部接口_支付回调'],
  ['动作映射_物流模块', '外部接口_物流轨迹'],
  ['动作映射_物流模块', '外部接口_物流签收'],
  ['动作映射_用户模块', '外部接口_通用查询'],
  ['动作映射_订单模块', '外部接口_通用同步'],
  ['外部接口_用户查询', '内部核心接口_数据查询'],
  ['外部接口_订单查询', '内部核心接口_数据查询'],
  ['外部接口_商品列表', '内部核心接口_数据查询'],
  ['外部接口_商品详情', '内部核心接口_数据查询'],
  ['外部接口_物流轨迹', '内部核心接口_数据查询'],
  ['外部接口_通用查询', '内部核心接口_数据查询'],
  ['外部接口_用户新增', '内部核心接口_数据写入'],
  ['外部接口_订单创建', '内部核心接口_数据写入'],
  ['外部接口_物流签收', '内部核心接口_数据写入'],
  ['外部接口_支付发起', '内部核心接口_状态同步'],
  ['外部接口_通用同步', '内部核心接口_状态同步'],
  ['外部接口_支付回调', '内部核心接口_结果回调'],
].map(([source, target]) => ({ source, target, value: 1 }));

onMounted(() => {
  renderEcharts({
    series: [
      {
        bottom: 30,
        data: nodes,
        left: 20,
        lineStyle: {
          color: '#86909c',
          curveness: 0.12,
          opacity: 0.9,
          width: 1.5,
        },
        links,
        nodeGap: 12,
        nodeWidth: 22,
        right: 20,
        top: 42,
        type: 'sankey',
        label: {
          fontSize: 11,
          formatter: ({ name }: { name: string }) =>
            name.length > 10 ? `${name.slice(0, 10)}...` : name,
        },
      },
    ],
    title: {
      left: 'center',
      text: '接口多层级映射（纯配置、多数据、高复用、纯线条）',
      textStyle: { color: '#333', fontSize: 16, fontWeight: 'normal' },
    },
    tooltip: {
      formatter: (params: any) =>
        params.dataType === 'edge'
          ? `映射关系<br>${params.data.source} -> ${params.data.target}`
          : `${params.data.categoryName}<br>节点：${params.name}`,
      trigger: 'item',
      triggerOn: 'mousemove',
    },
  });
});
</script>

<template>
  <section class="sankey-page">
    <header class="chart-header">
      <h2>Interface Mapping Sankey</h2>
      <p>展示入口、动作映射、外部接口与内部接口之间的多层映射关系。</p>
    </header>
    <EchartsUI ref="chartRef" class="chart" />
  </section>
</template>

<style scoped>
.sankey-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 16px;
}
.chart-header h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}
.chart-header p {
  margin: 4px 0 0;
  color: var(--td-text-color-secondary);
  font-size: 13px;
}
.chart {
  height: 520px;
  border: 1px solid var(--td-component-border);
  border-radius: 8px;
  background: var(--td-bg-color-container);
}
</style>
