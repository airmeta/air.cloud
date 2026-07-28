<script lang="ts" setup>
defineOptions({ name: 'FallbackOfflineDemo' });
</script>

<template>
  <section class="fallback-page">
    <strong>Offline</strong>
    <h1>网络不可用</h1>
    <p>请检查网络连接后重试。</p>
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
  font-size: 44px;
}

.fallback-page h1 {
  margin: 8px 0;
  color: hsl(var(--foreground));
}
</style>
