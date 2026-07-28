<script lang="ts" setup>
import { Fallback } from '@vben/common-ui';

defineOptions({ name: 'FallbackOfflineDemo' });
</script>

<template>
  <div class="fallback-page">
    <Fallback status="offline" />
  </div>
</template>
