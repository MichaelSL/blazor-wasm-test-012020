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

# Using NUKE build system
./build.sh Compile    # Linux/Mac
./build.cmd Compile   # Windows
```

### NUKE Build System
This project uses **NUKE** for advanced build automation ([build/Build.cs](build/Build.cs)):

Key targets:
- `Compile`: Standard build
- `Publish`: Publishes to `.artifacts/publish/`
- `SetVersion`: Injects version from `Directory.build.props.template` (replaces `$ver` placeholder)
- `BuildDockerContainer` / `BuildArmDockerContainer`: Build Docker images
- `InsertCounterCode`: Injects analytics code at `<!-- Web Counter here -->` placeholder in `index.html`

**Version Management**: Versions are generated dynamically: `{Year}.{Month}.{Day}.{BuildNumber}` from `GITHUB_RUN_NUMBER` env var.

### Docker Deployment
Two Dockerfiles for multi-arch support:
- `Dockerfile`: x64 builds (uses .NET 7 Alpine images)
- `Dockerfile-Arm`: ARM builds

**Important**: `Directory.Build.props` is dynamically generated during build - never edit it directly. Modify `Directory.build.props.template` instead.

## Code Conventions

### Blazor Component Patterns
1. **State Persistence**: Uses `Blazored.LocalStorage` to persist regex and test inputs between sessions
   - Keys: `REGEX_SESSION_STORAGE_KEY`, `TESTS_SESSION_STORAGE_KEY`
   
2. **Regex Caching**: Compiles regex once and reuses (`prevRegexText` tracking prevents recompilation)

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
- `service-worker.published.js`: Production version (NUKE build appends timestamp for cache busting)

### External Dependencies
- **BlazorStrap** (v5.2.0): Bootstrap UI components
- **Blazored.LocalStorage** (v4.5.0): Browser storage API

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
