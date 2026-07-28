<script lang="ts" setup>
import { Fallback } from '@vben/common-ui';

defineOptions({ name: 'Fallback500Demo' });
</script>

<template>
  <div class="fallback-page">
    <Fallback status="500" />
  </div>
</template>
