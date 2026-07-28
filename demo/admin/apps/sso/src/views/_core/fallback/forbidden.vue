<script lang="ts" setup>
defineOptions({ name: 'Fallback403Demo' });
</script>

<template>
  <section class="fallback-page">
    <strong>403</strong>
    <h1>无权访问</h1>
    <p>当前账号没有访问该页面的权限。</p>
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
