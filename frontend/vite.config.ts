import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
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
})
