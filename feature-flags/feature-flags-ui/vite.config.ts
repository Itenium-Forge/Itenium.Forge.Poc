import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import federation from '@originjs/vite-plugin-federation'

export default defineConfig({
  plugins: [
    react(),
    federation({
      name: 'featureFlags',
      filename: 'remoteEntry.js',
      exposes: {
        './App': './src/App.tsx',
        './navConfig': './src/navConfig.ts',
      },
      shared: ['react', 'react-dom'],
    }),
  ],
  server: { port: 3001 },
  preview: { port: 3001 },
  build: { target: 'esnext' },
})
