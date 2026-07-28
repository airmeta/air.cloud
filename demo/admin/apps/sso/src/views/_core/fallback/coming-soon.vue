<script lang="ts" setup>
defineOptions({ name: 'FallbackComingSoonDemo' });
</script>

<template>
  <section class="fallback-page">
    <strong>Soon</strong>
    <h1>功能建设中</h1>
    <p>该功能暂未开放。</p>
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
