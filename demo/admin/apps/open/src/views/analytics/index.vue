<script lang="ts" setup>
import SankeyChart from './sankey.vue';

defineOptions({ name: 'Analytics' });
</script>

<template>
  <div class="analytics-wrapper"><SankeyChart /></div>
</template>

<style scoped>
.analytics-wrapper {
  padding: 16px;
}
</style>
