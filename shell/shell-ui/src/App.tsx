import { lazy, Suspense } from 'react'
import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom'
import { loadRemote } from '@module-federation/runtime'
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
  name.replace(/([A-Z])/g, ' $1').trim()

function buildRemoteApps(apps: AppEntry[]): RemoteApp[] {
  return apps.map(app => ({
    name: app.name,
    path: toKebabCase(app.name),
    label: toLabel(app.name),
    Component: lazy(() =>
      (loadRemote(`${app.name}/App`) as Promise<{ default: React.ComponentType }>)
    ),
  }))
}

function Shell({ apps }: { apps: AppEntry[] }) {
  const remoteApps = buildRemoteApps(apps)

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
          <Route path="/" element={<h1>Shell</h1>} />
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
