<script lang="ts" setup>
import { useRouter } from 'vue-router';

const router = useRouter();
</script>

<template>
  <main class="fallback-page">
    <section class="fallback-card">
      <p class="fallback-code">403</p>
      <h1>未授权访问</h1>
      <p>当前登录身份没有访问该资源的权限，或本次访问缺少有效的单点登录票据。</p>
      <button type="button" @click="router.replace('/')">返回首页</button>
    </section>
  </main>
</template>

<style scoped>
.fallback-page {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  padding: 24px;
  background: #f5f7fb;
}

.fallback-card {
  width: min(420px, 100%);
  padding: 40px;
  text-align: center;
  background: #fff;
  border: 1px solid #e7eaf0;
  border-radius: 16px;
  box-shadow: 0 12px 32px rgb(20 40 80 / 8%);
}

.fallback-code {
  margin: 0;
  font-size: 56px;
  font-weight: 700;
  color: #d54941;
}

.fallback-card h1 {
  margin: 8px 0 12px;
  font-size: 24px;
  color: #1f2329;
}

.fallback-card p {
  line-height: 1.7;
  color: #646a73;
}

.fallback-card button {
  height: 36px;
  padding: 0 18px;
  margin-top: 16px;
  color: #fff;
  cursor: pointer;
  background: #165dff;
  border: 0;
  border-radius: 6px;
}
</style>
