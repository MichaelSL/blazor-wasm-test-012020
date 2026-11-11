# Blazor WASM Regex Tester - AI Agent Instructions

## Project Overview
This is a Blazor WebAssembly application that runs a regex tester entirely in the browser. It uses the ASP.NET Core hosted model with three projects: Client (WASM), Server (host), and Shared (common code/services).

**Live Demo**: https://dotnet-regex.com/  
**Key Characteristic**: All regex processing happens client-side in WebAssembly - the server only hosts static files.

## Architecture

### Project Structure
```
BlazorWasmRegex/
├── Client/          # Blazor WASM app (runs in browser)
├── Server/          # ASP.NET Core host (serves static files)
└── Shared/          # Shared interfaces and services
```

### Service Pattern
The codebase uses a **shared service pattern** where services are defined in `Shared/` and used by both Client and Server:

- **Interfaces**: Define contracts in `Shared/Interfaces/` (e.g., `IRegexService`, `IHtmlHelperService`)
- **Implementations**: Implement in `Shared/Services/` (e.g., `RegexService`, `HtmlHelperService`)
- **Registration**: Services are registered in `Client/Program.cs` using `AddTransient` for DI

Example from [Client/Program.cs](BlazorWasmRegex/Client/Program.cs):
```csharp
services.AddTransient<IRegexService, RegexService>();
services.AddTransient<IHtmlHelperService, HtmlHelperService>();
```

### Key Components
- **RegexTester.razor**: Main component handling regex testing UI and logic
- **RegexService**: Executes regex operations (matches, splits) with timing
- **HtmlHelperService**: Generates HTML-safe markup with highlighting (using `WebUtility.HtmlEncode`)

## Development Workflows

### Building & Running
```bash
# Standard build (from repo root)
dotnet build

# Run server (serves WASM app)
dotnet run --project BlazorWasmRegex/Server/BlazorWasmRegex.Server.csproj

# Tests
dotnet test tests/RegexService.Tests
```

### CI and images
There is no build tool; GitHub Actions calls `dotnet` and Docker directly:
- `.github/workflows/create-images.yml`: runs the tests, then builds the x64 (`Dockerfile`) and ARM (`Dockerfile-Arm`, `linux/arm/v7`) images and pushes them to `ghcr.io/<owner>/regex-tester:x64` / `:arm` (with a `-<branch>` suffix off master). Pull requests build but don't push.
- `.github/workflows/cleanup-images.yml`: deletes a branch's image tags when the branch is deleted, and weekly removes untagged versions (>7 days) and branch tags (>30 days).

**Version Management**: `Directory.Build.props` holds a default `Version` of `0.0.0.1`. CI passes `VERSION={Year}.{Month}.{Day}.{GITHUB_RUN_NUMBER}` and `SOURCE_REV=<short sha>` as Docker build args, which become `-p:Version` / `-p:SourceRevisionId`. The nav menu shows the informational version, e.g. `2026.11.10.57+a1b2c3d`.

## Code Conventions

### Blazor Component Patterns
1. **State Persistence**: Uses an in-repo `ILocalStorageService` (Blazored.LocalStorage-compatible format, JS interop to `localStorage`) to persist regex and test inputs between sessions
   - Keys: `REGEX_SESSION_STORAGE_KEY`, `TESTS_SESSION_STORAGE_KEY`
   
2. **Regex Caching**: Compiles regex once and reuses (`prevRegexConfig`, a `(pattern, options)` tuple, prevents recompilation; it is reset when a build fails)

3. **Error Handling**: Catches both `RegexParseException` (modern) and `ArgumentException` (fallback) for regex parsing

4. **HTML Safety**: Always use `WebUtility.HtmlEncode` when displaying user input in markup (see `HtmlHelperService`)

### UI Framework
- **BlazorStrap**: Bootstrap components (e.g., `<BSButton>`, `<BSTabGroup>`)
- **Open Iconic**: Icon font (e.g., `oi oi-beaker`, `oi oi-terminal`)

### Target Framework
Currently targeting **net10.0** (.NET 10) across all projects - check project files when adding dependencies.

## Integration Points

### Service Worker (PWA)
The app is a Progressive Web App with offline support:
- `service-worker.js`: Development service worker
- `service-worker.published.js`: Production version (the Docker build appends `// build: <commit>` to the container copy for cache busting)

### External Dependencies
- **BlazorStrap** (v5.2.0): Bootstrap UI components

## Common Tasks

### Adding a New Service
1. Define interface in `Shared/Interfaces/I{ServiceName}.cs`
2. Implement in `Shared/Services/{ServiceName}.cs`
3. Register in `Client/Program.cs` ConfigureServices method
4. Inject via `@inject` directive in Razor components

### Modifying Regex Logic
Edit [RegexService.cs](BlazorWasmRegex/Shared/Services/RegexService.cs) - methods return tuples including timing data for performance display.

### UI Changes
Main UI is in [RegexTester.razor](BlazorWasmRegex/Client/Shared/RegexTester.razor) - uses three tabs: Matches, Split list, and Table view.

## Testing
Currently **no automated tests** exist (see `Build.cs` Test target). 

**Planned**: Implement automated tests in a new `tests/` directory following the solution structure. When adding tests, consider:
- Unit tests for `RegexService` and `HtmlHelperService`
- Component tests for `RegexTester.razor`
- Integration tests for the full regex workflow

## Future Development
- **Testing Infrastructure**: Add comprehensive test coverage (unit, component, and integration tests)
- **UI/UX Enhancements**: Build a more robust regex tester interface with improved usability and features

## Local Development
This is a **test/demo application** intended for local development. The build system includes Docker and CI/CD features, but primary development is done locally using `dotnet build` and `dotnet run`.
