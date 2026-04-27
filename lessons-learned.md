# Lessons Learned

## 1. The naming 1.convention between packages is implicit

  AddForgeHttpClient<IFeatureFlagsClient>("FeatureFlags") must match the appsettings key Forge.HttpClients.FeatureFlags exactly. There's no   compile-time check — a typo silently fails at runtime with a null base address. The same applies to the Apps section key needing to match the Module Federation container name (and that one IS case-sensitive: featureFlags ≠ FeatureFlags).
  
  ***Suggestion***: a typed options class or a named constant would make mismatches compile-errors instead of runtime surprises.

  ---
  ## 2. The IForgeSettings contract is strict but undiscoverable

  Every settings class must implement IForgeSettings and expose ForgeSettings Forge { get; } — the property name must literally be Forge.
  Nothing in the compiler or IDE tells you this until AddForgeSettings<T>() fails at startup. The ExampleApp is the only documentation.

  ***Suggestion***: an analyzer warning or a more descriptive startup exception would help onboarding.

  ---
  ## 3. AddForgeProblemDetails is split from AddForgeControllers

  You call four methods instead of two — AddForgeControllers + AddForgeProblemDetails on the builder side, UseForgeControllers +
  UseForgeProblemDetails on the app side. It's not obvious why ProblemDetails is separate. A new developer following the pattern will
  forget one and get confusing error responses without knowing why.

  ***Suggestion***: either merge ProblemDetails into AddForgeControllers, or document the reason for the split explicitly.

  ---
  ## 4. GitHub Packages feed requires manual nuget.config setup for CI

  The Forge packages live on GitHub Packages, which requires packageSourceCredentials with a %NUGET_AUTH_TOKEN% environment variable. This
  isn't included by default when you add the nuget.config — CI fails silently on restore until you add it manually.

  ***Suggestion***: ship a nuget.config template in the Forge repo or document the CI setup step prominently.

  ---
  ## 5. The public partial class Program pragma suppression is friction

```
  #pragma warning disable S1118 // needed for WebApplicationFactory<Program> in tests
  public partial class Program { }
```

  This is required boilerplate in every service that has integration tests, and it comes with a SonarAnalyzer warning that needs
  suppression. It's a leaky concern — test infrastructure bleeds into production code.

  ***Suggestion***: document this pattern in Forge's testing guide, or provide a ForgeWebApplicationFactory<T> base class that handles it.

  ---
  ## 6. The startup pattern is more boilerplate than minimal API

  The try/catch/finally around the whole Program.cs is the correct pattern for capturing fatal startup errors via Serilog, but it's
  meaningfully more code than a standard minimal API. Developers unfamiliar with Serilog's two-stage initialization won't understand why
  it's structured this way.

  ***Suggestion***: a code snippet or project template (dotnet new forge-api) would eliminate copy-paste errors and make the pattern
  self-documenting.

  ---
  ## Overall

  The framework is coherent and consistent — once you understand one service, the second one is nearly identical. The main friction is
  discoverability: the right way to do things lives in the ExampleApp, not in the package APIs themselves. Better error messages, an
  analyzer, or a project template would significantly reduce the onboarding curve for new microservices.



# **Lesson 1: Implicit naming conventions between packages**

The problem in detail  
There are actually three separate naming systems that must align manually:

| appsettings.json | C# code | Module Federation |
| --- | --- | --- |
| Forge:  <br>        HttpClients:  <br>                FeatureFlags: "http://..." | AddForgeHttpClient<t>("FeatureFlags")</t> |     |
| Apps:  <br>        featureFlags:   <br>                RemoteUrl: "http://..." | (read by /apps endpoint) | federation({ name: 'featureFlags' }) |

&nbsp;

All three must match, none are validated at startup, and the casing rules differ (FeatureFlags for HttpClients, featureFlags for  
federation). A typo gives you either a null HttpClient base address or an empty page with a cryptic RUNTIME-001 error in the browser.

## Detailed solution

**Option A — Typed options with validation (minimal change to Forge)**

Instead of a free-form string key, introduce a typed registration:

```
  // In Forge.HttpClients
  public static IHttpClientBuilder AddForgeHttpClient<TClient>(
      this WebApplicationBuilder builder,
      Expression<Func<ForgeHttpClients, string>> selector) // e.g. s => s.FeatureFlags
```

The ForgeHttpClients class is bound from appsettings.json, so the key is validated at startup via DataAnnotations or IValidateOptions<t>.</t>  
<t>If Forge.HttpClients.FeatureFlags is missing, you get a descriptive OptionsValidationException at startup, not a null reference at the</t>  
<t>first HTTP call.</t>

**Option B — Startup validation (simplest, no API change)**

Add a Forge convention: AddForgeHttpClient<t>("key") immediately verifies the key exists in configuration and throws an</t>  
<t>InvalidOperationException with a message like:</t>

▎ Missing configuration key 'Forge:HttpClients:FeatureFlags'. Add it to appsettings.json.

This is a one-line check inside AddForgeHttpClient and catches the problem at startup instead of at runtime.

**Option C — Source generator (most robust)**

A Roslyn source generator reads the appsettings.json at build time and emits a strongly-typed ForgeConfig class with properties for each  
key. You then write:

`builder.AddForgeHttpClient<IFeatureFlagsClient>(ForgeConfig.HttpClients.FeatureFlags);`

This is a compile-time check. The tradeoff is significant complexity in the Forge tooling.

***Recommended***: Option B immediately, Option A as a follow-up. Option C is ideal long-term but expensive to build.

* * *

# **Lesson 2: IForgeSettings contract is undiscoverable**

## The problem in detail

The contract looks like this in the Forge source:

```dotnet
  public interface IForgeSettings
  {
      ForgeSettings Forge { get; }
  }
```

When you implement it, nothing tells you:

- The property must be named exactly Forge (not ForgeConfig, not Settings)
- It must have a get initializer, not just { get; set; }
- AddForgeSettings<t>() reads configuration.Get<t>() which silently returns a default instance if the binding fails</t></t>

The failure mode is subtle: if you name the property Config instead of Forge, the CORS origins and HttpClient base URLs silently default  
to empty strings. No exception, no warning — the app just doesn't work as expected.

## Detailed solution

**Immediate: Better exception message in AddForgeSettings**

```dotnet
  public static T AddForgeSettings<T>(this WebApplicationBuilder builder)
      where T : class, IForgeSettings, new()
  {
      var settings = builder.Configuration.Get<T>() ?? new T();

      if (string.IsNullOrEmpty(settings.Forge.Hosting.Port.ToString()))
          throw new ForgeConfigurationException(
              $"Forge settings not found. Ensure appsettings.json contains a 'Forge' section " +
              $"and that {typeof(T).Name} has a property named 'Forge' of type ForgeSettings.");

      builder.Services.AddSingleton(settings);
      return settings;
  }
```

**Better: Roslyn analyzer**

A custom Roslyn analyzer checks any class implementing IForgeSettings:

warning FORGE001: 'ShellSettings' implements IForgeSettings but property 'Forge'  
is not of type ForgeSettings. Configuration binding will silently fail.

This surfaces the mistake in the IDE before the app even runs. Writing a Roslyn analyzer for a simple property-name check is  
approximately 1-2 days of work.

**Better: Abstract base class instead of interface**

```
  public abstract class ForgeSettingsBase
  {
      public ForgeSettings Forge { get; } = new();
  }
```

```
  // Usage — no contract to misremember:
  public class ShellSettings : ForgeSettingsBase
  {
      public string MyCustomSetting { get; set; } = "";
  }
```

An abstract base class enforces the Forge property at compile time and provides the default instance. The downside is you lose the  
flexibility of implementing IForgeSettings on a class that already inherits something else — but in practice Forge settings classes never  
inherit anything else.

***Recommended***: abstract base class (eliminates the problem entirely) + the better exception message as a safety net during migration.

* * *

# **Lesson 3: AddForgeProblemDetails is split from AddForgeControllers**

## The problem in detail

The startup pattern requires 4 calls instead of 2:

```
  builder.AddForgeControllers();
  builder.AddForgeProblemDetails();   // ← easy to forget

  app.UseForgeControllers();
  app.UseForgeProblemDetails();       // ← easy to forget, and order matters
```

The split means a developer can have working controllers with broken error responses, or correct error responses that aren't applied  
because UseForgeProblemDetails() was forgotten. The middleware order also matters — UseForgeProblemDetails must come before  
UseForgeControllers — but nothing enforces this.

When forgotten, unhandled exceptions produce a raw 500 with a full stack trace in development instead of a clean RFC 7807 response. In  
production the stack trace is suppressed, but the response format is inconsistent.

## Detailed solution

**Option A — Merge ProblemDetails into ForgeControllers**

The simplest fix: ProblemDetails is not optional for an API, so include it unconditionally:

```
  public static void AddForgeControllers(this WebApplicationBuilder builder)
  {
      // existing: MVC, JSON, CORS, compression
      builder.Services.AddProblemDetails();  // ← merged in
      builder.Services.Configure<ProblemDetailsOptions>(opts => { /* Forge defaults */ });
  }
```

```
  public static void UseForgeControllers(this WebApplication app)
  {
      app.UseExceptionHandler();  // ← uses ProblemDetails automatically
      app.UseStatusCodePages();
      // existing: routing, CORS, endpoints
  }
```

This reduces the API surface to 2 calls and makes the correct behavior the default.

**Option B — Builder pattern with validation**

If ProblemDetails must remain opt-in for legitimate reasons (e.g. some services are not HTTP APIs), use a builder that validates  
completeness:

```
  builder.AddForge(forge => forge
      .AddControllers()
      .AddProblemDetails()
      .AddHealthChecks());

  app.UseForge(); // applies all registered middleware in the correct order
```

The UseForge() call applies middleware in the Forge-prescribed order internally, removing the ordering problem entirely.

**Option C — Startup validation**

At app.Build() time, check that if AddForgeProblemDetails was called, UseForgeProblemDetails was also called:

```
  // Forge registers a marker in DI:
  builder.Services.AddSingleton<ForgeProblemDetailsMarker>();

  // UseForgeControllers checks for the marker:
  if (app.Services.GetService<ForgeProblemDetailsMarker>() != null
      && !app.Properties.ContainsKey("ForgeProblemDetailsUsed"))
  {
      throw new InvalidOperationException(
          "AddForgeProblemDetails() was called but UseForgeProblemDetails() was not. " +
          "Call app.UseForgeProblemDetails() before app.UseForgeControllers().");
  }
```

***Recommended:*** Option A (merge) — ProblemDetails is not a meaningful opt-in for an HTTP API. If there's a specific reason it was split out,  
Option B (builder pattern) gives the cleanest API surface and solves the ordering problem.

* * *

# **Lesson 4: GitHub Packages feed requires manual CI setup**

## The problem in detail

The nuget.config that developers add to consume Forge packages looks like this:

```
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/Itenium-Forge/index.json" />
  </packageSources>
```

This works locally if the developer is authenticated via dotnet nuget add source or a ~/.nuget/NuGet/NuGet.Config with their PAT. But in  
CI (GitHub Actions), there is no user-level config. dotnet restore silently falls back to trying the feed unauthenticated, which GitHub  
rejects with a 401. The error message from MSBuild (error MSB1003) doesn't mention authentication — it says the project file isn't found,  
which is deeply misleading.

The fix — adding packageSourceCredentials to nuget.config — is a non-obvious step that isn't in any Forge documentation:

```
  <packageSourceCredentials>
    <github>
      <add key="Username" value="x-access-token" />
      <add key="ClearTextPassword" value="%NUGET_AUTH_TOKEN%" />
    </github>
  </packageSourceCredentials>
```

And the GitHub Actions workflow needs:

```
  - name: Restore dependencies
    run: dotnet restore
    env:
      NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

## Detailed solution

**Immediate: Ship a complete nuget.config template**

Include a ready-to-use nuget.config in the Forge repository (or its documentation) that already contains the packageSourceCredentials  
block with the %NUGET_AUTH_TOKEN% placeholder. Developers copy the whole file, not just the packageSources section.

**Better: A GitHub Actions reusable workflow**

Forge publishes a reusable workflow at .github/workflows/forge-build.yml in the Forge repo:

### In consuming repo:

```
  jobs:
    build:
      uses: Itenium-Forge/workflows/.github/workflows/forge-build.yml@main
      secrets:
        NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

The reusable workflow handles restore, build, test, and code analysis with the correct authentication already wired in. Consuming repos  
get CI for free with 5 lines.

**Better: Public NuGet feed**

If the packages aren't confidential, publish to nuget.org. The authentication problem disappears entirely — dotnet restore works  
everywhere without any nuget.config. GitHub Packages is well-suited for private packages tied to an org, but it adds friction for every  
new consumer.

***Recommended***: reusable workflow (eliminates per-repo CI boilerplate and the auth problem at once). Pair it with a complete nuget.config  
template for local development.

* * *

# **Lesson 5: public partial class Program pragma suppression**

## The problem in detail

ASP.NET's WebApplicationFactory <tentrypoint>requires access to the Program class from the test project. In minimal API style, Program is</tentrypoint>  
<tentrypoint>an implicit top-level class and is internal by default. The standard workaround is:</tentrypoint>

```
  // At the bottom of Program.cs in the API project:
  #pragma warning disable S1118
  public partial class Program { }
```

This has two problems:

1.  SonarAnalyzer S1118 fires because Program has no accessible constructor (it's a utility class in Sonar's view). The pragma suppression  
    is noise in every production Program.cs.
2.  The concern is inverted — test infrastructure is leaking a requirement into production code. Program.cs shouldn't know or care that  
    tests exist.

## Detailed solution

**Immediate: Move the partial class to the test project**

.NET 9+ allows the partial class declaration in the test assembly itself via InternalsVisibleTo:

In Shell.Api.csproj:

```
  <ItemGroup>
    <InternalsVisibleTo Include="Shell.Api.Tests" />
  </ItemGroup>
```

In Shell.Api.Tests/ (not in Shell.Api/):

```
  // TestEntryPoint.cs — lives in the test project, not the API
  public partial class Program { }
```

This removes the pragma and the partial class from production code entirely.

**Better: Forge provides ForgeWebApplicationFactory**

```
  // In Itenium.Forge.Testing (new package):
  public class ForgeWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
      where TProgram : class
  {
      protected override void ConfigureWebHost(IWebHostBuilder builder)
      {
          builder.UseEnvironment("Testing");
          // Forge defaults: suppress Serilog output, use in-memory config, etc.
      }
  }
```

Consuming test projects inherit from this instead of WebApplicationFactory <program>directly. The InternalsVisibleTo approach still</program>  
<program>applies, but the base class handles the common ConfigureWebHost setup that every Forge service repeats (environment, logging</program>  
<program>suppression).</program>

This also solves the copy-paste pattern visible across ShellFactory.cs and FeatureFlagsFactory.cs — both have identical  
UseEnvironment("Testing") boilerplate.

***Recommended***: InternalsVisibleTo immediately (zero friction, removes the pragma), ForgeWebApplicationFactory <t>as a proper package to</t>  
<t>eliminate test boilerplate.</t>

* * *

# **Lesson 6: Startup pattern is more boilerplate than minimal API**

## The problem in detail

A standard minimal API Program.cs is ~10 lines. A Forge Program.cs is ~45 lines, with the bulk being the try/catch/finally and the  
Serilog two-stage initialization:

```
  Log.Logger = LoggingExtensions.CreateBootstrapLogger();  // stage 1: before DI

  try
  {
      var builder = WebApplication.CreateBuilder(args);
      builder.AddForgeLogging();  // stage 2: Serilog via DI
      // ...
      await app.RunAsync().ConfigureAwait(false);
  }
  catch (Exception ex)
  {
      Log.Fatal(ex, "Application terminated unexpectedly");
  }
  finally
  {
      await Log.CloseAndFlushAsync().ConfigureAwait(false);
  }
```

The pattern is correct — it ensures startup exceptions are logged and Serilog is flushed before the process exits. But a developer who  
doesn't know Serilog's two-stage initialization doesn't understand why any of this exists, and is likely to simplify it incorrectly (e.g.  
removing the try/catch, losing fatal startup error logging).

## Detailed solution

**Immediate: dotnet new project template**

A Forge project template installs via:

dotnet new install Itenium.Forge.Templates  
dotnet new forge-api -n MyService

It generates the complete Program.cs, appsettings.json, nuget.config, .github/workflows/build.yaml, and a settings class — all pre-wired.  
Developers never write the boilerplate from scratch and never copy it incorrectly from the ExampleApp.

**Better: ForgeApplication host abstraction**

Forge wraps the entire pattern into a single call:

```
  // Program.cs — the entire file:
  await ForgeApplication.RunAsync<ShellSettings>(args, (builder, settings) =>
  {
      builder.AddForgeControllers();
      builder.AddForgeHealthChecks();
      builder.AddForgeHttpClient<IFeatureFlagsClient>("FeatureFlags");
  }, app =>
  {
      app.MapGet("/", () => "Hello World");
      app.MapGet("/apps", ...);
  });
```

ForgeApplication.RunAsync handles bootstrap logger, try/catch/finally, AddForgeSettings, AddForgeLogging, UseForgeLogging,  
UseForgeControllers, and CloseAndFlushAsync internally. The consuming code only specifies what's unique to that service.

The tradeoff: the pipeline becomes less transparent, and advanced customization (custom middleware order, conditional registration)  
requires escape hatches. These can be provided as overloads.

***Recommended***: project template immediately (zero runtime change, high discoverability value). ForgeApplication abstraction as a V2 if the  
number of Forge services grows — the boilerplate cost compounds with each new service.



\# How much is solved with a project template

| #   | Lesson | Template verdict | Why |
| --- | --- | --- | --- |
| 1   | Implicit naming conventions | Partial | &nbsp;Template ships with correct names pre-wired for the initial client. Adding a second HttpClient later still has no validation — typo risk remains. |
| 2   | *IForgeSettings* undiscoverable | Partial | Template generates a correctly implemented settings class. But if someone copies it to a new project manually or renames the *Forge* property, the problem returns. |
| 3   | *AddForgeProblemDetails* split | Partial | Template includes all 4 calls in the correct order. Forgetting one when modifying an existing service is still possible. |
| 4   | GitHub Packages CI auth | **Fully solved** | &nbsp;It's a pure file-content problem — template ships *nuget.config* + *build.yaml*  already correct, nothing left to get wrong. |
| 5   | *partial class Program* pragma | **Fully solved** | &nbsp;Template generates the test project alongside the API project with *InternalsVisibleTo* already set up. |
| 6   | Startup boilerplate | **Fully solved** | &nbsp;Template generates the complete *Program.cs*. Developers never write or copy the try/catch/Serilog pattern manually. |

---

# **Lesson 7: The "Duplicate DTO" Problem**

## The problem in detail
Models like `Flag.cs` are duplicated across the Microservice (`FeatureFlags.Api`) and the Gateway (`Shell.Api`). When a property changes in the service, the Gateway silently breaks until the DTO is manually updated.

## Detailed solution
*   **Option A: Shared Contract Projects**: Standardize on a `.Contracts` NuGet package for every service.
*   **Option B: Source Generators**: Generate clients and DTOs in the Gateway directly from the Microservice's assembly or OpenAPI spec.
*   **Option C: Git Submodules**: Use git-level sharing (generally discouraged for DX).
*   **🏆 Recommendation**: **Option A** for POC, **Option B** for Production.

# **Lesson 8: Middleware Order Fragility**

## The problem in detail
`UseForgeSecurityHeaders`, `UseForgeLogging`, and `UseForgeProblemDetails` must be called in a specific sequence to work correctly. There is no validation to prevent a developer from putting the Logger before the Error Handler.

## Detailed solution
*   **Option A: Unified Middleware**: Provide `app.UseForgeDefaults()` that wraps all of them in the correct order.
*   **Option B: Order Verification**: Register a "Middleware Sequence" list in DI and have each middleware check if its predecessors were called.
*   **Option C: Documentation**: Rely on the Project Template (Lesson 6).
*   **🏆 Recommendation**: **Option A**.

# **Lesson 9: Remote Discovery Boilerplate**

## The problem in detail
`Shell.Api` contains manual logic to expose an `/apps` endpoint so the React frontend can discover Module Federation remotes. This is a recurring pattern for every Shell.

## Detailed solution
*   **Option 1: AddForgeRemoteDiscovery()**: A framework method that automatically maps a standard `Apps` config section to a `/api/discovery` endpoint.
*   **Option 2: Config Helper**: Provide a helper to map the section but keep the endpoint manual.
*   **Option 3: API Gateway**: Use YARP or Ocelot (too heavy for current POC).
*   **🏆 Recommendation**: **Option 1**.

# **Lesson 10: CORS Propagation Pain**

## The problem in detail
When the Shell UI port changes (e.g., from 3000 to 3005), every single microservice's `appsettings.json` must be updated manually.

## Detailed solution
*   **Option 1: Wildcard/Pattern Dev CORS**: Support `*.itenium.be` or `localhost:*` patterns in Development environments via Forge settings.
*   **Option 2: Centralized Config**: Use a shared `forge-settings.json` file across repos (Lesson 13).
*   **Option 3: Gateway Header**: Have the Gateway strip/add CORS headers (advanced).
*   **🏆 Recommendation**: **Option 1**.

# **Lesson 11: Refit Exception Masking**

## The problem in detail
When a microservice returns a validation error (400 Problem Details), the Shell catches a generic `ApiException`. The specific "Field X is required" message is lost to the frontend.

## Detailed solution
*   **Option 1: Refit ProblemDetails Handler**: Add a global delegating handler that unwraps `ApiException` and re-throws the inner `ProblemDetails`.
*   **Option 2: Manual Catching**: Catch every Refit call in the controller (Boilerplate heavy).
*   **Option 3: ProblemDetails Propagation**: Use a custom `JsonConverter` to ensure `ProblemDetails` survives the trip.
*   **🏆 Recommendation**: **Option 1**.


