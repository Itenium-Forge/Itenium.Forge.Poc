# Itenium Forge: Implementation & Setup Guide

This document describes the architecture of the current Proof of Concept (POC) and provides a blueprint for setting up new applications using the **Itenium Forge Core** framework.

---

## 1. The Proof of Concept (POC) Architecture

The POC demonstrates a **Micro-Frontend (MFE)** architecture combined with a **Backend-for-Frontend (BFF)** pattern.

### Components
*   **Shell UI (Port 3005)**: The host application. It discovers remotes at runtime and lazily loads them.
*   **Shell API (Port 5100)**: The BFF. It manages the list of available apps and proxies requests to downstream microservices.
*   **Feature Flags UI (Port 3001)**: A remote micro-frontend.
*   **Feature Flags API (Port 5200)**: A downstream microservice using Forge Core features.
*   **Demo UI (Port 3002)**: A secondary remote demonstrating zero-code discovery.
*   **Observability Stack (Ports 3000/3100)**: Grafana, Loki, Tempo, and Mimir (LGTM stack).

### Key Workflows
1.  **Runtime Discovery**: The Shell UI fetches its configuration from `Shell.Api/apps` at startup. No URLs are hardcoded in the frontend build.
2.  **Distributed Tracing**: A request from the UI to the Shell API is assigned a `traceparent`. This header is propagated to the Feature Flags API via Forge's `HttpClient` handler.
3.  **Unified Styling**: Remotes detect if they are "Standalone" or "Remote" and hide their own headers/padding when embedded in the shell.

---

## 2. Setting Up the Existing POC

To get the complete ecosystem running on a new machine:

### Prerequisites
*   **.NET 10 SDK**
*   **Node.js 20+**
*   **Podman or Docker Desktop** (for observability)
*   **GitHub PAT**: A Personal Access Token with `read:packages` scope.

### Step 1: Start the Observability Stack
```powershell
cd Itenium.Forge.Core
podman compose up -d
```

### Step 2: Start the Backends
Run these in separate terminals:
```powershell
# Shell API
cd Itenium.Forge.Poc/shell/Shell.Api
dotnet run

# Feature Flags API
cd Itenium.Forge.FeatureFlags/FeatureFlags.Api
dotnet run
```

### Step 3: Start the Frontends
```powershell
# Shell UI
cd Itenium.Forge.Poc/shell/shell-ui
npm run preview

# Feature Flags UI
cd Itenium.Forge.FeatureFlags/feature-flags-ui
npm run preview
```

---

## 3. Creating a New "Forge-Ready" Application

Follow these steps to build a new microservice that integrates perfectly with the Forge ecosystem.

### Step 1: Project Setup
Create a new ASP.NET Core Web API and add the standard Forge NuGet configuration (`nuget.config`).

### Step 2: Add Core Dependencies
Add the following packages to your `.csproj` (ensure Central Package Management is used):
*   `Itenium.Forge.Settings`
*   `Itenium.Forge.Logging`
*   `Itenium.Forge.Controllers`
*   `Itenium.Forge.Telemetry`
*   `Itenium.Forge.HealthChecks`

### Step 3: Configure `Program.cs`
Implement the "Two-Stage Initialization" pattern:

```csharp
using Itenium.Forge.Logging;
using Itenium.Forge.Settings;
// ... other usings

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try {
    var builder = WebApplication.CreateBuilder(args);
    
    // 1. Forge Registrations
    builder.AddForgeSettings<MySettings>();
    builder.AddForgeLogging();
    builder.AddForgeControllers();
    builder.AddForgeProblemDetails();
    builder.AddForgeTelemetry();
    builder.AddForgeHealthChecks();

    var app = builder.Build();

    // 2. Middleware Pipeline (Order Matters!)
    app.UseForgeSecurityHeaders();
    app.UseForgeProblemDetails();
    app.UseForgeLogging();
    app.UseForgeTelemetry();
    app.UseForgeControllers();
    app.UseForgeHealthChecks();

    await app.RunAsync();
}
catch (Exception ex) {
    Log.Fatal(ex, "Start-up failed");
}
finally {
    await Log.CloseAndFlushAsync();
}
```

### Step 4: Add Configuration
Update your `appsettings.json`:
```json
{
  "Hosting": {
    "CorsOrigins": "http://localhost:3005" 
  },
  "Forge": {
    "ServiceName": "MyNewService",
    "Application": "MyService",
    "Environment": "Development"
  },
  "ForgeConfiguration": {
    "Logging": { "LokiUrl": "http://localhost:3100" },
    "Telemetry": { "OtlpEndpoint": "http://localhost:4317" }
  }
}
```

### Step 5: Register as a Remote (MFE)
1.  **UI**: In your Vite config, use `@module-federation/vite` to expose your main component.
2.  **Shell API**: Add your new UI URL to the `Apps` section of `Shell.Api/appsettings.json`.
3.  **Discovery**: Refresh the Shell UI; your new module will appear in the navigation automatically.

---

## 4. Key Implementation Lessons
*   **Always use `IForgeSettings`**: Never use `builder.Configuration["Key"]` directly.
*   **Centralize Models**: Use the `Itenium.Forge.Core` package for shared structures like `ForgePagedResult`.
*   **Watch Mode**: Use `dotnet watch run --no-hot-reload` when developing to ensure JSON configuration changes are always picked up correctly.
