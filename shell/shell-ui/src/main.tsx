import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'

interface AppEntry {
  name: string
  remoteUrl: string
}

interface Flag {
  name: string
  enabled: boolean
}

async function bootstrap() {
  const [apps, flags]: [AppEntry[], Flag[]] = await Promise.all([
    fetch('http://localhost:5100/apps').then(r => r.json()).catch(() => []),
    fetch('http://localhost:5100/api/flags').then(r => r.json()).catch(() => []),
  ])

  const darkMode = (flags as Flag[]).find(f => f.name === 'dark-mode')?.enabled ?? false
  document.documentElement.classList.toggle('dark', darkMode)

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
