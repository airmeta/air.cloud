import { createApp } from 'vue';

import { createPinia } from 'pinia';

import App from './app.vue';
import { router } from './router';

import 'tdesign-vue-next/es/style/index.css';

async function bootstrap() {
  const app = createApp(App);
  app.use(createPinia());
  app.use(router);
  app.mount('#app');
}

export { bootstrap };
