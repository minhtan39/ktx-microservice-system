import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  server: {
    host: true,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:8080',
        changeOrigin: true,
      },
    },
  },
  resolve: {
    alias: {
      // Định nghĩa ký tự @ sẽ trỏ thẳng vào thư mục src
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  }
})
