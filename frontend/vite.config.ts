import { fileURLToPath, URL } from 'node:url'
import { readFileSync } from 'node:fs'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const { version } = JSON.parse(readFileSync(fileURLToPath(new URL('./package.json', import.meta.url)), 'utf-8')) as { version: string }

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    // Mirrored in tsconfig.app.json's compilerOptions.paths - keep both in sync.
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  define: {
    __APP_VERSION__: JSON.stringify(version),
  },
  server: {
    watch: {
      // A Windows/macOS bind mount into the Linux dev container delivers no filesystem events to
      // the watcher inside it, so an edit made on the host is invisible to Vite until the container
      // is restarted - polling is the only thing that sees those. Off unless asked for: the native
      // host workflow gets real events already, and shouldn't pay a poll of the whole tree for
      // nothing. docker-compose.yml sets this for the frontend container.
      usePolling: process.env.CHOKIDAR_USEPOLLING === 'true',
    },
  },
})
