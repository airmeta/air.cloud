<script lang="ts" setup>
import type { AuthApi } from '#/api';

import { computed, onMounted, reactive, ref, useTemplateRef } from 'vue';

import { $t } from '@vben/locales';

import { Check, ChevronsRight, RotateCw } from '@vben/icons';

import { getCaptchaApi } from '#/api';

type CaptchaValue = {
  id: string;
  x: number;
};

const modelValue = defineModel<CaptchaValue | false>({ default: false });

const challenge = ref<AuthApi.CaptchaChallengeResult>();
const trackRef = useTemplateRef<HTMLDivElement>('trackRef');
const loading = ref(false);
const dragging = ref(false);
const verified = ref(false);
const failed = ref(false);
const sliderLeft = ref(0);
const dragStart = reactive({
  left: 0,
  x: 0,
});

const TOLERANCE = 12;

const scale = computed(() => {
  const data = challenge.value;
  const trackWidth = trackRef.value?.clientWidth;
  return data && trackWidth ? trackWidth / data.width : 1;
});

const displayedSliderWidth = computed(() => {
  return Math.round((challenge.value?.sliderWidth ?? 48) * scale.value);
});

const displayedTargetX = computed(() => {
  return Math.round((challenge.value?.targetX ?? 0) * scale.value);
});

const maxLeft = computed(() => {
  const data = challenge.value;
  const trackWidth = trackRef.value?.clientWidth ?? data?.width ?? 320;
  return data ? trackWidth - displayedSliderWidth.value : 0;
});

const handleStyle = computed(() => ({
  left: `${sliderLeft.value}px`,
  width: `${displayedSliderWidth.value}px`,
}));

const targetStyle = computed(() => {
  return {
    left: `${displayedTargetX.value}px`,
    width: `${displayedSliderWidth.value}px`,
  };
});

defineExpose({
  resume,
});

onMounted(() => {
  void loadChallenge();
});

async function loadChallenge() {
  loading.value = true;
  try {
    challenge.value = await getCaptchaApi();
    resetState();
  } finally {
    loading.value = false;
  }
}

function resetState() {
  dragging.value = false;
  verified.value = false;
  failed.value = false;
  sliderLeft.value = 0;
  modelValue.value = false;
}

function resume() {
  return loadChallenge();
}

function getClientX(event: MouseEvent | TouchEvent) {
  if ('touches' in event && event.touches[0]) {
    return event.touches[0].clientX;
  }

  if ('changedTouches' in event && event.changedTouches[0]) {
    return event.changedTouches[0].clientX;
  }

  return 'clientX' in event ? event.clientX : 0;
}

function handleDragStart(event: MouseEvent | TouchEvent) {
  if (!challenge.value || verified.value || loading.value) {
    return;
  }

  failed.value = false;
  dragging.value = true;
  dragStart.x = getClientX(event);
  dragStart.left = sliderLeft.value;
}

function handleDragMove(event: MouseEvent | TouchEvent) {
  if (!dragging.value) {
    return;
  }

  event.preventDefault();
  const nextLeft = dragStart.left + getClientX(event) - dragStart.x;
  sliderLeft.value = Math.min(Math.max(nextLeft, 0), maxLeft.value);
}

function handleDragEnd() {
  if (!dragging.value || !challenge.value) {
    return;
  }

  dragging.value = false;
  const x = Math.round(sliderLeft.value / scale.value);
  const passed = Math.abs(x - challenge.value.targetX) <= TOLERANCE;

  if (passed) {
    sliderLeft.value = displayedTargetX.value;
    verified.value = true;
    modelValue.value = {
      id: challenge.value.id,
      x: challenge.value.targetX,
    };
    return;
  }

  failed.value = true;
  modelValue.value = false;
  window.setTimeout(() => {
    void loadChallenge();
  }, 320);
}
</script>

<template>
  <div class="backend-slider-captcha">
    <div
      ref="trackRef"
      class="backend-slider-captcha__track"
      :class="{
        'is-dragging': dragging,
        'is-failed': failed,
        'is-loading': loading,
        'is-verified': verified,
      }"
      @mouseleave="handleDragEnd"
      @mousemove="handleDragMove"
      @mouseup="handleDragEnd"
      @touchend="handleDragEnd"
      @touchmove="handleDragMove"
    >
      <div
        v-if="challenge && !verified"
        class="backend-slider-captcha__target"
        :style="targetStyle"
      ></div>
      <div
        class="backend-slider-captcha__bar"
        :style="{ width: `${sliderLeft + displayedSliderWidth / 2}px` }"
      ></div>
      <button
        class="backend-slider-captcha__handle"
        type="button"
        :style="handleStyle"
        @mousedown="handleDragStart"
        @touchstart="handleDragStart"
      >
        <Check v-if="verified" class="size-4" />
        <ChevronsRight v-else class="size-4" />
      </button>
      <span>
        {{ verified ? $t('ui.captcha.sliderSuccessText') : $t('ui.captcha.sliderDefaultText') }}
      </span>
      <button
        class="backend-slider-captcha__refresh"
        type="button"
        :aria-label="$t('ui.captcha.refreshAriaLabel')"
        @click="loadChallenge"
      >
        <RotateCw class="size-4" />
      </button>
    </div>
  </div>
</template>

<style scoped>
.backend-slider-captcha {
  width: 100%;
}

.backend-slider-captcha__track {
  position: relative;
  display: flex;
  width: 100%;
  max-width: 100%;
  height: 40px;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  border: 1px solid hsl(var(--border));
  border-radius: 8px;
  color: hsl(var(--muted-foreground));
  background: hsl(var(--background-deep));
  user-select: none;
}

.backend-slider-captcha__track.is-loading {
  opacity: 0.72;
}

.backend-slider-captcha__track.is-failed {
  border-color: hsl(var(--destructive));
}

.backend-slider-captcha__track.is-verified {
  color: hsl(var(--primary));
  border-color: hsl(var(--primary) / 55%);
  background: hsl(var(--primary) / 8%);
}

.backend-slider-captcha__target {
  position: absolute;
  top: 5px;
  height: 28px;
  border: 1px dashed hsl(var(--primary) / 72%);
  border-radius: 7px;
  background: hsl(var(--primary) / 10%);
}

.backend-slider-captcha__bar {
  position: absolute;
  left: 0;
  height: 100%;
  background: hsl(var(--primary) / 14%);
}

.backend-slider-captcha__handle {
  position: absolute;
  top: 0;
  z-index: 2;
  display: flex;
  height: 100%;
  align-items: center;
  justify-content: center;
  border: 0;
  color: hsl(var(--foreground));
  background: hsl(var(--background));
  box-shadow: 0 2px 8px rgb(15 23 42 / 18%);
  cursor: move;
}

.backend-slider-captcha__track.is-verified .backend-slider-captcha__handle {
  color: white;
  background: hsl(var(--primary));
}

.backend-slider-captcha__track span {
  position: relative;
  z-index: 1;
  max-width: calc(100% - 96px);
  overflow: hidden;
  font-size: 13px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.backend-slider-captcha__refresh {
  position: absolute;
  right: 6px;
  z-index: 3;
  display: flex;
  width: 28px;
  height: 28px;
  align-items: center;
  justify-content: center;
  border: 0;
  border-radius: 6px;
  color: hsl(var(--muted-foreground));
  background: transparent;
  cursor: pointer;
}

.backend-slider-captcha__refresh:hover {
  color: hsl(var(--foreground));
  background: hsl(var(--accent));
}
</style>
