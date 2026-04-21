import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { federation } from '@module-federation/vite'

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'shell',
      remotes: {
        featureFlags: {
          type: 'module',
          name: 'featureFlags',
          entry: 'http://localhost:3001/remoteEntry.js',
        },
      },
      shared: { react: { singleton: true }, 'react-dom': { singleton: true } },
    }),
  ],
  server: { port: 3000 },
  preview: { port: 3000 },
})
