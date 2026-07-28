<script lang="ts" setup>
defineOptions({ name: 'SsoAuthLayout' });
</script>

<template>
  <main class="sso-auth-layout">
    <section class="sso-auth-layout__intro">
      <div class="sso-auth-layout__logo" aria-hidden="true"></div>
      <p class="sso-auth-layout__eyebrow">Air.Cloud SSO</p>
      <h1>Air.Cloud 单点登录</h1>
      <p>统一认证、应用入口与票据授权</p>
    </section>

    <section class="sso-auth-layout__panel">
      <RouterView />
    </section>
  </main>
</template>

<style scoped>
.sso-auth-layout {
  display: grid;
  min-height: 100vh;
  grid-template-columns: minmax(360px, 0.9fr) minmax(420px, 1.1fr);
  color: hsl(var(--foreground));
  background:
    radial-gradient(circle at 15% 20%, hsl(var(--primary) / 22%), transparent 30%),
    linear-gradient(135deg, hsl(var(--background)) 0%, hsl(var(--background-deep, var(--background))) 100%);
}

.sso-auth-layout__intro,
.sso-auth-layout__panel {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 56px;
}

.sso-auth-layout__intro {
  border-right: 1px solid hsl(var(--border));
}

.sso-auth-layout__logo {
  position: relative;
  width: 54px;
  height: 54px;
  margin-bottom: 28px;
  background: linear-gradient(135deg, #8b5cf6 0%, #06b6d4 100%);
  border-radius: 14px;
  transform: skew(-10deg);
}

.sso-auth-layout__logo::after {
  position: absolute;
  right: -15px;
  bottom: 4px;
  width: 32px;
  height: 32px;
  content: '';
  background: #22d3ee;
  border-radius: 10px;
}

.sso-auth-layout__eyebrow {
  margin: 0 0 12px;
  color: hsl(var(--primary));
  font-size: 14px;
  font-weight: 700;
}

.sso-auth-layout h1 {
  max-width: 520px;
  margin: 0 0 16px;
  font-size: 44px;
  line-height: 1.2;
}

.sso-auth-layout p {
  max-width: 520px;
  margin: 0;
  color: hsl(var(--muted-foreground));
  font-size: 16px;
  line-height: 1.8;
}

.sso-auth-layout__panel {
  align-items: center;
}

.sso-auth-layout__panel :deep(.authentication-login),
.sso-auth-layout__panel :deep(.login-form) {
  width: min(100%, 440px);
}

@media (max-width: 900px) {
  .sso-auth-layout {
    grid-template-columns: 1fr;
  }

  .sso-auth-layout__intro {
    display: none;
  }

  .sso-auth-layout__panel {
    padding: 32px 20px;
  }
}
</style>
