<script lang="ts" setup>
import { Fallback } from '@vben/common-ui';

defineOptions({ name: 'Fallback404Demo' });
</script>

<template>
  <div class="fallback-page">
    <Fallback status="404" />
  </div>
</template>
