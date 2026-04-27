# shell-ui

Module Federation **host** voor de Itenium Forge POC. Laadt micro-frontends lazy at runtime op basis van de remote URLs die Shell.Api levert.

## Opstarten

```bash
npm install
npm run build && npm run preview   # http://localhost:3000
```

> `npm run dev` werkt niet met Module Federation remotes — gebruik `build + preview`.

## Structuur

```
src/
├── main.tsx          # fetcht /apps, registreert remotes, rendert React
├── App.tsx           # router + dynamische nav vanuit de apps array
└── remotes.d.ts      # TypeScript type declarations voor remote modules
```

## Remote toevoegen

Enkel `appsettings.json` van Shell.Api aanpassen — geen shell rebuild nodig:

```json
"Apps": {
  "featureFlags": { "RemoteUrl": "http://localhost:3001" }
}
```

De naam-conventie bepaalt pad en label: `featureFlags` → `/feature-flags` + `Feature Flags`.
