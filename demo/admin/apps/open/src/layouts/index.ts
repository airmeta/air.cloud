const BasicLayout = () => import('./basic.vue');

const IFrameView = () => import('@vben/layouts').then((m) => m.IFrameView);

export { BasicLayout, IFrameView };
