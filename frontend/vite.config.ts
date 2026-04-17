import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const devApiTarget = env.VITE_DEV_API_TARGET;

  if (!devApiTarget) {
    throw new Error('VITE_DEV_API_TARGET is required in your .env file');
  }

  return {
    plugins: [react()],
    server: {
      host: true,
      port: 5173,
      proxy: {
        '/api': {
          target: devApiTarget,
          changeOrigin: true,
          secure: false,
        },
        '/hubs': {
          target: devApiTarget,
          changeOrigin: true,
          secure: false,
          ws: true,
        },
      },
    },
    build: {
      chunkSizeWarningLimit: 1200,
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (!id.includes('node_modules')) return;

            const isPkg = (pkg: string) =>
              id.includes(`/node_modules/${pkg}/`) || id.includes(`\\node_modules\\${pkg}\\`);

            if (isPkg('react') || isPkg('react-dom') || isPkg('scheduler') || isPkg('react-is')) {
              return 'vendor-react';
            }

            if (isPkg('antd') || isPkg('@ant-design')) {
              return 'vendor-antd';
            }

            if (isPkg('dayjs')) {
              return 'vendor-dayjs';
            }

            if (isPkg('leaflet') || isPkg('react-leaflet')) {
              return 'vendor-leaflet';
            }

            if (isPkg('jspdf') || isPkg('html2canvas')) {
              return 'vendor-export';
            }

            return;
          },
        },
      },
    },
  };
})
