import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    environment: 'jsdom',
    globals: false,
    setupFiles: ['./src/test/setup.ts'],
    pool: 'forks',
    maxWorkers: '50%',
    testTimeout: 20000,
    hookTimeout: 30000,
    teardownTimeout: 15000,
  },
});
