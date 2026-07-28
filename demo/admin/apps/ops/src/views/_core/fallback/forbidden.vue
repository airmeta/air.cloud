<script lang="ts" setup>
import { Fallback } from '@vben/common-ui';

defineOptions({ name: 'Fallback403Demo' });
</script>

<template>
  <div class="fallback-page">
    <Fallback status="403" />
  </div>
</template>
