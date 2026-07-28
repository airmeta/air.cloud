import { defineConfig } from '@vben/vite-config';

export default defineConfig(async () => {
  return {
    application: {},
    vite: {
      server: {
        proxy: {
          '/api': {
            changeOrigin: true,
            rewrite: (path) => path.replace(/^\/api/, ''),
            // mock代理目标地址
            target: process.env.VITE_PROXY_API_TARGET || 'http://192.168.80.101:18080',
            ws: true,
          },
        },
      },
    },
  };
});
