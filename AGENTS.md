# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project overview

Blazor WebAssembly app for testing .NET regular expressions in the browser (hosted at https://dotnet-regex.com/). All regex work runs client-side in WASM; the ASP.NET Core server only hosts the static client.

Target framework: **.NET 11** (`net11.0`) for all projects.

## Layout

- `BlazorWasmRegex/Client/` — Blazor WASM UI. Main component is `Shared/RegexTester.razor` (pattern input, options, samples, result tabs, replace, export, local-storage persistence). DI registration in `Program.cs` (`ConfigureServices`).
- `BlazorWasmRegex/Shared/` — UI-independent logic:
  - `Services/RegexService.cs` (`IRegexService`) — match/split/replace helpers.
  - `Services/HtmlHelperService.cs` (`IHtmlHelperService`) — match highlighting markup.
  - `Services/SampleRegexLibrary.cs` — sample patterns + sample test data.
- `BlazorWasmRegex/Server/` — ASP.NET Core host; `Dockerfile` (x64) and `Dockerfile-Arm`. Containers expose ports 8080/8081.
- `tests/RegexService.Tests/` — xUnit tests for the Shared services.
- `build/` — NUKE build project (`Build.cs`), invoked via `build.sh` / `build.ps1` / `build.cmd`.
- `docs/release-notes/` — per-upgrade/release notes (what changed, why, how it was verified, known issues).

## Common commands

```bash
dotnet build BlazorWasmRegex2.sln
dotnet test tests/RegexService.Tests
dotnet run --project BlazorWasmRegex/Server   # run app locally

./build.sh -Target Test       # NUKE: compile + run all *.Tests projects
./build.sh -Target Publish    # publish server+client to .artifacts/publish
```

Docker image targets (`BuildDockerContainer`, `BuildArmDockerContainer`, `Push*`) are used by CI (`.github/workflows/create-images.yml`) and push to ghcr.io.

## Conventions and gotchas

- Put testable logic in `BlazorWasmRegex/Shared` services behind an interface, register it in `Client/Program.cs`, and cover it with tests in `tests/RegexService.Tests`. Keep `.razor` components thin.
- Regexes are built by `IRegexOptionsService.TryCreateRegex` (`Shared/Services/RegexOptionsService.cs`), which requires a match timeout. `RegexTester.razor` passes its 2 s `RegexTimeout` to guard against catastrophic backtracking and catches `RegexMatchTimeoutException`. Keep the timeout required in `TryCreateRegex` (no overload without it), make every caller pass it, and force any lazy regex work (`MatchCollection`, `Split` results) inside the `try` block so timeouts are not thrown during rendering.
- User state (pattern, tests, options) is persisted in browser `localStorage` through `ILocalStorageService` (`Shared/Services/LocalStorageService.cs`, with the `Client/Services/JsBrowserStorage.cs` JS interop wrapper). It replaced `Blazored.LocalStorage` and keeps its on-disk format: each value is stored as a JSON-encoded string, and regex options are a JSON object inside that string. Keep stored formats backward compatible or handle migration. Storage failures are swallowed, so the app works without persistence.
- UI uses BlazorStrap (Bootstrap 5).
- `SetVersion` / `InsertCounterCode` build targets temporarily rewrite `Directory.Build.props`, `wwwroot/service-worker.published.js`, and `wwwroot/index.html`, then restore them. Don't commit those generated changes; `Directory.Build.props` is generated from `Directory.build.props.template` and is not gitignored, so check `git status` after running these targets.
- Dependabot opens NuGet update PRs; keep package versions consistent across projects.
- Update `README.md` when user-facing features change.
