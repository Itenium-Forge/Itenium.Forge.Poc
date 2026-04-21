declare module 'featureFlags/App' {
  const App: React.ComponentType
  export default App
}

declare module 'featureFlags/navConfig' {
  export const navConfig: { label: string; path: string }
}
