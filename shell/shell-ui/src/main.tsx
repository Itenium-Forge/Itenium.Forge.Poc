import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'

interface AppEntry {
  name: string
  remoteUrl: string
}

async function bootstrap() {
  const apps: AppEntry[] = await fetch('http://localhost:5100/apps')
    .then(r => r.json())
    .catch(() => [])

  const { registerRemotes } = await import('@module-federation/runtime')
  registerRemotes(apps.map(app => ({
    name: app.name,
    entry: `${app.remoteUrl}/remoteEntry.js`,
    type: 'module' as const,
  })))

  const { default: App } = await import('./App.tsx')

  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <App apps={apps} />
    </StrictMode>,
  )
}

bootstrap()
