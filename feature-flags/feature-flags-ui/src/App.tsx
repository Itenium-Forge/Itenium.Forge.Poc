import { useEffect, useState } from 'react'
import './App.css'

interface Flag {
  name: string
  enabled: boolean
}

function App() {
  const [flags, setFlags] = useState<Flag[]>([])

  useEffect(() => {
    fetch('http://localhost:5200/flags')
      .then(r => r.json())
      .then(setFlags)
      .catch(() => setFlags([]))
  }, [])

  return (
    <div className="feature-flags">
      <header className="ff-header">
        <span className="ff-logo">Feature Flags</span>
      </header>
      <main className="ff-content">
        <h1>Feature Flags</h1>
        <table className="ff-table">
          <thead>
            <tr>
              <th>Flag</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {flags.map(flag => (
              <tr key={flag.name}>
                <td>{flag.name}</td>
                <td className={flag.enabled ? 'enabled' : 'disabled'}>
                  {flag.enabled ? 'aan' : 'uit'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </main>
    </div>
  )
}

export default App
