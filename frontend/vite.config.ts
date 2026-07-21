import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    // Mirrored in tsconfig.app.json's compilerOptions.paths - keep both in sync.
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
