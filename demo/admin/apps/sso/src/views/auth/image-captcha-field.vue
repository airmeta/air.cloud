<script lang="ts" setup>
const modelValue = defineModel<string>({ default: '' });

const props = withDefaults(
  defineProps<{
    disabled?: boolean;
    imageUrl?: string;
    placeholder?: string;
  }>(),
  {
    disabled: false,
    imageUrl: '',
    placeholder: '请输入验证码',
  },
);

const emit = defineEmits<{
  refresh: [];
}>();

function handleRefresh() {
  emit('refresh');
}
</script>

<template>
  <div class="captcha-field">
    <input
      v-model="modelValue"
      :disabled="disabled"
      :placeholder="placeholder"
      class="captcha-field__input"
      type="text"
    />
    <button
      :disabled="disabled"
      class="captcha-field__button"
      type="button"
      @click="handleRefresh"
    >
      <img
        v-if="props.imageUrl"
        :src="props.imageUrl"
        alt="验证码"
        class="captcha-field__image"
      />
      <span v-else>获取验证码</span>
    </button>
  </div>
</template>

<style scoped>
.captcha-field {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(108px, 132px);
  gap: 10px;
  min-width: 0;
  width: 100%;
}

.captcha-field__input {
  box-sizing: border-box;
  width: 100%;
  height: 40px;
  min-width: 0;
  padding: 0 12px;
  color: hsl(var(--foreground));
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  outline: none;
}

.captcha-field__input:focus {
  border-color: hsl(var(--primary));
  box-shadow: 0 0 0 1px hsl(var(--primary));
}

.captcha-field__input:disabled {
  cursor: not-allowed;
  opacity: 0.7;
}

.captcha-field__button {
  box-sizing: border-box;
  display: flex;
  width: 100%;
  height: 40px;
  min-width: 0;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  padding: 0;
  color: hsl(var(--foreground));
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  cursor: pointer;
}

.captcha-field__button:disabled {
  cursor: not-allowed;
  opacity: 0.7;
}

.captcha-field__image {
  display: block;
  max-width: 100%;
  width: 100%;
  height: 100%;
  object-fit: cover;
}
</style>
