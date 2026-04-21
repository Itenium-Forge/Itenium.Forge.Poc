import { lazy, Suspense, useEffect, useState } from 'react'
import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom'
import './App.css'

const FeatureFlagsApp = lazy(() => import('featureFlags/App'))

interface AppEntry {
  name: string
  remoteUrl: string
}

function Shell() {
  const [message, setMessage] = useState<string>('')
  const [apps, setApps] = useState<AppEntry[]>([])

  useEffect(() => {
    fetch('http://localhost:5100')
      .then(r => r.text())
      .then(setMessage)
      .catch(() => setMessage('backend offline'))

    fetch('http://localhost:5100/apps')
      .then(r => r.json())
      .then(setApps)
      .catch(() => setApps([]))
  }, [])

  return (
    <div className="shell">
      <header className="shell-header">
        <span className="shell-logo">Forge POC</span>
        <nav>
          <NavLink to="/">Home</NavLink>
          {apps.map(app => {
            const path = app.name.replace(/([a-z])([A-Z])/g, '$1-$2').toLowerCase()
            const label = app.name.replace(/([A-Z])/g, ' $1').trim()
            return <NavLink key={app.name} to={`/${path}`}>{label}</NavLink>
          })}
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
          <Route path="/feature-flags" element={
            <Suspense fallback={<p>Laden...</p>}>
              <FeatureFlagsApp />
            </Suspense>
          } />
        </Routes>
      </main>
    </div>
  )
}

function App() {
  return (
    <BrowserRouter>
      <Shell />
    </BrowserRouter>
  )
}

export default App
