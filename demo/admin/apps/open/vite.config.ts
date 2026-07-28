import { defineConfig } from '@vben/vite-config';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig(async () => {
  return {
    application: {},
    vite: {
      resolve: {
        alias: {
          '@root/base': fileURLToPath(
            new URL('../../packages/base', import.meta.url),
          ),
        },
      },
      server: {
        proxy: {
          '/api': {
            changeOrigin: true,
            target:
              process.env.VITE_PROXY_API_TARGET ||
              'http://192.168.80.101:18080',
            ws: true,
          },
          '/v1': {
            changeOrigin: true,
            target:
              process.env.VITE_PROXY_API_TARGET ||
              'http://192.168.80.101:18080',
            ws: true,
          },
        },
      },
    },
  };
});
