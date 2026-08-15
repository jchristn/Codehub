import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

// The dashboard talks to the backend using the absolute serverUrl captured at
// login, so this proxy is only a dev-time convenience for same-origin calls.
const BACKEND = 'http://127.0.0.1:8090';

export default defineConfig({
  // Served by the backend under /dashboard, so all asset URLs are prefixed with it.
  base: '/dashboard/',
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
      '@components': path.resolve(__dirname, './src/components'),
      '@views': path.resolve(__dirname, './src/views'),
      '@context': path.resolve(__dirname, './src/context'),
      '@utils': path.resolve(__dirname, './src/utils'),
      '@hooks': path.resolve(__dirname, './src/hooks'),
      '@i18n': path.resolve(__dirname, './src/i18n')
    }
  },
  server: {
    port: 3000,
    host: true,
    proxy: {
      '/v1.0': { target: BACKEND, changeOrigin: true },
      '/openapi.json': { target: BACKEND, changeOrigin: true }
    }
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom', 'react-router-dom'],
          i18n: ['i18next', 'react-i18next', 'i18next-browser-languagedetector']
        }
      }
    }
  }
});
