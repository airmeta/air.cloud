<script lang="ts" setup>
defineOptions({ name: 'Fallback500Demo' });
</script>

<template>
  <section class="fallback-page">
    <strong>500</strong>
    <h1>服务异常</h1>
    <p>系统处理请求时发生异常，请稍后重试。</p>
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
