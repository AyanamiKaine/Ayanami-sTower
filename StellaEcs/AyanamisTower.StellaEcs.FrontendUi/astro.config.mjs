import { defineConfig } from 'astro/config';
import svelte from '@astrojs/svelte';
import tailwind from '@astrojs/tailwind';

export default defineConfig({
  integrations: [svelte(), tailwind({ applyBaseStyles: true })],
  server: {
    host: true,
    port: 4321
  },
  // Vite configuration for dev proxy to backend
  vite: {
    server: {
      proxy: {
        '/api': {
          target: 'http://localhost:5123',
          changeOrigin: true,
          // Do not rewrite; backend already expects /api prefix
          // If backend had no /api prefix we would rewrite here.
        }
      }
    }
  }
});

