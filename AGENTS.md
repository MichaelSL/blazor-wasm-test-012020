# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project overview

Blazor WebAssembly app for testing .NET regular expressions in the browser (hosted at https://dotnet-regex.com/). All regex work runs client-side in WASM; the ASP.NET Core server only hosts the static client.

Target framework: **.NET 10** (`net10.0`) for all projects.

## Layout

- `BlazorWasmRegex/Client/` — Blazor WASM UI. Main component is `Shared/RegexTester.razor` (pattern input, options, samples, result tabs, replace, export, local-storage persistence). DI registration in `Program.cs` (`ConfigureServices`).
- `BlazorWasmRegex/Shared/` — UI-independent logic:
  - `Services/RegexService.cs` (`IRegexService`) — match/split/replace helpers.
  - `Services/HtmlHelperService.cs` (`IHtmlHelperService`) — match highlighting markup.
  - `Services/SampleRegexLibrary.cs` — sample patterns + sample test data.
- `BlazorWasmRegex/Server/` — ASP.NET Core host; `Dockerfile` (x64) and `Dockerfile-Arm`. Containers expose ports 8080/8081.
- `tests/RegexService.Tests/` — xUnit tests for the Shared services.
- `build/` — NUKE build project (`Build.cs`), invoked via `build.sh` / `build.ps1` / `build.cmd`.
- `docs/` — design notes for past improvements.

## Common commands

```bash
dotnet build BlazorWasmRegex2.sln
dotnet test tests/RegexService.Tests
dotnet run --project BlazorWasmRegex/Server   # run app locally

./build.sh -Target Test       # NUKE: compile + run all *.Tests projects
./build.sh -Target Publish    # publish server+client to artifacts/publish
```

Docker image targets (`BuildDockerContainer`, `BuildArmDockerContainer`, `Push*`) are used by CI (`.github/workflows/create-images.yml`) and push to ghcr.io.

## Conventions and gotchas

- Put testable logic in `BlazorWasmRegex/Shared` services behind an interface, register it in `Client/Program.cs`, and cover it with tests in `tests/RegexService.Tests`. Keep `.razor` components thin.
- Regex objects are constructed in `RegexTester.razor` with a 2 s `RegexTimeout` to guard against catastrophic backtracking (`RegexMatchTimeoutException` is caught there) — preserve it when changing how regexes are built.
- User state (pattern, tests, options) is persisted with `Blazored.LocalStorage`; regex options are stored as JSON. Keep stored formats backward compatible or handle migration.
- UI uses BlazorStrap (Bootstrap 5).
- `SetVersion` / `InsertCounterCode` build targets temporarily rewrite `Directory.Build.props`, `wwwroot/service-worker.published.js`, and `wwwroot/index.html`, then restore them. Don't commit those generated changes; `Directory.Build.props` is generated from `Directory.build.props.template` and is not gitignored, so check `git status` after running these targets.
- Dependabot opens NuGet update PRs; keep package versions consistent across projects.
- Update `README.md` when user-facing features change.
