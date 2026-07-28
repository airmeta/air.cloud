<script lang="ts" setup>
defineOptions({ name: 'Fallback404Demo' });
</script>

<template>
  <section class="fallback-page">
    <strong>404</strong>
    <h1>页面不存在</h1>
    <p>请求的页面不存在或已经被移除。</p>
  </section>
</template>

<style scoped>
.fallback-page {
  display: flex;
  min-height: 60vh;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: hsl(var(--muted-foreground));
}

.fallback-page strong {
  color: hsl(var(--primary));
  font-size: 56px;
}

.fallback-page h1 {
  margin: 8px 0;
  color: hsl(var(--foreground));
}
</style>
