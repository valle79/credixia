import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

const apiTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5111'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/health': { target: apiTarget, changeOrigin: true },
      '/openapi': { target: apiTarget, changeOrigin: true },
      '/scalar': { target: apiTarget, changeOrigin: true },
    },
  },
  preview: {
    port: 4173,
    strictPort: true,
  },
  build: {
    sourcemap: true,
  },
  test: {
    environment: 'jsdom',
    pool: 'threads',
    setupFiles: ['./src/test/setup.ts'],
    css: true,
    restoreMocks: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/main.tsx', 'src/test/**', 'src/vite-env.d.ts'],
    },
  },
})
