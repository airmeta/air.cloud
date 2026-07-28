import path from 'node:path';
import { fileURLToPath } from 'node:url';

import vue from '@vitejs/plugin-vue';
import { defineConfig, loadEnv } from 'vite';

const dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, dirname, '');
  return {
    base: env.VITE_BASE || '/',
    plugins: [vue()],
    resolve: {
      alias: {
        '#': path.resolve(dirname, 'src'),
      },
    },
    server: {
      port: Number(env.VITE_PORT || 6103),
      proxy: {
        '/api': {
          changeOrigin: true,
          rewrite: (requestPath) => requestPath.replace(/^\/api/, ''),
          target: env.VITE_PROXY_API_TARGET || 'http://192.168.80.101:18080',
          ws: true,
        },
      },
    },
  };
});
