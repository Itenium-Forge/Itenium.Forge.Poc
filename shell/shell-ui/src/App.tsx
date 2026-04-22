import { lazy, Suspense, useEffect, useState } from 'react'
import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom'
import './App.css'

interface AppEntry {
  name: string
  remoteUrl: string
}

interface RemoteApp {
  name: string
  path: string
  label: string
  Component: React.LazyExoticComponent<React.ComponentType>
}

const toKebabCase = (name: string) =>
  name.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase()

const toLabel = (name: string) =>
  name.replace(/([A-Z])/g, ' $1').trim().replace(/^\w/, c => c.toUpperCase())

function buildRemoteApps(apps: AppEntry[]): RemoteApp[] {
  return apps.map(app => ({
    name: app.name,
    path: toKebabCase(app.name),
    label: toLabel(app.name),
    Component: lazy(async () => {
      const { loadRemote } = await import('@module-federation/runtime')
      return loadRemote(`${app.name}/App`) as Promise<{ default: React.ComponentType }>
    }),
  }))
}

function applyFlags(flags: { name: string; enabled: boolean }[]) {
  const dark = flags.find(f => f.name === 'dark-mode')?.enabled ?? false
  document.documentElement.classList.toggle('dark', dark)
}

function Shell({ apps }: { apps: AppEntry[] }) {
  const remoteApps = buildRemoteApps(apps)
  const [message, setMessage] = useState<string>('')

  useEffect(() => {
    fetch('http://localhost:5100')
      .then(r => r.text())
      .then(setMessage)
      .catch(() => setMessage('backend offline'))
  }, [])

  useEffect(() => {
    const poll = () =>
      fetch('http://localhost:5100/api/flags')
        .then(r => r.json())
        .then(applyFlags)
        .catch(() => {})

    poll()
    const id = setInterval(poll, 3000)
    return () => clearInterval(id)
  }, [])

  return (
    <div className="shell">
      <header className="shell-header">
        <span className="shell-logo">Forge POC</span>
        <nav>
          <NavLink to="/">Home</NavLink>
          {remoteApps.map(app => (
            <NavLink key={app.name} to={`/${app.path}`}>{app.label}</NavLink>
          ))}
        </nav>
      </header>
      <main className="shell-content">
        <Routes>
          <Route path="/" element={
            <>
              <h1>Shell</h1>
              <p>Backend: <strong>{message || '...'}</strong></p>
            </>
          } />
          {remoteApps.map(app => (
            <Route key={app.name} path={`/${app.path}`} element={
              <Suspense fallback={<p>Laden...</p>}>
                <app.Component />
              </Suspense>
            } />
          ))}
        </Routes>
      </main>
    </div>
  )
}

function App({ apps }: { apps: AppEntry[] }) {
  return (
    <BrowserRouter>
      <Shell apps={apps} />
    </BrowserRouter>
  )
}

export default App
