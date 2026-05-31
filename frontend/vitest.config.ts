import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [vue()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      provider: 'v8',
      all: true,
      include: ['src/**/*.{ts,vue}'],
      exclude: [
        'src/main.ts',
        'src/style.css',
        'src/types/**',
        'src/**/*.test.ts',
        'src/test/**',
      ],
      reporter: ['text', 'json-summary', 'html'],
    },
  },
})
