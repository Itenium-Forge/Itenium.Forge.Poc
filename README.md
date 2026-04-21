# Itenium Forge POC

Tracer Bullet 2 — bewijst dat een shell-applicatie een aparte micro-frontend lazy kan inladen via **Module Federation**, en dat beide backends het **Forge framework** gebruiken.

Beantwoordt open vraag **Q#5**: hoe werkt cross-microservice navigatie in een shell?

---

## Structuur

```
Itenium.Forge.Poc/
├── shell/
│   ├── Shell.Api/              .NET backend, poort 5100
│   ├── Shell.Api.Tests/        NUnit integratie tests
│   └── shell-ui/               React/Vite host, poort 3000
└── feature-flags/
    ├── FeatureFlags.Api/       .NET backend, poort 5200
    └── feature-flags-ui/       React/Vite remote, poort 3001
```

---

## Opstarten

### Vereisten

- .NET 10 SDK
- Node.js 20+
- GitHub PAT met `read:packages` scope (voor de Forge NuGet feed)

### Backends

```bash
cd shell/Shell.Api && dotnet run          # http://localhost:5100
cd feature-flags/FeatureFlags.Api && dotnet run  # http://localhost:5200
```

### Frontends

> **Belangrijk:** `@originjs/vite-plugin-federation` werkt **niet** in `npm run dev` voor de remote.
> De feature-flags-ui moet gebuild en via `preview` geserveerd worden.
> De shell-ui kan wel in dev mode draaien.

```bash
# Remote (feature-flags-ui) — eerst builden, dan preview
cd feature-flags/feature-flags-ui
npm install
npm run build
npm run preview        # http://localhost:3001

# Host (shell-ui) — dev mode is OK
cd shell/shell-ui
npm install
npm run dev            # http://localhost:3000
```

### Testen

```bash
cd shell && dotnet test
```

---

## Hoe werkt het?

De shell haalt bij opstart de geregistreerde apps op via `GET /apps` (Shell.Api). Die endpoint leest de remote URLs uit `appsettings.json`. Voor elke app wordt een nav-item aangemaakt.

De Feature Flags component wordt **lazy geladen** via Module Federation wanneer de gebruiker op "Feature Flags" klikt. De component draait in de shell maar komt van `http://localhost:3001/assets/remoteEntry.js`.

```
Browser → shell-ui (3000)
           ├── GET /apps → Shell.Api (5100)         nav items ophalen
           └── lazy import → feature-flags-ui (3001)  component laden
                              └── GET /flags → FeatureFlags.Api (5200)  data ophalen
```

---

## Forge Framework

Beide backends gebruiken dezelfde Forge packages (v0.3.13):

| Package | Wat het doet | Waar gebruikt |
|---------|-------------|---------------|
| `Itenium.Forge.Settings` | appsettings laden, `IForgeSettings` valideren, metadata loggen bij startup | `ShellSettings`, `FeatureFlagsSettings` |
| `Itenium.Forge.Logging` | Serilog bootstrap, structured request logging, log naar bestand | Logs naar `c:\temp\ForgePoc` |
| `Itenium.Forge.Controllers` | CORS, response compression (Brotli/Gzip), camelCase JSON | CORS op `/flags` en `/apps`, gecomprimeerde responses |
| `Itenium.Forge.HealthChecks` | `/health/live` en `/health/ready` endpoints | Beide backends |

### Niet (nog) gebruikt in deze POC

| Package | Reden |
|---------|-------|
| `Itenium.Forge.Security` | Geen authenticatie — bewuste scope-beperking |
| `Itenium.Forge.Swagger` | Geen API documentatie nodig |
| `Itenium.Forge.Telemetry` | Geen OpenTelemetry |
| `Itenium.Forge.HttpClient` | Geen backend-to-backend calls (Refit) |

### Bekende workaround

`Itenium.Forge.Controllers` v0.3.13 registreert de CORS policy maar roept `UseCors()` niet aan in `UseForgeControllers()`. Dit is gefixed in Forge Core (branch `fix/use-forge-controllers-cors`, gemerged naar master). Zolang er geen nieuwe versie gepubliceerd is, staat er in beide `Program.cs` bestanden een handmatige `app.UseCors("CorsPolicy")` call met een TODO comment.

---

## NuGet configuratie

De Forge packages komen van GitHub Packages. Voeg een PAT toe:

```bash
dotnet nuget add source "https://nuget.pkg.github.com/Itenium-Forge/index.json" \
  --name itenium \
  --username <github-username> \
  --password <PAT-met-read:packages>
```

---

## Module Federation — bekende beperkingen

- `npm run dev` werkt **niet** voor de remote — gebruik altijd `build` + `preview`
- React versie moet exact overeenkomen tussen host en remote (beide `react` en `react-dom` als `shared`)
- CSS imports in exposed modules vereisen een plugin patch op Windows (backtick vs quote in regex) — zie `node_modules/@originjs/vite-plugin-federation/dist/index.js` en `.mjs`
