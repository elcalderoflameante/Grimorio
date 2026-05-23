import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const devApiTarget = env.VITE_DEV_API_TARGET;

  if (command === 'serve' && !devApiTarget) {
    throw new Error('VITE_DEV_API_TARGET is required in your .env file');
  }

  return {
    plugins: [react()],
    server: {
      host: true,
      port: 5173,
      proxy: {
        '/api': {
          target: devApiTarget ?? 'http://localhost:5186',
          changeOrigin: true,
          secure: false,
        },
        '/hubs': {
          target: devApiTarget ?? 'http://localhost:5186',
          changeOrigin: true,
          secure: false,
          ws: true,
        },
      },
    },
    build: {
      chunkSizeWarningLimit: 1500,
      rollupOptions: {
        onwarn(warning, warn) {
          const id = warning.id ?? '';

          if (warning.code === 'INVALID_ANNOTATION' && id.includes('@microsoft/signalr')) {
            return;
          }

          if (warning.code === 'EVAL' && id.includes('lottie-web')) {
            return;
          }

          warn(warning);
        },
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

            if (isPkg('@microsoft/signalr')) {
              return 'vendor-signalr';
            }

            if (isPkg('lottie-react') || isPkg('lottie-web')) {
              return 'vendor-lottie';
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
