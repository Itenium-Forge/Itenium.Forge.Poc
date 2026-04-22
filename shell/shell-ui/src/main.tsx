import { init } from '@module-federation/runtime'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

interface AppEntry {
  name: string
  remoteUrl: string
}

const apps: AppEntry[] = await fetch('/apps')
  .then(r => r.json())
  .catch(() => [])

await init({
  name: 'shell',
  remotes: apps.map(app => ({
    name: app.name,
    entry: `${app.remoteUrl}/remoteEntry.js`,
  })),
})

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App apps={apps} />
  </StrictMode>,
)
