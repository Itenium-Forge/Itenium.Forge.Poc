import { useEffect, useState } from 'react'
import './App.css'

function App() {
  const [message, setMessage] = useState<string>('')

  useEffect(() => {
    fetch('http://localhost:5100')
      .then(r => r.text())
      .then(setMessage)
      .catch(() => setMessage('backend offline'))
  }, [])

  return (
    <div className="shell">
      <header className="shell-header">
        <span className="shell-logo">Forge POC</span>
        <nav>
          <a href="/">Home</a>
        </nav>
      </header>
      <main className="shell-content">
        <h1>Shell</h1>
        <p>Backend: <strong>{message || '...'}</strong></p>
      </main>
    </div>
  )
}

export default App
