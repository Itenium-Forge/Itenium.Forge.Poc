# Itenium Forge POC

Tracer Bullet 2 — bewijst dat een shell-applicatie een aparte micro-frontend lazy kan inladen via **Module Federation**, en dat beide backends het **Forge framework** gebruiken, inclusief backend-to-backend communicatie via **Refit**.

Beantwoordt open vraag **Q#5**: hoe werkt cross-microservice navigatie in een shell?

---

## Structuur

```
Itenium.Forge.Poc/
└── shell/
    ├── Shell.Api/              .NET backend, poort 5100
    ├── Shell.Api.Tests/        NUnit integratie tests
    └── shell-ui/               React/Vite host, poort 3000
```

De Feature Flags microservice leeft in een aparte repo: [Itenium.Forge.FeatureFlags](https://github.com/Itenium-Forge/Itenium.Forge.FeatureFlags)

---

## Opstarten

### Vereisten

- .NET 10 SDK
- Node.js 20+
- GitHub PAT met `read:packages` scope (voor de Forge NuGet feed)
- [Itenium.Forge.FeatureFlags](https://github.com/Itenium-Forge/Itenium.Forge.FeatureFlags) repo lokaal gekloned en opgestart

### Backend

```bash
cd shell/Shell.Api && dotnet run          # http://localhost:5100
```

### Frontend

```bash
cd shell/shell-ui
npm install
npm run build
npm run preview        # http://localhost:3000
```

### Testen

```bash
cd shell && dotnet test
```

---

## Hoe werkt het?

De shell haalt bij opstart de geregistreerde apps op via `GET /apps` (Shell.Api). Die endpoint leest de remote URLs uit `appsettings.json`. Voor elke app wordt een nav-item aangemaakt.

De Feature Flags component wordt **lazy geladen** via Module Federation wanneer de gebruiker op "Feature Flags" klikt. De flags data loopt via Shell.Api als proxy naar FeatureFlags.Api.

```
Browser → shell-ui (3000)
           ├── GET /apps      → Shell.Api (5100)              nav items ophalen
           ├── lazy import    → feature-flags-ui (3001)       component laden
           └── GET /api/flags → Shell.Api (5100)              proxy
                                 └── GET /api/flags → FeatureFlags.Api (5200)  data ophalen
```

---

## Forge Framework

Shell.Api gebruikt de volgende Forge packages (v0.3.14):

| Package | Wat het doet |
|---------|-------------|
| `Itenium.Forge.Settings` | appsettings laden, `IForgeSettings` valideren, metadata loggen bij startup |
| `Itenium.Forge.Logging` | Serilog bootstrap, structured request logging, log naar bestand |
| `Itenium.Forge.Controllers` | CORS, response compression (Brotli/Gzip), camelCase JSON |
| `Itenium.Forge.HealthChecks` | `/health/live` en `/health/ready` endpoints |
| `Itenium.Forge.HttpClient` | Refit client registratie, `http-FeatureFlags` readiness check, traceparent propagatie |

### Niet (nog) gebruikt in deze POC

| Package | Reden |
|---------|-------|
| `Itenium.Forge.Security` | Geen authenticatie — bewuste scope-beperking |
| `Itenium.Forge.Swagger` | Geen API documentatie nodig |
| `Itenium.Forge.Telemetry` | Geen OpenTelemetry |

---

## Backend-to-backend: Refit via Forge.HttpClient

Shell.Api heeft een `FlagsController` die als proxy fungeert naar FeatureFlags.Api. De Refit client wordt geregistreerd via `AddForgeHttpClient<T>`, dat de base URL leest uit `appsettings.json` en een readiness health check toevoegt.

```json
"ForgeConfiguration": {
  "HttpClients": {
    "FeatureFlags": { "BaseUrl": "http://localhost:5200" }
  }
}
```

```csharp
builder.AddForgeHttpClient<IFeatureFlagsClient>("FeatureFlags");
```

---

## Module Federation — Runtime Manifest

De shell gebruikt `@module-federation/vite` op Vite 7 als host. Er zijn **geen hardcoded remote URLs** in de shell build. In plaats daarvan:

1. `main.tsx` fetcht `GET /apps` van Shell.Api vóór de React render
2. Roept `init()` aan met de ontvangen app entries — elke entry krijgt `{remoteUrl}/remoteEntry.js` als entry point
3. `App.tsx` bouwt nav en routes dynamisch op basis van de apps array
4. De naam-conventie bepaalt pad en label: `FeatureFlags` → `/feature-flags` + `Feature Flags`

Een nieuwe micro-frontend toevoegen = enkel `appsettings.json` updaten, geen shell rebuild nodig.

```json
"Apps": {
  "FeatureFlags": { "RemoteUrl": "http://localhost:3001" }
}
```

> **Opmerking:** Vite 8 (Rolldown) is niet compatibel met `@module-federation/vite` vanwege een CJS/ESM conflict in de gegenereerde virtual modules. Vite 7 (Rollup) werkt wel correct.

---

## NuGet configuratie

De Forge packages komen van GitHub Packages. Voeg een PAT toe:

```bash
dotnet nuget add source "https://nuget.pkg.github.com/Itenium-Forge/index.json" \
  --name itenium \
  --username <github-username> \
  --password <PAT-met-read:packages>
```
