import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    port: 3000,
    host: true,
    watch: {
      ignored: [
        '**/Temp/**',
        '**/Library/**',
        '**/Logs/**',
        '**/Assets/**',
        '**/ProjectSettings/**',
        '**/UserSettings/**',
        '**/.git/**',
      ],
    },
  },
  optimizeDeps: {
    exclude: ['@dimforge/rapier3d-compat'],
  },
  build: {
    target: 'esnext',
  },
});
